using AutoMapper;
using CivicHero.Backend.Core.DTOs.Common;
using CivicHero.Backend.Core.DTOs.Users;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Enums;
using CivicHero.Backend.Core.Exceptions;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Core.Validation;

namespace CivicHero.Backend.Core.Services;

public sealed class UserService : IUserService
{
    private readonly IUserRepository _users;
    private readonly IDepartmentRepository _departments;
    private readonly IWardRepository _wards;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UserService(
        IUserRepository users,
        IDepartmentRepository departments,
        IWardRepository wards,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _users = users;
        _departments = departments;
        _wards = wards;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<UserProfileDto> GetProfileAsync(long userId, CancellationToken cancellationToken = default) =>
        _mapper.Map<UserProfileDto>(await GetUserEntityAsync(userId, false, cancellationToken));

    public async Task<UserProfileDto> UpdateProfileAsync(long userId, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var user = await GetUserEntityAsync(userId, true, cancellationToken);
        ApplyProfile(user, request);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetProfileAsync(user.Id, cancellationToken);
    }

    public async Task<PagedResponse<UserListDto>> GetUsersAsync(UserQuery query, CancellationToken cancellationToken = default)
    {
        query.Page = Math.Max(1, query.Page);
        query.PageSize = Math.Clamp(query.PageSize, 1, 100);
        var result = await _users.GetPagedAsync(query, cancellationToken);
        return new PagedResponse<UserListDto>
        {
            Items = result.Items.Select(item => _mapper.Map<UserListDto>(item)).ToList(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = result.TotalCount
        };
    }

    public async Task<UserProfileDto> GetUserAsync(long requesterId, string requesterRole, long targetUserId, CancellationToken cancellationToken = default)
    {
        EnsureSelfOrAdmin(requesterId, requesterRole, targetUserId);
        return await GetProfileAsync(targetUserId, cancellationToken);
    }

    public async Task<UserProfileDto> UpdateUserAsync(long requesterId, string requesterRole, long targetUserId, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        EnsureSelfOrAdmin(requesterId, requesterRole, targetUserId);
        return await UpdateProfileAsync(targetUserId, request, cancellationToken);
    }

    public async Task<UserProfileDto> ChangeRoleAsync(long requesterId, string requesterRole, long targetUserId, ChangeRoleRequest request, CancellationToken cancellationToken = default)
    {
        var actorRole = ParseRole(requesterRole);
        var target = await GetUserEntityAsync(targetUserId, true, cancellationToken);
        EnsureCanManage(actorRole, requesterId, target);
        var newRole = ParseRole(request.Role);

        if (newRole == UserRole.SuperAdmin && actorRole != UserRole.SuperAdmin)
            throw new UnauthorizedAccessException("Only a SuperAdmin can assign the SuperAdmin role.");

        target.Role = newRole;
        target.DepartmentId = request.DepartmentId;
        target.WardId = request.WardId;
        await NormalizeAndValidateScopeAsync(target, cancellationToken);
        InvalidateSessions(target);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetProfileAsync(target.Id, cancellationToken);
    }

    public async Task<UserProfileDto> AssignDepartmentAsync(long requesterId, string requesterRole, long targetUserId, AssignDepartmentRequest request, CancellationToken cancellationToken = default)
    {
        var actorRole = ParseRole(requesterRole);
        var target = await GetUserEntityAsync(targetUserId, true, cancellationToken);
        EnsureCanManage(actorRole, requesterId, target);
        target.DepartmentId = request.DepartmentId;
        if (!request.DepartmentId.HasValue) target.WardId = null;
        await NormalizeAndValidateScopeAsync(target, cancellationToken);
        InvalidateSessions(target);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetProfileAsync(target.Id, cancellationToken);
    }

    public async Task<UserProfileDto> AssignWardAsync(long requesterId, string requesterRole, long targetUserId, AssignWardRequest request, CancellationToken cancellationToken = default)
    {
        var actorRole = ParseRole(requesterRole);
        var target = await GetUserEntityAsync(targetUserId, true, cancellationToken);
        EnsureCanManage(actorRole, requesterId, target);
        target.WardId = request.WardId;
        await NormalizeAndValidateScopeAsync(target, cancellationToken);
        InvalidateSessions(target);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetProfileAsync(target.Id, cancellationToken);
    }

    public async Task<UserProfileDto> SetActiveAsync(long requesterId, string requesterRole, long targetUserId, bool isActive, CancellationToken cancellationToken = default)
    {
        var actorRole = ParseRole(requesterRole);
        var target = await GetUserEntityAsync(targetUserId, true, cancellationToken);
        EnsureCanManage(actorRole, requesterId, target);
        if (requesterId == targetUserId && !isActive)
            throw new BusinessRuleViolationException("You cannot deactivate your own account.");
        target.IsActive = isActive;
        InvalidateSessions(target);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetProfileAsync(target.Id, cancellationToken);
    }

    public async Task ForceLogoutAsync(long requesterId, string requesterRole, long targetUserId, CancellationToken cancellationToken = default)
    {
        var actorRole = ParseRole(requesterRole);
        var target = await GetUserEntityAsync(targetUserId, true, cancellationToken);
        EnsureCanManage(actorRole, requesterId, target);
        InvalidateSessions(target);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<UserManagementMetadataDto> GetManagementMetadataAsync(CancellationToken cancellationToken = default)
    {
        var departments = (await _departments.ListAsync(cancellationToken)).Where(x => x.IsActive).OrderBy(x => x.Name).ToList();
        var wards = (await _wards.ListAsync(cancellationToken)).Where(x => x.IsActive).OrderBy(x => x.Name).ToList();
        return new UserManagementMetadataDto
        {
            Roles = Enum.GetNames<UserRole>(),
            Departments = departments.Select(x => new LookupDto { Id = x.Id, Name = x.Name, Code = x.Code }).ToList(),
            Wards = wards.Select(x => new WardLookupDto { Id = x.Id, DepartmentId = x.DepartmentId, Name = x.Name, Code = x.Code }).ToList()
        };
    }

    private async Task<User> GetUserEntityAsync(long id, bool tracking, CancellationToken cancellationToken) =>
        await _users.GetProfileByIdAsync(id, tracking, cancellationToken)
        ?? throw new NotFoundException("User account was not found.");

    private static void ApplyProfile(User user, UpdateProfileRequest request)
    {
        user.FullName = PersonNameRules.Normalize(request.FullName);
        user.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
    }

    private async Task NormalizeAndValidateScopeAsync(User target, CancellationToken cancellationToken)
    {
        if (target.Role is UserRole.Citizen or UserRole.Admin or UserRole.SuperAdmin)
        {
            target.DepartmentId = null;
            target.WardId = null;
            return;
        }

        if (!target.DepartmentId.HasValue)
            throw new BusinessRuleViolationException("Officer and Supervisor accounts require a department.");

        var department = await _departments.GetByIdAsync(target.DepartmentId.Value, cancellationToken)
            ?? throw new NotFoundException("Selected department was not found.");
        if (!department.IsActive)
            throw new BusinessRuleViolationException("Selected department is inactive.");

        if (target.Role == UserRole.Officer && !target.WardId.HasValue)
            throw new BusinessRuleViolationException("Officer accounts require a ward.");

        if (target.WardId.HasValue)
        {
            var ward = await _wards.GetByIdAsync(target.WardId.Value, cancellationToken)
                ?? throw new NotFoundException("Selected ward was not found.");
            if (!ward.IsActive || ward.DepartmentId != target.DepartmentId.Value)
                throw new BusinessRuleViolationException("Selected ward must be active and belong to the selected department.");
        }
    }

    private static void EnsureSelfOrAdmin(long requesterId, string requesterRole, long targetUserId)
    {
        var role = ParseRole(requesterRole);
        if (requesterId != targetUserId && role is not UserRole.Admin and not UserRole.SuperAdmin)
            throw new UnauthorizedAccessException("You do not have permission to access this user.");
    }

    private static void EnsureCanManage(UserRole actorRole, long requesterId, User target)
    {
        if (actorRole is not UserRole.Admin and not UserRole.SuperAdmin)
            throw new UnauthorizedAccessException("Administrator permission is required.");
        if (target.Role == UserRole.SuperAdmin && actorRole != UserRole.SuperAdmin)
            throw new UnauthorizedAccessException("Only a SuperAdmin can manage another SuperAdmin.");
        if (requesterId == target.Id && target.Role == UserRole.SuperAdmin && actorRole == UserRole.SuperAdmin)
            return;
    }

    private static UserRole ParseRole(string role) =>
        Enum.TryParse<UserRole>(role, true, out var parsed)
            ? parsed
            : throw new UnauthorizedAccessException("Authenticated role is invalid.");

    private static void InvalidateSessions(User user)
    {
        user.AuthorizationVersion++;
        user.RefreshTokenHash = null;
        user.RefreshTokenCreatedAt = null;
        user.RefreshTokenExpiresAt = null;
    }
}
