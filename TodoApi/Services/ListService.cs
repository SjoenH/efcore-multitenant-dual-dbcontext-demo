using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Dtos;
using TodoApi.Models;

namespace TodoApi.Services;

public sealed class ListService : IListService
{
    private readonly TodoDbContext _db;

    public ListService(TodoDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<TodoListResponse>> GetMyLists(Guid userId)
    {
        return await _db.TodoLists.AsNoTracking()
            .Where(l => l.OwnerUserId == userId)
            .OrderBy(l => l.CreatedAt)
            .Select(l => new TodoListResponse
            {
                Id = l.Id,
                Name = l.Name,
                OwnerUserId = l.OwnerUserId,
                CreatedAt = l.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<TodoListResponse?> GetMyList(Guid userId, Guid listId)
    {
        return await _db.TodoLists.AsNoTracking()
            .Where(l => l.Id == listId && l.OwnerUserId == userId)
            .Select(l => new TodoListResponse
            {
                Id = l.Id,
                Name = l.Name,
                OwnerUserId = l.OwnerUserId,
                CreatedAt = l.CreatedAt
            })
            .SingleOrDefaultAsync();
    }

    public async Task<TodoListResponse> CreateList(Guid userId, CreateListRequest request)
    {
        var name = request.Name.Trim();

        var list = new TodoList
        {
            Id = Guid.NewGuid(),
            OwnerUserId = userId,
            Name = name,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.TodoLists.Add(list);
        await _db.SaveChangesAsync();

        return new TodoListResponse
        {
            Id = list.Id,
            Name = list.Name,
            OwnerUserId = list.OwnerUserId,
            CreatedAt = list.CreatedAt
        };
    }

    public async Task<bool> DeleteList(Guid userId, Guid listId)
    {
        var list = await _db.TodoLists.SingleOrDefaultAsync(l => l.Id == listId && l.OwnerUserId == userId);
        if (list is null)
        {
            return false;
        }

        _db.TodoLists.Remove(list);
        await _db.SaveChangesAsync();
        return true;
    }
}
