namespace CivicHero.Backend.Core.Interfaces;

public interface ICurrentUserService
{
    bool IsAuthenticated { get; }
    long? UserId { get; }
    string? Email { get; }
    string? Role { get; }
    long? DepartmentId { get; }
    long? WardId { get; }
}
