using WHY.MCP.Extensions;
using WHY.Shared.Api;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// 
builder.Services.AddWhyMcpApiClients();

// Register WHY MCP server with all API clients and tools (HTTP transport with service discovery)
builder.Services
    .AddMcpServer()
    .WithWhyTools()
    .WithHttpTransport()
    ;

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapMcp("/mcp");

app.Run();