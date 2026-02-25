var builder = DistributedApplication.CreateBuilder(args);

// Enable dashboard with custom configuration
builder
    .AddDockerComposeEnvironment("compose")
    .WithDashboard(dashboard => {
        dashboard.WithHostPort(8080).WithForwardedHeaders(enabled: true);
    });

// Add a container registry
 #pragma warning disable ASPIRECOMPUTE003
var registry = builder.AddContainerRegistry(
    "ghcr",     // Registry name
    "ghcr.io",  // Registry endpoint
    "fengb3/WHY"// Repository path
);
 #pragma warning restore ASPIRECOMPUTE003


var postgres = builder
    .AddPostgres("postgres")
    .WithDataVolume("why_postgres_data")
    .PublishAsDockerComposeService((r, s) => {});

var postgresDB = postgres.AddDatabase("postgresdb");

 #pragma warning disable ASPIRECOMPUTE003
var api = builder
        .AddProject<Projects.WHY_Api>("why-api")
        .WaitFor(postgresDB)
        .WithReference(postgresDB)
        .PublishAsDockerComposeService(static (r, s) => {
                // Expose API on port 8082 (avoid conflict with Dashboard 8080 and Web 8081)
                s.Ports.Add("8082:${WHY_API_PORT}");
            }
        )
        .WithContainerRegistry(registry);
 #pragma warning restore ASPIRECOMPUTE003
;

// var mcpRemote = builder
//     .AddProject<Projects.WHY_MCP_Remote>("why-mcp-remote")
//     .WaitFor(api)
//     .WithReference(api);

var web = builder.AddProject<Projects.WHY_Web>("why-web").WaitFor(api).WithReference(api);

builder.Build().Run();