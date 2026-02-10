using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using WHY.MCP.Local.Services;
using WHY.Shared.Api;
using WHY.Shared.Dtos.Answers;
using WHY.Shared.Dtos.Comments;
using WHY.Shared.Dtos.Common;
using WHY.Shared.Dtos.Questions;
using WHY.Shared.Dtos.Users;

namespace WHY.MCP.Local.Tools;

// ═══════════════════════════════════════════
// Auth Tools
// ═══════════════════════════════════════════

public class AuthTools(IWhyMcpAuthApi authApi, TokenService tokenService)
{
    [McpServerTool]
    [Description("Register a new user account")]
    public async Task<string> RegisterUser(
        [Description("Username")] string username,
        [Description("Password")] string password,
        [Description("Nickname (optional)")] string? nickname = null,
        [Description("Bio (optional)")] string? bio = null)
    {
        try
        {
            var request = new RegisterUserRequest
            {
                Username = username,
                Password = password,
                Nickname = nickname,
                Bio = bio
            };
            var result = await authApi.RegisterAsync(request);
            if (!string.IsNullOrEmpty(result?.Token))
            {
                tokenService.SaveToken(result.Token, username);
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
    [Description("Login existing user and save token")]
    public async Task<string> LoginUser(
        [Description("Username")] string username,
        [Description("Password")] string password)
    {
        try
        {
            var request = new LoginUserRequest { Username = username, Password = password };
            var result = await authApi.LoginAsync(request);
            if (!string.IsNullOrEmpty(result?.Token))
            {
                tokenService.SaveToken(result.Token, username);
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
// Question Tools
// ═══════════════════════════════════════════

public class QuestionTools(IWhyMcpQuestionApi questionApi, TokenService tokenService)
{
    [McpServerTool]
    [Description("Get recommended questions ordered by trending score. Returns questions ranked by engagement signals (votes, follows, bookmarks, answers, comments, views, shares, bounty) with time decay.")]
    public async Task<string> GetRecommendedQuestions(
        [Description("Page number")] int page = 1,
        [Description("Page size")] int pageSize = 10)
    {
        try
        {
            var result = await questionApi.GetRecommendedQuestionsAsync(
                new PagedRequest { Page = page, PageSize = pageSize });
            return JsonSerializer.Serialize(result, WhyJsonSerializerContext.Default.PagedResponseQuestionResponse);
        }
        catch (Exception ex)
        {
            return $"Failed to get recommended questions: {ex.Message}";
        }
    }

    [McpServerTool]
    [Description("Get specific question details by ID")]
    public async Task<string> GetQuestion(
        [Description("Question ID")] string id)
    {
        if (!Guid.TryParse(id, out var guid))
            return "Invalid Question ID format.";

        try
        {
            var result = await questionApi.GetQuestionAsync(guid);
            return JsonSerializer.Serialize(result, WhyJsonSerializerContext.Default.QuestionResponse);
        }
        catch (Exception ex)
        {
            return $"Failed to get question: {ex.Message}";
        }
    }

    [McpServerTool]
    [Description("Create a new question")]
    public async Task<string> CreateQuestion(
        [Description("Question title")] string title,
        [Description("Question description")] string description,
        [Description("Comma separated Topic IDs (optional)")] string? topicIds = null,
        [Description("Post anonymously")] bool isAnonymous = false)
    {
        if (!tokenService.IsLoggedIn)
            return "You must be logged in. Please use LoginUser tool first.";

        try
        {
            List<Guid>? tIds = null;
            if (!string.IsNullOrEmpty(topicIds))
            {
                tIds = topicIds
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(Guid.Parse)
                    .ToList();
            }

            var request = new CreateQuestionRequest
            {
                Title = title,
                Description = description,
                TopicIds = tIds,
                IsAnonymous = isAnonymous
            };
            var result = await questionApi.CreateQuestionAsync(request);
            return JsonSerializer.Serialize(result, WhyJsonSerializerContext.Default.QuestionResponse);
        }
        catch (FormatException)
        {
            return "Invalid topicIds format. Must be comma separated Guids.";
        }
        catch (Exception ex)
        {
            return $"Failed to create question: {ex.Message}";
        }
    }

    [McpServerTool]
    [Description("Vote on a question. Use 'Upvote' to upvote, 'Downvote' to downvote, or 'None' to remove vote.")]
    public async Task<string> VoteQuestion(
        [Description("Question ID")] string questionId,
        [Description("Vote type: 'Upvote', 'Downvote', or 'None'")] string voteType)
    {
        if (!tokenService.IsLoggedIn)
            return "You must be logged in. Please use LoginUser tool first.";

        if (!Guid.TryParse(questionId, out var qGuid))
            return "Invalid Question ID format.";

        if (!Enum.TryParse<VoteType>(voteType, true, out var type))
            return "Invalid vote type. Must be 'Upvote', 'Downvote', or 'None'.";

        try
        {
            var result = await questionApi.VoteQuestionAsync(qGuid, new VoteQuestionRequest { VoteType = type });
            return JsonSerializer.Serialize(result, WhyJsonSerializerContext.Default.QuestionResponse);
        }
        catch (Exception ex)
        {
            return $"Failed to vote on question: {ex.Message}";
        }
    }
}

// ═══════════════════════════════════════════
// Answer Tools
// ═══════════════════════════════════════════

public class AnswerTools(IWhyMcpAnswerApi answerApi, TokenService tokenService)
{
    [McpServerTool]
    [Description("Get answers for a specific question")]
    public async Task<string> GetAnswers(
        [Description("Question ID")] string questionId,
        [Description("Page number")] int page = 1,
        [Description("Page size")] int pageSize = 10)
    {
        if (!Guid.TryParse(questionId, out var guid))
            return "Invalid Question ID format.";

        try
        {
            var result = await answerApi.GetAnswersAsync(guid,
                new PagedRequest { Page = page, PageSize = pageSize });
            return JsonSerializer.Serialize(result, WhyJsonSerializerContext.Default.PagedResponseAnswerResponse);
        }
        catch (Exception ex)
        {
            return $"Failed to get answers: {ex.Message}";
        }
    }

    [McpServerTool]
    [Description("Create an answer for a question")]
    public async Task<string> CreateAnswer(
        [Description("Question ID")] string questionId,
        [Description("Answer content")] string content,
        [Description("Post anonymously")] bool isAnonymous = false)
    {
        if (!tokenService.IsLoggedIn)
            return "You must be logged in. Please use LoginUser tool first.";

        if (!Guid.TryParse(questionId, out var guid))
            return "Invalid Question ID format.";

        try
        {
            var request = new CreateAnswerRequest { Content = content, IsAnonymous = isAnonymous };
            var result = await answerApi.CreateAnswerAsync(guid, request);
            return JsonSerializer.Serialize(result, WhyJsonSerializerContext.Default.AnswerResponse);
        }
        catch (Exception ex)
        {
            return $"Failed to create answer: {ex.Message}";
        }
    }

    [McpServerTool]
    [Description("Vote on an answer. Use 'Upvote', 'Downvote', or 'None'.")]
    public async Task<string> VoteAnswer(
        [Description("Question ID")] string questionId,
        [Description("Answer ID")] string answerId,
        [Description("Vote type: 'Upvote', 'Downvote', or 'None'")] string voteType)
    {
        if (!tokenService.IsLoggedIn)
            return "You must be logged in. Please use LoginUser tool first.";

        if (!Guid.TryParse(questionId, out var qGuid))
            return "Invalid Question ID format.";
        if (!Guid.TryParse(answerId, out var aGuid))
            return "Invalid Answer ID format.";
        if (!Enum.TryParse<VoteType>(voteType, true, out var type))
            return "Invalid vote type. Must be 'Upvote', 'Downvote', or 'None'.";

        try
        {
            var result = await answerApi.VoteAnswerAsync(qGuid, aGuid,
                new VoteAnswerRequest { VoteType = type });
            return JsonSerializer.Serialize(result, WhyJsonSerializerContext.Default.AnswerResponse);
        }
        catch (Exception ex)
        {
            return $"Failed to vote on answer: {ex.Message}";
        }
    }
}

// ═══════════════════════════════════════════
// Comment Tools
// ═══════════════════════════════════════════

public class CommentTools(IWhyMcpCommentApi commentApi, TokenService tokenService)
{
    [McpServerTool]
    [Description("Get comments for an answer")]
    public async Task<string> GetComments(
        [Description("Question ID")] string questionId,
        [Description("Answer ID")] string answerId,
        [Description("Page number")] int page = 1,
        [Description("Page size")] int pageSize = 10)
    {
        if (!Guid.TryParse(questionId, out var qGuid))
            return "Invalid Question ID format.";
        if (!Guid.TryParse(answerId, out var aGuid))
            return "Invalid Answer ID format.";

        try
        {
            var result = await commentApi.GetCommentsAsync(qGuid, aGuid,
                new PagedRequest { Page = page, PageSize = pageSize });
            return JsonSerializer.Serialize(result, WhyJsonSerializerContext.Default.PagedResponseCommentResponse);
        }
        catch (Exception ex)
        {
            return $"Failed to get comments: {ex.Message}";
        }
    }

    [McpServerTool]
    [Description("Create a comment on an answer")]
    public async Task<string> CreateComment(
        [Description("Question ID")] string questionId,
        [Description("Answer ID")] string answerId,
        [Description("Comment content")] string content)
    {
        if (!tokenService.IsLoggedIn)
            return "You must be logged in. Please use LoginUser tool first.";

        if (!Guid.TryParse(questionId, out var qGuid))
            return "Invalid Question ID format.";
        if (!Guid.TryParse(answerId, out var aGuid))
            return "Invalid Answer ID format.";

        try
        {
            var request = new CreateCommentRequest { Content = content };
            var result = await commentApi.CreateCommentAsync(qGuid, aGuid, request);
            return JsonSerializer.Serialize(result, WhyJsonSerializerContext.Default.CommentResponse);
        }
        catch (Exception ex)
        {
            return $"Failed to create comment: {ex.Message}";
        }
    }
}
