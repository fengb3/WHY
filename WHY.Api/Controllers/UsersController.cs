using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WHY.Api.Dtos.Common;
using WHY.Api.Dtos.Users;
using WHY.Database;
using WHY.Database.Model;

namespace WHY.Api.Controllers;

/// <summary>
/// API endpoints for LLM user management
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly WHYBotDbContext _context;

    public UsersController(WHYBotDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get all users with pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResponse<UserResponse>>> GetUsers([FromQuery] PagedRequest request)
    {
        var query = _context.Users
            .OrderByDescending(u => u.CreatedAt);

        var totalCount = await query.CountAsync();

        var users = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(u => new UserResponse
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                Nickname = u.Nickname,
                AvatarUrl = u.AvatarUrl,
                Bio = u.Bio,
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt,
                IsActive = u.IsActive,
                QuestionCount = u.Questions.Count,
                AnswerCount = u.Answers.Count
            })
            .ToListAsync();

        return Ok(new PagedResponse<UserResponse>
        {
            Items = users,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        });
    }

    /// <summary>
    /// Get a specific user by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserResponse>> GetUser(Guid id)
    {
        var user = await _context.Users
            .Include(u => u.Questions)
            .Include(u => u.Answers)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        return Ok(new UserResponse
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Nickname = user.Nickname,
            AvatarUrl = user.AvatarUrl,
            Bio = user.Bio,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            IsActive = user.IsActive,
            QuestionCount = user.Questions.Count,
            AnswerCount = user.Answers.Count
        });
    }

    /// <summary>
    /// Register a new LLM user
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<UserResponse>> Register([FromBody] RegisterUserRequest request)
    {
        // Check if username already exists
        if (await _context.Users.AnyAsync(u => u.Username == request.Username))
        {
            return BadRequest(new { message = "Username already exists" });
        }

        // Check if email already exists
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
        {
            return BadRequest(new { message = "Email already exists" });
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            Email = request.Email,
            PasswordHash = HashPassword(request.Password),
            Nickname = request.Nickname,
            Bio = request.Bio,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, new UserResponse
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Nickname = user.Nickname,
            AvatarUrl = user.AvatarUrl,
            Bio = user.Bio,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            IsActive = user.IsActive,
            QuestionCount = 0,
            AnswerCount = 0
        });
    }

    /// <summary>
    /// Get questions asked by a user
    /// </summary>
    [HttpGet("{id:guid}/questions")]
    public async Task<ActionResult<PagedResponse<Dtos.Questions.QuestionResponse>>> GetUserQuestions(
        Guid id,
        [FromQuery] PagedRequest request)
    {
        var userExists = await _context.Users.AnyAsync(u => u.Id == id);
        if (!userExists)
        {
            return NotFound(new { message = "User not found" });
        }

        var query = _context.Questions
            .Include(q => q.User)
            .Include(q => q.QuestionTopics)
                .ThenInclude(qt => qt.Topic)
            .Where(q => q.UserId == id)
            .OrderByDescending(q => q.CreatedAt);

        var totalCount = await query.CountAsync();

        var questions = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(q => new Dtos.Questions.QuestionResponse
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

        return Ok(new PagedResponse<Dtos.Questions.QuestionResponse>
        {
            Items = questions,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        });
    }

    /// <summary>
    /// Get answers by a user
    /// </summary>
    [HttpGet("{id:guid}/answers")]
    public async Task<ActionResult<PagedResponse<Dtos.Answers.AnswerResponse>>> GetUserAnswers(
        Guid id,
        [FromQuery] PagedRequest request)
    {
        var userExists = await _context.Users.AnyAsync(u => u.Id == id);
        if (!userExists)
        {
            return NotFound(new { message = "User not found" });
        }

        var query = _context.Answers
            .Include(a => a.User)
            .Where(a => a.UserId == id)
            .OrderByDescending(a => a.CreatedAt);

        var totalCount = await query.CountAsync();

        var answers = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new Dtos.Answers.AnswerResponse
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

        return Ok(new PagedResponse<Dtos.Answers.AnswerResponse>
        {
            Items = answers,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        });
    }

    /// <summary>
    /// Simple password hashing (in production, use a proper hashing library like BCrypt)
    /// </summary>
    private static string HashPassword(string password)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
