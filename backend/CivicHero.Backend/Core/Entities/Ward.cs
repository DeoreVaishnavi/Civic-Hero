using CivicHero.Backend.Core.Common;

namespace CivicHero.Backend.Core.Entities;

/// <summary>
/// Represents a municipal ward within the CivicHero system.
/// A ward groups complaints and users by geographical area.
/// </summary>
public sealed class Ward : SoftDeleteEntity
{
    /// <summary>
    /// Gets the ward number.
    /// </summary>
    public int Number { get; private set; }

    /// <summary>
    /// Gets the ward name.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets the ward description.
    /// </summary>
    public string Description { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the ward is active.
    /// </summary>
    public bool IsActive { get; private set; }

    // ==========================
    // Navigation Properties
    // ==========================

    /// <summary>
    /// Complaints registered in this ward.
    /// </summary>
    public ICollection<Complaint> Complaints { get; private set; }

    /// <summary>
    /// Users belonging to this ward.
    /// (Optional relationship if User has WardId)
    /// </summary>
    public ICollection<User> Users { get; private set; }

    private Ward()
    {
        Name = string.Empty;
        Description = string.Empty;

        Complaints = new List<Complaint>();
        Users = new List<User>();
    }

    /// <summary>
    /// Creates a new ward.
    /// </summary>
    public Ward(
        int number,
        string name,
        string description)
    {
        if (number <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(number),
                "Ward number must be greater than zero.");

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "Ward name is required.",
                nameof(name));

        Number = number;
        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        IsActive = true;

        Complaints = new List<Complaint>();
        Users = new List<User>();
    }

    /// <summary>
    /// Updates ward information.
    /// </summary>
    public void Update(
        string name,
        string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "Ward name is required.",
                nameof(name));

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Activates the ward.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }

    /// <summary>
    /// Deactivates the ward.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }
}