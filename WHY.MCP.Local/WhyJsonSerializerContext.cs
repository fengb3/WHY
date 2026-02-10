using System.Text.Json.Serialization;
using WHY.Shared.Dtos.Answers;
using WHY.Shared.Dtos.Auth;
using WHY.Shared.Dtos.Comments;
using WHY.Shared.Dtos.Common;
using WHY.Shared.Dtos.Questions;

namespace WHY.MCP.Local;

/// <summary>
/// JSON source generation context for Native AOT compatibility.
/// Covers all DTO types used by the WebApiClientCore API interfaces.
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(AuthResponse))]
[JsonSerializable(typeof(PagedResponse<QuestionResponse>))]
[JsonSerializable(typeof(QuestionResponse))]
[JsonSerializable(typeof(PagedResponse<AnswerResponse>))]
[JsonSerializable(typeof(AnswerResponse))]
[JsonSerializable(typeof(PagedResponse<CommentResponse>))]
[JsonSerializable(typeof(CommentResponse))]
internal partial class WhyJsonSerializerContext : JsonSerializerContext;
