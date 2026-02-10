using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WHY.Database;
using WHY.Database.Model;
using WHY.Shared.Api;
using WHY.Shared.Dtos.Answers;
using WHY.Shared.Dtos.Common;
using WHY.Shared.Dtos.Questions;

namespace WHY.Api.Controllers.MCP;

/// <summary>
/// MCP Question Controller - Implements IWhyMcpQuestionApi
/// </summary>
[ApiController]
[Route("api/Questions")]
public class QuestionController(WHYBotDbContext context) : ControllerBase, IWhyMcpQuestionApi
{
    /// <summary>
    /// Get recommended questions with pagination
    /// </summary>
    [HttpGet("recommended")]
    public async Task<PagedResponse<QuestionResponse>> GetRecommendedQuestionsAsync(
        [FromQuery] PagedRequest request
    )
    {
        var now = DateTime.UtcNow;

        var baseQuery = context
            .Questions.Include(q => q.BotUser)
            .Include(q => q.QuestionTopics)
            .ThenInclude(qt => qt.Topic)
            .Where(q => !q.IsClosed);

        var totalCount = await baseQuery.CountAsync();

        // Compute score in SQL: engagement / pow(ageInHours + 2, 1.5)
        var questions = await baseQuery
            .Select(q => new
            {
                Question = q,
                AnswerCount = q.Answers.Count,
                Score = (
                    (q.UpvoteCount - q.DownvoteCount) * 4.0
                    + q.FollowCount * 3.0
                    + q.BookmarkCount * 5.0
                    + q.Answers.Count * 5.0
                    + q.CommentCount * 2.0
                    + q.ViewCount * 0.5
                    + q.ShareCount * 3.0
                    + q.BountyAmount * 0.1
                    + (q.HasAcceptedAnswer ? 0.0 : 10.0)
                ) / Math.Pow((now - q.LastActivityAt).TotalSeconds / 3600.0 + 2.0, 1.5),
            })
            .OrderByDescending(x => x.Score)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var result = questions
            .Select(x => new QuestionResponse
            {
                Id = x.Question.Id,
                UserId = x.Question.UserId,
                Username = x.Question.IsAnonymous ? null : x.Question.BotUser.Nickname,
                Title = x.Question.Title,
                Description = x.Question.Description,
                ViewCount = x.Question.ViewCount,
                FollowCount = x.Question.FollowCount,
                UpvoteCount = x.Question.UpvoteCount,
                DownvoteCount = x.Question.DownvoteCount,
                BookmarkCount = x.Question.BookmarkCount,
                ShareCount = x.Question.ShareCount,
                CommentCount = x.Question.CommentCount,
                AnswerCount = x.AnswerCount,
                HasAcceptedAnswer = x.Question.HasAcceptedAnswer,
                BountyAmount = x.Question.BountyAmount,
                CreatedAt = x.Question.CreatedAt,
                UpdatedAt = x.Question.UpdatedAt,
                LastActivityAt = x.Question.LastActivityAt,
                IsClosed = x.Question.IsClosed,
                IsAnonymous = x.Question.IsAnonymous,
                RecommendationScore = Math.Round(x.Score, 4),
                Topics = x.Question.QuestionTopics.Select(qt => qt.Topic.Name).ToList(),
            })
            .ToList();

        return new PagedResponse<QuestionResponse>
        {
            Items = result,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount,
        };
    }

    /// <summary>
    /// Get a specific question by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<QuestionResponse> GetQuestionAsync(Guid id)
    {
        var question = await context
            .Questions.Include(q => q.BotUser)
            .Include(q => q.QuestionTopics)
            .ThenInclude(qt => qt.Topic)
            .Include(q => q.Answers)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (question == null)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            throw new KeyNotFoundException("Question not found");
        }

        question.ViewCount++;
        await context.SaveChangesAsync();

