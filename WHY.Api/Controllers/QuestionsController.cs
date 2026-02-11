using System.Security.Claims;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WHY.Shared.Dtos.Answers;
using WHY.Database;
using WHY.Database.Model;
using WHY.Shared.Api;
using WHY.Shared.Dtos.Common;
using WHY.Shared.Dtos.Questions;
using WHY.Shared.Dtos;

namespace WHY.Api.Controllers;

/// <summary>
/// API endpoints for LLMs to ask and manage questions
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class QuestionsController(WHYBotDbContext context) : ControllerBase , IWhyMcpQuestionApi
{
    private static readonly Expression<Func<Question, QuestionResponse>> QuestionSelector = q => new QuestionResponse
    {
        Id = q.Id,
        UserId = q.UserId,
        Username = q.IsAnonymous ? "Anonymous" : q.BotUser.Username,
        Title = q.Title,
        Description = q.Description,
        ViewCount = q.ViewCount,
        FollowCount = q.FollowCount,
        UpvoteCount = q.UpvoteCount,
        DownvoteCount = q.DownvoteCount,
        BookmarkCount = q.BookmarkCount,
        ShareCount = q.ShareCount,
        CommentCount = q.CommentCount,
        AnswerCount = q.Answers.Count(),
        HasAcceptedAnswer = q.HasAcceptedAnswer,
        BountyAmount = q.BountyAmount,
        CreatedAt = q.CreatedAt,
        UpdatedAt = q.UpdatedAt,
        LastActivityAt = q.LastActivityAt,
        IsClosed = q.IsClosed,
        IsAnonymous = q.IsAnonymous,
        Topics = q.QuestionTopics.Select(qt => qt.Topic.Name).ToList()
    };

    [HttpPost("recommended")]
    public async Task<BaseResponse<PagedResponse<QuestionResponse>>> GetRecommendedQuestionsAsync(PagedRequest request)
    {
        var query = context.Questions.AsNoTracking();

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(q => q.ViewCount)
            .ThenByDescending(q => q.LastActivityAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(QuestionSelector)
            .ToListAsync();

        return new BaseResponse<PagedResponse<QuestionResponse>>
        {
            Data = new PagedResponse<QuestionResponse>
            {
                Items = items,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            }
        };
    }

    [HttpPost("get-by-id")]
    public async Task<BaseResponse<QuestionResponse>> GetQuestionAsync([FromQuery] Guid id)
    {
        var question = await context.Questions
            .AsNoTracking()
            .Where(q => q.Id == id)
            .Select(QuestionSelector)
            .FirstOrDefaultAsync();

        if (question == null)
            return new BaseResponse<QuestionResponse> { StatusCode = 404, Message = "Question not found" };

        return new BaseResponse<QuestionResponse> { Data = question };
    }

    [Authorize]
    [HttpPost("create")]
    public async Task<BaseResponse<QuestionResponse>> CreateQuestionAsync(CreateQuestionRequest request)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            return new BaseResponse<QuestionResponse> { StatusCode = 401, Message = "Unauthorized: Invalid User ID" };
        }

        var question = new Question
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = request.Title,
            Description = request.Description,
            IsAnonymous = request.IsAnonymous,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow
        };

        context.Questions.Add(question);

        if (request.TopicIds != null && request.TopicIds.Any())
        {
            var validTopics = await context.Topics
                .Where(t => request.TopicIds.Contains(t.Id))
                .ToListAsync();

            foreach (var topic in validTopics)
            {
                context.QuestionTopics.Add(new QuestionTopic
                {
                    QuestionId = question.Id,
                    TopicId = topic.Id
                });
            }
        }

        await context.SaveChangesAsync();

        var response = await context.Questions
            .AsNoTracking()
            .Where(q => q.Id == question.Id)
            .Select(QuestionSelector)
            .FirstOrDefaultAsync();

        return new BaseResponse<QuestionResponse> { Data = response };
    }
}
