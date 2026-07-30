namespace CivicHero.Backend.Core.DTOs.Verification;
public sealed record VerifyComplaintRequest(bool Approved, int Rating, string? Remarks, decimal Latitude, decimal Longitude);
public sealed record GeoVerifyRequest(decimal Latitude, decimal Longitude);
public sealed record VerificationResponse(long ComplaintId, string ReferenceNumber, string Title, string Status, string Decision, int? Rating, string? Remarks, double? DistanceMetres, DateTimeOffset DueAt, DateTimeOffset? CompletedAt, bool CanVerify, bool CanRemind);
public sealed record VerificationQueueItem(long ComplaintId, string ReferenceNumber, string Title, string Priority, string DepartmentName, string WardName, DateTimeOffset DueAt, long RemainingMinutes, bool IsOverdue);
