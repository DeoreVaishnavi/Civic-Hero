
using CivicHero.Backend.Core.DTOs.Auth;
using CivicHero.Backend.Core.Entities;

namespace CivicHero.Backend.Infrastructure.Security;

public interface ITwoFactorService
{
    TwoFactorStatusResponse GetStatus(User user);
    TwoFactorSetupResponse BeginSetup(User user);
    TwoFactorEnableResponse Enable(User user, string code);
    void Disable(User user, string code);
    IReadOnlyList<string> RegenerateRecoveryCodes(User user, string code);
    void VerifyForLogin(User user, string? code);
}
