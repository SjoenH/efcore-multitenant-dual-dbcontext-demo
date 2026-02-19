using TodoApi.Dtos;

namespace TodoApi.Services;

public interface ITodoService
{
    Task<IReadOnlyList<TodoResponse>> GetTodos(Guid userId, Guid listId, bool? isComplete);
    Task<TodoResponse?> GetTodo(Guid userId, Guid listId, long id);
    Task<TodoResponse> CreateTodo(Guid userId, Guid listId, CreateTodoRequest request);
    Task<bool> UpdateTodo(Guid userId, Guid listId, long id, UpdateTodoRequest request);
    Task<bool> DeleteTodo(Guid userId, Guid listId, long id);
}
