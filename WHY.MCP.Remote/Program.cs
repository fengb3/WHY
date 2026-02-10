using WHY.MCP;
using WHY.MCP.Extensions;
using Microsoft.Extensions.DependencyInjection;
using WebApiClientCore;
using WHY.Shared.Api;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// API base URL: Use service discovery URL or configurable fallback
var apiBaseUrl = new Uri(
    builder.Configuration["ApiBaseUrl"]
    ?? Environment.GetEnvironmentVariable("WHY_API_BASE_URL")
    ?? "http+https://why-api"
);

// Register WebApiClientCore with AOT JSON source generator context
builder.Services
    .AddWebApiClient()
    .ConfigureHttpApi(options =>
    {
        options.PrependJsonSerializerContext(WhyJsonSerializerContext.Default);
    });

// Register WHY MCP server with all API clients and tools (HTTP transport with service discovery)
builder.Services.AddWhyMcpServerHttp(apiBaseUrl);

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapMcp("/mcp");

app.Run();