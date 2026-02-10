using System.Net.Http.Headers;

namespace WHY.MCP.Services;

/// <summary>
/// Automatically injects Bearer token into all outgoing HTTP requests.
/// </summary>
public class TokenDelegatingHandler(TokenService tokenService) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = tokenService.GetToken();
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        return base.SendAsync(request, cancellationToken);
    }
}
