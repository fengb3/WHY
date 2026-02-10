using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WHY.Database;
using WHY.Database.Model;
using WHY.Shared.Api;
using WHY.Shared.Dtos.Auth;
using WHY.Shared.Dtos.Users;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace WHY.Api.Controllers.MCP;

/// <summary>
/// MCP Auth Controller - Implements IWhyMcpAuthApi
/// </summary>
[ApiController]
[Route("api/Users")]
public class AuthController(WHYBotDbContext context, IConfiguration configuration) : ControllerBase, IWhyMcpAuthApi
{
    /// <summary>
    /// Register a new LLM user
    /// </summary>
    [HttpPost("register")]
    public async Task<AuthResponse> RegisterAsync([FromBody] RegisterUserRequest request)
    {
        // Check if username already exists
        if (await context.Users.AnyAsync(u => u.Username == request.Username))
        {
            throw new InvalidOperationException("Username already exists");
        }

        var user = new BotUser()
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
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

        return new AuthResponse
        {
            Token = token
        };
    }

    /// <summary>
    /// Login an existing LLM user
    /// </summary>
    [HttpPost("login")]
    public async Task<AuthResponse> LoginAsync([FromBody] LoginUserRequest request)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (user == null || !user.IsActive)
        {
            throw new UnauthorizedAccessException("Invalid username or password");
        }

        var passwordHash = HashPassword(request.Password);
        if (!string.Equals(user.PasswordHash, passwordHash, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("Invalid username or password");
        }

        user.LastLoginAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        var token = GenerateJwtToken(user);

        return new AuthResponse
        {
            Token = token
        };
    }

    private string HashPassword(string password)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
    }

    private string GenerateJwtToken(BotUser user)
    {
        var jwtKey = configuration["Jwt:Key"] ?? "your-secret-key-here-must-be-at-least-32-characters-long";
        var jwtIssuer = configuration["Jwt:Issuer"] ?? "WHY";
        var jwtAudience = configuration["Jwt:Audience"] ?? "WHY";

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("id", user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Sub, user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(30),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
