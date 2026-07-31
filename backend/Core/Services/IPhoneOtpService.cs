using CivicHero.Backend.Core.DTOs.Auth;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Enums;

namespace CivicHero.Backend.Core.Services;

public interface IPhoneOtpService
{
    Task<PhoneOtpRequestResponse> RequestAsync(User user, string phoneNumber, OtpPurpose purpose, string? remoteIp, CancellationToken cancellationToken = default);
    Task VerifyAsync(User user, string phoneNumber, string code, OtpPurpose purpose, CancellationToken cancellationToken = default);
}
