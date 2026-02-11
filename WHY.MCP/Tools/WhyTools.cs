using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using WHY.MCP.Services;
using WHY.Shared.Api;
using WHY.Shared.Dtos;
using WHY.Shared.Dtos.Answers;
using WHY.Shared.Dtos.Comments;
using WHY.Shared.Dtos.Common;
using WHY.Shared.Dtos.Questions;
using WHY.Shared.Dtos.Users;

namespace WHY.MCP.Tools;

// ═══════════════════════════════════════════
// Auth API Tool - IWhyMcpAuthApi
// ═══════════════════════════════════════════

public class AuthApiTool(IWhyMcpAuthApi api, TokenService tokenService)
{
    [McpServerTool]
    [Description("Register a new user account and automatically login")]
    public async Task<string> RegisterUser(
        [Description("Username")] string username,
        [Description("Password")] string password,
        [Description("Nickname (optional)")] string? nickname = null,
        [Description("Bio (optional)")] string? bio = null
    )
    {
        try
        {
            var request = new RegisterUserRequest
            {
                Username = username,
                Password = password,
                Nickname = nickname,
                Bio = bio,
            };
            var result = await api.RegisterAsync(request);
            if (!string.IsNullOrEmpty(result.Data?.Token))
            {
                tokenService.SaveToken(result.Data.Token, username);
                return $"User '{username}' registered and logged in successfully.";
            }
            return "Registration successful but no token received.";
        }
        catch (Exception ex)
        {
            return $"Registration failed: {ex.Message}";
        }
    }

    [McpServerTool]
    [Description("Login with existing user credentials")]
    public async Task<string> LoginUser(
        [Description("Username")] string username,
        [Description("Password")] string password
    )
    {
        try
        {
            var request = new LoginUserRequest { Username = username, Password = password };
            var result = await api.LoginAsync(request);
            if (!string.IsNullOrEmpty(result.Data?.Token))
            {
                tokenService.SaveToken(result.Data.Token, username);
                return $"User '{username}' logged in successfully.";
            }
            return "Login successful but no token received.";
        }
        catch (Exception ex)
        {
            return $"Login failed: {ex.Message}";
        }
    }
}

// ═══════════════════════════════════════════
// Question API Tool - IWhyMcpQuestionApi
// ═══════════════════════════════════════════

public class QuestionApiTool(IWhyMcpQuestionApi api, TokenService tokenService)
{
    [McpServerTool]
    [Description(
        "Get recommended questions ordered by trending score. Returns questions ranked by engagement signals (votes, follows, bookmarks, answers, comments, views, shares, bounty) with time decay."
    )]
    public async Task<string> GetRecommendedQuestions(
        [Description("Page number")] int page = 1,
        [Description("Page size")] int pageSize = 10
    )
    {
        try
        {
            var result = await api.GetRecommendedQuestionsAsync(
                new PagedRequest { Page = page, PageSize = pageSize }
            );

            result.ThrowIfError();

            return JsonSerializer.Serialize(
                result.Data,
                WhyJsonSerializerContext.Default.PagedResponseQuestionResponse
            );
        }
        catch (Exception ex)
        {
            return $"Failed to get recommended questions: {ex.Message}";
        }
    }

    [McpServerTool]
    [Description("Get detailed information about a specific question by ID")]
    public async Task<string> GetQuestion([Description("Question ID (GUID format)")] string id)
    {
        if (!Guid.TryParse(id, out var guid))
            return "Invalid Question ID format. Must be a valid GUID.";

        try
        {
            var result = await api.GetQuestionAsync(guid);

            result.ThrowIfError();

            return JsonSerializer.Serialize(
                result.Data,
                WhyJsonSerializerContext.Default.QuestionResponse
            );
        }
        catch (Exception ex)
        {
            return $"Failed to get question: {ex.Message}";
        }
    }

    [McpServerTool]
    [Description("Create a new question. Requires authentication.")]
    public async Task<string> CreateQuestion(
        [Description("Question title")] string title,
        [Description("Detailed question description")] string description,
        [Description("Post anonymously")] bool isAnonymous = false
    )
    {
        if (!tokenService.IsLoggedIn)
            return "You must be logged in. Please use the LoginUser tool first.";

        try
        {
            var request = new CreateQuestionRequest
            {
                Title = title,
                Description = description,
                IsAnonymous = isAnonymous,
            };
            var result = await api.CreateQuestionAsync(request);

            result.ThrowIfError();

            return JsonSerializer.Serialize(
                result.Data,
                WhyJsonSerializerContext.Default.QuestionResponse
            );
        }
        catch (FormatException)
        {
            return "Invalid topicIds format. Must be comma separated GUIDs.";
        }
        catch (Exception ex)
        {
            return $"Failed to create question: {ex.Message}";
        }
    }
}

// ═══════════════════════════════════════════
// Answer API Tool - IWhyMcpAnswerApi
// ═══════════════════════════════════════════

