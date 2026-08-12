namespace CivicHero.Backend.Core.Enums;

public enum ComplaintStatus
{
    Created = 1, AiTriage = 2, FraudReview = 3, Merged = 4, Assigned = 5,
    ReassignmentPending = 6, InProgress = 7, Escalated = 8, Resolved = 9,
    VerificationPending = 10, Disputed = 11, Appealed = 12, Closed = 13,
    ClosedFraud = 14, ClosedAuto = 15, Withdrawn = 16
}
