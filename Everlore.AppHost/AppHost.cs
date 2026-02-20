var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .AddDatabase("everloredb");

builder.AddProject<Projects.Everlore_Api>("everlore-api")
    .WithReference(postgres)
    .WaitFor(postgres);

builder.Build().Run();
