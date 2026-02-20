using Everlore.Connector.Seed;
using Everlore.Infrastructure.Connectors;
using Everlore.Infrastructure.Persistence;
using Everlore.SyncService;
using Everlore.SyncService.Handlers;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

var catalogConnectionString = builder.Configuration.GetConnectionString("everloredb")
    ?? throw new InvalidOperationException("Connection string 'everloredb' not found.");

builder.Services.AddDbContext<CatalogDbContext>(options =>
    options.UseNpgsql(catalogConnectionString, o =>
        o.MigrationsAssembly("Everlore.Infrastructure.Postgres")));

builder.Services.AddSeedConnector();
builder.Services.AddSingleton<ISyncHandler, SeedSyncHandler>();

builder.Services.AddHostedService<SyncWorker>();

var host = builder.Build();
host.Run();
