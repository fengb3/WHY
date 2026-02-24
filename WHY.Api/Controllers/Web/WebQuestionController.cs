using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WHY.Database;
using WHY.Shared.Dtos.Common;
using WHY.Shared.Dtos.Web;

namespace WHY.Api.Controllers.Web;

/// <summary>
/// Web Question Controller - Dedicated endpoints for the UI
/// </summary>
[ApiController]
[Route("api/web/questions")]
public class WebQuestionController(WHYBotDbContext context) : ControllerBase
{
    /// <summary>
    /// Get recommended questions ordered by trending score
    /// </summary>
    [HttpGet("recommended")]
    public async Task<ActionResult<PagedResponse<WebQuestionResponse>>> GetRecommendedQuestionsAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var now = DateTime.UtcNow;

        var baseQuery = context.Questions
            .Include(q => q.BotUser)
            .Include(q => q.QuestionTopics)
            .ThenInclude(qt => qt.Topic)
            .Where(q => !q.IsClosed);

        var totalCount = await baseQuery.CountAsync();

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
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = questions
            .Select(x => new WebQuestionResponse
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

        return Ok(new PagedResponse<WebQuestionResponse>
        {
            Items = result,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        });
    }

    /// <summary>
    /// Get a specific question by ID
    /// </summary>
    [HttpGet("{questionId}")]
    public async Task<ActionResult<WebQuestionResponse>> GetQuestionAsync(Guid questionId)
    {
        var question = await context.Questions
            .Include(q => q.BotUser)
            .Include(q => q.QuestionTopics)
            .ThenInclude(qt => qt.Topic)
            .FirstOrDefaultAsync(q => q.Id == questionId);

        if (question == null)
        {
            return NotFound();
        }

        var answerCount = await context.Answers.CountAsync(a => a.QuestionId == questionId);

        var response = new WebQuestionResponse
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
            BookmarkCount = question.BookmarkCount,
            ShareCount = question.ShareCount,
            CommentCount = question.CommentCount,
            AnswerCount = answerCount,
            HasAcceptedAnswer = question.HasAcceptedAnswer,
            BountyAmount = question.BountyAmount,
            CreatedAt = question.CreatedAt,
            UpdatedAt = question.UpdatedAt,
            LastActivityAt = question.LastActivityAt,
            IsClosed = question.IsClosed,
            IsAnonymous = question.IsAnonymous,
            Topics = question.QuestionTopics.Select(qt => qt.Topic.Name).ToList(),
        };

        return Ok(response);
    }
}
