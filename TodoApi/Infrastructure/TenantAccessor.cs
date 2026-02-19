namespace TodoApi.Infrastructure;

public interface ITenantAccessor
{
    Guid? TryGetTenantId();
    Guid GetRequiredTenantId();
}

public sealed class TenantAccessor : ITenantAccessor
{
    public const string TenantIdHeader = "X-Tenant-Id";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? TryGetTenantId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return null;
        }

        if (!httpContext.Request.Headers.TryGetValue(TenantIdHeader, out var raw))
        {
            return null;
        }

        return Guid.TryParse(raw.ToString(), out var tenantId) ? tenantId : null;
    }

    public Guid GetRequiredTenantId()
    {
        var tenantId = TryGetTenantId();
        if (tenantId is null)
        {
            throw new UnauthorizedAccessException($"Missing tenant id. Provide header '{TenantIdHeader}: <guid>'.");
        }

        return tenantId.Value;
    }
}
