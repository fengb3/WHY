using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WHY.Database;
using WHY.Shared.Dtos.Comments;
using WHY.Shared.Dtos.Common;

namespace WHY.Api.Controllers.Web;

/// <summary>
/// Web Comment Controller - Dedicated endpoints for the UI
/// </summary>
[ApiController]
[Route("api/web/questions/{questionId}/answers/{answerId}/comments")]
public class WebCommentController(WHYBotDbContext context) : ControllerBase
{
    /// <summary>
    /// Get paginated comments for an answer
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResponse<CommentResponse>>> GetAnswerCommentsAsync(
        Guid questionId,
        Guid answerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        // Verify the answer belongs to the question
        var answerExists = await context.Answers
            .AnyAsync(a => a.Id == answerId && a.QuestionId == questionId);

        if (!answerExists)
        {
            return NotFound("Answer not found for the specified question.");
        }

        var baseQuery = context.Comments
            .Include(c => c.BotUser)
            .Where(c => c.AnswerId == answerId && !c.IsDeleted);

        var totalCount = await baseQuery.CountAsync();

        var comments = await baseQuery
            .OrderBy(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = comments.Select(c => new CommentResponse
        {
            Id = c.Id,
            AnswerId = c.AnswerId,
            UserId = c.UserId,
            Username = c.BotUser.Nickname,
            Content = c.Content,
            LikeCount = c.LikeCount,
            CreatedAt = c.CreatedAt,
            IsDeleted = c.IsDeleted
        }).ToList();

        return Ok(new PagedResponse<CommentResponse>
        {
            Items = result,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        });
    }
}
