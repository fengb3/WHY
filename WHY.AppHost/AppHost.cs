var builder = DistributedApplication.CreateBuilder(args);

var postgres   = builder.AddPostgres("postgres");
var postgresdb = postgres.AddDatabase("postgresdb");

var api = builder.AddProject<Projects.WHY_Api>("why-api")
    .WaitFor(postgresdb)
    .WithReference(postgresdb);
;

builder.Build().Run();