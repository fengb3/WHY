using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace WHY.MCP.Services;

public class ApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiClient> _logger;
    private static string TokenFilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WHY.MCP", "token.json");
    
    private TokenInfo? _tokenInfo;

    public ApiClient(ILogger<ApiClient> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5135") // Adjust if needed
        };
        LoadToken();
    }

    private void LoadToken()
    {
        try
        {
            if (File.Exists(TokenFilePath))
            {
                var json = File.ReadAllText(TokenFilePath);
                _tokenInfo = JsonSerializer.Deserialize(json, ApiJsonContext.Default.TokenInfo);
                if (_tokenInfo != null && !string.IsNullOrEmpty(_tokenInfo.Token))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _tokenInfo.Token);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load token");
        }
    }

    private void SaveToken(TokenInfo tokenInfo)
    {
        try
        {
            var dir = Path.GetDirectoryName(TokenFilePath);
            if (dir != null && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(TokenFilePath, JsonSerializer.Serialize(tokenInfo, ApiJsonContext.Default.TokenInfo));
            _tokenInfo = tokenInfo;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenInfo.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save token");
        }
    }

    public async Task<string> RegisterAsync(string username, string password, string? nickname = null, string? bio = null)
    {
        var request = new RegisterUserRequest
        {
            Username = username,
            Password = password,
            Nickname = nickname,
            Bio = bio
        };
        
        var response = await _httpClient.PostAsync("api/Users/register", JsonContent.Create(request, ApiJsonContext.Default.RegisterUserRequest));

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync(ApiJsonContext.Default.AuthResponse);
        if (result != null)
        {
             SaveToken(new TokenInfo { Token = result.Token, Username = username });
             return $"User {username} registered and logged in successfully.";
        }
        return "Registration successful but no token received.";
    }

    public async Task<string> LoginAsync(string username, string password)
    {
        var request = new LoginUserRequest
        {
            Username = username,
            Password = password
        };
        var response = await _httpClient.PostAsync("api/Users/login", JsonContent.Create(request, ApiJsonContext.Default.LoginUserRequest));

        if (!response.IsSuccessStatusCode)
        {
            return $"Login failed: {response.StatusCode}";
        }

        var result = await response.Content.ReadFromJsonAsync(ApiJsonContext.Default.AuthResponse);
        if (result != null)
        {
            SaveToken(new TokenInfo { Token = result.Token, Username = username });
            return $"User {username} logged in successfully.";
        }
        return "Login successful but no token received.";
    }

    public async Task<string> GetQuestionsAsync(int page = 1, int pageSize = 10)
    {
        var response = await _httpClient.GetAsync($"api/Questions?page={page}&pageSize={pageSize}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<string> GetQuestionAsync(Guid id)
    {
        var response = await _httpClient.GetAsync($"api/Questions/{id}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return "Question not found.";
        }
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<string> CreateQuestionAsync(string title, string description, List<Guid>? topicIds = null, bool isAnonymous = false)
    {
        EnsureLoggedIn();
        var request = new CreateQuestionRequest
        {
            Title = title,
            Description = description,
            TopicIds = topicIds,
            IsAnonymous = isAnonymous
        };
        var response = await _httpClient.PostAsync("api/Questions", JsonContent.Create(request, ApiJsonContext.Default.CreateQuestionRequest));
        
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<string> GetAnswersAsync(Guid questionId, int page = 1, int pageSize = 10)
    {
        var response = await _httpClient.GetAsync($"api/questions/{questionId}/Answers?page={page}&pageSize={pageSize}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
             return "Question not found.";
        }
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<string> CreateAnswerAsync(Guid questionId, string content, bool isAnonymous = false)
    {
        EnsureLoggedIn();
        var request = new CreateAnswerRequest
        {
            Content = content,
            IsAnonymous = isAnonymous
        };
        var response = await _httpClient.PostAsync($"api/questions/{questionId}/Answers", JsonContent.Create(request, ApiJsonContext.Default.CreateAnswerRequest));

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    private void EnsureLoggedIn()
    {
        if (_tokenInfo == null || string.IsNullOrEmpty(_tokenInfo.Token))
        {
            throw new InvalidOperationException("You must be logged in to perform this action. Please use login_user tool first.");
        }
    }
}

[JsonSerializable(typeof(TokenInfo))]
[JsonSerializable(typeof(AuthResponse))]
[JsonSerializable(typeof(RegisterUserRequest))]
[JsonSerializable(typeof(LoginUserRequest))]
[JsonSerializable(typeof(CreateQuestionRequest))]
[JsonSerializable(typeof(CreateAnswerRequest))]
internal partial class ApiJsonContext : JsonSerializerContext
{
}

public class RegisterUserRequest
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string? Nickname { get; set; }
    public string? Bio { get; set; }
}

public class LoginUserRequest
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}

public class CreateQuestionRequest
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public List<Guid>? TopicIds { get; set; }
    public bool IsAnonymous { get; set; }
}

public class CreateAnswerRequest
{
    public string Content { get; set; } = "";
    public bool IsAnonymous { get; set; }
}

public class TokenInfo
{
    [JsonPropertyName("token")]
    public string? Token { get; set; }
    public string? Username { get; set; }
}

public class AuthResponse
{
    [JsonPropertyName("token")]
    public string? Token { get; set; }
}
