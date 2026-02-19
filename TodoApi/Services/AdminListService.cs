using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Dtos;
using TodoApi.Models;

namespace TodoApi.Services;

public sealed class AdminListService : IAdminListService
{
    private readonly AdminDbContext _db;

    public AdminListService(AdminDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<TodoListResponse>> GetAll()
    {
        return await _db.TodoLists.AsNoTracking()
            .OrderBy(l => l.TenantId)
            .ThenBy(l => l.Name)
            .Select(l => new TodoListResponse
            {
                Id = l.Id,
                TenantId = l.TenantId,
                Name = l.Name,
                CreatedAt = l.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<TodoListResponse?> GetById(Guid id)
    {
        return await _db.TodoLists.AsNoTracking()
            .Where(l => l.Id == id)
            .Select(l => new TodoListResponse
            {
                Id = l.Id,
                TenantId = l.TenantId,
                Name = l.Name,
                CreatedAt = l.CreatedAt
            })
            .SingleOrDefaultAsync();
    }

    public async Task<TodoListResponse> Create(CreateAdminListRequest request)
    {
        if (request.TenantId == Guid.Empty)
        {
            throw new InvalidOperationException("TenantId is required");
        }

        var list = new TodoList
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            Name = request.Name.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.TodoLists.Add(list);
        await _db.SaveChangesAsync();

        return new TodoListResponse
        {
            Id = list.Id,
            TenantId = list.TenantId,
            Name = list.Name,
            CreatedAt = list.CreatedAt
        };
    }

    public async Task<bool> Update(Guid id, UpdateListRequest request)
    {
        var list = await _db.TodoLists.SingleOrDefaultAsync(l => l.Id == id);
        if (list is null)
        {
            return false;
        }

        list.Name = request.Name.Trim();
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> Delete(Guid id)
    {
        var list = await _db.TodoLists.SingleOrDefaultAsync(l => l.Id == id);
        if (list is null)
        {
            return false;
        }

        _db.TodoLists.Remove(list);
        await _db.SaveChangesAsync();
        return true;
    }
}
