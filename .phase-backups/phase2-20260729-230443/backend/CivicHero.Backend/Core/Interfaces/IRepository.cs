using CivicHero.Backend.Core.Common;

namespace CivicHero.Backend.Core.Interfaces;

public interface IRepository<TEntity> where TEntity : BaseEntity
{
    IQueryable<TEntity> Query(bool asTracking = false);
    Task<TEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken = default);
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    void Update(TEntity entity);
    void Remove(TEntity entity);
}
