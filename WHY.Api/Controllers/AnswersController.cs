using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
public class AnswersController : ControllerBase
{
    private readonly WHYBotDbContext _context;

    public AnswersController(WHYBotDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get all answers for a question
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResponse<AnswerResponse>>> GetAnswers(
        Guid questionId,
        [FromQuery] PagedRequest request)
    {
        var questionExists = await _context.Questions.AnyAsync(q => q.Id == questionId);
        if (!questionExists)
        {
            return NotFound(new { message = "Question not found" });
        }

        var query = _context.Answers
            .Include(a => a.User)
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
                Username = a.IsAnonymous ? null : a.User.Username,
                Content = a.Content,
                UpvoteCount = a.UpvoteCount,
                DownvoteCount = a.DownvoteCount,
                CommentCount = a.CommentCount,
                CreatedAt = a.CreatedAt,
                UpdatedAt = a.UpdatedAt,
                IsAnonymous = a.IsAnonymous,
                IsAccepted = a.IsAccepted
            })
            .ToListAsync();

        return Ok(new PagedResponse<AnswerResponse>
        {
            Items = answers,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        });
    }

    /// <summary>
    /// Get a specific answer
    /// </summary>
    [HttpGet("{answerId:guid}")]
    public async Task<ActionResult<AnswerResponse>> GetAnswer(Guid questionId, Guid answerId)
    {
        var answer = await _context.Answers
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == answerId && a.QuestionId == questionId);

        if (answer == null)
        {
            return NotFound(new { message = "Answer not found" });
        }

        return Ok(new AnswerResponse
        {
            Id = answer.Id,
            QuestionId = answer.QuestionId,
            UserId = answer.UserId,
            Username = answer.IsAnonymous ? null : answer.User.Username,
            Content = answer.Content,
            UpvoteCount = answer.UpvoteCount,
            DownvoteCount = answer.DownvoteCount,
            CommentCount = answer.CommentCount,
            CreatedAt = answer.CreatedAt,
            UpdatedAt = answer.UpdatedAt,
            IsAnonymous = answer.IsAnonymous,
            IsAccepted = answer.IsAccepted
        });
    }

    /// <summary>
    /// Create an answer (LLM answers a question)
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<AnswerResponse>> CreateAnswer(
        Guid questionId,
        [FromBody] CreateAnswerRequest request)
    {
        var question = await _context.Questions.FindAsync(questionId);
        if (question == null)
        {
            return NotFound(new { message = "Question not found" });
        }

        if (question.IsClosed)
        {
            return BadRequest(new { message = "Cannot answer a closed question" });
        }

        var user = await _context.Users.FindAsync(request.UserId);
        if (user == null)
        {
            return BadRequest(new { message = "User not found" });
        }

        var answer = new Answer
        {
            Id = Guid.NewGuid(),
            QuestionId = questionId,
            UserId = request.UserId,
            Content = request.Content,
            IsAnonymous = request.IsAnonymous,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Answers.Add(answer);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAnswer), new { questionId, answerId = answer.Id }, new AnswerResponse
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
            IsAccepted = answer.IsAccepted
        });
    }

    /// <summary>
    /// Update an answer
    /// </summary>
    [HttpPut("{answerId:guid}")]
    public async Task<ActionResult<AnswerResponse>> UpdateAnswer(
        Guid questionId,
        Guid answerId,
        [FromBody] UpdateAnswerRequest request)
    {
        var answer = await _context.Answers
            .Include(a => a.User)
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
        await _context.SaveChangesAsync();

        return Ok(new AnswerResponse
        {
            Id = answer.Id,
            QuestionId = answer.QuestionId,
            UserId = answer.UserId,
            Username = answer.IsAnonymous ? null : answer.User.Username,
            Content = answer.Content,
            UpvoteCount = answer.UpvoteCount,
            DownvoteCount = answer.DownvoteCount,
            CommentCount = answer.CommentCount,
            CreatedAt = answer.CreatedAt,
            UpdatedAt = answer.UpdatedAt,
            IsAnonymous = answer.IsAnonymous,
            IsAccepted = answer.IsAccepted
        });
    }

    /// <summary>
    /// Delete an answer
    /// </summary>
    [HttpDelete("{answerId:guid}")]
    public async Task<IActionResult> DeleteAnswer(Guid questionId, Guid answerId)
    {
        var answer = await _context.Answers
            .FirstOrDefaultAsync(a => a.Id == answerId && a.QuestionId == questionId);

        if (answer == null)
        {
            return NotFound(new { message = "Answer not found" });
        }

        _context.Answers.Remove(answer);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Upvote an answer
    /// </summary>
    [HttpPost("{answerId:guid}/upvote")]
    public async Task<ActionResult<AnswerResponse>> UpvoteAnswer(Guid questionId, Guid answerId)
    {
        var answer = await _context.Answers
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == answerId && a.QuestionId == questionId);

        if (answer == null)
        {
            return NotFound(new { message = "Answer not found" });
        }

        answer.UpvoteCount++;
        await _context.SaveChangesAsync();

        return Ok(new AnswerResponse
        {
            Id = answer.Id,
            QuestionId = answer.QuestionId,
            UserId = answer.UserId,
            Username = answer.IsAnonymous ? null : answer.User.Username,
            Content = answer.Content,
            UpvoteCount = answer.UpvoteCount,
            DownvoteCount = answer.DownvoteCount,
            CommentCount = answer.CommentCount,
            CreatedAt = answer.CreatedAt,
            UpdatedAt = answer.UpdatedAt,
            IsAnonymous = answer.IsAnonymous,
            IsAccepted = answer.IsAccepted
        });
    }

    /// <summary>
    /// Downvote an answer
    /// </summary>
    [HttpPost("{answerId:guid}/downvote")]
    public async Task<ActionResult<AnswerResponse>> DownvoteAnswer(Guid questionId, Guid answerId)
    {
        var answer = await _context.Answers
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == answerId && a.QuestionId == questionId);

        if (answer == null)
        {
            return NotFound(new { message = "Answer not found" });
        }

        answer.DownvoteCount++;
        await _context.SaveChangesAsync();

        return Ok(new AnswerResponse
        {
            Id = answer.Id,
            QuestionId = answer.QuestionId,
            UserId = answer.UserId,
            Username = answer.IsAnonymous ? null : answer.User.Username,
            Content = answer.Content,
            UpvoteCount = answer.UpvoteCount,
            DownvoteCount = answer.DownvoteCount,
            CommentCount = answer.CommentCount,
            CreatedAt = answer.CreatedAt,
            UpdatedAt = answer.UpdatedAt,
            IsAnonymous = answer.IsAnonymous,
            IsAccepted = answer.IsAccepted
        });
    }

    /// <summary>
    /// Accept an answer (mark as best answer)
    /// </summary>
    [HttpPost("{answerId:guid}/accept")]
    public async Task<ActionResult<AnswerResponse>> AcceptAnswer(Guid questionId, Guid answerId)
    {
        var answer = await _context.Answers
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == answerId && a.QuestionId == questionId);

        if (answer == null)
        {
            return NotFound(new { message = "Answer not found" });
        }

        // Unaccept any previously accepted answer
        var previouslyAccepted = await _context.Answers
            .Where(a => a.QuestionId == questionId && a.IsAccepted)
            .ToListAsync();

        foreach (var prev in previouslyAccepted)
        {
            prev.IsAccepted = false;
        }

        answer.IsAccepted = true;
        await _context.SaveChangesAsync();

        return Ok(new AnswerResponse
        {
            Id = answer.Id,
            QuestionId = answer.QuestionId,
            UserId = answer.UserId,
            Username = answer.IsAnonymous ? null : answer.User.Username,
            Content = answer.Content,
            UpvoteCount = answer.UpvoteCount,
            DownvoteCount = answer.DownvoteCount,
            CommentCount = answer.CommentCount,
            CreatedAt = answer.CreatedAt,
            UpdatedAt = answer.UpdatedAt,
            IsAnonymous = answer.IsAnonymous,
            IsAccepted = answer.IsAccepted
        });
    }
}
