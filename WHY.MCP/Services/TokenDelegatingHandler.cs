using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;

namespace WHY.MCP.Services;

/// <summary>
/// Automatically injects Bearer token into all outgoing HTTP requests.
/// </summary>
public class TokenDelegatingHandler(
    TokenService tokenService,
    ILogger<TokenDelegatingHandler> logger
) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = tokenService.GetToken();
        logger.LogInformation("[TokenDelegatingHandler] Token: {token}", token);
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        return base.SendAsync(request, cancellationToken);
    }
}