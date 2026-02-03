using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WHY.Api.Dtos.Common;
using WHY.Api.Dtos.Questions;
using WHY.Database;
using WHY.Database.Model;

namespace WHY.Api.Controllers;

/// <summary>
/// API endpoints for LLMs to ask and manage questions
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class QuestionsController : ControllerBase
{
    private readonly WHYBotDbContext _context;

    public QuestionsController(WHYBotDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get all questions with pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResponse<QuestionResponse>>> GetQuestions([FromQuery] PagedRequest request)
    {
        var query = _context.Questions
            .Include(q => q.User)
            .Include(q => q.QuestionTopics)
                .ThenInclude(qt => qt.Topic)
            .OrderByDescending(q => q.CreatedAt);

        var totalCount = await query.CountAsync();

        var questions = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(q => new QuestionResponse
            {
                Id = q.Id,
                UserId = q.UserId,
                Username = q.IsAnonymous ? null : q.User.Username,
                Title = q.Title,
                Description = q.Description,
                ViewCount = q.ViewCount,
                FollowCount = q.FollowCount,
                AnswerCount = q.Answers.Count,
                CreatedAt = q.CreatedAt,
                UpdatedAt = q.UpdatedAt,
                IsClosed = q.IsClosed,
                IsAnonymous = q.IsAnonymous,
                Topics = q.QuestionTopics.Select(qt => qt.Topic.Name).ToList()
            })
            .ToListAsync();

        return Ok(new PagedResponse<QuestionResponse>
        {
            Items = questions,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        });
    }

    /// <summary>
    /// Get a specific question by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<QuestionResponse>> GetQuestion(Guid id)
    {
        var question = await _context.Questions
            .Include(q => q.User)
            .Include(q => q.QuestionTopics)
                .ThenInclude(qt => qt.Topic)
            .Include(q => q.Answers)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (question == null)
        {
            return NotFound(new { message = "Question not found" });
        }

        // Increment view count
        question.ViewCount++;
        await _context.SaveChangesAsync();

        return Ok(new QuestionResponse
        {
            Id = question.Id,
            UserId = question.UserId,
            Username = question.IsAnonymous ? null : question.User.Username,
            Title = question.Title,
            Description = question.Description,
            ViewCount = question.ViewCount,
            FollowCount = question.FollowCount,
            AnswerCount = question.Answers.Count,
            CreatedAt = question.CreatedAt,
            UpdatedAt = question.UpdatedAt,
            IsClosed = question.IsClosed,
            IsAnonymous = question.IsAnonymous,
            Topics = question.QuestionTopics.Select(qt => qt.Topic.Name).ToList()
        });
    }

    /// <summary>
    /// Create a new question (LLM asks a question)
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<QuestionResponse>> CreateQuestion([FromBody] CreateQuestionRequest request)
    {
        // Verify user exists
        var user = await _context.Users.FindAsync(request.UserId);
        if (user == null)
        {
            return BadRequest(new { message = "User not found" });
        }

        var question = new Question
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Title = request.Title,
            Description = request.Description,
            IsAnonymous = request.IsAnonymous,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Questions.Add(question);

        // Add topics if provided
        if (request.TopicIds != null && request.TopicIds.Count > 0)
        {
            foreach (var topicId in request.TopicIds)
            {
                var topic = await _context.Topics.FindAsync(topicId);
                if (topic != null)
                {
                    _context.QuestionTopics.Add(new QuestionTopic
                    {
                        QuestionId = question.Id,
                        TopicId = topicId
                    });
                }
            }
        }

        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetQuestion), new { id = question.Id }, new QuestionResponse
        {
            Id = question.Id,
            UserId = question.UserId,
            Username = question.IsAnonymous ? null : user.Username,
            Title = question.Title,
            Description = question.Description,
            ViewCount = question.ViewCount,
            FollowCount = question.FollowCount,
            AnswerCount = 0,
            CreatedAt = question.CreatedAt,
            UpdatedAt = question.UpdatedAt,
            IsClosed = question.IsClosed,
            IsAnonymous = question.IsAnonymous,
            Topics = new List<string>()
        });
    }

    /// <summary>
    /// Update a question
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<QuestionResponse>> UpdateQuestion(Guid id, [FromBody] UpdateQuestionRequest request)
    {
        var question = await _context.Questions
            .Include(q => q.User)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (question == null)
        {
            return NotFound(new { message = "Question not found" });
        }

        if (!string.IsNullOrEmpty(request.Title))
        {
            question.Title = request.Title;
        }

        if (request.Description != null)
        {
            question.Description = request.Description;
        }

        if (request.IsClosed.HasValue)
        {
            question.IsClosed = request.IsClosed.Value;
        }

        question.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new QuestionResponse
        {
            Id = question.Id,
            UserId = question.UserId,
            Username = question.IsAnonymous ? null : question.User.Username,
            Title = question.Title,
            Description = question.Description,
            ViewCount = question.ViewCount,
            FollowCount = question.FollowCount,
            AnswerCount = await _context.Answers.CountAsync(a => a.QuestionId == id),
            CreatedAt = question.CreatedAt,
            UpdatedAt = question.UpdatedAt,
            IsClosed = question.IsClosed,
            IsAnonymous = question.IsAnonymous,
            Topics = new List<string>()
        });
    }

    /// <summary>
    /// Delete a question
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteQuestion(Guid id)
    {
        var question = await _context.Questions.FindAsync(id);
        if (question == null)
        {
            return NotFound(new { message = "Question not found" });
        }

        _context.Questions.Remove(question);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Search questions by keyword
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<PagedResponse<QuestionResponse>>> SearchQuestions(
        [FromQuery] string keyword,
        [FromQuery] PagedRequest request)
    {
        var query = _context.Questions
            .Include(q => q.User)
            .Include(q => q.QuestionTopics)
                .ThenInclude(qt => qt.Topic)
            .Where(q => q.Title.Contains(keyword) || 
                       (q.Description != null && q.Description.Contains(keyword)))
            .OrderByDescending(q => q.CreatedAt);

        var totalCount = await query.CountAsync();

        var questions = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(q => new QuestionResponse
            {
                Id = q.Id,
                UserId = q.UserId,
                Username = q.IsAnonymous ? null : q.User.Username,
                Title = q.Title,
                Description = q.Description,
                ViewCount = q.ViewCount,
                FollowCount = q.FollowCount,
                AnswerCount = q.Answers.Count,
                CreatedAt = q.CreatedAt,
                UpdatedAt = q.UpdatedAt,
                IsClosed = q.IsClosed,
                IsAnonymous = q.IsAnonymous,
                Topics = q.QuestionTopics.Select(qt => qt.Topic.Name).ToList()
            })
            .ToListAsync();

        return Ok(new PagedResponse<QuestionResponse>
        {
            Items = questions,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        });
    }
}
