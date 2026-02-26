using Everlore.Application.Common.Interfaces;
using Everlore.Application.Reporting.DataSources;
using Everlore.QueryEngine.Caching;
using Everlore.QueryEngine.Connections;
using Everlore.QueryEngine.Execution;
using Everlore.QueryEngine.GraphQL;
using Everlore.QueryEngine.Schema;
using Everlore.QueryEngine.Translation;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Registry;
using Polly.Retry;

namespace Everlore.QueryEngine;

public static class DependencyInjection
{
    public static IServiceCollection AddQueryEngine(this IServiceCollection services)
    {
        // Connection factory
        services.AddScoped<ExternalConnectionFactory>();
        services.AddScoped<IExternalConnectionFactory>(sp =>
        {
            var inner = sp.GetRequiredService<ExternalConnectionFactory>();
            var pipeline = sp.GetRequiredService<ResiliencePipeline>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ResilientConnectionFactory>>();
            return new ResilientConnectionFactory(inner, pipeline, logger);
        });

        // Connection tester
        services.AddScoped<IConnectionTester, ConnectionTester>();

        // Cache
        services.AddSingleton<IQueryCacheService, DistributedQueryCacheService>();

        // Schema discovery
        services.AddSingleton<SchemaIntrospectorFactory>();
        services.AddScoped<SchemaService>();

        // Query translation + execution
        services.AddSingleton<SqlTranslatorFactory>();
        services.AddScoped<Execution.IQueryExecutionService, QueryExecutionService>();
        services.AddScoped<QueryExecutionServiceAdapter>();

        // GraphQL
        services.AddScoped<DynamicQueryResolver>();
        services.AddScoped<LocalExploreService>();

        // Resilience pipeline
        services.AddResiliencePipeline("external-db", builder =>
        {
            builder
                .AddRetry(new RetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromSeconds(1),
                    BackoffType = DelayBackoffType.Exponential,
                    ShouldHandle = new PredicateBuilder().Handle<Exception>(ex =>
                        IsTransient(ex))
                })
                .AddCircuitBreaker(new Polly.CircuitBreaker.CircuitBreakerStrategyOptions
                {
                    FailureRatio = 0.5,
                    SamplingDuration = TimeSpan.FromSeconds(60),
                    MinimumThroughput = 5,
                    BreakDuration = TimeSpan.FromSeconds(30),
                    ShouldHandle = new PredicateBuilder().Handle<Exception>(ex =>
                        IsTransient(ex))
                })
                .AddTimeout(TimeSpan.FromSeconds(30));
        });

        // Register default ResiliencePipeline from the named pipeline
        services.AddScoped(sp =>
        {
            var provider = sp.GetRequiredService<ResiliencePipelineProvider<string>>();
            return provider.GetPipeline("external-db");
        });

        return services;
    }

    private static bool IsTransient(Exception ex) =>
        ex is TimeoutException
        || ex is System.Net.Sockets.SocketException
        || (ex is Npgsql.NpgsqlException npgsqlEx && npgsqlEx.IsTransient)
        || ex is Microsoft.Data.SqlClient.SqlException { IsTransient: true }
        || ex is MySqlConnector.MySqlException { IsTransient: true };
}
