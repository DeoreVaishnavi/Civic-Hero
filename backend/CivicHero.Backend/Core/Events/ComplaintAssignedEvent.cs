using CivicHero.Backend.Core.Common;

namespace CivicHero.Backend.Core.Events;

/// <summary>
/// Raised when a complaint is assigned to a contractor.
/// </summary>
public sealed class ComplaintAssignedEvent : DomainEvent
{
    /// <summary>
    /// Complaint identifier.
    /// </summary>
    public Guid ComplaintId { get; }

    /// <summary>
    /// Assigned contractor identifier.
    /// </summary>
    public Guid ContractorId { get; }

    /// <summary>
    /// Department responsible for the complaint.
    /// </summary>
    public Guid DepartmentId { get; }

    /// <summary>
    /// Officer who assigned the contractor.
    /// </summary>
    public Guid AssignedByUserId { get; }

    /// <summary>
    /// Creates a new complaint assigned event.
    /// </summary>
    public ComplaintAssignedEvent(
        Guid complaintId,
        Guid contractorId,
        Guid departmentId,
        Guid assignedByUserId)
    {
        if (complaintId == Guid.Empty)
            throw new ArgumentException("Complaint ID is required.", nameof(complaintId));

        if (contractorId == Guid.Empty)
            throw new ArgumentException("Contractor ID is required.", nameof(contractorId));

        if (departmentId == Guid.Empty)
            throw new ArgumentException("Department ID is required.", nameof(departmentId));

        if (assignedByUserId == Guid.Empty)
            throw new ArgumentException("Assigned-by user ID is required.", nameof(assignedByUserId));

        ComplaintId = complaintId;
        ContractorId = contractorId;
        DepartmentId = departmentId;
        AssignedByUserId = assignedByUserId;
    }
}