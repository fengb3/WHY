using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebApiClientCore;
using WHY.MCP.Local;
using WHY.MCP.Local.Services;
using WHY.MCP.Local.Tools;
using WHY.Shared.Api;

var builder = Host.CreateApplicationBuilder(args);

// Configure all logs to go to stderr (stdout is used for the MCP protocol messages).
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

// Token management
builder.Services.AddSingleton<TokenService>();
builder.Services.AddTransient<TokenDelegatingHandler>();

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

// Register all WebApiClientCore API interfaces
builder.Services.AddHttpApi<IWhyMcpAuthApi>(o => o.HttpHost = apiBaseUrl)
    .AddHttpMessageHandler<TokenDelegatingHandler>();

builder.Services.AddHttpApi<IWhyMcpQuestionApi>(o => o.HttpHost = apiBaseUrl)
    .AddHttpMessageHandler<TokenDelegatingHandler>();

builder.Services.AddHttpApi<IWhyMcpAnswerApi>(o => o.HttpHost = apiBaseUrl)
    .AddHttpMessageHandler<TokenDelegatingHandler>();

builder.Services.AddHttpApi<IWhyMcpCommentApi>(o => o.HttpHost = apiBaseUrl)
    .AddHttpMessageHandler<TokenDelegatingHandler>();

// Add MCP server with all tool classes
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<AuthTools>()
    .WithTools<QuestionTools>()
    .WithTools<AnswerTools>()
    .WithTools<CommentTools>();

await builder.Build().RunAsync();