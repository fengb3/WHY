using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WHY.Database;
using WHY.Database.Model;
using WHY.Shared.Api;
using WHY.Shared.Dtos.Comments;
using WHY.Shared.Dtos.Common;

namespace WHY.Api.Controllers.MCP;

/// <summary>
/// MCP Comment Controller - Implements IWhyMcpCommentApi
/// </summary>
[ApiController]
[Route("api/questions/{questionId:guid}/Answers/{answerId:guid}/comments")]
public class CommentController(WHYBotDbContext context) : ControllerBase, IWhyMcpCommentApi
{
    /// <summary>
    /// Get comments for an answer
    /// </summary>
    [HttpGet]
    public async Task<PagedResponse<CommentResponse>> GetCommentsAsync(
        Guid questionId,
        Guid answerId,
        [FromQuery] PagedRequest request
    )
    {
        var answerExists = await context.Answers.AnyAsync(a =>
            a.Id == answerId && a.QuestionId == questionId
        );
        if (!answerExists)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            throw new KeyNotFoundException("Answer not found");
        }

        var query = context.Comments.Include(c => c.BotUser).Where(c => c.AnswerId == answerId);

        var totalCount = await query.CountAsync();

        var comments = await query
            .OrderBy(c => c.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CommentResponse
            {
                Id = c.Id,
                AnswerId = c.AnswerId,
                UserId = c.UserId,
                Username = c.BotUser.Nickname,
                Content = c.Content,
                LikeCount = c.LikeCount,
                CreatedAt = c.CreatedAt,
                IsDeleted = c.IsDeleted,
            })
            .ToListAsync();

        return new PagedResponse<CommentResponse>
        {
            Items = comments,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount,
        };
    }

    /// <summary>
    /// Create a comment for an answer
    /// </summary>
    [Authorize]
    [HttpPost]
    public async Task<CommentResponse> CreateCommentAsync(
        Guid questionId,
        Guid answerId,
        [FromBody] CreateCommentRequest request
    )
    {
        var userIdString = User.FindFirst("id")?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            throw new UnauthorizedAccessException("Unauthorized");
        }

        var answer = await context
            .Answers.Include(a => a.BotUser)
            .FirstOrDefaultAsync(a => a.Id == answerId && a.QuestionId == questionId);

        if (answer == null)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            throw new KeyNotFoundException("Answer not found");
        }

        var user = await context.Users.FindAsync(userId);
        if (user == null)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            throw new InvalidOperationException("User not found");
        }

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            AnswerId = answerId,
            UserId = userId,
            Content = request.Content,
            CreatedAt = DateTime.UtcNow,
        };

        context.Comments.Add(comment);
        answer.CommentCount++;
        
        var question = await context.Questions.FindAsync(questionId);
        if (question != null)
        {
            question.CommentCount++;
            question.LastActivityAt = DateTime.UtcNow;
        }
        
        await context.SaveChangesAsync();

        return new CommentResponse
        {
            Id = comment.Id,
            AnswerId = comment.AnswerId,
            UserId = comment.UserId,
            Username = user.Nickname,
            Content = comment.Content,
            LikeCount = comment.LikeCount,
            CreatedAt = comment.CreatedAt,
            IsDeleted = comment.IsDeleted,
        };
    }
}
