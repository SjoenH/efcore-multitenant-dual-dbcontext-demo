using BankingApi.Data;
using BankingApi.Dtos;
using BankingApi.Infrastructure;
using BankingApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BankingApi.Controllers;

[Route("api/auth")]
[ApiController]
public sealed class AuthController : ControllerBase
{
    private readonly AdminDbContext _db;
    private readonly IJwtTokenService _tokens;

    public AuthController(AdminDbContext db, IJwtTokenService tokens)
    {
        _db = db;
        _tokens = tokens;
    }

    // Demo login: provide an email, we'll find the seeded user and issue a JWT.
    // (No passwords in this demo.)
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == email);
        if (user is null)
        {
            return Unauthorized();
        }

        var (token, expiresAt) = _tokens.CreateToken(user);

        return Ok(
            new LoginResponse
            {
                AccessToken = token,
                ExpiresAt = expiresAt,
                UserId = user.Id,
                Role = user.Role.ToString(),
                BankId = user.BankId,
                CustomerId = user.CustomerId,
            }
        );
    }

    [AllowAnonymous]
    [HttpGet("seeded-logins")]
    public async Task<ActionResult<SeededLoginsResponse>> SeededLogins()
    {
        var bankAId = await _db
            .Banks.AsNoTracking()
            .Where(x => x.Code == "NO-001")
            .Select(x => (Guid?)x.Id)
            .SingleOrDefaultAsync();

        var bankBId = await _db
            .Banks.AsNoTracking()
            .Where(x => x.Code == "SE-001")
            .Select(x => (Guid?)x.Id)
            .SingleOrDefaultAsync();

        var customerAId = await _db
            .Customers.AsNoTracking()
            .Where(x => x.Email == SeedData.CustomerAEmail)
            .Select(x => (Guid?)x.Id)
            .SingleOrDefaultAsync();

        var customerBId = await _db
            .Customers.AsNoTracking()
            .Where(x => x.Email == SeedData.CustomerBEmail)
            .Select(x => (Guid?)x.Id)
            .SingleOrDefaultAsync();

        return Ok(
            new SeededLoginsResponse
            {
                AdminEmail = SeedData.AdminEmail,
                StaffBankAEmail = SeedData.StaffBankAEmail,
                StaffBankBEmail = SeedData.StaffBankBEmail,
                CustomerAEmail = SeedData.CustomerAEmail,
                CustomerBEmail = SeedData.CustomerBEmail,
                BankAId = bankAId,
                BankBId = bankBId,
                CustomerAId = customerAId,
                CustomerBId = customerBId,
            }
        );
    }
}
