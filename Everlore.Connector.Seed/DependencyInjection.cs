using Everlore.Infrastructure.Connectors;
using Microsoft.Extensions.DependencyInjection;

namespace Everlore.Connector.Seed;

public static class DependencyInjection
{
    public static IServiceCollection AddSeedConnector(this IServiceCollection services)
    {
        services.AddSingleton<SeedDataSource>();
        services.AddSingleton<IConnectorSource>(sp => sp.GetRequiredService<SeedDataSource>());
        return services;
    }
}
