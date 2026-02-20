using BankingApi.Dtos;
using BankingApi.Infrastructure;
using BankingApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankingApi.Controllers;

[Route("api/auth")]
[ApiController]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly IJwtTokenService _tokens;

    public AuthController(IAuthService auth, IJwtTokenService tokens)
    {
        _auth = auth;
        _tokens = tokens;
    }

    // Demo login: provide an email, we'll find the seeded user and issue a JWT.
    // (No passwords in this demo.)
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _auth.FindUserByEmailAsync(email);
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
        return Ok(await _auth.GetSeededLoginsAsync());
    }
}
