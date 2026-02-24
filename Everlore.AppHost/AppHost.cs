var builder = DistributedApplication.CreateBuilder(args);

var jwtSigningKey = builder.AddParameter("jwt-signing-key", secret: true);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();
var everloredb = postgres.AddDatabase("everloredb");
var everloretenantdb = postgres.AddDatabase("everloretenantdb");

var cache = builder.AddGarnet("cache")
    .WithDataVolume();

// SigNoz observability stack
var clickhouse = builder.AddContainer("signoz-clickhouse", "clickhouse/clickhouse-server", "24.1")
    .WithVolume("signoz-clickhouse-data", "/var/lib/clickhouse")
    .WithEndpoint(port: 9000, targetPort: 9000, name: "native", scheme: "tcp")
    .WithEndpoint(port: 8123, targetPort: 8123, name: "http");

var signozCollector = builder.AddContainer("signoz-collector", "signoz/signoz-otel-collector", "0.111.0")
    .WithBindMount("./signoz/otel-collector-config.yaml", "/etc/otel/config.yaml")
    .WithEndpoint(port: 4317, targetPort: 4317, name: "grpc")
    .WithEndpoint(port: 4318, targetPort: 4318, name: "http")
    .WaitFor(clickhouse);

var signozQueryService = builder.AddContainer("signoz-query-service", "signoz/query-service", "0.111.0")
    .WithEndpoint(port: 8080, targetPort: 8080, name: "http")
    .WithEnvironment("ClickHouseUrl", "tcp://signoz-clickhouse:9000")
    .WithEnvironment("STORAGE", "clickhouse")
    .WaitFor(clickhouse)
    .WaitFor(signozCollector);

var signozFrontend = builder.AddContainer("signoz-frontend", "signoz/frontend", "0.111.0")
    .WithEndpoint(port: 3301, targetPort: 3301, name: "http")
    .WithEnvironment("FRONTEND_API_ENDPOINT", "http://signoz-query-service:8080")
    .WaitFor(signozQueryService);

var migrations = builder.AddProject<Projects.Everlore_MigrationService>("migrations")
    .WithReference(everloredb)
    .WithReference(everloretenantdb)
    .WithEnvironment("SIGNOZ_OTLP_ENDPOINT", signozCollector.GetEndpoint("grpc"))
    .WaitFor(everloredb)
    .WaitFor(everloretenantdb)
    .WaitFor(signozCollector);

var sync = builder.AddProject<Projects.Everlore_SyncService>("sync")
    .WithReference(everloredb)
    .WithReference(everloretenantdb)
    .WithEnvironment("SIGNOZ_OTLP_ENDPOINT", signozCollector.GetEndpoint("grpc"))
    .WaitForCompletion(migrations)
    .WaitFor(signozCollector);

builder.AddProject<Projects.Everlore_Api>("everlore-api")
    .WithReference(everloredb)
    .WithReference(cache)
    .WithEnvironment("Jwt__SigningKey", jwtSigningKey)
    .WithEnvironment("Jwt__Issuer", "Everlore")
    .WithEnvironment("Jwt__Audience", "Everlore")
    .WithEnvironment("Jwt__ExpirationMinutes", "60")
    .WithEnvironment("Registration__Mode", "Open")
    .WithEnvironment("SIGNOZ_OTLP_ENDPOINT", signozCollector.GetEndpoint("grpc"))
    .WaitForCompletion(sync)
    .WaitFor(cache)
    .WaitFor(signozCollector);

builder.Build().Run();
