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

        // JWT claims store tenant as GUID; header fallback uses string identifier
        var tenant = Guid.TryParse(identifier, out var tenantId)
            ? await _catalogDb.Tenants
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == tenantId && t.IsActive)
            : await _catalogDb.Tenants
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Identifier == identifier && t.IsActive);

        return tenant?.ConnectionString;
    }
}
