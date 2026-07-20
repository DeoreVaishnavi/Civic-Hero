using CivicHero.Backend.Core.Common;
using CivicHero.Backend.Core.Enums;

namespace CivicHero.Backend.Core.Entities;

/// <summary>
/// Represents a contractor responsible for resolving complaints.
/// Each contractor is linked to exactly one user account.
/// </summary>
public sealed class Contractor : SoftDeleteEntity
{
    /// <summary>
    /// Linked user account.
    /// One User ↔ One Contractor.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Navigation property.
    /// </summary>
    public User? User { get; private set; }

    /// <summary>
    /// Department to which the contractor belongs.
    /// </summary>
    public Guid DepartmentId { get; private set; }

    /// <summary>
    /// Navigation property.
    /// </summary>
    public Department Department { get; private set; }

    /// <summary>
    /// Employee/contractor identification number.
    /// </summary>
    public string EmployeeNumber { get; private set; }

    /// <summary>
    /// Current contractor status.
    /// </summary>
    public ContractorStatus Status { get; private set; }

    /// <summary>
    /// Optional specialization.
    /// Example:
    /// Road Maintenance,
    /// Electrical,
    /// Water Supply.
    /// </summary>
    public string? Specialization { get; private set; }

    /// <summary>
    /// Complaints currently or previously assigned
    /// to this contractor.
    /// </summary>
    public ICollection<Complaint> Complaints { get; private set; }

private Contractor()
{
    User = null!;
    Department = null!;

    EmployeeNumber = string.Empty;

    Complaints = new List<Complaint>();
}

    /// <summary>
    /// Creates a contractor.
    /// </summary>
    public Contractor(
        Guid userId,
        Guid departmentId,
        string employeeNumber,
        string? specialization = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException(
                "User ID is required.",
                nameof(userId));

        if (departmentId == Guid.Empty)
            throw new ArgumentException(
                "Department ID is required.",
                nameof(departmentId));

        if (string.IsNullOrWhiteSpace(employeeNumber))
            throw new ArgumentException(
                "Employee number is required.",
                nameof(employeeNumber));

        UserId = userId;
        DepartmentId = departmentId;
        EmployeeNumber = employeeNumber.Trim();
        Specialization = string.IsNullOrWhiteSpace(specialization)
            ? null
            : specialization.Trim();

        Status = ContractorStatus.Available;

        Complaints = new List<Complaint>();
    }

    /// <summary>
    /// Marks the contractor as available.
    /// </summary>
    public void SetAvailable()
    {
        Status = ContractorStatus.Available;
    }

    /// <summary>
    /// Marks the contractor as busy.
    /// </summary>
    public void SetBusy()
    {
        Status = ContractorStatus.Busy;
    }

    /// <summary>
    /// Marks the contractor as inactive.
    /// </summary>
    public void SetInactive()
    {
        Status = ContractorStatus.Inactive;
    }

    /// <summary>
    /// Updates contractor specialization.
    /// </summary>
    public void UpdateSpecialization(string? specialization)
    {
        Specialization = string.IsNullOrWhiteSpace(specialization)
            ? null
            : specialization.Trim();
    }
}