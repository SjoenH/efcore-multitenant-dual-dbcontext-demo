using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BankingApi.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BankingApi.Infrastructure;

public interface IJwtTokenService
{
    string CreateToken(User user);
}

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;
    private readonly AdminOptions _admin;

    public JwtTokenService(IOptions<JwtOptions> options, IOptions<AdminOptions> admin)
    {
        _options = options.Value;
        _admin = admin.Value;
    }

    public string CreateToken(User user)
    {
        if (string.IsNullOrWhiteSpace(_options.SigningKey))
        {
            throw new InvalidOperationException("JWT signing key is not configured");
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        if (user.BankId is not null)
        {
            claims.Add(new Claim("BankId", user.BankId.Value.ToString()));
        }

        if (user.CustomerId is not null)
        {
            claims.Add(new Claim("CustomerId", user.CustomerId.Value.ToString()));
        }

        var isAdmin = _admin.Emails.Any(e => string.Equals(e.Trim(), user.Email, StringComparison.OrdinalIgnoreCase));
        if (isAdmin)
        {
            claims.Add(new Claim("IsAdmin", "true"));
        }

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
