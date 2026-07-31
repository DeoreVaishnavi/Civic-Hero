using CivicHero.Backend.Core.Common;

namespace CivicHero.Backend.Core.Interfaces;

public interface IUnitOfWork : IAsyncDisposable
{
    IRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
