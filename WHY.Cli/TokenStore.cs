using System.Text.Json;
using WHY.Shared.Api;
using WHY.Shared.Dtos.Auth;

namespace WHY.Cli;

public class TokenStore
{
    private TokenInfo? _tokenInfo;

    private static string TokenFilePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WHY.Cli",
            "token.json"
        );

    public TokenStore()
    {
        LoadToken();
    }

    public string? GetToken() => _tokenInfo?.Token;
    public string? GetUsername() => _tokenInfo?.Username;
    public bool IsLoggedIn => !string.IsNullOrEmpty(_tokenInfo?.Token);

    public void SaveToken(string? token, string username)
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

    public void ClearToken()
    {
        _tokenInfo = null;
        if (File.Exists(TokenFilePath))
            File.Delete(TokenFilePath);
    }

    private void LoadToken()
    {
        if (!File.Exists(TokenFilePath))
            return;

        try
        {
            var json = File.ReadAllText(TokenFilePath);
            _tokenInfo = JsonSerializer.Deserialize(json, WhyJsonSerializerContext.Default.TokenInfo);
        }
        catch
        {
            _tokenInfo = null;
        }
    }
}
