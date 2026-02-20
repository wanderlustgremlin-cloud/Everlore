using Everlore.Infrastructure;
using Everlore.Infrastructure.Persistence;
using Everlore.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Everlore.Infrastructure.Postgres;

public static class DependencyInjection
{
    public static IServiceCollection AddPostgresInfrastructure(this IServiceCollection services, string catalogConnectionString)
    {
        services.AddInfrastructureCore();

        services.AddDbContext<CatalogDbContext>(options =>
            options.UseNpgsql(catalogConnectionString));

        services.AddDbContext<EverloreDbContext>((serviceProvider, options) =>
        {
            var resolver = serviceProvider.GetRequiredService<TenantConnectionResolver>();
            var connectionString = resolver.GetConnectionStringAsync().GetAwaiter().GetResult();

            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                options.UseNpgsql(connectionString);
            }
        });

        return services;
    }
}
