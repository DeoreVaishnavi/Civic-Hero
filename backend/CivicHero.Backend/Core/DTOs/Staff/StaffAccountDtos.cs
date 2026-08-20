namespace CivicHero.Backend.Core.DTOs.Staff;

public sealed class CreateStaffAccountRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Role { get; set; } = string.Empty;
    public long DepartmentId { get; set; }
    public long? WardId { get; set; }
    public string TemporaryPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class ReviewStaffAccountRequest
{
    public string Decision { get; set; } = string.Empty;
    public string? Remarks { get; set; }
}

public sealed class StaffAccountQuery
{
    public string? Search { get; set; }
    public string? Status { get; set; }
}

public sealed class StaffAccountDto
{
    public long Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Role { get; set; } = string.Empty;
    public long? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public long? WardId { get; set; }
    public string? WardName { get; set; }
    public bool IsActive { get; set; }
    public string ApprovalStatus { get; set; } = string.Empty;
    public string? RequestedBy { get; set; }
    public DateTimeOffset? RequestedAt { get; set; }
    public string? ReviewedBy { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? ReviewRemarks { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class StaffAccountMetadataDto
{
    public IReadOnlyList<StaffDepartmentOptionDto> Departments { get; set; } = [];
    public IReadOnlyList<StaffWardOptionDto> Wards { get; set; } = [];
}

public sealed class StaffDepartmentOptionDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public sealed class StaffWardOptionDto
{
    public long Id { get; set; }
    public long DepartmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}
