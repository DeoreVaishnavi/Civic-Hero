using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Enums;
using CivicHero.Backend.Infrastructure.Configurations;
using CivicHero.Backend.Infrastructure.Data;
using CivicHero.Backend.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CivicHero.Backend.Infrastructure.Services;

public sealed class SuperAdminBootstrapper
{
    private readonly CivicDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly SuperAdminBootstrapOptions _options;
    private readonly ILogger<SuperAdminBootstrapper> _logger;

    public SuperAdminBootstrapper(CivicDbContext db, IPasswordHasher passwordHasher, IOptions<SuperAdminBootstrapOptions> options, ILogger<SuperAdminBootstrapper> logger)
    {
        _db = db; _passwordHasher = passwordHasher; _options = options.Value; _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled) return;
        if (string.IsNullOrWhiteSpace(_options.Email) || string.IsNullOrWhiteSpace(_options.Password) || _options.Password.Length < 12)
            throw new InvalidOperationException("BootstrapSuperAdmin requires an email and password of at least 12 characters in user-secrets.");
        var email = _options.Email.Trim().ToLowerInvariant();
        var existing = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
        if (existing is null)
        {
            _db.Users.Add(new User { Email = email, FullName = _options.FullName.Trim(), PasswordHash = _passwordHasher.Hash(_options.Password), Role = UserRole.SuperAdmin, IsEmailVerified = true, IsActive = true, AuthorizationVersion = 1 });
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogWarning("Bootstrap SuperAdmin created for {Email}. Disable BootstrapSuperAdmin:Enabled after first successful login.", email);
            return;
        }
        if (existing.Role != UserRole.SuperAdmin)
            _logger.LogWarning("Bootstrap email {Email} already belongs to a non-SuperAdmin account; no changes were made.", email);
    }
}
