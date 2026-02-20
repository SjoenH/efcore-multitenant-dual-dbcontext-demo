using System.Security.Claims;

namespace BankingApi.Infrastructure;

public static class ClaimsExtensions
{
    public static Guid GetRequiredUserId(this ClaimsPrincipal user)
    {
        var raw = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(raw, out var id) ? id : throw new UnauthorizedAccessException("Missing user id claim");
    }

    public static string GetRequiredRole(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Role)?.Value ?? throw new UnauthorizedAccessException("Missing role claim");
    }

    public static Guid? TryGetBankIdClaim(this ClaimsPrincipal user)
    {
        var raw = user.FindFirst(AppClaimTypes.BankId)?.Value;
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    public static Guid? TryGetCustomerIdClaim(this ClaimsPrincipal user)
    {
        var raw = user.FindFirst(AppClaimTypes.CustomerId)?.Value;
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
