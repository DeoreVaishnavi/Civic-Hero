using CivicHero.Backend.Core.Common;

namespace CivicHero.Backend.Core.Events;

/// <summary>
/// Raised when a complaint has been successfully resolved.
/// </summary>
public sealed class ComplaintResolvedEvent : DomainEvent
{
    /// <summary>
    /// Identifier of the resolved complaint.
    /// </summary>
    public Guid ComplaintId { get; }

    /// <summary>
    /// Citizen who created the complaint.
    /// </summary>
    public Guid CitizenId { get; }

    /// <summary>
    /// Contractor who resolved the complaint.
    /// </summary>
    public Guid ContractorId { get; }

    /// <summary>
    /// Department responsible for the complaint.
    /// </summary>
    public Guid DepartmentId { get; }

    /// <summary>
    /// UTC timestamp when the complaint was resolved.
    /// </summary>
    public DateTime ResolvedOnUtc { get; }

    /// <summary>
    /// Creates a new complaint resolved event.
    /// </summary>
    public ComplaintResolvedEvent(
        Guid complaintId,
        Guid citizenId,
        Guid contractorId,
        Guid departmentId)
    {
        if (complaintId == Guid.Empty)
            throw new ArgumentException("Complaint ID is required.", nameof(complaintId));

        if (citizenId == Guid.Empty)
            throw new ArgumentException("Citizen ID is required.", nameof(citizenId));

        if (contractorId == Guid.Empty)
            throw new ArgumentException("Contractor ID is required.", nameof(contractorId));

        if (departmentId == Guid.Empty)
            throw new ArgumentException("Department ID is required.", nameof(departmentId));

        ComplaintId = complaintId;
        CitizenId = citizenId;
        ContractorId = contractorId;
        DepartmentId = departmentId;
        ResolvedOnUtc = DateTime.UtcNow;
    }
}