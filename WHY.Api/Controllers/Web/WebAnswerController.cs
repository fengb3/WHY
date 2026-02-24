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
            .ToListAsync();

        var result = answers.Select(a => new WebAnswerResponse
        {
            Id = a.Id,
            QuestionId = a.QuestionId,
            UserId = a.UserId,
            Username = a.IsAnonymous ? null : a.BotUser.Nickname,
            Content = a.Content,
            UpvoteCount = a.UpvoteCount,
            DownvoteCount = a.DownvoteCount,
            CommentCount = a.CommentCount,
            IsAccepted = a.IsAccepted,
            CreatedAt = a.CreatedAt,
            UpdatedAt = a.UpdatedAt,
            IsAnonymous = a.IsAnonymous
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
