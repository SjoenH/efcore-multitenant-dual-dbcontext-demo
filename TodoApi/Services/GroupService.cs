using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Dtos;
using TodoApi.Models;

namespace TodoApi.Services;

public sealed class GroupService : IGroupService
{
    private readonly TodoDbContext _db;

    public GroupService(TodoDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<GroupResponse>> GetMyGroups(Guid userId)
    {
        return await _db.GroupMembers.AsNoTracking()
            .Where(m => m.UserId == userId)
            .Select(m => new GroupResponse
            {
                Id = m.Group.Id,
                Name = m.Group.Name,
                CreatedAt = m.Group.CreatedAt
            })
            .OrderBy(g => g.Name)
            .ToListAsync();
    }

    public async Task<CreateGroupResponse> CreateGroup(Guid userId, CreateGroupRequest request)
    {
        var name = request.Name.Trim();

        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = name,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var member = new GroupMember
        {
            GroupId = group.Id,
            UserId = userId,
            JoinedAt = DateTimeOffset.UtcNow
        };

        _db.Groups.Add(group);
        _db.GroupMembers.Add(member);
        await _db.SaveChangesAsync();

        return new CreateGroupResponse
        {
            Group = new GroupResponse
            {
                Id = group.Id,
                Name = group.Name,
                CreatedAt = group.CreatedAt
            },
            Me = new GroupMemberResponse
            {
                GroupId = member.GroupId,
                UserId = member.UserId,
                JoinedAt = member.JoinedAt
            }
        };
    }

    public async Task<bool> AddMember(Guid actingUserId, Guid groupId, Guid userIdToAdd)
    {
        var isMember = await _db.GroupMembers.AsNoTracking()
            .AnyAsync(m => m.GroupId == groupId && m.UserId == actingUserId);

        if (!isMember)
        {
            return false;
        }

        var userExists = await _db.Users.AsNoTracking().AnyAsync(u => u.Id == userIdToAdd);
        if (!userExists)
        {
            return false;
        }

        var already = await _db.GroupMembers.AsNoTracking()
            .AnyAsync(m => m.GroupId == groupId && m.UserId == userIdToAdd);
        if (already)
        {
            return true;
        }

        _db.GroupMembers.Add(new GroupMember
        {
            GroupId = groupId,
            UserId = userIdToAdd,
            JoinedAt = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<IReadOnlyList<GroupMemberResponse>> GetMembers(Guid actingUserId, Guid groupId)
    {
        var isMember = await _db.GroupMembers.AsNoTracking()
            .AnyAsync(m => m.GroupId == groupId && m.UserId == actingUserId);

        if (!isMember)
        {
            return Array.Empty<GroupMemberResponse>();
        }

        return await _db.GroupMembers.AsNoTracking()
            .Where(m => m.GroupId == groupId)
            .OrderBy(m => m.JoinedAt)
            .Select(m => new GroupMemberResponse
            {
                GroupId = m.GroupId,
                UserId = m.UserId,
                JoinedAt = m.JoinedAt
            })
            .ToListAsync();
    }
}
