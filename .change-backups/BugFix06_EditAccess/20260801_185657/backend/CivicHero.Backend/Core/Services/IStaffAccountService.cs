using CivicHero.Backend.Core.DTOs.Staff;

namespace CivicHero.Backend.Core.Services;

public interface IStaffAccountService
{
    Task<StaffAccountMetadataDto> GetMetadataAsync(CancellationToken cancellationToken = default);
    Task<StaffAccountDto> CreateAsync(CreateStaffAccountRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StaffAccountDto>> GetAsync(StaffAccountQuery query, CancellationToken cancellationToken = default);
    Task<StaffAccountDto> ReviewAsync(long userId, ReviewStaffAccountRequest request, CancellationToken cancellationToken = default);
}
