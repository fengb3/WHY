using System.Text.Json.Serialization;
using WebApiClientCore.Attributes;
using WHY.Shared.Dtos;
using WHY.Shared.Dtos.Answers;
using WHY.Shared.Dtos.Auth;
using WHY.Shared.Dtos.Comments;
using WHY.Shared.Dtos.Common;
using WHY.Shared.Dtos.Questions;
using WHY.Shared.Dtos.Users;

namespace WHY.Shared.Api;


/// <summary>
/// Auth API (maps to UsersController register/login)
/// </summary>
public interface IWhyMcpAuthApi
{
    [HttpPost("api/auth/register")]
    Task<BaseResponse<AuthResponse>> RegisterAsync([JsonContent] RegisterUserRequest request);

    [HttpPost("api/auth/login")]
    Task<BaseResponse<AuthResponse>> LoginAsync([JsonContent] LoginUserRequest request);
}

/// <summary>
/// Question API (maps to QuestionsController)
/// </summary>
public interface IWhyMcpQuestionApi
{
    [HttpPost("api/question/recommended")]
    Task<BaseResponse<PagedResponse<QuestionResponse>>> GetRecommendedQuestionsAsync(
        [JsonContent] PagedRequest request
    );

    [HttpPost("api/question/get-by-id")]
    Task<BaseResponse<QuestionResponse>> GetQuestionAsync([PathQuery] Guid id);

    [HttpPost("api/question/create")]
    Task<BaseResponse<QuestionResponse>> CreateQuestionAsync(
        [JsonContent] CreateQuestionRequest request
    );
}

/// <summary>
/// Answer API (maps to AnswersController)
/// </summary>
public interface IWhyMcpAnswerApi
{
    [HttpPost("api/answer/get-by-question-id")]
    Task<BaseResponse<PagedResponse<AnswerResponse>>> GetAnswersAsync(
        [PathQuery] Guid questionId,
        [JsonContent] PagedRequest request
    );

    [HttpPost("api/answer/create")]
    Task<BaseResponse<AnswerResponse>> CreateAnswerAsync(
        [PathQuery] Guid questionId,
        [JsonContent] CreateAnswerRequest request
    );

    [HttpPost("api/answer/vote")]
    Task<BaseResponse<AnswerResponse>> VoteAnswerAsync(
        [PathQuery] Guid answerId,
        [JsonContent] VoteAnswerRequest request
    );
}

/// <summary>
/// Comment API (maps to AnswersController comment endpoints)
/// </summary>
public interface IWhyMcpCommentApi
{
    [HttpPost("api/comment/get-under-answer")]
    Task<BaseResponse<PagedResponse<CommentResponse>>> GetCommentsAsync(
        [PathQuery] Guid answerId,
        [JsonContent] PagedRequest request
    );

    [HttpPost("api/comment/create")]
    Task<BaseResponse<CommentResponse>> CreateCommentAsync(
        [PathQuery] Guid answerId,
        [JsonContent] CreateCommentRequest request
    );
}

[JsonSerializable(typeof(TokenInfo))]
[JsonSerializable(typeof(AuthResponse))]
[JsonSerializable(typeof(RegisterUserRequest))]
[JsonSerializable(typeof(CreateQuestionRequest))]
[JsonSerializable(typeof(CreateAnswerRequest))]
[JsonSerializable(typeof(VoteAnswerRequest))]
[JsonSerializable(typeof(LoginUserRequest))]
[JsonSerializable(typeof(PagedRequest))]
[JsonSerializable(typeof(VoteQuestionRequest))]
[JsonSerializable(typeof(CreateCommentRequest))]
[JsonSerializable(typeof(AuthResponse))]
[JsonSerializable(typeof(PagedResponse<QuestionResponse>))]
[JsonSerializable(typeof(QuestionResponse))]
[JsonSerializable(typeof(PagedResponse<AnswerResponse>))]
[JsonSerializable(typeof(AnswerResponse))]
[JsonSerializable(typeof(PagedResponse<CommentResponse>))]
[JsonSerializable(typeof(CommentResponse))]
[JsonSerializable(typeof(BaseResponse<AuthResponse>))]
[JsonSerializable(typeof(BaseResponse<PagedResponse<QuestionResponse>>))]
[JsonSerializable(typeof(BaseResponse<QuestionResponse>))]
[JsonSerializable(typeof(BaseResponse<PagedResponse<AnswerResponse>>))]
[JsonSerializable(typeof(BaseResponse<AnswerResponse>))]
[JsonSerializable(typeof(BaseResponse<PagedResponse<CommentResponse>>))]
[JsonSerializable(typeof(BaseResponse<CommentResponse>))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
public partial class WhyJsonSerializerContext : JsonSerializerContext;
