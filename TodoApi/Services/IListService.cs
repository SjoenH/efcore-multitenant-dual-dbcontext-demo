using TodoApi.Dtos;

namespace TodoApi.Services;

public interface IListService
{
    Task<IReadOnlyList<TodoListResponse>> GetMyLists(Guid userId);
    Task<TodoListResponse?> GetMyList(Guid userId, Guid listId);
    Task<TodoListResponse> CreateList(Guid userId, CreateListRequest request);
    Task<bool> DeleteList(Guid userId, Guid listId);
}
