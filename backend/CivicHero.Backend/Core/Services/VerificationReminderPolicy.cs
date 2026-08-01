namespace CivicHero.Backend.Core.Services;

public sealed record VerificationReminderPolicyDecision(
    bool Allowed,
    int SentCount,
    int MaximumAllowed,
    DateTimeOffset? LastReminderAt,
    DateTimeOffset? NextAllowedAt,
    string? UnavailableReason);

public static class VerificationReminderPolicy
{
    public const int OfficerMaximumReminders = 2;
    public const int StaffMaximumReminders = 4;

    public static readonly TimeSpan OfficerCooldown = TimeSpan.FromHours(12);
    public static readonly TimeSpan StaffCooldown = TimeSpan.FromHours(6);

    public static VerificationReminderPolicyDecision Evaluate(
        string? role,
        int sentCount,
        DateTimeOffset? lastReminderAt,
        DateTimeOffset now)
    {
        var isOfficer = string.Equals(role, "Officer", StringComparison.OrdinalIgnoreCase);
        var isStaff = new[] { "Supervisor", "Admin", "SuperAdmin" }
            .Contains(role, StringComparer.OrdinalIgnoreCase);

        if (!isOfficer && !isStaff)
        {
            return new VerificationReminderPolicyDecision(
                false,
                Math.Max(0, sentCount),
                0,
                lastReminderAt,
                null,
                "Only the assigned Officer or authorized supervisory staff may send a verification reminder.");
        }

        var maximumAllowed = isOfficer ? OfficerMaximumReminders : StaffMaximumReminders;
        var cooldown = isOfficer ? OfficerCooldown : StaffCooldown;
        var normalizedCount = Math.Max(0, sentCount);
        var nextAllowedAt = lastReminderAt?.Add(cooldown);

        if (normalizedCount >= maximumAllowed)
        {
            return new VerificationReminderPolicyDecision(
                false,
                normalizedCount,
                maximumAllowed,
                lastReminderAt,
                nextAllowedAt,
                $"The {role} reminder limit of {maximumAllowed} has been reached for this verification cycle.");
        }

        if (nextAllowedAt.HasValue && nextAllowedAt.Value > now)
        {
            var nextUtc = nextAllowedAt.Value.ToUniversalTime();
            return new VerificationReminderPolicyDecision(
                false,
                normalizedCount,
                maximumAllowed,
                lastReminderAt,
                nextAllowedAt,
                $"A verification reminder was sent recently. Try again after {nextUtc:yyyy-MM-dd HH:mm} UTC.");
        }

        return new VerificationReminderPolicyDecision(
            true,
            normalizedCount,
            maximumAllowed,
            lastReminderAt,
            nextAllowedAt,
            null);
    }
}
