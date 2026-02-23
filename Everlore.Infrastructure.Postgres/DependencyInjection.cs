using Everlore.Application.Common.Interfaces;
using Everlore.Infrastructure;
using Everlore.Infrastructure.Persistence;
using Everlore.Infrastructure.Postgres.Tenancy;
using Everlore.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Everlore.Infrastructure.Postgres;

public static class DependencyInjection
{
    public static IServiceCollection AddPostgresInfrastructure(this IServiceCollection services, string catalogConnectionString)
    {
        services.AddInfrastructureCore();
        services.AddAuthServices();

        services.AddDbContext<CatalogDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(catalogConnectionString, o =>
                o.MigrationsAssembly("Everlore.Infrastructure.Postgres"));
            options.AddInterceptors(serviceProvider.GetRequiredService<AuditSaveChangesInterceptor>());
        });

        // Defense-in-depth: if no tenant is resolved, use a dummy connection string
        // that will fail loudly rather than silently falling back to the catalog DB.
        // The real guard is TenantRequiredMiddleware which rejects requests without a tenant.
        const string noTenantConnectionString = "Host=invalid;Database=no_tenant_resolved";

        services.AddDbContext<EverloreDbContext>((serviceProvider, options) =>
        {
            var resolver = serviceProvider.GetRequiredService<TenantConnectionResolver>();
            var connectionString = resolver.GetConnectionStringAsync().GetAwaiter().GetResult();

            options.UseNpgsql(connectionString ?? noTenantConnectionString, o =>
                o.MigrationsAssembly("Everlore.Infrastructure.Postgres"));
            options.AddInterceptors(serviceProvider.GetRequiredService<AuditSaveChangesInterceptor>());
        });

        services.AddScoped<ITenantDatabaseProvisioner, PostgresTenantDatabaseProvisioner>();

        return services;
    }
}
