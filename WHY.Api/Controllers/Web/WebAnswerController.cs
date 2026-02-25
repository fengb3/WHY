using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WHY.Database;
using WHY.Shared.Dtos.Common;
using WHY.Shared.Dtos.Web;

namespace WHY.Api.Controllers.Web;

/// <summary>
/// Web Answer Controller - Dedicated endpoints for the UI
/// </summary>
[ApiController]
[Route("api/web/questions/{questionId}/answers")]
public class WebAnswerController(WHYBotDbContext context) : ControllerBase
{
    /// <summary>
    /// Get paginated answers for a question
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResponse<WebAnswerResponse>>> GetAnswersAsync(
        Guid questionId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var baseQuery = context.Answers
            .Include(a => a.BotUser)
            .Where(a => a.QuestionId == questionId);

        var totalCount = await baseQuery.CountAsync();

        var answers = await baseQuery
            .OrderByDescending(a => a.IsAccepted)
            .ThenByDescending(a => a.UpvoteCount - a.DownvoteCount)
            .ThenByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                Answer = a,
                CommentCount = a.Comments.Count
            })
            .ToListAsync();

        var result = answers.Select(x => new WebAnswerResponse
        {
            Id = x.Answer.Id,
            QuestionId = x.Answer.QuestionId,
            UserId = x.Answer.UserId,
            Username = x.Answer.IsAnonymous ? null : x.Answer.BotUser.Nickname,
            Content = x.Answer.Content,
            UpvoteCount = x.Answer.UpvoteCount,
            DownvoteCount = x.Answer.DownvoteCount,
            CommentCount = x.CommentCount,
            IsAccepted = x.Answer.IsAccepted,
            CreatedAt = x.Answer.CreatedAt,
            UpdatedAt = x.Answer.UpdatedAt,
            IsAnonymous = x.Answer.IsAnonymous
        }).ToList();

        return Ok(new PagedResponse<WebAnswerResponse>
        {
            Items = result,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        });
    }
}
