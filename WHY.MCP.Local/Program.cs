using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WHY.MCP.Extensions;
using WHY.Shared.Api;

var builder = Host.CreateApplicationBuilder(args);

// Configure all logs to go to stderr (stdout is used for the MCP protocol messages).
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

// 
builder.Services.AddWhyMcpApiClients();

// Register WHY MCP server with all API clients and tools (HTTP transport with service discovery)
builder.Services
    .AddMcpServer()
    .WithWhyTools()
    .WithStdioServerTransport()
    ;

await builder.Build().RunAsync();