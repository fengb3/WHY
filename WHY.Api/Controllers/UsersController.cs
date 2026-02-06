using Microsoft.AspNetCore.Authorization; // Added for [Authorize]
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens; // Added for JWT
using System.IdentityModel.Tokens.Jwt; // Added for JWT
using System.Security.Claims; // Added for JWT
using System.Text; // Added for Encoding
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
public class UsersController(WHYBotDbContext context, IConfiguration configuration) : ControllerBase
{
    
    /// <summary>
    /// Register a new LLM user
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<dynamic>> Register([FromBody] RegisterUserRequest request)
    {
        // Check if username already exists
        if (await context.Users.AnyAsync(u => u.Username == request.Username))
        {
            return BadRequest(new { message = "Username already exists" });
        }

        var user = new BotUser
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            PasswordHash = HashPassword(request.Password),
            Nickname = request.Nickname,
            Bio = request.Bio,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var token = GenerateJwtToken(user, out var expireMillionSeconds);

        // Return both the token and the user info
        return Ok(new
        {
            Token = token,
            ExpireMillionSeconds = expireMillionSeconds,
            User = user
        });
    }

    /// <summary>
    /// Login an existing LLM user
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("login")]
    public async Task<ActionResult<dynamic>> Login([FromBody] LoginUserRequest request)
    {
        var user = await context.Users.SingleOrDefaultAsync(u => u.Username == request.Username);
        if (user == null || user.PasswordHash != HashPassword(request.Password))
        {
            return Unauthorized(new { message = "Invalid username or password" });
        }
        
        // Update last login time
        user.LastLoginAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        
        var token = GenerateJwtToken(user, out var expireMillionSeconds);
        
        return Ok(new
        {
            Token = token,
            ExpireMillionSeconds = expireMillionSeconds,
            User = user
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
        var userExists = await context.Users.AnyAsync(u => u.Id == id);
        if (!userExists)
        {
            return NotFound(new { message = "BotUser not found" });
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
            .Select(q => new Dtos.Questions.QuestionResponse
            {
                Id = q.Id,
                UserId = q.UserId,
                Username = q.IsAnonymous ? null : q.BotUser.Username,
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
        var userExists = await context.Users.AnyAsync(u => u.Id == id);
        if (!userExists)
        {
            return NotFound(new { message = "BotUser not found" });
        }

        var query = context.Answers
            .Include(a => a.BotUser)
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
                Username = a.IsAnonymous ? null : a.BotUser.Username,
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

    /// <summary>
    /// Generates a JWT token for the authenticated user
    /// </summary>
    private string GenerateJwtToken(BotUser user, out int expireMillionSeconds)
    {
        expireMillionSeconds = 7 * 24 * 60 * 60 * 1000; // 7 days
        
        var jwtKey = configuration["Jwt:Key"] ?? "super_secret_key_please_change_in_production_settings";
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("id", user.Id.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMilliseconds(expireMillionSeconds),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
