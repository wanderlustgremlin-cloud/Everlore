using Microsoft.AspNetCore.Http;

namespace Everlore.Infrastructure.Tenancy;

public class TenantProvider : ITenantProvider
{
    private const string TenantHeader = "X-Tenant-Id";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? GetTenantIdentifier()
    {
        return _httpContextAccessor.HttpContext?.Request.Headers[TenantHeader].FirstOrDefault();
    }
}
