using CivicHero.Backend.Core.DTOs.Users;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Enums;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CivicHero.Backend.Infrastructure.Repositories;

public sealed class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(CivicDbContext dbContext) : base(dbContext) { }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        Query(asTracking: true)
            .FirstOrDefaultAsync(entity => entity.Email == email, cancellationToken);

    public Task<User?> GetByRefreshTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default) =>
        Query(asTracking: true)
            .FirstOrDefaultAsync(entity => entity.RefreshTokenHash == tokenHash, cancellationToken);

    public Task<User?> GetProfileByIdAsync(
        long id,
        bool asTracking = false,
        CancellationToken cancellationToken = default) =>
        Query(asTracking)
            .Include(entity => entity.Department)
            .Include(entity => entity.Ward)
            .FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(
        UserQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var users = Query()
            .Include(entity => entity.Department)
            .Include(entity => entity.Ward)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            users = users.Where(entity =>
                entity.FullName.Contains(search) || entity.Email.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(query.Role) &&
            Enum.TryParse<UserRole>(query.Role, true, out var role))
        {
            users = users.Where(entity => entity.Role == role);
        }

        if (query.IsActive.HasValue)
        {
            users = users.Where(entity => entity.IsActive == query.IsActive.Value);
        }

        if (query.DepartmentId.HasValue)
        {
            users = users.Where(entity => entity.DepartmentId == query.DepartmentId.Value);
        }

        if (query.WardId.HasValue)
        {
            users = users.Where(entity => entity.WardId == query.WardId.Value);
        }

        var totalCount = await users.CountAsync(cancellationToken);
        var items = await users
            .OrderByDescending(entity => entity.CreatedAt)
            .ThenBy(entity => entity.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
