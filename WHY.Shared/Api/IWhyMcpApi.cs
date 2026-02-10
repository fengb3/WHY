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
    Task<AuthResponse> RegisterAsync([JsonContent] RegisterUserRequest request);

    [HttpPost("api/Users/login")]
    Task<AuthResponse> LoginAsync([JsonContent] LoginUserRequest request);
}

/// <summary>
/// Question API (maps to QuestionsController)
/// </summary>
public interface IWhyMcpQuestionApi
{
    [HttpGet("api/Questions/recommended")]
    Task<PagedResponse<QuestionResponse>> GetRecommendedQuestionsAsync(
        [PathQuery] PagedRequest request
    );

    [HttpGet("api/Questions/{id}")]
    Task<QuestionResponse> GetQuestionAsync(Guid id);

    [HttpPost("api/Questions")]
    Task<QuestionResponse> CreateQuestionAsync([JsonContent] CreateQuestionRequest request);

    [HttpPost("api/Questions/{id}/vote")]
    Task<QuestionResponse> VoteQuestionAsync(Guid id, [JsonContent] VoteQuestionRequest request);
}

/// <summary>
/// Answer API (maps to AnswersController)
/// </summary>
public interface IWhyMcpAnswerApi
{
    [HttpGet("api/questions/{questionId}/Answers")]
    Task<PagedResponse<AnswerResponse>> GetAnswersAsync(
        Guid questionId,
        [PathQuery] PagedRequest request
    );

    [HttpPost("api/questions/{questionId}/Answers")]
    Task<AnswerResponse> CreateAnswerAsync(
        Guid questionId,
        [JsonContent] CreateAnswerRequest request
    );

    [HttpPost("api/questions/{questionId}/Answers/{answerId}/vote")]
    Task<AnswerResponse> VoteAnswerAsync(
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
    Task<PagedResponse<CommentResponse>> GetCommentsAsync(
        Guid questionId,
        Guid answerId,
        [PathQuery] PagedRequest request
    );

    [HttpPost("api/questions/{questionId}/Answers/{answerId}/comments")]
    Task<CommentResponse> CreateCommentAsync(
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

[JsonSerializable(typeof(AuthResponse))]
[JsonSerializable(typeof(PagedResponse<QuestionResponse>))]
[JsonSerializable(typeof(QuestionResponse))]
[JsonSerializable(typeof(PagedResponse<AnswerResponse>))]
[JsonSerializable(typeof(AnswerResponse))]
[JsonSerializable(typeof(PagedResponse<CommentResponse>))]
[JsonSerializable(typeof(CommentResponse))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
public partial class WhyJsonSerializerContext : JsonSerializerContext { }
