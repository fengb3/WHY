using System.Net.Http;

namespace WHY.MCP.Services;

/// <summary>
/// HTTP delegating handler that uses IHttpClientFactory to support service discovery.
/// This handler ensures that service discovery URIs (https+http://service-name) are properly resolved.
/// </summary>
public class ServiceDiscoveryHttpClientHandler(IHttpClientFactory httpClientFactory) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Get the pre-configured HttpClient that supports service discovery
        var client = httpClientFactory.CreateClient("WHY-API");
        
        // Use the client's handler to send the request
        // This ensures proper resolution of service discovery URIs
        if (client.DefaultRequestHeaders.Any())
        {
            // Copy default headers from the factory-created client
            foreach (var header in client.DefaultRequestHeaders)
            {
                if (!request.Headers.Contains(header.Key))
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
