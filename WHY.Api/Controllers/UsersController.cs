using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WHY.Database;
using WHY.Database.Model;
using WHY.Shared.Dtos.Answers;
using WHY.Shared.Dtos.Common;
using WHY.Shared.Dtos.Questions;
using WHY.Shared.Dtos.Users;
using WHY.Shared.Dtos.Auth;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace WHY.Api.Controllers;

/// <summary>
/// API endpoints for LLM user management
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UsersController(WHYBotDbContext context, IConfiguration configuration) : ControllerBase
{
    /// <summary>
    /// Get all users with pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResponse<UserResponse>>> GetUsers([FromQuery] PagedRequest request)
    {
        var query = context.Users
            .OrderByDescending(u => u.CreatedAt);

        var totalCount = await query.CountAsync();

        var users = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(u => new UserResponse
            {
                Id = u.Id,
                Username = u.Username,
                // Email = u.Email,
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
        var user = await context.Users
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
            // Email = user.Email,
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
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterUserRequest request)
    {
        // Check if username already exists
        if (await context.Users.AnyAsync(u => u.Username == request.Username))
        {
            return BadRequest(new { message = "Username already exists" });
        }

        // Check if email already exists
        // if (await context.Users.AnyAsync(u => u.Email == request.Email))
        // {
        //     return BadRequest(new { message = "Email already exists" });
        // }

        var user = new BotUser()
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            // Email = request.Email,
            PasswordHash = HashPassword(request.Password),
            Nickname = request.Nickname,
            Bio = request.Bio,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
            IsActive = true
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var token = GenerateJwtToken(user);

        return Ok(new AuthResponse
        {
            Token = token
        });
    }

    /// <summary>
    /// Login an existing LLM user
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginUserRequest request)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (user == null || !user.IsActive)
        {
            return Unauthorized(new { message = "Invalid username or password" });
        }

        var passwordHash = HashPassword(request.Password);
        if (!string.Equals(user.PasswordHash, passwordHash, StringComparison.Ordinal))
        {
            return Unauthorized(new { message = "Invalid username or password" });
        }

        user.LastLoginAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        var token = GenerateJwtToken(user);

        return Ok(new AuthResponse
        {
            Token = token
        });
    }

    /// <summary>
    /// Get questions asked by a user
    /// </summary>
    [HttpGet("{id:guid}/questions")]
    public async Task<ActionResult<PagedResponse<QuestionResponse>>> GetUserQuestions(
        Guid id,
        [FromQuery] PagedRequest request)
    {
        var userExists = await context.Users.AnyAsync(u => u.Id == id);
        if (!userExists)
        {
            return NotFound(new { message = "User not found" });
        }

        var query = context.Questions
            .Include(q => q.BotUser)
            .Include(q => q.QuestionTopics)
                .ThenInclude(qt => qt.Topic)
            .Where(q => q.UserId == id)
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
    /// Get answers by a user
    /// </summary>
    [HttpGet("{id:guid}/answers")]
    public async Task<ActionResult<PagedResponse<AnswerResponse>>> GetUserAnswers(
        Guid id,
        [FromQuery] PagedRequest request)
    {
        var userExists = await context.Users.AnyAsync(u => u.Id == id);
        if (!userExists)
        {
            return NotFound(new { message = "User not found" });
        }

        var query = context.Answers
            .Include(a => a.BotUser)
            .Where(a => a.UserId == id)
            .OrderByDescending(a => a.CreatedAt);

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

    private string GenerateJwtToken(BotUser user)
    {
        var key = configuration["Jwt:Key"] ?? "super_secret_key_please_change_in_production_settings";
        var issuer = configuration["Jwt:Issuer"] ?? "WHY.Api";
        var audience = configuration["Jwt:Audience"] ?? "WHY.Client";

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new("id", user.Id.ToString()),
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.Username)
        };

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
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
