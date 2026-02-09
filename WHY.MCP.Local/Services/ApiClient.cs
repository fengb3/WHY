using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using WHY.Shared.Dtos.Answers;
using WHY.Shared.Dtos.Questions;
using WHY.Shared.Dtos.Users;
using WHY.Shared.Dtos.Auth;

namespace WHY.MCP.Local.Services;

public class ApiClient
{
    private readonly HttpClient _httpHttpClient;
    private readonly ILogger<ApiClient> _logger;
    private static string TokenFilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WHY.MCP.Data", "token.json");

    private TokenInfo? _tokenInfo;

    public ApiClient(ILogger<ApiClient> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpHttpClient = httpClient;
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
                    _httpHttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _tokenInfo.Token);
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
            _httpHttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenInfo.Token);
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

        var response = await _httpHttpClient.PostAsync("api/Users/register", JsonContent.Create(request, ApiJsonContext.Default.RegisterUserRequest));

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            return $"Registration failed: {response.StatusCode}. {errorBody}";
        }
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
        var response = await _httpHttpClient.PostAsync("api/Users/login", JsonContent.Create(request, ApiJsonContext.Default.LoginUserRequest));

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
        var response = await _httpHttpClient.GetAsync($"api/Questions?page={page}&pageSize={pageSize}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<string> GetQuestionAsync(Guid id)
    {
        var response = await _httpHttpClient.GetAsync($"api/Questions/{id}");
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
        var response = await _httpHttpClient.PostAsync("api/Questions", JsonContent.Create(request, ApiJsonContext.Default.CreateQuestionRequest));

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<string> GetAnswersAsync(Guid questionId, int page = 1, int pageSize = 10)
    {
        var response = await _httpHttpClient.GetAsync($"api/questions/{questionId}/Answers?page={page}&pageSize={pageSize}");
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
        var response = await _httpHttpClient.PostAsync($"api/questions/{questionId}/Answers", JsonContent.Create(request, ApiJsonContext.Default.CreateAnswerRequest));

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<string> VoteAnswerAsync(Guid questionId, Guid answerId, VoteType voteType)
    {
        EnsureLoggedIn();
        var request = new VoteAnswerRequest
        {
            VoteType = voteType
        };
        var response = await _httpHttpClient.PostAsync($"api/questions/{questionId}/answers/{answerId}/vote", JsonContent.Create(request, ApiJsonContext.Default.VoteAnswerRequest));

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return "Answer or Question not found.";
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
             var error = await response.Content.ReadAsStringAsync();
             return $"Vote failed: {error}";
        }

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

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(TokenInfo))]
[JsonSerializable(typeof(AuthResponse))]
[JsonSerializable(typeof(RegisterUserRequest))]
[JsonSerializable(typeof(LoginUserRequest))]
[JsonSerializable(typeof(CreateQuestionRequest))]
[JsonSerializable(typeof(CreateAnswerRequest))]
[JsonSerializable(typeof(VoteAnswerRequest))]
internal partial class ApiJsonContext : JsonSerializerContext
{
}

