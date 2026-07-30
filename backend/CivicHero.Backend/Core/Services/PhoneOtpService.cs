using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CivicHero.Backend.Core.DTOs.Auth;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Enums;
using CivicHero.Backend.Core.Exceptions;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.Configurations;
using CivicHero.Backend.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CivicHero.Backend.Core.Services;

public sealed class PhoneOtpService : IPhoneOtpService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISmsSender _smsSender;
    private readonly IOptionsMonitor<SmsOptions> _options;
    private readonly IHostEnvironment _environment;

    public PhoneOtpService(IUnitOfWork unitOfWork, ISmsSender smsSender, IOptionsMonitor<SmsOptions> options, IHostEnvironment environment)
    {
        _unitOfWork = unitOfWork;
        _smsSender = smsSender;
        _options = options;
        _environment = environment;
    }

    public async Task<PhoneOtpRequestResponse> RequestAsync(User user, string phoneNumber, OtpPurpose purpose, string? remoteIp, CancellationToken cancellationToken = default)
    {
        var normalized = PhoneNumberNormalizer.Normalize(phoneNumber);
        var options = _options.CurrentValue;
        var now = DateTimeOffset.UtcNow;
        var recentCount = await _unitOfWork.Repository<PhoneOtpChallenge>().Query()
            .CountAsync(entity => entity.PhoneNumber == normalized && entity.Purpose == purpose && entity.CreatedAt >= now.AddHours(-1), cancellationToken);
        if (recentCount >= Math.Clamp(options.MaximumRequestsPerHour, 1, 20))
            throw new BusinessRuleViolationException("Too many OTP requests. Please try again later.");

        var latest = await _unitOfWork.Repository<PhoneOtpChallenge>().Query(true)
            .Where(entity => entity.PhoneNumber == normalized && entity.Purpose == purpose && entity.ConsumedAt == null)
            .OrderByDescending(entity => entity.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (latest is not null && latest.CreatedAt.AddSeconds(Math.Clamp(options.ResendCooldownSeconds, 30, 300)) > now)
        {
            var seconds = (int)Math.Ceiling((latest.CreatedAt.AddSeconds(options.ResendCooldownSeconds) - now).TotalSeconds);
            throw new BusinessRuleViolationException($"Wait {Math.Max(1, seconds)} seconds before requesting another OTP.");
        }

        if (latest is not null) latest.ConsumedAt = now;
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
        var expiry = now.AddMinutes(Math.Clamp(options.OtpExpiryMinutes, 2, 15));
        var challenge = new PhoneOtpChallenge
        {
            UserId = user.Id,
            PhoneNumber = normalized,
            Purpose = purpose,
            CodeHash = Hash(normalized, purpose, code),
            ExpiresAt = expiry,
            MaximumAttempts = 5,
            RequestedIpHash = string.IsNullOrWhiteSpace(remoteIp) ? null : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(remoteIp)))
        };
        await _unitOfWork.Repository<PhoneOtpChallenge>().AddAsync(challenge, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var message = $"{options.AppName} verification code: {code}. It expires in {Math.Clamp(options.OtpExpiryMinutes, 2, 15)} minutes. Do not share this code.";
        var sent = await _smsSender.SendAsync(normalized, message, cancellationToken);
        challenge.ProviderMessageId = sent.ProviderMessageId;
        if (!sent.Success)
        {
            challenge.ConsumedAt = DateTimeOffset.UtcNow;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new BusinessRuleViolationException(sent.Error ?? "Unable to send the OTP right now.");
        }
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new PhoneOtpRequestResponse
        {
            MaskedPhoneNumber = PhoneNumberNormalizer.Mask(normalized),
            ExpiresAtUtc = expiry,
            ResendAfterSeconds = Math.Clamp(options.ResendCooldownSeconds, 30, 300),
            DevelopmentCode = _environment.IsDevelopment() && string.Equals(options.Provider, "Development", StringComparison.OrdinalIgnoreCase) ? code : null
        };
    }

    public async Task VerifyAsync(User user, string phoneNumber, string code, OtpPurpose purpose, CancellationToken cancellationToken = default)
    {
        var normalized = PhoneNumberNormalizer.Normalize(phoneNumber);
        var challenge = await _unitOfWork.Repository<PhoneOtpChallenge>().Query(true)
            .Where(entity => entity.UserId == user.Id && entity.PhoneNumber == normalized && entity.Purpose == purpose && entity.ConsumedAt == null)
            .OrderByDescending(entity => entity.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new BusinessRuleViolationException("Request a new OTP before continuing.");

        if (challenge.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            challenge.ConsumedAt = DateTimeOffset.UtcNow;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new BusinessRuleViolationException("The OTP has expired.");
        }
        if (challenge.FailedAttempts >= challenge.MaximumAttempts)
            throw new BusinessRuleViolationException("Too many incorrect OTP attempts. Request a new code.");

        if (!FixedTimeEquals(challenge.CodeHash, Hash(normalized, purpose, code.Trim())))
        {
            challenge.FailedAttempts++;
            if (challenge.FailedAttempts >= challenge.MaximumAttempts) challenge.ConsumedAt = DateTimeOffset.UtcNow;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new BusinessRuleViolationException("The OTP is incorrect.");
        }

        challenge.ConsumedAt = DateTimeOffset.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static string Hash(string phone, OtpPurpose purpose, string code) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{phone}|{purpose}|{code}")));

    private static bool FixedTimeEquals(string first, string second) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(first), Encoding.UTF8.GetBytes(second));
}
