using CivicHero.Backend.Core.Common;

namespace CivicHero.Backend.Core.Events;

/// <summary>
/// Raised when a new complaint is created.
/// </summary>
public sealed class ComplaintCreatedEvent : DomainEvent
{
    /// <summary>
    /// Identifier of the newly created complaint.
    /// </summary>
    public Guid ComplaintId { get; }

    /// <summary>
    /// Citizen who created the complaint.
    /// </summary>
    public Guid CitizenId { get; }

    /// <summary>
    /// Department responsible for handling the complaint.
    /// </summary>
    public Guid DepartmentId { get; }

    /// <summary>
    /// Ward in which the complaint was created.
    /// </summary>
    public Guid WardId { get; }

    /// <summary>
    /// Creates a new complaint created event.
    /// </summary>
    public ComplaintCreatedEvent(
        Guid complaintId,
        Guid citizenId,
        Guid departmentId,
        Guid wardId)
    {
        if (complaintId == Guid.Empty)
            throw new ArgumentException("Complaint ID is required.", nameof(complaintId));

        if (citizenId == Guid.Empty)
            throw new ArgumentException("Citizen ID is required.", nameof(citizenId));

        if (departmentId == Guid.Empty)
            throw new ArgumentException("Department ID is required.", nameof(departmentId));

        if (wardId == Guid.Empty)
            throw new ArgumentException("Ward ID is required.", nameof(wardId));

        ComplaintId = complaintId;
        CitizenId = citizenId;
        DepartmentId = departmentId;
        WardId = wardId;
    }
}