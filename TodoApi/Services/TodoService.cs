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

    public async Task<IReadOnlyList<TodoResponse>> GetTodos(Guid userId, Guid listId, bool? isComplete)
    {
        IQueryable<Todo> query = _context.Todos.AsNoTracking()
            .Where(t => t.ListId == listId)
            .Where(t => _context.TodoLists.Any(l => l.Id == t.ListId && l.OwnerUserId == userId));

        if (isComplete is not null)
        {
            query = query.Where(t => t.IsComplete == isComplete.Value);
        }

        return await query
            .OrderBy(t => t.Id)
            .Select(t => new TodoResponse { Id = t.Id, Name = t.Name, IsComplete = t.IsComplete })
            .ToListAsync();
    }

    public async Task<TodoResponse?> GetTodo(Guid userId, Guid listId, long id)
    {
        return await _context.Todos.AsNoTracking()
            .Where(t => t.Id == id && t.ListId == listId)
            .Where(t => _context.TodoLists.Any(l => l.Id == t.ListId && l.OwnerUserId == userId))
            .Select(t => new TodoResponse { Id = t.Id, Name = t.Name, IsComplete = t.IsComplete })
            .SingleOrDefaultAsync();
    }

    public async Task<TodoResponse> CreateTodo(Guid userId, Guid listId, CreateTodoRequest request)
    {
        var listExists = await _context.TodoLists.AsNoTracking()
            .AnyAsync(l => l.Id == listId && l.OwnerUserId == userId);
        if (!listExists)
        {
            throw new UnauthorizedAccessException("List not found or not accessible");
        }

        var todo = new Todo
        {
            ListId = listId,
            Name = request.Name.Trim(),
            IsComplete = request.IsComplete
        };

        _context.Todos.Add(todo);
        await _context.SaveChangesAsync();

        return new TodoResponse { Id = todo.Id, Name = todo.Name, IsComplete = todo.IsComplete };
    }

    public async Task<bool> UpdateTodo(Guid userId, Guid listId, long id, UpdateTodoRequest request)
    {
        var todo = await _context.Todos
            .Include(t => t.List)
            .SingleOrDefaultAsync(t => t.Id == id && t.ListId == listId);

        if (todo is null || todo.List.OwnerUserId != userId)
        {
            return false;
        }

        if (todo is null)
        {
            return false;
        }

        todo.Name = request.Name.Trim();
        todo.IsComplete = request.IsComplete;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteTodo(Guid userId, Guid listId, long id)
    {
        var todo = await _context.Todos
            .Include(t => t.List)
            .SingleOrDefaultAsync(t => t.Id == id && t.ListId == listId);

        if (todo is null || todo.List.OwnerUserId != userId)
        {
            return false;
        }

        if (todo == null)
        {
            return false;
        }

        _context.Todos.Remove(todo);
        await _context.SaveChangesAsync();

        return true;
    }

}
