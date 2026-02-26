using Everlore.Application.Common.Interfaces;
using Everlore.Gateway;
using Everlore.Gateway.Configuration;
using Everlore.Gateway.Connections;
using Everlore.Gateway.Handlers;
using Everlore.QueryEngine.Caching;
using Everlore.QueryEngine.Connections;
using Everlore.QueryEngine.Execution;
using Everlore.QueryEngine.Schema;
using Everlore.QueryEngine.Translation;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Registry;
using Polly.Retry;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

// Gateway settings
builder.Services.Configure<GatewaySettings>(builder.Configuration.GetSection("Gateway"));

// Local connection factory (reads connection strings from config, not from encrypted DB)
builder.Services.AddSingleton<IExternalConnectionFactory, GatewayConnectionFactory>();

// Local schema service (introspects directly, no catalog DB)
builder.Services.AddSingleton<ISchemaService, GatewaySchemaService>();

// No-op cache and progress notifier (not needed on the agent side)
builder.Services.AddSingleton<IQueryCacheService, NoOpCacheService>();
builder.Services.AddSingleton<IQueryProgressNotifier, NoOpProgressNotifier>();

// QueryEngine components needed for local execution
builder.Services.AddSingleton<SchemaIntrospectorFactory>();
builder.Services.AddSingleton<SqlTranslatorFactory>();
builder.Services.AddSingleton<Everlore.QueryEngine.Execution.IQueryExecutionService, QueryExecutionService>();

// Resilience pipeline
builder.Services.AddResiliencePipeline("external-db", pipeline =>
{
    pipeline
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(1),
            BackoffType = DelayBackoffType.Exponential,
            ShouldHandle = new PredicateBuilder().Handle<Exception>(ex =>
                ex is TimeoutException or System.Net.Sockets.SocketException)
        })
        .AddTimeout(TimeSpan.FromSeconds(30));
});

builder.Services.AddSingleton(sp =>
{
    var provider = sp.GetRequiredService<ResiliencePipelineProvider<string>>();
    return provider.GetPipeline("external-db");
});

// Handlers
builder.Services.AddSingleton<ExecuteQueryHandler>();
builder.Services.AddSingleton<DiscoverSchemaHandler>();
builder.Services.AddSingleton<ExploreHandler>();

// Tenant DB CRUD support (optional — only when TenantDbConnectionString is configured)
var tenantDbConnectionString = builder.Configuration["Gateway:TenantDbConnectionString"]
    ?? builder.Configuration.GetConnectionString("everloretenantdb");
if (!string.IsNullOrWhiteSpace(tenantDbConnectionString))
{
    builder.Services.AddDbContext<Everlore.Infrastructure.Persistence.EverloreDbContext>(options =>
    {
        options.UseNpgsql(tenantDbConnectionString, o =>
            o.MigrationsAssembly("Everlore.Infrastructure.Postgres"));
    });
    builder.Services.AddScoped<CrudHandler>();
}

// SignalR client + worker
builder.Services.AddSingleton<GatewaySignalRClient>();
builder.Services.AddHostedService<GatewayWorker>();

var host = builder.Build();
host.Run();
