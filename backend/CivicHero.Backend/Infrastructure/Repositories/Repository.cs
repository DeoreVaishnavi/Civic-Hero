using CivicHero.Backend.Core.Common;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CivicHero.Backend.Infrastructure.Repositories;

public class Repository<TEntity> : IRepository<TEntity> where TEntity : BaseEntity
{
    protected readonly CivicDbContext DbContext;
    protected readonly DbSet<TEntity> DbSet;

    public Repository(CivicDbContext dbContext)
    {
        DbContext = dbContext;
        DbSet = dbContext.Set<TEntity>();
    }

    public IQueryable<TEntity> Query(bool asTracking = false) =>
        asTracking ? DbSet.AsQueryable() : DbSet.AsNoTracking();

    public Task<TEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        DbSet.FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);

    public async Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken = default) =>
        await DbSet.AsNoTracking().ToListAsync(cancellationToken);

    public Task AddAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        DbSet.AddAsync(entity, cancellationToken).AsTask();

    public void Update(TEntity entity) => DbSet.Update(entity);

    public void Remove(TEntity entity) => DbSet.Remove(entity);
}
