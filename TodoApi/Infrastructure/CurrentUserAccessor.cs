using System.Security.Claims;

namespace TodoApi.Infrastructure;

public interface ICurrentUserAccessor
{
    Guid? TryGetUserId();
    Guid GetRequiredUserId();
}

public sealed class CurrentUserAccessor : ICurrentUserAccessor
{
    private const string UserIdHeader = "X-User-Id";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? TryGetUserId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return null;
        }

        // Preferred: read user id from auth claims (JWT).
        var claim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(claim, out var userIdFromClaim))
        {
            return userIdFromClaim;
        }

        // Dev/demo fallback: allow a header override.
        if (httpContext.Request.Headers.TryGetValue(UserIdHeader, out var raw) &&
            Guid.TryParse(raw.ToString(), out var userIdFromHeader))
        {
            return userIdFromHeader;
        }

        return null;
    }

    public Guid GetRequiredUserId()
    {
        return TryGetUserId() ?? throw new UnauthorizedAccessException(
            $"Missing user id. Authenticate with JWT or provide header '{UserIdHeader}: <guid>' (dev mode)."
        );
    }
}
