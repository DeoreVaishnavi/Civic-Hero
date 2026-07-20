using CivicHero.Backend.Core.Common;
using CivicHero.Backend.Core.Enums;
using CivicHero.Backend.Core.Events;
using CivicHero.Backend.Core.ValueObjects;

namespace CivicHero.Backend.Core.Entities;

/// <summary>
/// Represents a complaint submitted by a citizen.
/// Aggregate Root of the Complaint domain.
/// </summary>
public sealed class Complaint : AuditableEntity
{
    private readonly List<ComplaintImage> _images = new();
    private readonly List<ComplaintTimeline> _timelineEntries = new();
    private readonly List<ComplaintVote> _votes = new();
    private readonly List<AiFraudAnalysis> _fraudAnalyses = new();

    #region Properties

    public string Title { get; private set; }

    public string Description { get; private set; }

    public ComplaintPriority Priority { get; private set; }

    public ComplaintStatus Status { get; private set; }

    public GeoLocation Location { get; private set; }

    public Guid CitizenId { get; private set; }

    public User Citizen { get; private set; }= null!;

    public Guid WardId { get; private set; }

    public Ward Ward { get; private set; } = null!;

    public Guid DepartmentId { get; private set; }

    public Department Department { get; private set; }

    public Guid? ContractorId { get; private set; }

    public Contractor? Contractor { get; private set; }

    public IReadOnlyCollection<ComplaintImage> Images =>
        _images.AsReadOnly();

    public IReadOnlyCollection<ComplaintTimeline> TimelineEntries =>
        _timelineEntries.AsReadOnly();

    public IReadOnlyCollection<ComplaintVote> Votes =>
        _votes.AsReadOnly();

    public IReadOnlyCollection<AiFraudAnalysis> FraudAnalyses =>
        _fraudAnalyses.AsReadOnly();

    #endregion

    #region Constructors
private Complaint()
{
    Title = string.Empty;
    Description = string.Empty;

    Citizen = null!;
    Ward = null!;
    Department = null!;
    Location = null!;
}

    public Complaint(
        string title,
        string description,
        ComplaintPriority priority,
        GeoLocation location,
        Guid citizenId,
        Guid wardId,
        Guid departmentId)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));

        if (citizenId == Guid.Empty)
            throw new ArgumentException("Citizen is required.", nameof(citizenId));

        if (wardId == Guid.Empty)
            throw new ArgumentException("Ward is required.", nameof(wardId));

        if (departmentId == Guid.Empty)
            throw new ArgumentException("Department is required.", nameof(departmentId));

        Title = title.Trim();
        Description = description.Trim();

        Priority = priority;
        Status = ComplaintStatus.Submitted;

        Location = location ?? throw new ArgumentNullException(nameof(location));

        CitizenId = citizenId;
        WardId = wardId;
        DepartmentId = departmentId;

        AddDomainEvent(
            new ComplaintCreatedEvent(
                Id,
                CitizenId,
                DepartmentId,
                WardId));
    }

    #endregion

    #region Business Rules

    public bool CanAssign()
    {
        return Status == ComplaintStatus.Submitted
            || Status == ComplaintStatus.Reopened;
    }

    public bool CanResolve()
    {
        return Status == ComplaintStatus.InProgress;
    }

    public bool CanClose()
    {
        return Status == ComplaintStatus.Resolved;
    }

    #endregion

    #region Business Methods

    public void AssignContractor(
        Guid contractorId,
        Guid assignedByUserId)
    {
        if (!CanAssign())
            throw new InvalidOperationException(
                "Complaint cannot be assigned in its current state.");

        if (contractorId == Guid.Empty)
            throw new ArgumentException(
                "Contractor is required.",
                nameof(contractorId));

        if (assignedByUserId == Guid.Empty)
            throw new ArgumentException(
                "Assigned-by user is required.",
                nameof(assignedByUserId));

        ContractorId = contractorId;
        Status = ComplaintStatus.Assigned;

        AddDomainEvent(
            new ComplaintAssignedEvent(
                Id,
                contractorId,
                DepartmentId,
                assignedByUserId));
    }

    public void StartWork()
    {
        if (Status != ComplaintStatus.Assigned)
            throw new InvalidOperationException(
                "Complaint must be assigned before work can start.");

        Status = ComplaintStatus.InProgress;
    }

    public void Resolve()
    {
        if (!CanResolve())
            throw new InvalidOperationException(
                "Only complaints in progress can be resolved.");

        if (!ContractorId.HasValue)
            throw new InvalidOperationException(
                "Complaint must have an assigned contractor.");

        Status = ComplaintStatus.Resolved;

        AddDomainEvent(
            new ComplaintResolvedEvent(
                Id,
                CitizenId,
                ContractorId.Value,
                DepartmentId));
    }

    public void Close()
    {
        if (!CanClose())
            throw new InvalidOperationException(
                "Only resolved complaints can be closed.");

        Status = ComplaintStatus.Closed;
    }

    public void Reopen()
    {
        if (Status != ComplaintStatus.Closed)
            throw new InvalidOperationException(
                "Only closed complaints can be reopened.");

        Status = ComplaintStatus.Reopened;
    }

    public void ChangePriority(ComplaintPriority priority)
    {
        Priority = priority;
    }

    public void UpdateDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException(
                "Description is required.",
                nameof(description));

        description = description.Trim();

        if (Description == description)
            return;

        Description = description;
    }

    #endregion
}