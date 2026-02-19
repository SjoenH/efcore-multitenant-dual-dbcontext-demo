using TodoApi.Dtos;

namespace TodoApi.Services;

public interface IAdminListService
{
    Task<IReadOnlyList<TodoListResponse>> GetAll();
    Task<TodoListResponse?> GetById(Guid id);
    Task<TodoListResponse> Create(CreateAdminListRequest request);
    Task<bool> Update(Guid id, UpdateListRequest request);
    Task<bool> Delete(Guid id);
}
