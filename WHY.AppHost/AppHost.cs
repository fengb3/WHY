var builder = DistributedApplication.CreateBuilder(args);

var postgres   = builder.AddPostgres("postgres").WithDataVolume("why_postgres_data");
var postgresDB = postgres.AddDatabase("postgresdb");

var api = builder.AddProject<Projects.WHY_Api>("why-api")
    .WaitFor(postgresDB)
    .WithReference(postgresDB);
;

builder.Build().Run();