using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using CivicHero.Backend.Core.DTOs.Auth;
using CivicHero.Backend.Core.DTOs.Users;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Enums;
using CivicHero.Backend.Core.Exceptions;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.Configurations;
using CivicHero.Backend.Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace CivicHero.Backend.Core.Services;

public sealed class AuthService : IAuthService
{
    private const int MaximumFailedLoginAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(30);

    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IMapper _mapper;
    private readonly JwtOptions _jwtOptions;
    private readonly IHostEnvironment _environment;

    public AuthService(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IMapper mapper,
        IOptions<JwtOptions> jwtOptions,
        IHostEnvironment environment)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _mapper = mapper;
        _jwtOptions = jwtOptions.Value;
        _environment = environment;
    }

    public async Task<RegistrationResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);
        if (await _userRepository.GetByEmailAsync(email, cancellationToken) is not null)
        {
            throw new ConflictException("An account with this email already exists.");
        }

        var verificationToken = GenerateSecureToken();
        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = UserRole.Citizen,
            IsActive = true,
            IsEmailVerified = false,
            EmailVerificationTokenHash = HashToken(verificationToken),
            EmailVerificationTokenExpiresAt = DateTimeOffset.UtcNow.AddHours(24)
        };

        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RegistrationResponse
        {
            Email = user.Email,
            DevelopmentVerificationToken = _environment.IsDevelopment() ? verificationToken : null
        };
    }

    public async Task<AuthSessionResult> VerifyEmailAsync(
        VerifyEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(NormalizeEmail(request.Email), cancellationToken)
                   ?? throw new NotFoundException("User account was not found.");

        if (user.IsEmailVerified)
        {
            return await CreateSessionAsync(user, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(user.EmailVerificationTokenHash) ||
            user.EmailVerificationTokenExpiresAt <= DateTimeOffset.UtcNow ||
            !FixedTimeEquals(user.EmailVerificationTokenHash, HashToken(request.Token)))
        {
            throw new BusinessRuleViolationException("The email verification token is invalid or expired.");
        }

        user.IsEmailVerified = true;
        user.EmailVerificationTokenHash = null;
        user.EmailVerificationTokenExpiresAt = null;
        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await CreateSessionAsync(user, cancellationToken);
    }

    public async Task<AuthSessionResult> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(NormalizeEmail(request.Email), cancellationToken);
        if (user is null)
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("This account is inactive.");
        }

        if (user.LockoutEnd > DateTimeOffset.UtcNow)
        {
            throw new UnauthorizedAccessException(
                $"Account is temporarily locked until {user.LockoutEnd:O}.");
        }

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= MaximumFailedLoginAttempts)
            {
                user.LockoutEnd = DateTimeOffset.UtcNow.Add(LockoutDuration);
                user.FailedLoginAttempts = 0;
            }

            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        if (!user.IsEmailVerified)
        {
            throw new BusinessRuleViolationException("Verify your email before logging in.");
        }

        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        user.LastLoginAt = DateTimeOffset.UtcNow;
        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await CreateSessionAsync(user, cancellationToken);
    }

    public async Task<AuthSessionResult> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(refreshToken);
        var user = await _userRepository.GetByRefreshTokenHashAsync(tokenHash, cancellationToken)
                   ?? throw new UnauthorizedAccessException("Refresh token is invalid.");

        if (!user.IsActive || !user.IsEmailVerified ||
            user.RefreshTokenExpiresAt <= DateTimeOffset.UtcNow ||
            !FixedTimeEquals(user.RefreshTokenHash!, tokenHash))
        {
            ClearRefreshToken(user);
            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedAccessException("Refresh token is invalid or expired.");
        }

        return await CreateSessionAsync(user, cancellationToken);
    }

    public async Task LogoutAsync(long userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return;
        }

        ClearRefreshToken(user);
        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<UserDto> GetCurrentUserAsync(
        long userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
                   ?? throw new NotFoundException("User account was not found.");
        return _mapper.Map<UserDto>(user);
    }

    private async Task<AuthSessionResult> CreateSessionAsync(
        User user,
        CancellationToken cancellationToken)
    {
        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshToken = GenerateSecureToken();
        var refreshExpiry = DateTimeOffset.UtcNow.AddDays(
            Math.Clamp(_jwtOptions.RefreshTokenExpiryDays, 1, 30));

        user.RefreshTokenHash = HashToken(refreshToken);
        user.RefreshTokenCreatedAt = DateTimeOffset.UtcNow;
        user.RefreshTokenExpiresAt = refreshExpiry;
        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthSessionResult(
            new AuthResponse
            {
                AccessToken = accessToken.Token,
                ExpiresAtUtc = accessToken.ExpiresAtUtc,
                User = _mapper.Map<UserDto>(user)
            },
            refreshToken,
            refreshExpiry);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static string GenerateSecureToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static bool FixedTimeEquals(string first, string second) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(first),
            Encoding.UTF8.GetBytes(second));

    private static void ClearRefreshToken(User user)
    {
        user.RefreshTokenHash = null;
        user.RefreshTokenCreatedAt = null;
        user.RefreshTokenExpiresAt = null;
    }
}
