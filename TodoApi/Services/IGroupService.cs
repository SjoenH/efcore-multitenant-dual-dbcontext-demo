using TodoApi.Dtos;

namespace TodoApi.Services;

public interface IGroupService
{
    Task<IReadOnlyList<GroupResponse>> GetMyGroups(Guid userId);
    Task<CreateGroupResponse> CreateGroup(Guid userId, CreateGroupRequest request);
    Task<bool> AddMember(Guid actingUserId, Guid groupId, Guid userIdToAdd);
    Task<IReadOnlyList<GroupMemberResponse>> GetMembers(Guid actingUserId, Guid groupId);
}
