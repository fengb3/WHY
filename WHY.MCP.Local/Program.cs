using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WHY.MCP.Extensions;
using WHY.Shared.Api;

var builder = Host.CreateApplicationBuilder(args);

// Configure all logs to go to stderr (stdout is used for the MCP protocol messages).
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

// API base URL: configurable via environment variable or appsettings
var apiBaseUrl = new Uri(
    builder.Configuration["ApiBaseUrl"]
    ?? Environment.GetEnvironmentVariable("WHY_API_BASE_URL")
    ?? "http://localhost:5001");

// Register WebApiClientCore with AOT JSON source generator context
builder.Services
    .AddWebApiClient()
    .ConfigureHttpApi(options =>
    {
        options.PrependJsonSerializerContext(WhyJsonSerializerContext.Default);
    });

// Register WHY MCP server with all API clients and tools
builder.Services.AddWhyMcpServer(apiBaseUrl);

await builder.Build().RunAsync();