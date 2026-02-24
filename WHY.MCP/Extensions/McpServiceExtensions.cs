using Microsoft.Extensions.DependencyInjection;
using WebApiClientCore;
using WHY.MCP.Services;
using WHY.MCP.Tools;
using WHY.Shared.Api;

namespace WHY.MCP.Extensions;

public static class McpServiceExtensions
{
    /// <summary>
    /// Add why tools to a mcp server
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IMcpServerBuilder WithWhyTools(this IMcpServerBuilder builder) =>
        builder
            .WithTools<AuthApiTool>()
            .WithTools<QuestionApiTool>()
            .WithTools<AnswerApiTool>()
            .WithTools<CommentApiTool>();

    /// <summary>
    /// Register WHY API clients and dependencies (without MCP server)
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="apiBaseUrl">Base URL for WHY API</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddWhyMcpApiClients(
        this IServiceCollection services,
        Uri? apiBaseUrl = null
    )
    {
        apiBaseUrl ??= new Uri(
            Environment.GetEnvironmentVariable("WHY_API_HTTPS")
                ?? Environment.GetEnvironmentVariable("WHY_API_HTTP")
                ?? "http+https://why-api"
        );

        // Register WebApiClientCore with AOT JSON source generator context
        services
            .AddWebApiClient()
            .ConfigureHttpApi(options =>
            {
                options.PrependJsonSerializerContext(WhyJsonSerializerContext.Default);
            });

        // Register TokenService
        services.AddSingleton<TokenService>();
        services.AddTransient<TokenDelegatingHandler>();

        // Register all WebApiClientCore API interfaces
        services.AddHttpApi<IWhyMcpAuthApi>(ConfigureHttpOptions);

        services
            .AddHttpApi<IWhyMcpQuestionApi>(ConfigureHttpOptions)
            .AddHttpMessageHandler<TokenDelegatingHandler>();

        services
            .AddHttpApi<IWhyMcpAnswerApi>(ConfigureHttpOptions)
            .AddHttpMessageHandler<TokenDelegatingHandler>();

        services
            .AddHttpApi<IWhyMcpCommentApi>(ConfigureHttpOptions)
            .AddHttpMessageHandler<TokenDelegatingHandler>();

        return services;

        void ConfigureHttpOptions(HttpApiOptions options)
        {
            options.HttpHost = apiBaseUrl;
            options.UseLogging = true;
        }
    }
}
