using Everlore.Domain.Reporting;
using Everlore.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Everlore.Application.Common.Interfaces;

public interface ICatalogDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<TenantUser> TenantUsers { get; }
    DbSet<TenantSetting> TenantSettings { get; }
    DbSet<DataSource> DataSources { get; }
    DbSet<ReportDefinition> ReportDefinitions { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
