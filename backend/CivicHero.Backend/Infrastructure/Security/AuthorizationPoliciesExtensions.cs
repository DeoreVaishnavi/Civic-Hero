using CivicHero.Backend.Core.Constants;
using CivicHero.Backend.Core.Enums;

namespace CivicHero.Backend.Infrastructure.Security;

public static class AuthorizationPoliciesExtensions
{
    public static IServiceCollection AddCivicHeroAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(PermissionConstants.CitizenOnly, policy => policy.RequireRole(UserRole.Citizen.ToString()));
            options.AddPolicy(PermissionConstants.OfficerOnly, policy => policy.RequireRole(UserRole.Officer.ToString()));
            options.AddPolicy(PermissionConstants.SupervisorOrAbove, policy => policy.RequireRole(
                UserRole.Supervisor.ToString(), UserRole.Admin.ToString(), UserRole.SuperAdmin.ToString()));
            options.AddPolicy(PermissionConstants.AdminOrAbove, policy => policy.RequireRole(
                UserRole.Admin.ToString(), UserRole.SuperAdmin.ToString()));
            options.AddPolicy(PermissionConstants.SuperAdminOnly, policy => policy.RequireRole(UserRole.SuperAdmin.ToString()));
        });
        return services;
    }
}
