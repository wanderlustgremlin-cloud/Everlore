using Everlore.Application.Common.Interfaces;
using Everlore.Domain.Common;
using Everlore.Domain.Reporting;
using Everlore.Domain.Tenancy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Everlore.Infrastructure.Persistence;

public class CatalogDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, ICatalogDbContext
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options) { }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<ConnectorConfiguration> ConnectorConfigurations => Set<ConnectorConfiguration>();
    public DbSet<TenantUser> TenantUsers => Set<TenantUser>();
    public DbSet<TenantSetting> TenantSettings => Set<TenantSetting>();
    public DbSet<DataSource> DataSources => Set<DataSource>();
    public DbSet<ReportDefinition> ReportDefinitions => Set<ReportDefinition>();
    public DbSet<GatewayApiKey> GatewayApiKeys => Set<GatewayApiKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new Configurations.TenantConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.ConnectorConfigurationConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.TenantUserConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.TenantSettingConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.DataSourceConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.ReportDefinitionConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.GatewayApiKeyConfiguration());
    }

    public override int SaveChanges()
    {
        SetTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void SetTimestamps()
    {
        var entries = ChangeTracker.Entries<BaseEntity>();
        var now = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}
