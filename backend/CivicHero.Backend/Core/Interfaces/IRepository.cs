using CivicHero.Backend.Core.Common;

namespace CivicHero.Backend.Core.Interfaces;

/// <summary>
/// Generic repository contract for aggregate roots.
/// </summary>
/// <typeparam name="TEntity">
/// Entity type.
/// </typeparam>
public interface IRepository<TEntity>
    where TEntity : BaseEntity
{
    /// <summary>
    /// Gets an entity by its identifier.
    /// </summary>
    Task<TEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all entities.
    /// </summary>
    Task<IReadOnlyList<TEntity>> GetAllAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds an entity.
    /// </summary>
    Task AddAsync(
        TEntity entity,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an entity.
    /// </summary>
    void Update(TEntity entity);

    /// <summary>
    /// Deletes an entity.
    /// </summary>
    void Delete(TEntity entity);

    /// <summary>
    /// Determines whether an entity exists.
    /// </summary>
    Task<bool> ExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}