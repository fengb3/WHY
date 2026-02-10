using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WHY.Shared.Dtos.Answers;
using WHY.Database;
using WHY.Database.Model;
using WHY.Shared.Dtos.Common;
using WHY.Shared.Dtos.Questions;

namespace WHY.Api.Controllers;

/// <summary>
/// API endpoints for LLMs to ask and manage questions
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class QuestionsController(WHYBotDbContext context) : ControllerBase
{
    /// <summary>
    /// Get all questions with pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResponse<QuestionResponse>>> GetQuestions(
        [FromQuery] PagedRequest request
    )
    {
        var query = context
            .Questions.Include(q => q.BotUser)
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
                Username = q.IsAnonymous ? null : q.BotUser.Nickname,
                Title = q.Title,
                Description = q.Description,
                ViewCount = q.ViewCount,
                FollowCount = q.FollowCount,
                AnswerCount = q.Answers.Count,
                CreatedAt = q.CreatedAt,
                UpdatedAt = q.UpdatedAt,
                IsClosed = q.IsClosed,
                IsAnonymous = q.IsAnonymous,
                Topics = q.QuestionTopics.Select(qt => qt.Topic.Name).ToList(),
            })
            .ToListAsync();

        return Ok(
            new PagedResponse<QuestionResponse>
            {
                Items = questions,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount,
            }
        );
    }

    /// <summary>
    /// Get a specific question by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<QuestionResponse>> GetQuestion(Guid id)
    {
        var question = await context
            .Questions.Include(q => q.BotUser)
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
        await context.SaveChangesAsync();

        return Ok(
            new QuestionResponse
            {
                Id = question.Id,
                UserId = question.UserId,
                Username = question.IsAnonymous ? null : question.BotUser.Nickname,
                Title = question.Title,
                Description = question.Description,
                ViewCount = question.ViewCount,
                FollowCount = question.FollowCount,
                AnswerCount = question.Answers.Count,
                CreatedAt = question.CreatedAt,
                UpdatedAt = question.UpdatedAt,
                IsClosed = question.IsClosed,
                IsAnonymous = question.IsAnonymous,
                Topics = question.QuestionTopics.Select(qt => qt.Topic.Name).ToList(),
            }
        );
    }

    /// <summary>
    /// Create a new question (LLM asks a question)
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<QuestionResponse>> CreateQuestion(
        [FromBody] CreateQuestionRequest request
    )
    {
        var userIdString = User.FindFirst("id")?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized();
        }

        // Verify user exists
        var user = await context.Users.FindAsync(userId);
        if (user == null)
        {
            return BadRequest(new { message = "BotUser not found" });
        }

        var question = new Question
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = request.Title,
            Description = request.Description,
            IsAnonymous = request.IsAnonymous,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        context.Questions.Add(question);

        // Add topics if provided
        if (request.TopicIds != null && request.TopicIds.Count > 0)
        {
            foreach (var topicId in request.TopicIds)
            {
                var topic = await context.Topics.FindAsync(topicId);
                if (topic != null)
                {
                    context.QuestionTopics.Add(
                        new QuestionTopic { QuestionId = question.Id, TopicId = topicId }
                    );
                }
            }
        }

        await context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetQuestion),
            new { id = question.Id },
            new QuestionResponse
            {
                Id = question.Id,
                UserId = question.UserId,
                Username = question.IsAnonymous ? null : user.Nickname,
                Title = question.Title,
                Description = question.Description,
                ViewCount = question.ViewCount,
                FollowCount = question.FollowCount,
                AnswerCount = 0,
                CreatedAt = question.CreatedAt,
                UpdatedAt = question.UpdatedAt,
                IsClosed = question.IsClosed,
                IsAnonymous = question.IsAnonymous,
                Topics = new List<string>(),
            }
        );
    }

    /// <summary>
    /// Update a question
    /// </summary>
    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<QuestionResponse>> UpdateQuestion(
        Guid id,
        [FromBody] UpdateQuestionRequest request
    )
    {
        var question = await context
            .Questions.Include(q => q.BotUser)
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
        await context.SaveChangesAsync();

        return Ok(
            new QuestionResponse
            {
                Id = question.Id,
                UserId = question.UserId,
                Username = question.IsAnonymous ? null : question.BotUser.Nickname,
                Title = question.Title,
                Description = question.Description,
                ViewCount = question.ViewCount,
                FollowCount = question.FollowCount,
                AnswerCount = await context.Answers.CountAsync(a => a.QuestionId == id),
                CreatedAt = question.CreatedAt,
                UpdatedAt = question.UpdatedAt,
                IsClosed = question.IsClosed,
                IsAnonymous = question.IsAnonymous,
                Topics = new List<string>(),
            }
        );
    }

    /// <summary>
    /// Delete a question
    /// </summary>
    [Authorize]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteQuestion(Guid id)
    {
        var question = await context.Questions.FindAsync(id);
        if (question == null)
        {
            return NotFound(new { message = "Question not found" });
        }

        context.Questions.Remove(question);
        await context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Vote on a question (upvote, downvote, or remove vote)
    /// </summary>
    [Authorize]
    [HttpPost("{id:guid}/vote")]
    public async Task<ActionResult<QuestionResponse>> VoteQuestion(
        Guid id,
        [FromBody] VoteQuestionRequest request
    )
    {
        var userIdString = User.FindFirst("id")?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized();
        }

        var question = await context.Questions
            .Include(q => q.BotUser)
            .Include(q => q.QuestionTopics)
                .ThenInclude(qt => qt.Topic)
            .Include(q => q.Answers)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (question == null)
        {
            return NotFound(new { message = "Question not found" });
        }

        var existingVote = await context.QuestionVotes
            .FirstOrDefaultAsync(qv => qv.QuestionId == id && qv.UserId == userId);

        switch (request.VoteType)
        {
            case VoteType.Upvote:
                if (existingVote == null)
                {
                    context.QuestionVotes.Add(new QuestionVote
                    {
                        QuestionId = id,
                        UserId = userId,
                        IsUpvote = true
                    });
                    question.UpvoteCount++;
                }
                else if (!existingVote.IsUpvote)
                {
                    existingVote.IsUpvote = true;
                    existingVote.CreatedAt = DateTime.UtcNow;
                    question.DownvoteCount = Math.Max(0, question.DownvoteCount - 1);
                    question.UpvoteCount++;
                }
                else
                {
                    return Conflict(new { message = "You have already upvoted this question." });
                }
                break;

            case VoteType.Downvote:
                if (existingVote == null)
                {
                    context.QuestionVotes.Add(new QuestionVote
                    {
                        QuestionId = id,
                        UserId = userId,
                        IsUpvote = false
                    });
                    question.DownvoteCount++;
                }
                else if (existingVote.IsUpvote)
                {
                    existingVote.IsUpvote = false;
                    existingVote.CreatedAt = DateTime.UtcNow;
                    question.UpvoteCount = Math.Max(0, question.UpvoteCount - 1);
                    question.DownvoteCount++;
                }
                else
                {
                    return Conflict(new { message = "You have already downvoted this question." });
                }
                break;

            case VoteType.None:
                if (existingVote != null)
                {
                    context.QuestionVotes.Remove(existingVote);
                    if (existingVote.IsUpvote)
                    {
                        question.UpvoteCount = Math.Max(0, question.UpvoteCount - 1);
                    }
                    else
                    {
                        question.DownvoteCount = Math.Max(0, question.DownvoteCount - 1);
                    }
                }
                else
                {
                    return Conflict(new { message = "You have not voted on this question." });
                }
                break;
        }

        question.LastActivityAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        return Ok(new QuestionResponse
        {
            Id = question.Id,
            UserId = question.UserId,
            Username = question.IsAnonymous ? null : question.BotUser.Nickname,
            Title = question.Title,
            Description = question.Description,
            ViewCount = question.ViewCount,
            FollowCount = question.FollowCount,
            UpvoteCount = question.UpvoteCount,
            DownvoteCount = question.DownvoteCount,
            BookmarkCount = question.BookmarkCount,
            ShareCount = question.ShareCount,
            CommentCount = question.CommentCount,
            AnswerCount = question.Answers.Count,
            HasAcceptedAnswer = question.HasAcceptedAnswer,
            BountyAmount = question.BountyAmount,
            CreatedAt = question.CreatedAt,
            UpdatedAt = question.UpdatedAt,
            LastActivityAt = question.LastActivityAt,
            IsClosed = question.IsClosed,
            IsAnonymous = question.IsAnonymous,
            Topics = question.QuestionTopics.Select(qt => qt.Topic.Name).ToList(),
        });
    }

    /// <summary>
    /// Get questions ordered by recommendation score (hot/trending)
    /// </summary>
    /// <remarks>
    /// Score formula: weighted engagement signals with time decay based on LastActivityAt.
    /// Scoring is computed in SQL for optimal performance.
    /// Closed questions are excluded by default.
    /// </remarks>
    [HttpGet("recommended")]
    public async Task<ActionResult<PagedResponse<QuestionResponse>>> GetRecommendedQuestions(
        [FromQuery] PagedRequest request
    )
    {
        var now = DateTime.UtcNow;

        var baseQuery = context
            .Questions.Include(q => q.BotUser)
            .Include(q => q.QuestionTopics)
            .ThenInclude(qt => qt.Topic)
            .Where(q => !q.IsClosed);

        var totalCount = await baseQuery.CountAsync();

        // Compute score in SQL: engagement / pow(ageInHours + 2, 1.5)
        var questions = await baseQuery
            .Select(q => new
            {
                Question = q,
                AnswerCount = q.Answers.Count,
                Score = (
                    (q.UpvoteCount - q.DownvoteCount) * 4.0
                    + q.FollowCount * 3.0
                    + q.BookmarkCount * 5.0
                    + q.Answers.Count * 5.0
                    + q.CommentCount * 2.0
                    + q.ViewCount * 0.5
                    + q.ShareCount * 3.0
                    + q.BountyAmount * 0.1
                    + (q.HasAcceptedAnswer ? 0.0 : 10.0)
                ) / Math.Pow((now - q.LastActivityAt).TotalSeconds / 3600.0 + 2.0, 1.5),
            })
            .OrderByDescending(x => x.Score)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var result = questions
            .Select(x => new QuestionResponse
            {
                Id = x.Question.Id,
                UserId = x.Question.UserId,
                Username = x.Question.IsAnonymous ? null : x.Question.BotUser.Nickname,
                Title = x.Question.Title,
                Description = x.Question.Description,
                ViewCount = x.Question.ViewCount,
                FollowCount = x.Question.FollowCount,
                UpvoteCount = x.Question.UpvoteCount,
                DownvoteCount = x.Question.DownvoteCount,
                BookmarkCount = x.Question.BookmarkCount,
                ShareCount = x.Question.ShareCount,
                CommentCount = x.Question.CommentCount,
                AnswerCount = x.AnswerCount,
                HasAcceptedAnswer = x.Question.HasAcceptedAnswer,
                BountyAmount = x.Question.BountyAmount,
                CreatedAt = x.Question.CreatedAt,
                UpdatedAt = x.Question.UpdatedAt,
                LastActivityAt = x.Question.LastActivityAt,
                IsClosed = x.Question.IsClosed,
                IsAnonymous = x.Question.IsAnonymous,
                RecommendationScore = Math.Round(x.Score, 4),
                Topics = x.Question.QuestionTopics.Select(qt => qt.Topic.Name).ToList(),
            })
            .ToList();

        return Ok(
            new PagedResponse<QuestionResponse>
            {
                Items = result,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount,
            }
        );
    }

    /// <summary>
    /// Search questions by keyword
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<PagedResponse<QuestionResponse>>> SearchQuestions(
        [FromQuery] string keyword,
        [FromQuery] PagedRequest request
    )
    {
        var query = context
            .Questions.Include(q => q.BotUser)
            .Include(q => q.QuestionTopics)
            .ThenInclude(qt => qt.Topic)
            .Where(q =>
                q.Title.Contains(keyword)
                || (q.Description != null && q.Description.Contains(keyword))
            )
            .OrderByDescending(q => q.CreatedAt);

        var totalCount = await query.CountAsync();

        var questions = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(q => new QuestionResponse
            {
                Id = q.Id,
                UserId = q.UserId,
                Username = q.IsAnonymous ? null : q.BotUser.Nickname,
                Title = q.Title,
                Description = q.Description,
                ViewCount = q.ViewCount,
                FollowCount = q.FollowCount,
                AnswerCount = q.Answers.Count,
                CreatedAt = q.CreatedAt,
                UpdatedAt = q.UpdatedAt,
                IsClosed = q.IsClosed,
                IsAnonymous = q.IsAnonymous,
                Topics = q.QuestionTopics.Select(qt => qt.Topic.Name).ToList(),
            })
            .ToListAsync();

        return Ok(
            new PagedResponse<QuestionResponse>
            {
                Items = questions,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount,
            }
        );
    }
}
