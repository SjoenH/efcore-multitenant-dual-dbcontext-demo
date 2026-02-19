using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Dtos;
using TodoApi.Models;

namespace TodoApi.Services;

public sealed class UserService : IUserService
{
    private readonly TodoDbContext _db;

    public UserService(TodoDbContext db)
    {
        _db = db;
    }

    public async Task<UserResponse> CreateUser(CreateUserRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var displayName = request.DisplayName?.Trim();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.Users.Add(user);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // likely unique email constraint
            var existing = await _db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Email == email);
            if (existing is not null)
            {
                return new UserResponse
                {
                    Id = existing.Id,
                    Email = existing.Email,
                    DisplayName = existing.DisplayName,
                    CreatedAt = existing.CreatedAt
                };
            }

            throw;
        }

        return new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<UserResponse?> GetUser(Guid id)
    {
        return await _db.Users.AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UserResponse
            {
                Id = u.Id,
                Email = u.Email,
                DisplayName = u.DisplayName,
                CreatedAt = u.CreatedAt
            })
            .SingleOrDefaultAsync();
    }

    public async Task<IReadOnlyList<UserResponse>> SearchUsers(string? q, int take = 50)
    {
        take = Math.Clamp(take, 1, 200);
        var query = _db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(u => u.Email.Contains(term) || (u.DisplayName != null && u.DisplayName.Contains(term)));
        }

        return await query
            .OrderBy(u => u.Email)
            .Take(take)
            .Select(u => new UserResponse
            {
                Id = u.Id,
                Email = u.Email,
                DisplayName = u.DisplayName,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();
    }
}
