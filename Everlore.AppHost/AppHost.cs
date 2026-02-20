var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();
var everloredb = postgres.AddDatabase("everloredb");
var everloretenantdb = postgres.AddDatabase("everloretenantdb");

var migrations = builder.AddProject<Projects.Everlore_MigrationService>("migrations")
    .WithReference(everloredb)
    .WithReference(everloretenantdb)
    .WaitFor(everloredb)
    .WaitFor(everloretenantdb);

builder.AddProject<Projects.Everlore_Api>("everlore-api")
    .WithReference(everloredb)
    .WaitForCompletion(migrations);

builder.Build().Run();
