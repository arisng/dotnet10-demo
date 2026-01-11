namespace Demo5_1.ApiService.Services;

public interface ITenantProvider
{
    string? GetTenantId();
}

public class TenantProvider(IHttpContextAccessor httpContextAccessor) : ITenantProvider
{
    private const string TenantHeader = "X-Tenant-Id";
    private const string DefaultTenantId = "demo-tenant-1";

    public string? GetTenantId()
    {
        var context = httpContextAccessor.HttpContext;
        if (context == null) return null;

        // 1. Try Header (Simulated)
        if (context.Request.Headers.TryGetValue(TenantHeader, out var tenantId))
        {
            return tenantId.ToString();
        }

        // 2. Fallback to default for demo purposes
        return DefaultTenantId;
    }
}
