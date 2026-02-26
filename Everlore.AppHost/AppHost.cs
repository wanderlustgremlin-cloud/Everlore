var builder = DistributedApplication.CreateBuilder(args);

var jwtSigningKey = builder.AddParameter("jwt-signing-key", secret: true);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();
var everloredb = postgres.AddDatabase("everloredb");
var everloretenantdb = postgres.AddDatabase("everloretenantdb");

var cache = builder.AddGarnet("cache")
    .WithDataVolume();

// SigNoz observability stack
var zookeeper = builder.AddContainer("signoz-zookeeper", "signoz/zookeeper", "3.7.1")
    .WithVolume("signoz-zookeeper-data", "/bitnami/zookeeper")
    .WithEnvironment("ALLOW_ANONYMOUS_LOGIN", "yes")
    .WithEnvironment("ZOO_AUTOPURGE_INTERVAL", "1")
    .WithEndpoint(port: 2181, targetPort: 2181, name: "client", scheme: "tcp");

var clickhouse = builder.AddContainer("signoz-clickhouse", "clickhouse/clickhouse-server", "25.5.6")
    .WithVolume("signoz-clickhouse-data", "/var/lib/clickhouse")
    .WithBindMount("./signoz/clickhouse-cluster.xml", "/etc/clickhouse-server/config.d/cluster.xml")
    .WithEnvironment("CLICKHOUSE_SKIP_USER_SETUP", "1")
    .WithEndpoint(port: 9000, targetPort: 9000, name: "native", scheme: "tcp")
    .WithEndpoint(port: 8123, targetPort: 8123, name: "http")
    .WaitFor(zookeeper);

var signozCollector = builder.AddContainer("signoz-collector", "signoz/signoz-otel-collector", "v0.142.1")
    .WithEntrypoint("/bin/sh")
    .WithArgs("-c",
        "/signoz-otel-collector migrate bootstrap && " +
        "/signoz-otel-collector migrate sync up && " +
        "/signoz-otel-collector migrate async up && " +
        "/signoz-otel-collector --config=/etc/otel-collector-config.yaml")
    .WithBindMount("./signoz/otel-collector-config.yaml", "/etc/otel-collector-config.yaml")
    .WithEnvironment("SIGNOZ_OTEL_COLLECTOR_CLICKHOUSE_DSN", "tcp://signoz-clickhouse:9000")
    .WithEnvironment("SIGNOZ_OTEL_COLLECTOR_CLICKHOUSE_CLUSTER", "cluster")
    .WithEnvironment("SIGNOZ_OTEL_COLLECTOR_CLICKHOUSE_REPLICATION", "true")
    .WithEnvironment("LOW_CARDINAL_EXCEPTION_GROUPING", "false")
    .WithEndpoint(port: 4317, targetPort: 4317, name: "grpc")
    .WithEndpoint(port: 4318, targetPort: 4318, name: "http")
    .WaitFor(clickhouse);

var signoz = builder.AddContainer("signoz", "signoz/signoz", "v0.112.1")
    .WithVolume("signoz-sqlite", "/var/lib/signoz")
    .WithHttpEndpoint(port: 3301, targetPort: 8080, name: "http")
    .WithEnvironment("SIGNOZ_TELEMETRYSTORE_CLICKHOUSE_DSN", "tcp://signoz-clickhouse:9000")
    .WithEnvironment("SIGNOZ_SQLSTORE_SQLITE_PATH", "/var/lib/signoz/signoz.db")
    .WithEnvironment("SIGNOZ_TOKENIZER_JWT_SECRET", "dev-secret")
    .WaitFor(clickhouse)
    .WaitFor(signozCollector);

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

var api = builder.AddProject<Projects.Everlore_Api>("everlore-api")
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

builder.AddProject<Projects.Everlore_Gateway>("gateway")
    .WithEnvironment("Gateway__ServerUrl", api.GetEndpoint("https"))
    .WithEnvironment("Gateway__ApiKey", "dev-gateway-key")
    .WithReference(everloretenantdb)
    .WithEnvironment("SIGNOZ_OTLP_ENDPOINT", signozCollector.GetEndpoint("grpc"))
    .WaitFor(api)
    .WaitFor(signozCollector);

builder.Build().Run();
