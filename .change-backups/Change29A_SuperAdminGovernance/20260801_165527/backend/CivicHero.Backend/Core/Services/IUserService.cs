using CivicHero.Backend.Core.DTOs.Common;
using CivicHero.Backend.Core.DTOs.Users;

namespace CivicHero.Backend.Core.Services;

public interface IUserService
{
    Task<UserProfileDto> GetProfileAsync(long userId, CancellationToken cancellationToken = default);
    Task<UserProfileDto> UpdateProfileAsync(long userId, UpdateProfileRequest request, CancellationToken cancellationToken = default);
    Task<ChangeEmailResponse> ChangeEmailAsync(long userId, ChangeEmailRequest request, CancellationToken cancellationToken = default);
    Task<ProfileAvatarResult> UploadAvatarAsync(long userId, UploadProfileAvatarRequest request, CancellationToken cancellationToken = default);
    Task<ProfileAvatarContent?> GetAvatarAsync(long userId, CancellationToken cancellationToken = default);
    Task<ProfileAvatarResult> DeleteAvatarAsync(long userId, CancellationToken cancellationToken = default);
    Task<PagedResponse<UserListDto>> GetUsersAsync(UserQuery query, CancellationToken cancellationToken = default);
    Task<UserProfileDto> GetUserAsync(long requesterId, string requesterRole, long targetUserId, CancellationToken cancellationToken = default);
    Task<UserProfileDto> UpdateUserAsync(long requesterId, string requesterRole, long targetUserId, UpdateProfileRequest request, CancellationToken cancellationToken = default);
    Task<UserProfileDto> ChangeRoleAsync(long requesterId, string requesterRole, long targetUserId, ChangeRoleRequest request, CancellationToken cancellationToken = default);
    Task<UserProfileDto> AssignDepartmentAsync(long requesterId, string requesterRole, long targetUserId, AssignDepartmentRequest request, CancellationToken cancellationToken = default);
    Task<UserProfileDto> AssignWardAsync(long requesterId, string requesterRole, long targetUserId, AssignWardRequest request, CancellationToken cancellationToken = default);
    Task<UserProfileDto> SetActiveAsync(long requesterId, string requesterRole, long targetUserId, bool isActive, CancellationToken cancellationToken = default);
    Task ForceLogoutAsync(long requesterId, string requesterRole, long targetUserId, CancellationToken cancellationToken = default);
    Task<UserManagementMetadataDto> GetManagementMetadataAsync(CancellationToken cancellationToken = default);
}
