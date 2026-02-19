using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace TodoApi.Infrastructure;

public interface IJwtTokenService
{
    string CreateToken(Guid userId, string email);
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

    public string CreateToken(Guid userId, string email)
    {
        if (string.IsNullOrWhiteSpace(_options.SigningKey))
        {
            throw new InvalidOperationException("JWT signing key is not configured");
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(ClaimTypes.Email, email)
        };

        var isAdmin = _admin.Emails.Any(e => string.Equals(e.Trim(), email, StringComparison.OrdinalIgnoreCase));
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
