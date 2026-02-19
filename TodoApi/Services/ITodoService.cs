using TodoApi.Dtos;

namespace TodoApi.Services;

public interface ITodoService
{
    Task<IReadOnlyList<TodoResponse>> GetTodos(bool? isComplete);
    Task<TodoResponse?> GetTodo(long id);
    Task<TodoResponse> CreateTodo(CreateTodoRequest request);
    Task<bool> UpdateTodo(long id, UpdateTodoRequest request);
    Task<bool> DeleteTodo(long id);
}
