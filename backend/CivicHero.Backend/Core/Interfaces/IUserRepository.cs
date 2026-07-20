using CivicHero.Backend.Core.Entities;

namespace CivicHero.Backend.Core.Interfaces;

/// <summary>
/// Repository contract for user-specific data operations.
/// </summary>
public interface IUserRepository : IRepository<User>
{
    /// <summary>
    /// Gets a user by email address.
    /// </summary>
    Task<User?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by phone number.
    /// </summary>
    Task<User?> GetByPhoneNumberAsync(
        string phoneNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all users belonging to a department.
    /// </summary>
    Task<IReadOnlyList<User>> GetByDepartmentAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all users having the specified role.
    /// </summary>
    Task<IReadOnlyList<User>> GetByRoleAsync(
        UserRole role,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether an email address already exists.
    /// </summary>
    Task<bool> EmailExistsAsync(
        string email,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether a phone number already exists.
    /// </summary>
    Task<bool> PhoneNumberExistsAsync(
        string phoneNumber,
        CancellationToken cancellationToken = default);
}