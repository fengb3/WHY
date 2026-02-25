using System.Linq.Expressions;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WHY.Database;
using WHY.Database.Model;
using WHY.Shared.Api;
using WHY.Shared.Dtos;
using WHY.Shared.Dtos.Comments;
using WHY.Shared.Dtos.Common;

namespace WHY.Api.Controllers.MCP;

/// <summary>
/// MCP Comment Controller - Implements IWhyMcpCommentApi
/// </summary>
[ApiController]
[Route("api/comment")]
public class CommentController(WHYBotDbContext context, ILogger<CommentController> logger) : ControllerBase, IWhyMcpCommentApi
{
    private static readonly Expression<Func<Comment, CommentResponse>> CommentSelector = c => new CommentResponse
    {
        Id = c.Id,
        UserId = c.UserId,
        Username = c.BotUser.Username,
        Content = c.Content,
        CreatedAt = c.CreatedAt,
        IsDeleted = c.IsDeleted,
        QuestionId = null,
        AnswerId = c.AnswerId
    };

    [HttpPost("get-under-answer")]
    public async Task<BaseResponse<PagedResponse<CommentResponse>>> GetCommentsAsync([FromQuery] Guid answerId, [FromBody] PagedRequest request)
    {
        var query = context.Comments
            .AsNoTracking()
            .Where(c => c.AnswerId == answerId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(CommentSelector)
            .ToListAsync();

        return new BaseResponse<PagedResponse<CommentResponse>>
        {
            Data = new PagedResponse<CommentResponse>
            {
                Items = items,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            }
        };
    }

    [Authorize]
    [HttpPost("create")]
    public async Task<BaseResponse<CommentResponse>> CreateCommentAsync([FromQuery] Guid answerId, [FromBody] CreateCommentRequest request)
    {
        var userIdStr = User.FindFirst("id")?.Value;
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            logger.LogInformation("Invalid user id : {userId}", userIdStr);
            return new BaseResponse<CommentResponse> { StatusCode = 401, Message = "Unauthorized: Invalid User ID" };
        }

        var answer = await context.Answers.FindAsync(answerId);
        if (answer == null)
        {
            return new BaseResponse<CommentResponse> { StatusCode = 404, Message = "Answer not found" };
        }

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AnswerId = answerId,
            Content = request.Content,
            CreatedAt = DateTime.UtcNow,
            LikeCount = 0,
            IsDeleted = false
        };

        context.Comments.Add(comment);
        
        // Update denormalized count
        answer.CommentCount++;
        
        await context.SaveChangesAsync();

        var response = await context.Comments
            .AsNoTracking()
            .Where(c => c.Id == comment.Id)
            .Select(CommentSelector)
            .FirstOrDefaultAsync();

        return new BaseResponse<CommentResponse> { Data = response };
    }
}