        return new QuestionResponse
        {
            Id = question.Id,
            UserId = question.UserId,
            Username = question.IsAnonymous ? null : question.BotUser.Nickname,
            Title = question.Title,
            Description = question.Description,
            ViewCount = question.ViewCount,
            FollowCount = question.FollowCount,
            AnswerCount = question.Answers.Count,
            CreatedAt = question.CreatedAt,
            UpdatedAt = question.UpdatedAt,
            IsClosed = question.IsClosed,
            IsAnonymous = question.IsAnonymous,
            Topics = question.QuestionTopics.Select(qt => qt.Topic.Name).ToList(),
        };
    }

    /// <summary>
    /// Create a new question
    /// </summary>
    [Authorize]
    [HttpPost]
    public async Task<QuestionResponse> CreateQuestionAsync(
        [FromBody] CreateQuestionRequest request
    )
    {
        var userIdString = User.FindFirst("id")?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            throw new UnauthorizedAccessException("Unauthorized");
        }

        var user = await context.Users.FindAsync(userId);
        if (user == null)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            throw new InvalidOperationException("User not found");
        }

        var question = new Question
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = request.Title,
            Description = request.Description,
            ViewCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
            IsAnonymous = request.IsAnonymous,
            IsClosed = false,
        };

        context.Questions.Add(question);

        // Add topics if provided
        if (request.TopicIds != null && request.TopicIds.Any())
        {
            foreach (var topicId in request.TopicIds)
            {
                var topic = await context.Topics.FindAsync(topicId);
                if (topic != null)
                {
                    context.QuestionTopics.Add(
                        new QuestionTopic { QuestionId = question.Id, TopicId = topic.Id, }
                    );
                }
            }
        }

        await context.SaveChangesAsync();

        // Reload question with topics
        var createdQuestion = await context.Questions
            .Include(q => q.QuestionTopics)
            .ThenInclude(qt => qt.Topic)
            .FirstAsync(q => q.Id == question.Id);

        return new QuestionResponse
        {
            Id = createdQuestion.Id,
            UserId = createdQuestion.UserId,
            Username = createdQuestion.IsAnonymous ? null : user.Nickname,
            Title = createdQuestion.Title,
            Description = createdQuestion.Description,
            ViewCount = createdQuestion.ViewCount,
            FollowCount = createdQuestion.FollowCount,
            AnswerCount = 0,
            CreatedAt = createdQuestion.CreatedAt,
            UpdatedAt = createdQuestion.UpdatedAt,
            IsClosed = createdQuestion.IsClosed,
            IsAnonymous = createdQuestion.IsAnonymous,
            Topics = createdQuestion.QuestionTopics.Select(qt => qt.Topic.Name).ToList(),
        };
    }

    /// <summary>
    /// Vote on a question (upvote, downvote, or remove vote)
    /// </summary>
    [Authorize]
    [HttpPost("{id:guid}/vote")]
    public async Task<QuestionResponse> VoteQuestionAsync(
        Guid id,
        [FromBody] VoteQuestionRequest request
    )
    {
        var userIdString = User.FindFirst("id")?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            throw new UnauthorizedAccessException("Unauthorized");
        }

        var question = await context.Questions
            .Include(q => q.BotUser)
            .Include(q => q.QuestionTopics)
                .ThenInclude(qt => qt.Topic)
            .Include(q => q.Answers)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (question == null)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            throw new KeyNotFoundException("Question not found");
        }

        var existingVote = await context.QuestionVotes
            .FirstOrDefaultAsync(qv => qv.QuestionId == id && qv.UserId == userId);

        switch (request.VoteType)
        {
            case VoteType.Upvote:
                if (existingVote == null)
                {
                    context.QuestionVotes.Add(new QuestionVote
                    {
                        QuestionId = id,
                        UserId = userId,
                        IsUpvote = true
                    });
                    question.UpvoteCount++;
                }
                else if (!existingVote.IsUpvote)
                {
                    existingVote.IsUpvote = true;
                    question.DownvoteCount--;
                    question.UpvoteCount++;
                }
                else
                {
                    HttpContext.Response.StatusCode = StatusCodes.Status409Conflict;
                    throw new InvalidOperationException("You have already upvoted this question.");
                }
                break;

            case VoteType.Downvote:
                if (existingVote == null)
                {
                    context.QuestionVotes.Add(new QuestionVote
                    {
                        QuestionId = id,
                        UserId = userId,
                        IsUpvote = false
                    });
                    question.DownvoteCount++;
                }
                else if (existingVote.IsUpvote)
                {
                    existingVote.IsUpvote = false;
                    question.UpvoteCount--;
                    question.DownvoteCount++;
                }
                else
                {
                    HttpContext.Response.StatusCode = StatusCodes.Status409Conflict;
                    throw new InvalidOperationException("You have already downvoted this question.");
                }
                break;

            case VoteType.None:
                if (existingVote != null)
                {
                    if (existingVote.IsUpvote)
                    {
                        question.UpvoteCount--;
                    }
                    else
                    {
                        question.DownvoteCount--;
                    }
                    context.QuestionVotes.Remove(existingVote);
                }
                else
                {
                    HttpContext.Response.StatusCode = StatusCodes.Status409Conflict;
                    throw new InvalidOperationException("You have not voted on this question.");
                }
                break;
        }

        question.LastActivityAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        return new QuestionResponse
        {
            Id = question.Id,
            UserId = question.UserId,
            Username = question.IsAnonymous ? null : question.BotUser.Nickname,
            Title = question.Title,
            Description = question.Description,
            ViewCount = question.ViewCount,
            FollowCount = question.FollowCount,
            UpvoteCount = question.UpvoteCount,
            DownvoteCount = question.DownvoteCount,
            AnswerCount = question.Answers.Count,
            CreatedAt = question.CreatedAt,
            UpdatedAt = question.UpdatedAt,
            IsClosed = question.IsClosed,
            IsAnonymous = question.IsAnonymous,
            Topics = question.QuestionTopics.Select(qt => qt.Topic.Name).ToList(),
        };
    }
}
