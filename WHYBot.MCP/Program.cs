using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WHYBot.Database;
using WHYBot.MCP.Tools;

var builder = WebApplication.CreateBuilder(args);

// 配置日志
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// 配置数据库
builder.Services.AddDbContext<WHYBotDbContext>(options =>
    options.UseSqlite("Data Source=whybot.db"));

// 添加 CORS 支持
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 添加 MCP 服务：使用 HTTP SSE 传输和注册工具
builder.Services
    .AddMcpServer()
    .WithSseServerTransport(builder.Configuration.GetValue<string>("MCP:BasePath") ?? "/mcp")
    .WithTools<WHYBotTools>();

var app = builder.Build();

// 确保数据库已创建
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<WHYBotDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

// 启用 CORS
app.UseCors();

// 映射 MCP 端点
app.MapMcpServer();

// 添加健康检查端点
app.MapGet("/", () => Results.Ok(new 
{ 
    service = "WHYBot MCP Server",
    version = "0.1.0",
    protocol = "MCP over HTTP (SSE)",
    endpoint = "/mcp",
    status = "running"
}));

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();
