using Everlore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Everlore.Infrastructure.Tenancy;

public class TenantConnectionResolver
{
    private readonly CatalogDbContext _catalogDb;
    private readonly ITenantProvider _tenantProvider;

    public TenantConnectionResolver(CatalogDbContext catalogDb, ITenantProvider tenantProvider)
    {
        _catalogDb = catalogDb;
        _tenantProvider = tenantProvider;
    }

    public async Task<string?> GetConnectionStringAsync()
    {
        var identifier = _tenantProvider.GetTenantIdentifier();
        if (string.IsNullOrWhiteSpace(identifier))
            return null;

        var tenant = await _catalogDb.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Identifier == identifier && t.IsActive);

        return tenant?.ConnectionString;
    }
}
