using System.Text.Json;
using Microsoft.Extensions.Logging;
using WHY.Shared.Api;
using WHY.Shared.Dtos.Auth;

namespace WHY.MCP.Services;

/// <summary>
/// Manages JWT token persistence (load/save from disk)
/// </summary>
public class TokenService
{
    private readonly ILogger<TokenService> _logger;
    private TokenInfo? _tokenInfo;

    private static string TokenFilePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WHY.MCP.Data",
            "token.json"
        );

    public TokenService(ILogger<TokenService> logger)
    {
        _logger = logger;
        LoadToken();
    }

    public string? GetToken() => _tokenInfo?.Token;
    public string? GetUsername() => _tokenInfo?.Username;
    public bool IsLoggedIn => !string.IsNullOrEmpty(_tokenInfo?.Token);

    public void SaveToken(string? token, string username)
    {
        try
        {
            var dir = Path.GetDirectoryName(TokenFilePath);
            if (dir != null && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var tokenInfo = new TokenInfo { Token = token, Username = username };
            File.WriteAllText(
                TokenFilePath,
                JsonSerializer.Serialize(tokenInfo, WhyJsonSerializerContext.Default.TokenInfo)
            );
            _tokenInfo = tokenInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save token");
        }
    }

    private void LoadToken()
    {
        try
        {
            if (File.Exists(TokenFilePath))
            {
                var json = File.ReadAllText(TokenFilePath);
                _tokenInfo = JsonSerializer.Deserialize(json, WhyJsonSerializerContext.Default.TokenInfo);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load token");
        }
    }

    public void ClearToken()
    {
        _tokenInfo = null;
        try
        {
            if (File.Exists(TokenFilePath))
                File.Delete(TokenFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete token file");
        }
    }
}
