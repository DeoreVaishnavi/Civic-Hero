using CivicHero.Backend.Core.Common;
using CivicHero.Backend.Core.Enums;
using CivicHero.Backend.Core.Events;
using CivicHero.Backend.Core.ValueObjects;

namespace CivicHero.Backend.Core.Entities;

/// <summary>
/// Represents a system user.
/// Acts as the aggregate root for user-related operations.
/// </summary>
public sealed class User : SoftDeleteEntity
{
    #region Properties

    /// <summary>
    /// User's first name.
    /// </summary>
    public string FirstName { get; private set; }

    /// <summary>
    /// User's last name.
    /// </summary>
    public string LastName { get; private set; }

    /// <summary>
    /// User email address.
    /// </summary>
    public EmailAddress Email { get; private set; }

    /// <summary>
    /// User phone number.
    /// </summary>
    public string PhoneNumber { get; private set; }

    /// <summary>
    /// Password hash.
    /// Never store plain text passwords.
    /// </summary>
    public string PasswordHash { get; private set; }

    /// <summary>
    /// User role.
    /// </summary>
    public UserRole Role { get; private set; }

    /// <summary>
    /// Indicates whether the email address has been confirmed.
    /// </summary>
    public bool EmailConfirmed { get; private set; }

    /// <summary>
    /// Indicates whether the phone number has been confirmed.
    /// </summary>
    public bool PhoneNumberConfirmed { get; private set; }

    /// <summary>
    /// Indicates whether the account is active.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Department identifier.
    /// Nullable because citizens may not belong to a department.
    /// </summary>
    public Guid? DepartmentId { get; private set; }

    /// <summary>
    /// Department navigation property.
    /// </summary>
    public Department? Department { get; private set; }

    #endregion

    #region Navigation Collections

    private readonly List<Complaint> _complaints = new();

    /// <summary>
    /// Complaints created by this user.
    /// </summary>
    public IReadOnlyCollection<Complaint> Complaints =>
        _complaints.AsReadOnly();

    private readonly List<ComplaintVote> _complaintVotes = new();

    /// <summary>
    /// Votes cast by this user.
    /// </summary>
    public IReadOnlyCollection<ComplaintVote> ComplaintVotes =>
        _complaintVotes.AsReadOnly();

    private readonly List<Notification> _notifications = new();

    /// <summary>
    /// Notifications received by this user.
    /// </summary>
    public IReadOnlyCollection<Notification> Notifications =>
        _notifications.AsReadOnly();

    private readonly List<ChatSession> _chatSessions = new();

    /// <summary>
    /// Chat sessions belonging to this user.
    /// </summary>
    public IReadOnlyCollection<ChatSession> ChatSessions =>
        _chatSessions.AsReadOnly();

    private readonly List<ReputationLog> _reputationLogs = new();

    /// <summary>
    /// Reputation history.
    /// </summary>
    public IReadOnlyCollection<ReputationLog> ReputationLogs =>
        _reputationLogs.AsReadOnly();

    private readonly List<Redemption> _redemptions = new();

    /// <summary>
    /// Reward redemption history.
    /// </summary>
    public IReadOnlyCollection<Redemption> Redemptions =>
        _redemptions.AsReadOnly();

    private readonly List<DisputeAuditLog> _disputeAuditLogs = new();

    /// <summary>
    /// Dispute audit entries created by this user.
    /// </summary>
    public IReadOnlyCollection<DisputeAuditLog> DisputeAuditLogs =>
        _disputeAuditLogs.AsReadOnly();

    #endregion

    #region Constructors

    private User()
    {
        FirstName = string.Empty;
        LastName = string.Empty;
        Email = null!;
        PhoneNumber = string.Empty;
        PasswordHash = string.Empty;
        IsActive = true;
    }

    /// <summary>
    /// Creates a new user.
    /// </summary>
    public User(
        string firstName,
        string lastName,
        EmailAddress email,
        string phoneNumber,
        string passwordHash,
        UserRole role,
        Guid? departmentId = null)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required.", nameof(firstName));

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required.", nameof(lastName));

        ArgumentNullException.ThrowIfNull(email);

        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Phone number is required.", nameof(phoneNumber));

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Email = email;
        PhoneNumber = phoneNumber.Trim();
        PasswordHash = passwordHash;
        Role = role;
        DepartmentId = departmentId;

        EmailConfirmed = false;
        PhoneNumberConfirmed = false;
        IsActive = true;

        AddDomainEvent(
            new UserRegisteredEvent(
                Id,
                Email.Value,
                Role));
    }

    #endregion

    #region Business Methods

    /// <summary>
    /// Confirms the user's email.
    /// </summary>
    public void ConfirmEmail()
    {
        EmailConfirmed = true;
    }

    /// <summary>
    /// Confirms the user's phone number.
    /// </summary>
    public void ConfirmPhone()
    {
        PhoneNumberConfirmed = true;
    }

    /// <summary>
    /// Activates the account.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }

    /// <summary>
    /// Deactivates the account.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Changes the user's password.
    /// </summary>
    public void ChangePassword(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));

        PasswordHash = passwordHash;
    }

    /// <summary>
    /// Updates profile information.
    /// </summary>
    public void UpdateProfile(
        string firstName,
        string lastName,
        EmailAddress email,
        string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required.", nameof(firstName));

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required.", nameof(lastName));

        ArgumentNullException.ThrowIfNull(email);

        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Phone number is required.", nameof(phoneNumber));

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Email = email;
        PhoneNumber = phoneNumber.Trim();
    }

    /// <summary>
    /// Changes the user's role.
    /// </summary>
    public void ChangeRole(UserRole role)
    {
        Role = role;
    }

    /// <summary>
    /// Assigns the user to a department.
    /// </summary>
    public void AssignDepartment(Guid? departmentId)
    {
        DepartmentId = departmentId;
    }

    #endregion
}