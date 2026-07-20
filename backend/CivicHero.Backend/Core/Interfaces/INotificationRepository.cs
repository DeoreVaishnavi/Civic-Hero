using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Enums;

namespace CivicHero.Backend.Core.Interfaces;

/// <summary>
/// Repository contract for notification-specific data operations.
/// </summary>
public interface INotificationRepository : IRepository<Notification>
{
    /// <summary>
    /// Gets all notifications for a user.
    /// </summary>
    Task<IReadOnlyList<Notification>> GetByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets unread notifications for a user.
    /// </summary>
    Task<IReadOnlyList<Notification>> GetUnreadByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets notifications by type.
    /// </summary>
    Task<IReadOnlyList<Notification>> GetByTypeAsync(
        NotificationType type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets notifications created within the specified date range.
    /// </summary>
    Task<IReadOnlyList<Notification>> GetByDateRangeAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the number of unread notifications for a user.
    /// </summary>
    Task<int> GetUnreadCountAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks all notifications as read for a user.
    /// </summary>
    Task MarkAllAsReadAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}