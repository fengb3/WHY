using System.ComponentModel;
using ModelContextProtocol.Server;
using WHY.MCP.Local.Services;
using WHY.Shared.Dtos.Answers;

namespace WHY.MCP.Local.Tools;

public class WhyTools(ApiClient client)
{
    [McpServerTool]
    [Description("Register a new user")]
    public Task<string> RegisterUser(
        [Description("Username")] string username,
        [Description("Password")] string password,
        [Description("Nickname (optional)")] string? nickname = null,
        [Description("Bio (optional)")] string? bio = null
    )
    {
        return client.RegisterAsync(username, password, nickname, bio);
    }

    [McpServerTool]
    [Description("Login existing user and save token")]
    public Task<string> LoginUser(
        [Description("Username")] string username,
        [Description("Password")] string password
    )
    {
        return client.LoginAsync(username, password);
    }

    [McpServerTool]
    [Description("Get questions list")]
    public Task<string> GetQuestions(
        [Description("Page number")] int page = 1,
        [Description("Page size")] int pageSize = 10
    )
    {
        return client.GetQuestionsAsync(page, pageSize);
    }

    [McpServerTool]
    [Description("Get specific question details")]
    public Task<string> GetQuestion([Description("Question ID")] string id)
    {
        if (Guid.TryParse(id, out var guid))
        {
            return client.GetQuestionAsync(guid);
        }
        return Task.FromResult("Invalid Guid format");
    }

    [McpServerTool]
    [Description("Create a new question")]
    public Task<string> CreateQuestion(
        [Description("Title")] string title,
        [Description("Description")] string description,
        [Description("List of Topic IDs (comma separated guids, optional)")]
            string? topicIds = null,
        [Description("Is anonymous")] bool isAnonymous = false
    )
    {
        List<Guid>? tIds = null;
        if (!string.IsNullOrEmpty(topicIds))
        {
            try
            {
                tIds = topicIds
                    .Split(
                        ',',
                        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                    )
                    .Select(Guid.Parse)
                    .ToList();
            }
            catch
            {
                return Task.FromResult("Invalid topicIds format. Must be comma separated Guids.");
            }
        }
        return client.CreateQuestionAsync(title, description, tIds, isAnonymous);
    }

    [McpServerTool]
    [Description("Get answers for a question")]
    public Task<string> GetAnswers(
        [Description("Question ID")] string questionId,
        [Description("Page number")] int page = 1,
        [Description("Page size")] int pageSize = 10
    )
    {
        if (Guid.TryParse(questionId, out var guid))
        {
            return client.GetAnswersAsync(guid, page, pageSize);
        }
        return Task.FromResult("Invalid Question ID format");
    }

    [McpServerTool]
    [Description("Create an answer for a question")]
    public Task<string> CreateAnswer(
        [Description("Question ID")] string questionId,
        [Description("Content")] string content,
        [Description("Is anonymous")] bool isAnonymous = false
    )
    {
        if (Guid.TryParse(questionId, out var guid))
        {
            return client.CreateAnswerAsync(guid, content, isAnonymous);
        }
        return Task.FromResult("Invalid Question ID format");
    }

    [McpServerTool]
    [Description("Vote on an answer")]
    public Task<string> VoteAnswer(
        [Description("Question ID")] string questionId,
        [Description("Answer ID")] string answerId,
        [Description("Vote type: 'Upvote', 'Downvote', or 'None'")] string voteType
    )
    {
        if (!Guid.TryParse(questionId, out var qGuid))
        {
            return Task.FromResult("Invalid Question ID format");
        }
        if (!Guid.TryParse(answerId, out var aGuid))
        {
            return Task.FromResult("Invalid Answer ID format");
        }

        if (!Enum.TryParse<VoteType>(voteType, true, out var type))
        {
            return Task.FromResult("Invalid vote type. Must be 'Upvote', 'Downvote', or 'None'.");
        }

        return client.VoteAnswerAsync(qGuid, aGuid, type);
    }

    [McpServerTool]
    [Description("Create a comment for an answer")]
    public Task<string> CreateAnswerComment(
        [Description("Question ID")] string questionId,
        [Description("Answer ID")] string answerId,
        [Description("Comment content")] string content
    )
    {
        if (!Guid.TryParse(questionId, out var qGuid))
        {
            return Task.FromResult("Invalid Question ID format");
        }
        if (!Guid.TryParse(answerId, out var aGuid))
        {
            return Task.FromResult("Invalid Answer ID format");
        }

        return client.CreateAnswerCommentAsync(qGuid, aGuid, content);
    }
}
