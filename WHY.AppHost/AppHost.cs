var builder = DistributedApplication.CreateBuilder(args);

// Enable dashboard with custom configuration
builder
    .AddDockerComposeEnvironment("compose")
    .WithDashboard(dashboard =>
    {
        dashboard.WithHostPort(8080).WithForwardedHeaders(enabled: true);
    });

var postgres = builder.AddPostgres("postgres").WithDataVolume("why_postgres_data");
var postgresDB = postgres.AddDatabase("postgresdb");

var api = builder
    .AddProject<Projects.WHY_Api>("why-api")
    .WaitFor(postgresDB)
    .WithReference(postgresDB);

// var mcpRemote = builder
//     .AddProject<Projects.WHY_MCP_Remote>("why-mcp-remote")
//     .WaitFor(api)
//     .WithReference(api);

var web = builder.AddProject<Projects.WHY_Web>("why-web").WaitFor(api).WithReference(api);

builder.Build().Run();
