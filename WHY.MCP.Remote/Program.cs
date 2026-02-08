using WHY.MCP.Local.Services;
using WHY.MCP.Local.Tools;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHttpClient<ApiClient>(static client => client.BaseAddress = new("https+http://why-api"));

// Add the MCP services: the transport to use (http) and the tools to register.
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithTools<WhyTools>() // Add WhyTools
    ;

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapMcp("/mcp");

app.Run();