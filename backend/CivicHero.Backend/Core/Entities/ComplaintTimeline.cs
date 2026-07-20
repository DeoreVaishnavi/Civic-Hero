using CivicHero.Backend.Core.Common;

namespace CivicHero.Backend.Core.Entities;

/// <summary>
/// Represents a historical event in the lifecycle of a complaint.
/// </summary>
public sealed class ComplaintTimeline : AuditableEntity
{
    /// <summary>
    /// Related complaint identifier.
    /// </summary>
    public Guid ComplaintId { get; private set; }

    /// <summary>
    /// Navigation property.
    /// </summary>
    public Complaint Complaint { get; private set; }

    /// <summary>
    /// Title of the timeline event.
    /// Example: Complaint Assigned.
    /// </summary>
    public string Title { get; private set; }

    /// <summary>
    /// Detailed description of the event.
    /// </summary>
    public string Description { get; private set; }

   private ComplaintTimeline()
{
    Complaint = null!;

    Title = string.Empty;
    Description = string.Empty;
}
    public ComplaintTimeline(
        Guid complaintId,
        string title,
        string description)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));

        ComplaintId = complaintId;
        Title = title.Trim();
        Description = description.Trim();
    }

    public void UpdateDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));

        Description = description.Trim();
    }
}