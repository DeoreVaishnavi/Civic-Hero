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
    private readonly ITwoFactorService _twoFactorService;
    private readonly IPhoneOtpService _phoneOtpService;
    private readonly SmsOptions _smsOptions;

    public AuthService(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IMapper mapper,
        IOptions<JwtOptions> jwtOptions,
        IHostEnvironment environment,
        ITwoFactorService twoFactorService,
        IPhoneOtpService phoneOtpService,
        IOptions<SmsOptions> smsOptions)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _mapper = mapper;
        _jwtOptions = jwtOptions.Value;
        _environment = environment;
        _twoFactorService = twoFactorService;
        _phoneOtpService = phoneOtpService;
        _smsOptions = smsOptions.Value;
    }

    public async Task<RegistrationResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);
        if (await _userRepository.GetByEmailAsync(email, cancellationToken) is not null)
            throw new ConflictException("An account with this email already exists.");

        string? phone = null;
        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            phone = PhoneNumberNormalizer.Normalize(request.Phone);
            if (await _userRepository.GetByPhoneAsync(phone, cancellationToken) is not null)
                throw new ConflictException("An account with this phone number already exists.");
        }

        var verificationToken = GenerateSecureToken();
        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            Phone = phone,
            NormalizedPhone = phone,
            IsPhoneVerified = false,
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

    public async Task<AuthSessionResult> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(NormalizeEmail(request.Email), cancellationToken)
                   ?? throw new NotFoundException("User account was not found.");
        if (user.IsEmailVerified) return await CreateSessionAsync(user, cancellationToken);
        if (string.IsNullOrWhiteSpace(user.EmailVerificationTokenHash) ||
            user.EmailVerificationTokenExpiresAt <= DateTimeOffset.UtcNow ||
            !FixedTimeEquals(user.EmailVerificationTokenHash, HashToken(request.Token)))
            throw new BusinessRuleViolationException("The email verification token is invalid or expired.");

        user.IsEmailVerified = true;
        user.EmailVerificationTokenHash = null;
        user.EmailVerificationTokenExpiresAt = null;
        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await CreateSessionAsync(user, cancellationToken);
    }

    public async Task<AuthSessionResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var identifier = request.EffectiveIdentifier.Trim();
        var user = await FindByIdentifierAsync(identifier, cancellationToken);
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            if (user is not null) await RecordFailedLoginAsync(user, cancellationToken);
            throw new UnauthorizedAccessException("Invalid email/phone or password.");
        }

        EnsureLoginAllowed(user);
        if (!identifier.Contains('@') && !user.IsPhoneVerified)
            throw new BusinessRuleViolationException("Verify this phone number before using phone login.");
        _twoFactorService.VerifyForLogin(user, request.TwoFactorCode);
        await RecordSuccessfulLoginAsync(user, cancellationToken);
        return await CreateSessionAsync(user, cancellationToken);
    }

    public async Task<PhoneOtpRequestResponse> RequestPhoneLoginOtpAsync(RequestPhoneLoginOtpRequest request, string? remoteIp, CancellationToken cancellationToken = default)
    {
        var phone = PhoneNumberNormalizer.Normalize(request.PhoneNumber);
        var user = await _userRepository.GetByPhoneAsync(phone, cancellationToken);
        if (user is null || !user.IsPhoneVerified || !user.IsActive || !user.IsEmailVerified || user.IsSystemAccount)
        {
            // Return the same shape as a real request so the endpoint does not reveal account existence.
            return new PhoneOtpRequestResponse
            {
                MaskedPhoneNumber = PhoneNumberNormalizer.Mask(phone),
                ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(Math.Clamp(_smsOptions.OtpExpiryMinutes, 2, 15)),
                ResendAfterSeconds = Math.Clamp(_smsOptions.ResendCooldownSeconds, 30, 300)
            };
        }
        return await _phoneOtpService.RequestAsync(user, phone, OtpPurpose.PhoneLogin, remoteIp, cancellationToken);
    }

    public async Task<AuthSessionResult> LoginWithPhoneOtpAsync(VerifyPhoneLoginOtpRequest request, CancellationToken cancellationToken = default)
    {
        var phone = PhoneNumberNormalizer.Normalize(request.PhoneNumber);
        var user = await _userRepository.GetByPhoneAsync(phone, cancellationToken)
                   ?? throw new UnauthorizedAccessException("The phone verification code is invalid.");
        EnsureLoginAllowed(user);
        if (!user.IsPhoneVerified) throw new BusinessRuleViolationException("This phone number is not verified.");
        await _phoneOtpService.VerifyAsync(user, phone, request.Code, OtpPurpose.PhoneLogin, cancellationToken);
        _twoFactorService.VerifyForLogin(user, request.TwoFactorCode);
        await RecordSuccessfulLoginAsync(user, cancellationToken);
        return await CreateSessionAsync(user, cancellationToken);
    }

    public async Task<PhoneOtpRequestResponse> RequestPhoneVerificationOtpAsync(long userId, RequestPhoneVerificationOtpRequest request, string? remoteIp, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
                   ?? throw new NotFoundException("User account was not found.");
        var phone = PhoneNumberNormalizer.Normalize(request.PhoneNumber);
        var owner = await _userRepository.GetByPhoneAsync(phone, cancellationToken);
        if (owner is not null && owner.Id != user.Id)
            throw new ConflictException("This phone number is already linked to another account.");
        if (user.IsPhoneVerified && user.NormalizedPhone == phone)
            throw new BusinessRuleViolationException("This phone number is already verified.");
        return await _phoneOtpService.RequestAsync(user, phone, OtpPurpose.PhoneVerification, remoteIp, cancellationToken);
    }

    public async Task<PhoneVerificationStatusResponse> VerifyPhoneAsync(long userId, VerifyPhoneNumberRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
                   ?? throw new NotFoundException("User account was not found.");
        var phone = PhoneNumberNormalizer.Normalize(request.PhoneNumber);
        var owner = await _userRepository.GetByPhoneAsync(phone, cancellationToken);
        if (owner is not null && owner.Id != user.Id)
            throw new ConflictException("This phone number is already linked to another account.");
        await _phoneOtpService.VerifyAsync(user, phone, request.Code, OtpPurpose.PhoneVerification, cancellationToken);
        user.Phone = phone;
        user.NormalizedPhone = phone;
        user.IsPhoneVerified = true;
        user.AuthorizationVersion++;
        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new PhoneVerificationStatusResponse { PhoneNumber = PhoneNumberNormalizer.Mask(phone), IsPhoneVerified = true };
    }

    public async Task<AuthSessionResult> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(refreshToken);
        var user = await _userRepository.GetByRefreshTokenHashAsync(tokenHash, cancellationToken)
                   ?? throw new UnauthorizedAccessException("Refresh token is invalid.");
        if (!user.IsActive || !user.IsEmailVerified || user.IsSystemAccount ||
            user.RefreshTokenExpiresAt <= DateTimeOffset.UtcNow || !FixedTimeEquals(user.RefreshTokenHash!, tokenHash))
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
        if (user is null) return;
        ClearRefreshToken(user);
        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<UserDto> GetCurrentUserAsync(long userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
                   ?? throw new NotFoundException("User account was not found.");
        return _mapper.Map<UserDto>(user);
    }

    private async Task<User?> FindByIdentifierAsync(string identifier, CancellationToken cancellationToken)
    {
        if (identifier.Contains('@')) return await _userRepository.GetByEmailAsync(NormalizeEmail(identifier), cancellationToken);
        try { return await _userRepository.GetByPhoneAsync(PhoneNumberNormalizer.Normalize(identifier), cancellationToken); }
        catch (ValidationException) { return null; }
    }

    private static void EnsureLoginAllowed(User user)
    {
        if (!user.IsActive || user.IsSystemAccount) throw new UnauthorizedAccessException("This account is inactive.");
        if (user.LockoutEnd > DateTimeOffset.UtcNow)
            throw new UnauthorizedAccessException($"Account is temporarily locked until {user.LockoutEnd:O}.");
        if (!user.IsEmailVerified) throw new BusinessRuleViolationException("Verify your email before logging in.");
    }

    private async Task RecordFailedLoginAsync(User user, CancellationToken cancellationToken)
    {
        user.FailedLoginAttempts++;
        if (user.FailedLoginAttempts >= MaximumFailedLoginAttempts)
        {
            user.LockoutEnd = DateTimeOffset.UtcNow.Add(LockoutDuration);
            user.FailedLoginAttempts = 0;
        }
        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task RecordSuccessfulLoginAsync(User user, CancellationToken cancellationToken)
    {
        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        user.LastLoginAt = DateTimeOffset.UtcNow;
        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<AuthSessionResult> CreateSessionAsync(User user, CancellationToken cancellationToken)
    {
        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshToken = GenerateSecureToken();
        var refreshExpiry = DateTimeOffset.UtcNow.AddDays(Math.Clamp(_jwtOptions.RefreshTokenExpiryDays, 1, 30));
        user.RefreshTokenHash = HashToken(refreshToken);
        user.RefreshTokenCreatedAt = DateTimeOffset.UtcNow;
        user.RefreshTokenExpiresAt = refreshExpiry;
        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new AuthSessionResult(new AuthResponse
        {
            AccessToken = accessToken.Token,
            ExpiresAtUtc = accessToken.ExpiresAtUtc,
            User = _mapper.Map<UserDto>(user)
        }, refreshToken, refreshExpiry);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
    private static string GenerateSecureToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    private static bool FixedTimeEquals(string first, string second) => CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(first), Encoding.UTF8.GetBytes(second));
    private static void ClearRefreshToken(User user) { user.RefreshTokenHash = null; user.RefreshTokenCreatedAt = null; user.RefreshTokenExpiresAt = null; }
}
