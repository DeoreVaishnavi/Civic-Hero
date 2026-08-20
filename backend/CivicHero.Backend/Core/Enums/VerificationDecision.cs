namespace CivicHero.Backend.Core.Enums;

public enum VerificationDecision
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    AutoClosed = 4,
    AdminOverride = 5,
    NotResolvedYet = 6,
    RequestRevisit = 7,
    PartiallyResolved = 8
}
