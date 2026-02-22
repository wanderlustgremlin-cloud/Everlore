using Everlore.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Everlore.Application.Common.Interfaces;

public interface ICatalogDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<TenantUser> TenantUsers { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
