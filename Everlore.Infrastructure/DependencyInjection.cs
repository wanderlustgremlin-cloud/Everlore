using Everlore.Infrastructure.Tenancy;
using Microsoft.Extensions.DependencyInjection;

namespace Everlore.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureCore(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();

        services.AddScoped<ITenantProvider, TenantProvider>();
        services.AddScoped<TenantConnectionResolver>();

        return services;
    }
}
