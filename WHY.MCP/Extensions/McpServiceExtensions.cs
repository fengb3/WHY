using Microsoft.Extensions.DependencyInjection;
using WHY.MCP.Services;
using WHY.MCP.Tools;
using WHY.Shared.Api;

namespace WHY.MCP.Extensions;

public static class McpServiceExtensions
{
    /// <summary>
    /// Add WHY MCP server with all API clients and tools (Stdio transport)
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="apiBaseUrl">Base URL for WHY API</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddWhyMcpServer(this IServiceCollection services, Uri apiBaseUrl)
    {
        services.AddWhyMcpApiClients(apiBaseUrl);

        // Add MCP server with tool classes - one tool class per API interface
        services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithTools<AuthApiTool>()
            .WithTools<QuestionApiTool>()
            .WithTools<AnswerApiTool>()
            .WithTools<CommentApiTool>();

        return services;
    }

    /// <summary>
    /// Add WHY MCP server with HTTP transport, API clients, and service discovery support
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="apiBaseUrl">Base URL for WHY API (supports https+http:// service discovery URIs)</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddWhyMcpServerHttp(this IServiceCollection services, Uri apiBaseUrl)
    {

        services.AddWhyMcpApiClients(apiBaseUrl);


        // Add MCP server with HTTP transport and tool classes
        services
            .AddMcpServer()
            .WithHttpTransport()
            .WithTools<AuthApiTool>()
            .WithTools<QuestionApiTool>()
            .WithTools<AnswerApiTool>()
            .WithTools<CommentApiTool>();

        return services;
    }

    /// <summary>
    /// Register WHY API clients and dependencies (without MCP server)
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="apiBaseUrl">Base URL for WHY API</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddWhyMcpApiClients(this IServiceCollection services, Uri apiBaseUrl)
    {
        // Register TokenService
        services.AddSingleton<TokenService>();
        services.AddSingleton<TokenDelegatingHandler>();

        // Register all WebApiClientCore API interfaces
        services.AddHttpApi<IWhyMcpAuthApi>(o => o.HttpHost = apiBaseUrl)
            ;

        services.AddHttpApi<IWhyMcpQuestionApi>(o => o.HttpHost = apiBaseUrl)
            .AddHttpMessageHandler<TokenDelegatingHandler>();

        services.AddHttpApi<IWhyMcpAnswerApi>(o => o.HttpHost = apiBaseUrl)
            .AddHttpMessageHandler<TokenDelegatingHandler>();

        services.AddHttpApi<IWhyMcpCommentApi>(o => o.HttpHost = apiBaseUrl)
            .AddHttpMessageHandler<TokenDelegatingHandler>();

        return services;
    }
}
