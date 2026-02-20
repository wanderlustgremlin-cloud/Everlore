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

var sync = builder.AddProject<Projects.Everlore_SyncService>("sync")
    .WithReference(everloredb)
    .WithReference(everloretenantdb)
    .WaitForCompletion(migrations);

builder.AddProject<Projects.Everlore_Api>("everlore-api")
    .WithReference(everloredb)
    .WaitForCompletion(sync);

builder.Build().Run();
