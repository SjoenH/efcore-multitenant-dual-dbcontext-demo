using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using TodoApi.Data;
using TodoApi.Dtos;
using TodoApi.Models;

namespace TodoApi.Services;

public sealed class ListService : IListService
{
    private readonly TodoDbContext _db;

    private static readonly Expression<Func<TodoList, TodoListResponse>> Projection = l => new TodoListResponse
    {
        Id = l.Id,
        Name = l.Name,
        OwnerUserId = l.OwnerUserId,
        GroupId = l.GroupId,
        AssignedUserId = l.AssignedUserId,
        CreatedAt = l.CreatedAt
    };

    public ListService(TodoDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<TodoListResponse>> GetAccessibleLists(Guid userId)
    {
        // Accessible if: personal owner OR member of owning group.
        var groupIds = _db.GroupMembers.AsNoTracking()
            .Where(m => m.UserId == userId)
            .Select(m => m.GroupId);

        return await _db.TodoLists.AsNoTracking()
            .Where(l => (l.OwnerUserId != null && l.OwnerUserId == userId) ||
                        (l.GroupId != null && groupIds.Contains(l.GroupId.Value)))
            .OrderBy(l => l.Name)
            .Select(Projection)
            .ToListAsync();
    }

    public async Task<TodoListResponse?> GetAccessibleList(Guid userId, Guid listId)
    {
        var groupIds = _db.GroupMembers.AsNoTracking()
            .Where(m => m.UserId == userId)
            .Select(m => m.GroupId);

        return await _db.TodoLists.AsNoTracking()
            .Where(l => l.Id == listId)
            .Where(l => (l.OwnerUserId != null && l.OwnerUserId == userId) ||
                        (l.GroupId != null && groupIds.Contains(l.GroupId.Value)))
            .Select(Projection)
            .SingleOrDefaultAsync();
    }

    public async Task<TodoListResponse> CreatePersonalList(Guid userId, CreateListRequest request)
    {
        var name = request.Name.Trim();

        var list = new TodoList
        {
            Id = Guid.NewGuid(),
            OwnerUserId = userId,
            GroupId = null,
            AssignedUserId = null,
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
            GroupId = list.GroupId,
            AssignedUserId = list.AssignedUserId,
            CreatedAt = list.CreatedAt
        };
    }

    public async Task<bool> DeletePersonalList(Guid userId, Guid listId)
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

    public async Task<IReadOnlyList<TodoListResponse>> GetGroupLists(Guid userId, Guid groupId)
    {
        var isMember = await _db.GroupMembers.AsNoTracking().AnyAsync(m => m.GroupId == groupId && m.UserId == userId);
        if (!isMember)
        {
            return Array.Empty<TodoListResponse>();
        }

        return await _db.TodoLists.AsNoTracking()
            .Where(l => l.GroupId == groupId)
            .OrderBy(l => l.Name)
            .Select(Projection)
            .ToListAsync();
    }

    public async Task<TodoListResponse> CreateGroupList(Guid userId, Guid groupId, CreateGroupListRequest request)
    {
        var isMember = await _db.GroupMembers.AsNoTracking().AnyAsync(m => m.GroupId == groupId && m.UserId == userId);
        if (!isMember)
        {
            throw new UnauthorizedAccessException("Not a member of this group");
        }

        Guid? assignedUserId = request.AssignedUserId;
        if (assignedUserId is not null)
        {
            var assigneeIsMember = await _db.GroupMembers.AsNoTracking()
                .AnyAsync(m => m.GroupId == groupId && m.UserId == assignedUserId.Value);
            if (!assigneeIsMember)
            {
                throw new InvalidOperationException("Assignee must be a member of the group");
            }
        }

        var list = new TodoList
        {
            Id = Guid.NewGuid(),
            OwnerUserId = null,
            GroupId = groupId,
            AssignedUserId = assignedUserId,
            Name = request.Name.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.TodoLists.Add(list);
        await _db.SaveChangesAsync();

        return new TodoListResponse
        {
            Id = list.Id,
            Name = list.Name,
            OwnerUserId = list.OwnerUserId,
            GroupId = list.GroupId,
            AssignedUserId = list.AssignedUserId,
            CreatedAt = list.CreatedAt
        };
    }

    public async Task<bool> AssignGroupList(Guid userId, Guid groupId, Guid listId, Guid? assignedUserId)
    {
        var isMember = await _db.GroupMembers.AsNoTracking().AnyAsync(m => m.GroupId == groupId && m.UserId == userId);
        if (!isMember)
        {
            return false;
        }

        var list = await _db.TodoLists.SingleOrDefaultAsync(l => l.Id == listId && l.GroupId == groupId);
        if (list is null)
        {
            return false;
        }

        if (assignedUserId is not null)
        {
            var assigneeIsMember = await _db.GroupMembers.AsNoTracking()
                .AnyAsync(m => m.GroupId == groupId && m.UserId == assignedUserId.Value);
            if (!assigneeIsMember)
            {
                return false;
            }
        }

        list.AssignedUserId = assignedUserId;
        await _db.SaveChangesAsync();
        return true;
    }

}
