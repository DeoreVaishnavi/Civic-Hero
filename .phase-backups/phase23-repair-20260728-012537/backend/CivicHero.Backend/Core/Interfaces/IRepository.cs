using CivicHero.Backend.Core.Common;
using System.Linq.Expressions;

namespace CivicHero.Backend.Core.Interfaces;

public interface IRepository<TEntity> where TEntity : BaseEntity
{
    IQueryable<TEntity> Query();
    Task<TEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    void Update(TEntity entity);
    void Remove(TEntity entity);
}
