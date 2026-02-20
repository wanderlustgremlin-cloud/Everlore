using Everlore.Infrastructure.Persistence;
using Everlore.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Everlore.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string catalogConnectionString)
    {
        services.AddHttpContextAccessor();

        services.AddDbContext<CatalogDbContext>(options =>
            options.UseNpgsql(catalogConnectionString));

        services.AddScoped<ITenantProvider, TenantProvider>();
        services.AddScoped<TenantConnectionResolver>();

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
