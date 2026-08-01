using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using CivicHero.Backend.Core.DTOs.Administration;
using CivicHero.Backend.Core.DTOs.Auth;
using CivicHero.Backend.Core.DTOs.Users;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Enums;
using CivicHero.Backend.Core.Exceptions;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Core.Validation;
using CivicHero.Backend.Infrastructure.Configurations;
using CivicHero.Backend.Infrastructure.Data;
using CivicHero.Backend.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
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
    private readonly CivicDbContext _db;

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
        IOptions<SmsOptions> smsOptions,
        CivicDbContext db)
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
        _db = db;
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
            FullName = PersonNameRules.Normalize(request.FullName),
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
        var policy = await ReadAuthenticationPolicyAsync(cancellationToken);
        var identifier = request.EffectiveIdentifier.Trim();
        var user = await FindByIdentifierAsync(identifier, cancellationToken);
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            if (user is not null) await RecordFailedLoginAsync(user, policy, cancellationToken);
            throw new UnauthorizedAccessException("Invalid email/phone or password.");
        }

        EnsureLoginAllowed(user, policy);
        if (!identifier.Contains('@') && !user.IsPhoneVerified)
            throw new BusinessRuleViolationException("Verify this phone number before using phone login.");
        var requirement = await EvaluateTwoFactorRequirementAsync(user, policy, cancellationToken);
        _twoFactorService.VerifyForLogin(user, request.TwoFactorCode);
        return await CreateSessionAsync(user, cancellationToken, recordSuccessfulLogin: true, requirement: requirement);
    }

    public async Task<PhoneOtpRequestResponse> RequestPasswordResetAsync(
        ForgotPasswordRequest request,
        string? remoteIp,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Identifier))
            throw new CivicHero.Backend.Core.Exceptions.ValidationException(["Email or verified phone number is required."]);

        var user = await FindByIdentifierAsync(request.Identifier.Trim(), cancellationToken);
        var verifiedPhone = user is null ? null : GetVerifiedPhone(user);
        if (user is null || !user.IsActive || user.IsSystemAccount || verifiedPhone is null)
        {
            // Always return the same response shape so this endpoint does not reveal account existence.
            return new PhoneOtpRequestResponse
            {
                MaskedPhoneNumber = "your verified phone",
                ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(Math.Clamp(_smsOptions.OtpExpiryMinutes, 2, 15)),
                ResendAfterSeconds = Math.Clamp(_smsOptions.ResendCooldownSeconds, 30, 300)
            };
        }

        return await _phoneOtpService.RequestAsync(
            user,
            verifiedPhone,
            OtpPurpose.PasswordReset,
            remoteIp,
            cancellationToken);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        ValidatePasswordReset(request);

        var user = await FindByIdentifierAsync(request.Identifier.Trim(), cancellationToken);
        var verifiedPhone = user is null ? null : GetVerifiedPhone(user);
        if (user is null || !user.IsActive || user.IsSystemAccount || verifiedPhone is null)
        {
            throw new BusinessRuleViolationException("The reset code is invalid or expired.");
        }

        try
        {
            await _phoneOtpService.VerifyAsync(
                user,
                verifiedPhone,
                request.Code,
                OtpPurpose.PasswordReset,
                cancellationToken);
        }
        catch (BusinessRuleViolationException)
        {
            throw new BusinessRuleViolationException("The reset code is invalid or expired.");
        }

        if (_passwordHasher.Verify(request.NewPassword, user.PasswordHash))
            throw new BusinessRuleViolationException("Choose a password different from your current password.");

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        user.AuthorizationVersion++;
        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        ClearRefreshToken(user);

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<PhoneOtpRequestResponse> RequestPhoneLoginOtpAsync(RequestPhoneLoginOtpRequest request, string? remoteIp, CancellationToken cancellationToken = default)
    {
        var phone = PhoneNumberNormalizer.Normalize(request.PhoneNumber);
        var user = await _userRepository.GetByPhoneAsync(phone, cancellationToken);
        var policy = await ReadAuthenticationPolicyAsync(cancellationToken);
        var roleEnabled = user is not null && await IsRoleEnabledAsync(user.Role, cancellationToken);
        if (user is null || !roleEnabled || !user.IsPhoneVerified || !user.IsActive ||
            (policy.RequireVerifiedEmail && !user.IsEmailVerified) || user.IsSystemAccount)
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
        var policy = await ReadAuthenticationPolicyAsync(cancellationToken);
        EnsureLoginAllowed(user, policy);
        if (!user.IsPhoneVerified) throw new BusinessRuleViolationException("This phone number is not verified.");
        await _phoneOtpService.VerifyAsync(user, phone, request.Code, OtpPurpose.PhoneLogin, cancellationToken);
        var requirement = await EvaluateTwoFactorRequirementAsync(user, policy, cancellationToken);
        _twoFactorService.VerifyForLogin(user, request.TwoFactorCode);
        return await CreateSessionAsync(user, cancellationToken, recordSuccessfulLogin: true, requirement: requirement);
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
        var policy = await ReadAuthenticationPolicyAsync(cancellationToken);
        var tokenHash = HashToken(refreshToken);
        var user = await _userRepository.GetByRefreshTokenHashAsync(tokenHash, cancellationToken)
                   ?? throw new UnauthorizedAccessException("Refresh token is invalid.");
        if (!user.IsActive || (policy.RequireVerifiedEmail && !user.IsEmailVerified) || user.IsSystemAccount ||
            user.RefreshTokenExpiresAt <= DateTimeOffset.UtcNow || !FixedTimeEquals(user.RefreshTokenHash!, tokenHash))
        {
            ClearRefreshToken(user);
            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedAccessException("Refresh token is invalid or expired.");
        }
        var requirement = await EvaluateTwoFactorRequirementAsync(user, policy, cancellationToken);
        return await CreateSessionAsync(user, cancellationToken, requirement: requirement);
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

    private static void EnsureLoginAllowed(User user, AuthenticationPolicyDto policy)
    {
        if (!user.IsActive || user.IsSystemAccount) throw new UnauthorizedAccessException("This account is inactive.");
        if (user.LockoutEnd > DateTimeOffset.UtcNow)
            throw new UnauthorizedAccessException($"Account is temporarily locked until {user.LockoutEnd:O}.");
        if (policy.RequireVerifiedEmail && !user.IsEmailVerified)
            throw new BusinessRuleViolationException("Verify your email before logging in.");
    }

    private async Task RecordFailedLoginAsync(User user, AuthenticationPolicyDto policy, CancellationToken cancellationToken)
    {
        user.FailedLoginAttempts++;
        if (user.FailedLoginAttempts >= policy.MaximumFailedLoginAttempts)
        {
            user.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(policy.LockoutMinutes);
            user.FailedLoginAttempts = 0;
        }
        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<AuthSessionResult> CreateSessionAsync(
        User user,
        CancellationToken cancellationToken,
        bool recordSuccessfulLogin = false,
        TwoFactorRequirement? requirement = null)
    {
        await EnsureRoleEnabledAsync(user.Role, cancellationToken);
        var policy = await ReadAuthenticationPolicyAsync(cancellationToken);
        requirement ??= await EvaluateTwoFactorRequirementAsync(user, policy, cancellationToken);
        if (recordSuccessfulLogin)
        {
            // Persist login metadata and the new refresh token in one database write.
            // This avoids a second RDS round trip on every password or OTP login.
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            user.LastLoginAt = DateTimeOffset.UtcNow;
        }

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
            User = _mapper.Map<UserDto>(user),
            RequiresTwoFactorSetup = requirement.SetupRequired,
            TwoFactorSetupDeadlineUtc = requirement.DeadlineUtc
        }, refreshToken, refreshExpiry);
    }

    private static string? GetVerifiedPhone(User user)
    {
        if (!user.IsPhoneVerified) return null;

        var value = string.IsNullOrWhiteSpace(user.NormalizedPhone) ? user.Phone : user.NormalizedPhone;
        if (string.IsNullOrWhiteSpace(value)) return null;

        try { return PhoneNumberNormalizer.Normalize(value); }
        catch (CivicHero.Backend.Core.Exceptions.ValidationException) { return null; }
    }

    private static void ValidatePasswordReset(ResetPasswordRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Identifier))
            errors.Add("Email or verified phone number is required.");
        if (string.IsNullOrWhiteSpace(request.Code) || request.Code.Length != 6 || !request.Code.All(char.IsDigit))
            errors.Add("Enter the six-digit reset code.");
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8 || request.NewPassword.Length > 128)
            errors.Add("Password must be between 8 and 128 characters.");
        else
        {
            if (!request.NewPassword.Any(char.IsUpper)) errors.Add("Password must contain an uppercase letter.");
            if (!request.NewPassword.Any(char.IsLower)) errors.Add("Password must contain a lowercase letter.");
            if (!request.NewPassword.Any(char.IsDigit)) errors.Add("Password must contain a number.");
            if (!request.NewPassword.Any(character => !char.IsLetterOrDigit(character))) errors.Add("Password must contain a special character.");
        }
        if (!string.Equals(request.NewPassword, request.ConfirmPassword, StringComparison.Ordinal))
            errors.Add("Password and confirmation password must match.");

        if (errors.Count > 0)
            throw new CivicHero.Backend.Core.Exceptions.ValidationException(errors);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
    private static string GenerateSecureToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    private static bool FixedTimeEquals(string first, string second) => CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(first), Encoding.UTF8.GetBytes(second));
    private async Task EnsureRoleEnabledAsync(UserRole role, CancellationToken cancellationToken)
    {
        if (!await IsRoleEnabledAsync(role, cancellationToken))
            throw new UnauthorizedAccessException("This role is temporarily disabled by the active global role policy.");
    }

    private async Task<bool> IsRoleEnabledAsync(UserRole role, CancellationToken cancellationToken)
    {
        var setting = await _db.SystemSettings.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Key == SuperAdminGovernanceService.RolePolicyKey, cancellationToken);
        if (setting is null) return true;
        try
        {
            var policy = System.Text.Json.JsonSerializer.Deserialize<RolePolicyConfigurationDto>(setting.Value,
                new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true });
            var entry = policy?.Roles?.FirstOrDefault(x => string.Equals(x.Role, role.ToString(), StringComparison.OrdinalIgnoreCase));
            return entry?.Enabled ?? true;
        }
        catch (System.Text.Json.JsonException)
        {
            return true;
        }
    }

    private async Task<AuthenticationPolicyDto> ReadAuthenticationPolicyAsync(CancellationToken cancellationToken)
    {
        var setting = await _db.SystemSettings.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Key == SuperAdminGovernanceService.AuthenticationPolicyKey, cancellationToken);
        if (setting is null) return new AuthenticationPolicyDto();
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<AuthenticationPolicyDto>(setting.Value,
                new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true })
                ?? new AuthenticationPolicyDto();
        }
        catch (System.Text.Json.JsonException)
        {
            return new AuthenticationPolicyDto();
        }
    }

    private async Task<TwoFactorRequirement> EvaluateTwoFactorRequirementAsync(
        User user,
        AuthenticationPolicyDto policy,
        CancellationToken cancellationToken)
    {
        var requiredRoles = policy.TwoFactorRequiredRoles ?? Array.Empty<string>();
        var required = requiredRoles.Contains(user.Role.ToString(), StringComparer.OrdinalIgnoreCase);
        if (!required || user.TwoFactorEnabled) return new TwoFactorRequirement(required, false, null);

        var setting = await _db.SystemSettings.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Key == SuperAdminGovernanceService.TwoFactorExemptionsKey, cancellationToken);
        DateTimeOffset? deadline = null;
        if (setting is not null)
        {
            try
            {
                var exemptions = System.Text.Json.JsonSerializer.Deserialize<List<TwoFactorEnrollmentExemptionDto>>(setting.Value,
                    new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true }) ?? [];
                deadline = exemptions.LastOrDefault(x => x.UserId == user.Id && x.ExpiresAtUtc > DateTimeOffset.UtcNow)?.ExpiresAtUtc;
            }
            catch (System.Text.Json.JsonException)
            {
                deadline = null;
            }
        }

        if (!deadline.HasValue)
            throw new BusinessRuleViolationException("Two-factor authentication is required for this role. Contact a SuperAdmin if the enrollment grace period has expired.");
        return new TwoFactorRequirement(true, true, deadline);
    }

    private sealed record TwoFactorRequirement(bool Required, bool SetupRequired, DateTimeOffset? DeadlineUtc);

    private static void ClearRefreshToken(User user) { user.RefreshTokenHash = null; user.RefreshTokenCreatedAt = null; user.RefreshTokenExpiresAt = null; }
}
