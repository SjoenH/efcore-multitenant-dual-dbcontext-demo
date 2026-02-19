using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Dtos;
using TodoApi.Models;

namespace TodoApi.Services;

public class TodoService : ITodoService
{
    private readonly TodoDbContext _context;

    public TodoService(TodoDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<TodoResponse>> GetTodos(bool? isComplete)
    {
        IQueryable<Todo> query = _context.Todos.AsNoTracking();

        if (isComplete is not null)
        {
            query = query.Where(t => t.IsComplete == isComplete.Value);
        }

        return await query
            .OrderBy(t => t.Id)
            .Select(t => new TodoResponse { Id = t.Id, Name = t.Name, IsComplete = t.IsComplete })
            .ToListAsync();
    }

    public async Task<TodoResponse?> GetTodo(long id)
    {
        return await _context.Todos.AsNoTracking()
            .Where(t => t.Id == id)
            .Select(t => new TodoResponse { Id = t.Id, Name = t.Name, IsComplete = t.IsComplete })
            .SingleOrDefaultAsync();
    }

    public async Task<TodoResponse> CreateTodo(CreateTodoRequest request)
    {
        var todo = new Todo
        {
            Name = request.Name.Trim(),
            IsComplete = request.IsComplete
        };

        _context.Todos.Add(todo);
        await _context.SaveChangesAsync();

        return new TodoResponse { Id = todo.Id, Name = todo.Name, IsComplete = todo.IsComplete };
    }

    public async Task<bool> UpdateTodo(long id, UpdateTodoRequest request)
    {
        var todo = await _context.Todos.SingleOrDefaultAsync(t => t.Id == id);
        if (todo is null)
        {
            return false;
        }

        todo.Name = request.Name.Trim();
        todo.IsComplete = request.IsComplete;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteTodo(long id)
    {
        var todo = await _context.Todos.FindAsync(id);
        if (todo == null)
        {
            return false;
        }

        _context.Todos.Remove(todo);
        await _context.SaveChangesAsync();

        return true;
    }

}
