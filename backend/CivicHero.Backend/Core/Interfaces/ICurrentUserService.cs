namespace CivicHero.Backend.Core.Interfaces;

/// <summary>
/// Provides information about the currently authenticated user.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the identifier of the current user.
    /// Returns null when no authenticated user exists.
    /// </summary>
    Guid? UserId { get; }
}