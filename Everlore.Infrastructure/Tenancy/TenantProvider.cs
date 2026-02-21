using Microsoft.AspNetCore.Http;

namespace Everlore.Infrastructure.Tenancy;

public class TenantProvider : ITenantProvider
{
    private const string TenantHeader = "X-Tenant-Id";
    private const string TenantClaim = "tenant";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? GetTenantIdentifier()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
            return null;

        // Primary: read tenant claim from JWT
        var tenantClaim = httpContext.User.FindFirst(TenantClaim)?.Value;
        if (!string.IsNullOrEmpty(tenantClaim))
            return tenantClaim;

        // Fallback: read X-Tenant-Id header (for SyncService, development, Swagger testing)
        return httpContext.Request.Headers[TenantHeader].FirstOrDefault();
    }
}
