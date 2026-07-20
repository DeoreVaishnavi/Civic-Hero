using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Enums;

namespace CivicHero.Backend.Core.Interfaces;

/// <summary>
/// Repository contract for complaint-specific data operations.
/// </summary>
public interface IComplaintRepository : IRepository<Complaint>
{
    /// <summary>
    /// Gets all complaints created by a citizen.
    /// </summary>
    Task<IReadOnlyList<Complaint>> GetByCitizenAsync(
        Guid citizenId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all complaints assigned to a contractor.
    /// </summary>
    Task<IReadOnlyList<Complaint>> GetByContractorAsync(
        Guid contractorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all complaints belonging to a department.
    /// </summary>
    Task<IReadOnlyList<Complaint>> GetByDepartmentAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all complaints in a ward.
    /// </summary>
    Task<IReadOnlyList<Complaint>> GetByWardAsync(
        Guid wardId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets complaints having the specified status.
    /// </summary>
    Task<IReadOnlyList<Complaint>> GetByStatusAsync(
        ComplaintStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets complaints having the specified priority.
    /// </summary>
    Task<IReadOnlyList<Complaint>> GetByPriorityAsync(
        ComplaintPriority priority,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets complaints created within the specified date range.
    /// </summary>
    Task<IReadOnlyList<Complaint>> GetByDateRangeAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets unresolved complaints.
    /// </summary>
    Task<IReadOnlyList<Complaint>> GetOpenComplaintsAsync(
        CancellationToken cancellationToken = default);
}