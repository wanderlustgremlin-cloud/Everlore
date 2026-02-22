using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace Everlore.Infrastructure.Tenancy;

public class TenantProvider : ITenantProvider
{
    private const string TenantHeader = "X-Tenant-Id";
    private const string TenantClaim = "tenant";
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHostEnvironment _environment;

    public TenantProvider(IHttpContextAccessor httpContextAccessor, IHostEnvironment environment)
    {
        _httpContextAccessor = httpContextAccessor;
        _environment = environment;
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

        // Fallback: read X-Tenant-Id header — only allowed in Development
        // to prevent header spoofing in production environments
        if (_environment.IsDevelopment())
            return httpContext.Request.Headers[TenantHeader].FirstOrDefault();

        return null;
    }
}
