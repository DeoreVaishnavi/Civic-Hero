using CivicHero.Backend.Core.Common;

namespace CivicHero.Backend.Core.Entities;

/// <summary>
/// Represents a government department responsible for handling complaints.
/// Examples:
/// - Road Department
/// - Water Department
/// - Electricity Department
/// </summary>
public sealed class Department : SoftDeleteEntity
{
    /// <summary>
    /// Department name.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Department description.
    /// </summary>
    public string Description { get; private set; }

    /// <summary>
    /// Indicates whether the department is active.
    /// </summary>
    public bool IsActive { get; private set; }

    // ============================================================
    // Navigation Properties
    // ============================================================

    /// <summary>
    /// Users belonging to this department.
    /// </summary>
    public ICollection<User> Users { get; private set; }

    /// <summary>
    /// Contractors assigned to this department.
    /// </summary>
    public ICollection<Contractor> Contractors { get; private set; }

    /// <summary>
    /// Complaints handled by this department.
    /// </summary>
    public ICollection<Complaint> Complaints { get; private set; }

    /// <summary>
    /// Required by Entity Framework Core.
    /// </summary>
    private Department()
    {
        Name = string.Empty;
        Description = string.Empty;
        IsActive = true;

        Users = new List<User>();
        Contractors = new List<Contractor>();
        Complaints = new List<Complaint>();
    }

    /// <summary>
    /// Creates a new department.
    /// </summary>
    public Department(
        string name,
        string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "Department name is required.",
                nameof(name));

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        IsActive = true;

        Users = new List<User>();
        Contractors = new List<Contractor>();
        Complaints = new List<Complaint>();
    }

    /// <summary>
    /// Updates department information.
    /// </summary>
    public void Update(
        string name,
        string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "Department name is required.",
                nameof(name));

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Activates the department.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }

    /// <summary>
    /// Deactivates the department.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }
}