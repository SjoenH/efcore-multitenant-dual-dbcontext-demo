using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Dtos;
using TodoApi.Infrastructure;
using TodoApi.Models;

namespace TodoApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public sealed class AuthController : ControllerBase
{
    private readonly TodoDbContext _db;
    private readonly IJwtTokenService _tokens;

    public AuthController(TodoDbContext db, IJwtTokenService tokens)
    {
        _db = db;
        _tokens = tokens;
    }

    // Demo login: provide an email, we'll create the user if missing and issue a JWT.
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == email);
        if (user is null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }

        var expiresAt = DateTimeOffset.UtcNow.AddHours(8);
        var token = _tokens.CreateToken(user.Id, user.Email);

        return Ok(new LoginResponse
        {
            AccessToken = token,
            ExpiresAt = expiresAt,
            UserId = user.Id
        });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> Me()
    {
        var userId = HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userId, out var id))
        {
            return Unauthorized();
        }

        var user = await _db.Users.AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UserResponse
            {
                Id = u.Id,
                Email = u.Email,
                DisplayName = u.DisplayName,
                CreatedAt = u.CreatedAt
            })
            .SingleOrDefaultAsync();

        return user is null ? Unauthorized() : Ok(user);
    }
}
