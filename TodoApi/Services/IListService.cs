using TodoApi.Dtos;

namespace TodoApi.Services;

public interface IListService
{
    Task<IReadOnlyList<TodoListResponse>> GetAccessibleLists(Guid userId);
    Task<TodoListResponse?> GetAccessibleList(Guid userId, Guid listId);

    Task<TodoListResponse> CreatePersonalList(Guid userId, CreateListRequest request);
    Task<bool> DeletePersonalList(Guid userId, Guid listId);

    Task<IReadOnlyList<TodoListResponse>> GetGroupLists(Guid userId, Guid groupId);
    Task<TodoListResponse> CreateGroupList(Guid userId, Guid groupId, CreateGroupListRequest request);
    Task<bool> AssignGroupList(Guid userId, Guid groupId, Guid listId, Guid? assignedUserId);
}
