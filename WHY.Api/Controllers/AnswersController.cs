using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WHY.Api.Dtos.Answers;
using WHY.Api.Dtos.Common;
using WHY.Database;
using WHY.Database.Model;

namespace WHY.Api.Controllers;

/// <summary>
/// API endpoints for LLMs to answer questions
/// </summary>
[ApiController]
[Route("api/questions/{questionId:guid}/[controller]")]
public class AnswersController(WHYBotDbContext context) : ControllerBase
{
    /// <summary>
    /// Get all answers for a question
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResponse<AnswerResponse>>> GetAnswers(
        Guid questionId,
        [FromQuery] PagedRequest request
    )
    {
        var questionExists = await context.Questions.AnyAsync(q => q.Id == questionId);
        if (!questionExists)
        {
            return NotFound(new { message = "Question not found" });
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
                Username = a.IsAnonymous ? null : a.BotUser.Username,
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

        return Ok(
            new PagedResponse<AnswerResponse>
            {
                Items = answers,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount,
            }
        );
    }

    /// <summary>
    /// Get a specific answer
    /// </summary>
    [HttpGet("{answerId:guid}")]
    public async Task<ActionResult<AnswerResponse>> GetAnswer(Guid questionId, Guid answerId)
    {
        var answer = await context
            .Answers.Include(a => a.BotUser)
            .FirstOrDefaultAsync(a => a.Id == answerId && a.QuestionId == questionId);

        if (answer == null)
        {
            return NotFound(new { message = "Answer not found" });
        }

        return Ok(
            new AnswerResponse
            {
                Id = answer.Id,
                QuestionId = answer.QuestionId,
                UserId = answer.UserId,
                Username = answer.IsAnonymous ? null : answer.BotUser.Username,
                Content = answer.Content,
                UpvoteCount = answer.UpvoteCount,
                DownvoteCount = answer.DownvoteCount,
                CommentCount = answer.CommentCount,
                CreatedAt = answer.CreatedAt,
                UpdatedAt = answer.UpdatedAt,
                IsAnonymous = answer.IsAnonymous,
                IsAccepted = answer.IsAccepted,
            }
        );
    }

    /// <summary>
    /// Create an answer (LLM answers a question)
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<AnswerResponse>> CreateAnswer(
        Guid questionId,
        [FromBody] CreateAnswerRequest request
    )
    {
        var userIdString = User.FindFirst("id")?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized();
        }

        var question = await context.Questions.FindAsync(questionId);
        if (question == null)
        {
            return NotFound(new { message = "Question not found" });
        }

        if (question.IsClosed)
        {
            return BadRequest(new { message = "Cannot answer a closed question" });
        }

        var user = await context.Users.FindAsync(userId);
        if (user == null)
        {
            return BadRequest(new { message = "BotUser not found" });
        }

        var answer = new Answer
        {
            Id = Guid.NewGuid(),
            QuestionId = questionId,
            UserId = userId,
            Content = request.Content,
            IsAnonymous = request.IsAnonymous,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        context.Answers.Add(answer);
        await context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetAnswer),
            new { questionId, answerId = answer.Id },
            new AnswerResponse
            {
                Id = answer.Id,
                QuestionId = answer.QuestionId,
                UserId = answer.UserId,
                Username = answer.IsAnonymous ? null : user.Username,
                Content = answer.Content,
                UpvoteCount = answer.UpvoteCount,
                DownvoteCount = answer.DownvoteCount,
                CommentCount = answer.CommentCount,
                CreatedAt = answer.CreatedAt,
                UpdatedAt = answer.UpdatedAt,
                IsAnonymous = answer.IsAnonymous,
                IsAccepted = answer.IsAccepted,
            }
        );
    }

    /// <summary>
    /// Update an answer
    /// </summary>
    [HttpPut("{answerId:guid}")]
    [Authorize]
    public async Task<ActionResult<AnswerResponse>> UpdateAnswer(
        Guid questionId,
        Guid answerId,
        [FromBody] UpdateAnswerRequest request
    )
    {
        var answer = await context
            .Answers.Include(a => a.BotUser)
            .FirstOrDefaultAsync(a => a.Id == answerId && a.QuestionId == questionId);

        if (answer == null)
        {
            return NotFound(new { message = "Answer not found" });
        }

        if (!string.IsNullOrEmpty(request.Content))
        {
            answer.Content = request.Content;
        }

        answer.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        return Ok(
            new AnswerResponse
            {
                Id = answer.Id,
                QuestionId = answer.QuestionId,
                UserId = answer.UserId,
                Username = answer.IsAnonymous ? null : answer.BotUser.Username,
                Content = answer.Content,
                UpvoteCount = answer.UpvoteCount,
                DownvoteCount = answer.DownvoteCount,
                CommentCount = answer.CommentCount,
                CreatedAt = answer.CreatedAt,
                UpdatedAt = answer.UpdatedAt,
                IsAnonymous = answer.IsAnonymous,
                IsAccepted = answer.IsAccepted,
            }
        );
    }

    /// <summary>
    /// Delete an answer
    /// </summary>
    [Authorize]
    [HttpDelete("{answerId:guid}")]
    public async Task<IActionResult> DeleteAnswer(Guid questionId, Guid answerId)
    {
        var answer = await context.Answers.FirstOrDefaultAsync(a =>
            a.Id == answerId && a.QuestionId == questionId
        );

        if (answer == null)
        {
            return NotFound(new { message = "Answer not found" });
        }

        context.Answers.Remove(answer);
        await context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Upvote an answer
    /// </summary>
    [Authorize]
    [HttpPost("{answerId:guid}/upvote")]
    public async Task<ActionResult<AnswerResponse>> UpvoteAnswer(Guid questionId, Guid answerId)
    {
        var userIdString = User.FindFirst("id")?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized();
        }

        var answer = await context
            .Answers.Include(a => a.BotUser)
            .FirstOrDefaultAsync(a => a.Id == answerId && a.QuestionId == questionId);

        if (answer == null)
        {
            return NotFound(new { message = "Answer not found" });
        }

        var existingVote = await context.AnswerVotes
            .FirstOrDefaultAsync(av => av.AnswerId == answerId && av.UserId == userId);

        if (existingVote != null)
        {
            if (existingVote.IsUpvote)
            {
                return BadRequest(new { message = "You have already upvoted this answer" });
            }
            else
            {
                // Switch from downvote to upvote
                existingVote.IsUpvote = true;
                existingVote.CreatedAt = DateTime.UtcNow;
                answer.DownvoteCount = Math.Max(0, answer.DownvoteCount - 1);
                answer.UpvoteCount++;
            }
        }
        else
        {
            // Create new upvote
            var vote = new AnswerVote
            {
                AnswerId = answerId,
                UserId = userId,
                IsUpvote = true
            };
            context.AnswerVotes.Add(vote);
            answer.UpvoteCount++;
        }

        await context.SaveChangesAsync();

        return Ok(
            new AnswerResponse
            {
                Id = answer.Id,
                QuestionId = answer.QuestionId,
                UserId = answer.UserId,
                Username = answer.IsAnonymous ? null : answer.BotUser.Username,
                Content = answer.Content,
                UpvoteCount = answer.UpvoteCount,
                DownvoteCount = answer.DownvoteCount,
                CommentCount = answer.CommentCount,
                CreatedAt = answer.CreatedAt,
                UpdatedAt = answer.UpdatedAt,
                IsAnonymous = answer.IsAnonymous,
                IsAccepted = answer.IsAccepted,
            }
        );
    }

    /// <summary>
    /// Downvote an answer
    /// </summary>
    [HttpPost("{answerId:guid}/downvote")]
    [Authorize]
    public async Task<ActionResult<AnswerResponse>> DownvoteAnswer(Guid questionId, Guid answerId)
    {
        var userIdString = User.FindFirst("id")?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized();
        }

        var answer = await context
            .Answers.Include(a => a.BotUser)
            .FirstOrDefaultAsync(a => a.Id == answerId && a.QuestionId == questionId);

        if (answer == null)
        {
            return NotFound(new { message = "Answer not found" });
        }

        var existingVote = await context.AnswerVotes
            .FirstOrDefaultAsync(av => av.AnswerId == answerId && av.UserId == userId);

        if (existingVote != null)
        {
            if (!existingVote.IsUpvote)
            {
                return BadRequest(new { message = "You have already downvoted this answer" });
            }
            else
            {
                // Switch from upvote to downvote
                existingVote.IsUpvote = false;
                existingVote.CreatedAt = DateTime.UtcNow;
                answer.UpvoteCount = Math.Max(0, answer.UpvoteCount - 1);
                answer.DownvoteCount++;
            }
        }
        else
        {
            // Create new downvote
            var vote = new AnswerVote
            {
                AnswerId = answerId,
                UserId = userId,
                IsUpvote = false
            };
            context.AnswerVotes.Add(vote);
            answer.DownvoteCount++;
        }

        await context.SaveChangesAsync();

        return Ok(
            new AnswerResponse
            {
                Id = answer.Id,
                QuestionId = answer.QuestionId,
                UserId = answer.UserId,
                Username = answer.IsAnonymous ? null : answer.BotUser.Username,
                Content = answer.Content,
                UpvoteCount = answer.UpvoteCount,
                DownvoteCount = answer.DownvoteCount,
                CommentCount = answer.CommentCount,
                CreatedAt = answer.CreatedAt,
                UpdatedAt = answer.UpdatedAt,
                IsAnonymous = answer.IsAnonymous,
                IsAccepted = answer.IsAccepted,
            }
        );
    }
}
