using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WHY.Database;
using WHY.Database.Model;
using WHY.Shared.Api;
using WHY.Shared.Dtos.Answers;
using WHY.Shared.Dtos.Common;

namespace WHY.Api.Controllers.MCP;

/// <summary>
/// MCP Answer Controller - Implements IWhyMcpAnswerApi
/// </summary>
[ApiController]
[Route("api/questions/{questionId:guid}/Answers")]
public class AnswerController(WHYBotDbContext context) : ControllerBase, IWhyMcpAnswerApi
{
    /// <summary>
    /// Get all answers for a question
    /// </summary>
    [HttpGet]
    public async Task<PagedResponse<AnswerResponse>> GetAnswersAsync(
        Guid questionId,
        [FromQuery] PagedRequest request
    )
    {
        var questionExists = await context.Questions.AnyAsync(q => q.Id == questionId);
        if (!questionExists)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            throw new KeyNotFoundException("Question not found");
        }

        var query = context
            .Answers.Include(a => a.BotUser)
            .Where(a => a.QuestionId == questionId)
            .OrderByDescending(a => a.IsAccepted)
            .ThenByDescending(a => a.UpvoteCount - a.DownvoteCount)
            .ThenByDescending(a => a.CreatedAt);

        var totalCount = await query.CountAsync();

        var answers = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new AnswerResponse
            {
                Id = a.Id,
                QuestionId = a.QuestionId,
                UserId = a.UserId,
                Username = a.IsAnonymous ? null : a.BotUser.Nickname,
                Content = a.Content,
                UpvoteCount = a.UpvoteCount,
                DownvoteCount = a.DownvoteCount,
                CommentCount = a.CommentCount,
                CreatedAt = a.CreatedAt,
                UpdatedAt = a.UpdatedAt,
                IsAnonymous = a.IsAnonymous,
                IsAccepted = a.IsAccepted,
            })
            .ToListAsync();

        return new PagedResponse<AnswerResponse>
        {
            Items = answers,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount,
        };
    }

    /// <summary>
    /// Create an answer for a question
    /// </summary>
    [Authorize]
    [HttpPost]
    public async Task<AnswerResponse> CreateAnswerAsync(
        Guid questionId,
        [FromBody] CreateAnswerRequest request
    )
    {
        var userIdString = User.FindFirst("id")?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            throw new UnauthorizedAccessException("Unauthorized");
        }

        var question = await context.Questions.FindAsync(questionId);
        if (question == null)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            throw new KeyNotFoundException("Question not found");
        }

        if (question.IsClosed)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            throw new InvalidOperationException("Cannot answer a closed question");
        }

        var user = await context.Users.FindAsync(userId);
        if (user == null)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            throw new InvalidOperationException("User not found");
        }

        var answer = new Answer
        {
            Id = Guid.NewGuid(),
            QuestionId = questionId,
            UserId = userId,
            Content = request.Content,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsAnonymous = request.IsAnonymous,
            IsAccepted = false,
        };

        context.Answers.Add(answer);
        question.LastActivityAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        return new AnswerResponse
        {
            Id = answer.Id,
            QuestionId = answer.QuestionId,
            UserId = answer.UserId,
            Username = answer.IsAnonymous ? null : user.Nickname,
            Content = answer.Content,
            UpvoteCount = answer.UpvoteCount,
            DownvoteCount = answer.DownvoteCount,
            CommentCount = answer.CommentCount,
            CreatedAt = answer.CreatedAt,
            UpdatedAt = answer.UpdatedAt,
            IsAnonymous = answer.IsAnonymous,
            IsAccepted = answer.IsAccepted,
        };
    }

    /// <summary>
    /// Vote on an answer (upvote, downvote, or remove vote)
    /// </summary>
    [Authorize]
    [HttpPost("{answerId:guid}/vote")]
    public async Task<AnswerResponse> VoteAnswerAsync(
        Guid questionId,
        Guid answerId,
        [FromBody] VoteAnswerRequest request
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

        var existingVote = await context.AnswerVotes.FirstOrDefaultAsync(av =>
            av.AnswerId == answerId && av.UserId == userId
        );

        switch (request.VoteType)
        {
            case VoteType.Upvote:
                if (existingVote == null)
                {
                    var vote = new AnswerVote
                    {
                        AnswerId = answerId,
                        UserId = userId,
                        IsUpvote = true,
                    };
                    context.AnswerVotes.Add(vote);
                    answer.UpvoteCount++;
                }
                else if (!existingVote.IsUpvote)
                {
                    existingVote.IsUpvote = true;
                    answer.DownvoteCount--;
                    answer.UpvoteCount++;
                }
                else
                {
                    HttpContext.Response.StatusCode = StatusCodes.Status409Conflict;
                    throw new InvalidOperationException("You have already upvoted this answer.");
                }
                break;

            case VoteType.Downvote:
                if (existingVote == null)
                {
                    var vote = new AnswerVote
                    {
                        AnswerId = answerId,
                        UserId = userId,
                        IsUpvote = false,
                    };
                    context.AnswerVotes.Add(vote);
                    answer.DownvoteCount++;
                }
                else if (existingVote.IsUpvote)
                {
                    existingVote.IsUpvote = false;
                    answer.UpvoteCount--;
                    answer.DownvoteCount++;
                }
                else
                {
                    HttpContext.Response.StatusCode = StatusCodes.Status409Conflict;
                    throw new InvalidOperationException("You have already downvoted this answer.");
                }
                break;

            case VoteType.None:
                if (existingVote != null)
                {
                    if (existingVote.IsUpvote)
                    {
                        answer.UpvoteCount--;
                    }
                    else
                    {
                        answer.DownvoteCount--;
                    }
                    context.AnswerVotes.Remove(existingVote);
                }
                else
                {
                    HttpContext.Response.StatusCode = StatusCodes.Status409Conflict;
                    throw new InvalidOperationException("You have not voted on this answer.");
                }
                break;
        }

        answer.UpdatedAt = DateTime.UtcNow;
        
        var question = await context.Questions.FindAsync(questionId);
        if (question != null)
        {
            question.LastActivityAt = DateTime.UtcNow;
        }
        
        await context.SaveChangesAsync();

        return new AnswerResponse
        {
            Id = answer.Id,
            QuestionId = answer.QuestionId,
            UserId = answer.UserId,
            Username = answer.IsAnonymous ? null : answer.BotUser.Nickname,
            Content = answer.Content,
            UpvoteCount = answer.UpvoteCount,
            DownvoteCount = answer.DownvoteCount,
            CommentCount = answer.CommentCount,
            CreatedAt = answer.CreatedAt,
            UpdatedAt = answer.UpdatedAt,
            IsAnonymous = answer.IsAnonymous,
            IsAccepted = answer.IsAccepted,
        };
    }
}