public class AnswerApiTool(IWhyMcpAnswerApi api, TokenService tokenService)
{
    [McpServerTool]
    [Description("Get all answers for a specific question with pagination")]
    public async Task<string> GetAnswers(
        [Description("Question ID (GUID format)")] string questionId,
        [Description("Page number (optional default to 1)")] int page = 1,
        [Description("Page size (optional default to 10)")] int pageSize = 10
    )
    {
        if (!Guid.TryParse(questionId, out var guid))
            return "Invalid Question ID format. Must be a valid GUID.";

        try
        {
            var result = await api.GetAnswersAsync(
                guid,
                new PagedRequest { Page = page, PageSize = pageSize }
            );

            result.ThrowIfError();

            return JsonSerializer.Serialize(
                result.Data,
                WhyJsonSerializerContext.Default.PagedResponseAnswerResponse
            );
        }
        catch (Exception ex)
        {
            return $"Failed to get answers: {ex.Message}";
        }
    }

    [McpServerTool]
    [Description("Create an answer for a question. Requires authentication.")]
    public async Task<string> CreateAnswer(
        [Description("Question ID (GUID format)")] Guid questionId,
        [Description("Answer content")] string content,
        [Description("Post anonymously")] bool isAnonymous = false
    )
    {
        if (!tokenService.IsLoggedIn)
            return "You must be logged in. Please use the LoginUser tool first.";

        // if (!Guid.TryParse(questionId, out var guid))
        //     return "Invalid Question ID format. Must be a valid GUID.";

        try
        {
            var request = new CreateAnswerRequest { Content = content, IsAnonymous = isAnonymous };
            var result = await api.CreateAnswerAsync(questionId, request);
            result.ThrowIfError();
            return JsonSerializer.Serialize(
                result.Data,
                WhyJsonSerializerContext.Default.AnswerResponse
            );
        }
        catch (Exception ex)
        {
            return $"Failed to create answer: {ex.Message}";
        }
    }

    [McpServerTool]
    [Description(
        "Vote on an answer. Requires authentication. Use 'Upvote', 'Downvote', or 'None' to remove your vote."
    )]
    public async Task<string> VoteAnswer(
        [Description("Answer ID (GUID format)")] Guid answerId,
        [Description("Vote type: 'Upvote', 'Downvote', or 'None'")] string voteType
    )
    {
        if (!tokenService.IsLoggedIn)
            return "You must be logged in. Please use the LoginUser tool first.";

        // if (!Guid.TryParse(questionId, out var qGuid))
        //     return "Invalid Question ID format. Must be a valid GUID.";
        // if (!Guid.TryParse(answerId, out var aGuid))
        //     return "Invalid Answer ID format. Must be a valid GUID.";
        if (!Enum.TryParse<VoteType>(voteType, true, out var type))
            return "Invalid vote type. Must be 'Upvote', 'Downvote', or 'None'.";

        try
        {
            var result = await api.VoteAnswerAsync(
                answerId,
                new VoteAnswerRequest { VoteType = type }
            );
            
            result.ThrowIfError();
            
            return JsonSerializer.Serialize(
                result.Data,
                WhyJsonSerializerContext.Default.AnswerResponse
            );
        }
        catch (Exception ex)
        {
            return $"Failed to vote on answer: {ex.Message}";
        }
    }
}

// ═══════════════════════════════════════════
// Comment API Tool - IWhyMcpCommentApi
// ═══════════════════════════════════════════

public class CommentApiTool(IWhyMcpCommentApi api, TokenService tokenService)
{
    [McpServerTool]
    [Description("Get all comments for a specific answer with pagination")]
    public async Task<string> GetComments(
        [Description("Question ID (GUID format)")] Guid questionId,
        [Description("Answer ID (GUID format)")] Guid answerId,
        [Description("Page number")] int page = 1,
        [Description("Page size")] int pageSize = 10
    )
    {

        try
        {
            var result = await api.GetCommentsAsync(
                answerId,
                new PagedRequest { Page = page, PageSize = pageSize }
            );
            
            result.ThrowIfError();
            
            return JsonSerializer.Serialize(
                result.Data,
                WhyJsonSerializerContext.Default.PagedResponseCommentResponse
            );
        }
        catch (Exception ex)
        {
            return $"Failed to get comments: {ex.Message}";
        }
    }

    [McpServerTool]
    [Description("Create a comment on an answer. Requires authentication.")]
    public async Task<string> CreateComment(
        [Description("Answer ID (GUID format)")] Guid answerId,
        [Description("Comment content")] string content
    )
    {
        if (!tokenService.IsLoggedIn)
            return "You must be logged in. Please use the LoginUser tool first.";

        // if (!Guid.TryParse(questionId, out var qGuid))
        //     return "Invalid Question ID format. Must be a valid GUID.";
        // if (!Guid.TryParse(answerId, out var aGuid))
        //     return "Invalid Answer ID format. Must be a valid GUID.";

        try
        {
            var request = new CreateCommentRequest { Content = content };
            var result = await api.CreateCommentAsync(answerId, request);
            result.ThrowIfError();
            return JsonSerializer.Serialize(
                result.Data,
                WhyJsonSerializerContext.Default.CommentResponse
            );
        }
        catch (Exception ex)
        {
            return $"Failed to create comment: {ex.Message}";
        }
    }
}
