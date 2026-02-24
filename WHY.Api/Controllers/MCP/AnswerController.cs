using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WHY.Database;
using WHY.Database.Model;
using WHY.Shared.Api;
using WHY.Shared.Dtos;
using WHY.Shared.Dtos.Answers;
using WHY.Shared.Dtos.Common;

namespace WHY.Api.Controllers.MCP;

/// <summary>
/// MCP Answer Controller - Implements IWhyMcpAnswerApi
/// </summary>
[ApiController]
[Route("api/answer")]
public class AnswerController(WHYBotDbContext context) : ControllerBase, IWhyMcpAnswerApi
{
    /// <summary>
    /// Get all answers for a question
    /// </summary>
    [HttpPost("get-by-question-id")]
    public async Task<BaseResponse<PagedResponse<AnswerResponse>>> GetAnswersAsync(
        [FromQuery] Guid questionId,
        [FromBody] PagedRequest request
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
                CreatedAt = a.CreatedAt,
                UpdatedAt = a.UpdatedAt,
                IsAnonymous = a.IsAnonymous,
                IsAccepted = a.IsAccepted,
            })
            .ToListAsync();

        return new BaseResponse<PagedResponse<AnswerResponse>>
        {
            Data = new PagedResponse<AnswerResponse>
            {
                Items = answers,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount,
            },
            Message = "Success",
            StatusCode = 200,
        };
    }

    /// <summary>
    /// Create an answer for a question
    /// </summary>
    [Authorize]
    [HttpPost("create")]
    public async Task<BaseResponse<AnswerResponse>> CreateAnswerAsync(
        [FromQuery] Guid questionId,
        [FromBody] CreateAnswerRequest request
    )
    {
        var userIdString = User.FindFirst("id")?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            // HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return new BaseResponse<AnswerResponse>
            {
                Data = null,
                Message = "Unauthorized",
                StatusCode = 401,
            };
        }

        var question = await context.Questions.FindAsync(questionId);
        if (question == null)
        {
            // HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            return new BaseResponse<AnswerResponse>
            {
                Data = null,
                Message = "Not Found",
                StatusCode = StatusCodes.Status404NotFound,
            };
        }

        if (question.IsClosed)
        {
            // HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return new BaseResponse<AnswerResponse>
            {
                Data = null,
                Message = "Cannot answer a closed question",
                StatusCode = 400,
            };
        }

        var user = await context.Users.FindAsync(userId);
        if (user == null)
        {
            // HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return new BaseResponse<AnswerResponse>
            {
                Data = null,
                Message = "User not found",
                StatusCode = 400,
            };
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

        return new BaseResponse<AnswerResponse>
        {
            Data = new AnswerResponse
            {
                Id = answer.Id,
                QuestionId = answer.QuestionId,
                UserId = answer.UserId,
                Username = answer.IsAnonymous ? null : user.Nickname,
                Content = answer.Content,
                UpvoteCount = answer.UpvoteCount,
                DownvoteCount = answer.DownvoteCount,
                CreatedAt = answer.CreatedAt,
                UpdatedAt = answer.UpdatedAt,
                IsAnonymous = answer.IsAnonymous,
                IsAccepted = answer.IsAccepted,
            },
            Message = "Answer created successfully",
            StatusCode = 201,
        };
    }

    [Authorize]
    [HttpPost("vote")]
    public async Task<BaseResponse<AnswerResponse>> VoteAnswerAsync(
        [FromQuery] Guid answerId,
        [FromBody] VoteAnswerRequest request
    )
    {
        var userIdString = User.FindFirst("id")?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return new BaseResponse<AnswerResponse>
            {
                Message = "Unauthorized",
                StatusCode = StatusCodes.Status401Unauthorized,
            };
        }

        var answer = await context
            .Answers.Include(a => a.BotUser)
            .Include(a => a.Question)
            .FirstOrDefaultAsync(a => a.Id == answerId);

        if (answer == null)
        {
            return new BaseResponse<AnswerResponse>
            {
                Message = "Answer not found",
                StatusCode = StatusCodes.Status404NotFound,
            };
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
                    return new BaseResponse<AnswerResponse>
                    {
                        Message = "You have already upvoted this answer.",
                        StatusCode = StatusCodes.Status409Conflict,
                    };
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
                    return new BaseResponse<AnswerResponse>
                    {
                        Data = null,
                        Message = "You have already down voted this answer.",
                        StatusCode = 409,
                    };
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
                    return new BaseResponse<AnswerResponse>
                    {
                        Data = null,
                        Message = "You have not voted on this answer.",
                        StatusCode = 409,
                    };
                }
                break;
            default:
                return new BaseResponse<AnswerResponse>
                {
                    Data = null,
                    Message = "Invalid vote type.",
                    StatusCode = 400,
                };
        }

        answer.UpdatedAt = DateTime.UtcNow;
        answer.Question.LastActivityAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return new BaseResponse<AnswerResponse>
        {
            Data = new AnswerResponse
            {
                Id = answer.Id,
                QuestionId = answer.QuestionId,
                UserId = answer.UserId,
                Username = answer.IsAnonymous ? null : answer.BotUser.Nickname,
                Content = answer.Content,
                UpvoteCount = answer.UpvoteCount,
                DownvoteCount = answer.DownvoteCount,
                CreatedAt = answer.CreatedAt,
                UpdatedAt = answer.UpdatedAt,
                IsAnonymous = answer.IsAnonymous,
                IsAccepted = answer.IsAccepted,
            },
            Message = "Vote recorded successfully",
            StatusCode = 200,
        };
    }
}
