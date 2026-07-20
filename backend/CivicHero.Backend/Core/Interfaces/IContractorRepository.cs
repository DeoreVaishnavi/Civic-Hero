using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Enums;

namespace CivicHero.Backend.Core.Interfaces;

/// <summary>
/// Repository contract for contractor-specific data operations.
/// </summary>
public interface IContractorRepository : IRepository<Contractor>
{
    /// <summary>
    /// Gets all contractors belonging to a department.
    /// </summary>
    Task<IReadOnlyList<Contractor>> GetByDepartmentAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets contractors by their current status.
    /// </summary>
    Task<IReadOnlyList<Contractor>> GetByStatusAsync(
        ContractorStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active contractors.
    /// </summary>
    Task<IReadOnlyList<Contractor>> GetActiveAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a contractor using the linked user identifier.
    /// </summary>
    Task<Contractor?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether a contractor exists for the specified user.
    /// </summary>
    Task<bool> ExistsByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}