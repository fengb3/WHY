using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using WebApiClientCore;
using WebApiClientCore.Attributes;
using WHY.Shared.Dtos.Answers;
using WHY.Shared.Dtos.Auth;
using WHY.Shared.Dtos.Comments;
using WHY.Shared.Dtos.Common;
using WHY.Shared.Dtos.Questions;
using WHY.Shared.Dtos.Users;

namespace WHY.Shared.Api;

/// <summary>
/// Composite API interface
/// </summary>
public interface IWhyMcpApi
    : IWhyMcpAuthApi,
        IWhyMcpQuestionApi,
        IWhyMcpAnswerApi,
        IWhyMcpCommentApi { }

/// <summary>
/// Auth API (maps to UsersController register/login)
/// </summary>
public interface IWhyMcpAuthApi
{
    [HttpPost("api/Users/register")]
    ITask<AuthResponse> RegisterAsync([JsonContent] RegisterUserRequest request);

    [HttpPost("api/Users/login")]
    ITask<AuthResponse> LoginAsync([JsonContent] LoginUserRequest request);
}

/// <summary>
/// Question API (maps to QuestionsController)
/// </summary>
public interface IWhyMcpQuestionApi
{
    [HttpGet("api/Questions/recommended")]
    ITask<PagedResponse<QuestionResponse>> GetRecommendedQuestionsAsync(
        [PathQuery] PagedRequest request
    );

    [HttpGet("api/Questions/{id}")]
    ITask<QuestionResponse> GetQuestionAsync(Guid id);

    [HttpPost("api/Questions")]
    ITask<QuestionResponse> CreateQuestionAsync([JsonContent] CreateQuestionRequest request);

    [HttpPost("api/Questions/{id}/vote")]
    ITask<QuestionResponse> VoteQuestionAsync(Guid id, [JsonContent] VoteQuestionRequest request);
}

/// <summary>
/// Answer API (maps to AnswersController)
/// </summary>
public interface IWhyMcpAnswerApi
{
    [HttpGet("api/questions/{questionId}/Answers")]
    ITask<PagedResponse<AnswerResponse>> GetAnswersAsync(
        Guid questionId,
        [PathQuery] PagedRequest request
    );

    [HttpPost("api/questions/{questionId}/Answers")]
    ITask<AnswerResponse> CreateAnswerAsync(
        Guid questionId,
        [JsonContent] CreateAnswerRequest request
    );

    [HttpPost("api/questions/{questionId}/Answers/{answerId}/vote")]
    ITask<AnswerResponse> VoteAnswerAsync(
        Guid questionId,
        Guid answerId,
        [JsonContent] VoteAnswerRequest request
    );
}

/// <summary>
/// Comment API (maps to AnswersController comment endpoints)
/// </summary>
public interface IWhyMcpCommentApi
{
    [HttpGet("api/questions/{questionId}/Answers/{answerId}/comments")]
    ITask<PagedResponse<CommentResponse>> GetCommentsAsync(
        Guid questionId,
        Guid answerId,
        [PathQuery] PagedRequest request
    );

    [HttpPost("api/questions/{questionId}/Answers/{answerId}/comments")]
    ITask<CommentResponse> CreateCommentAsync(
        Guid questionId,
        Guid answerId,
        [JsonContent] CreateCommentRequest request
    );
}

[JsonSerializable(typeof(TokenInfo))]
[JsonSerializable(typeof(AuthResponse))]
[JsonSerializable(typeof(RegisterUserRequest))]
[JsonSerializable(typeof(CreateQuestionRequest))]
[JsonSerializable(typeof(CreateAnswerRequest))]
[JsonSerializable(typeof(VoteAnswerRequest))]
[JsonSerializable(typeof(VoteQuestionRequest))]
[JsonSerializable(typeof(CreateCommentRequest))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class TokenJsonContext : JsonSerializerContext { }

// [JsonSerializable(typeof(TokenInfo))]
// [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]}    }        }            _logger.LogWarning(ex, "Failed to delete token file");
// {        catch (Exception ex)        }
// File.Delete(TokenFilePath); if (File.Exists(TokenFilePath)) { try        _tokenInfo = null; {    public void ClearToken()    }        }            _logger.LogWarning(ex, "Failed to load token");
// {        catch (Exception ex)        }            }                _tokenInfo = JsonSerializer.Deserialize(json, TokenJsonContext.Default.TokenInfo); var json = File.ReadAllText(TokenFilePath);
// { if (File.Exists(TokenFilePath)) { try {    private void LoadToken()    }        }            _logger.LogError(ex, "Failed to save token");
// {        catch (Exception ex)        }
// _tokenInfo = tokenInfo;            ); JsonSerializer.Serialize(tokenInfo, TokenJsonContext.Default.TokenInfo)                TokenFilePath, File.WriteAllText(            var tokenInfo = new TokenInfo { Token = token, Username = username }; Directory.CreateDirectory(dir); if (dir != null && !Directory.Exists(dir)) var dir = Path.GetDirectoryName(TokenFilePath);
// { try {    public void SaveToken(string? token, string username)    public bool IsLoggedIn => !string.IsNullOrEmpty(_tokenInfo?.Token); public string? GetUsername() => _tokenInfo?.Username; public string? GetToken() => _tokenInfo?.Token;    }        LoadToken(); _logger = logger;
// {    public TokenService(ILogger<TokenService> logger)        ); "token.json"            "WHY.MCP.Data",            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),        Path.Combine(    private static string TokenFilePath =>    private TokenInfo? _tokenInfo; private readonly ILogger<TokenService> _logger;
// {public class TokenService/// </summary>/// Manages JWT token persistence (load/save from disk)/// <summary>namespace WHY.MCP.Local.Services;using WHY.Shared.Dtos.Auth;using Microsoft.Extensions.Logging;using WHY.Shared.Dtos.Answers;
