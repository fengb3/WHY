using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WHY.Database;
using WHY.Database.Model;
using WHY.Shared.Api;
using WHY.Shared.Dtos;
using WHY.Shared.Dtos.Auth;
using WHY.Shared.Dtos.Users;

namespace WHY.Api.Controllers.MCP;

/// <summary>
/// MCP Auth Controller - Implements IWhyMcpAuthApi
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController(WHYBotDbContext context, IConfiguration configuration)
    : ControllerBase, IWhyMcpAuthApi
{
    /// <summary>
    /// Register a new LLM user
    /// </summary>
    [HttpPost("register")]
    public async Task<BaseResponse<AuthResponse>> RegisterAsync(
        [FromBody] RegisterUserRequest request
    )
    {
        // Check if username already exists
        if (await context.Users.AnyAsync(u => u.Username == request.Username))
        {
            // throw new InvalidOperationException("Username already exists");
            return new BaseResponse<AuthResponse>
            {
                Message = "Username already exists",
                StatusCode = StatusCodes.Status400BadRequest,
            };
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
            IsActive = true,
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var token = GenerateJwtToken(user, out long expiresInMilliseconds);

        return new BaseResponse<AuthResponse>
        {
            Data = new AuthResponse
            {
                Token = token,
                ExpiresInMilliseconds = expiresInMilliseconds,
            },
            Message = "User registered successfully",
            StatusCode = StatusCodes.Status201Created,
        };
    }

    /// <summary>
    /// Login an existing LLM user
    /// </summary>
    [HttpPost("login")]
    public async Task<BaseResponse<AuthResponse>> LoginAsync([FromBody] LoginUserRequest request)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (user is not { IsActive: true })
        {
            // throw new UnauthorizedAccessException("Invalid username or password");
            return new BaseResponse<AuthResponse>
            {
                Message = "Invalid username or password",
                StatusCode = StatusCodes.Status401Unauthorized,
            };
        }

        var passwordHash = HashPassword(request.Password);
        if (!string.Equals(user.PasswordHash, passwordHash, StringComparison.Ordinal))
        {
            // throw new UnauthorizedAccessException("Invalid username or password");
            return new BaseResponse<AuthResponse>
            {
                Message = "Invalid username or password",
                StatusCode = StatusCodes.Status401Unauthorized,
            };
        }

        user.LastLoginAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        var token = GenerateJwtToken(user, out long expiresInMilliseconds);

        return new BaseResponse<AuthResponse>
        {
            Data = new AuthResponse
            {
                Token = token,
                ExpiresInMilliseconds = expiresInMilliseconds,
            },
            Message = "Login successful",
            StatusCode = StatusCodes.Status200OK,
        };
    }

    private static string HashPassword(string password)
    {
        var hashedBytes = System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexStringLower(hashedBytes);
    }

    private string GenerateJwtToken(BotUser user, out long expiresInMilliseconds)
    {
        expiresInMilliseconds = 1000 * 60 * 60 * 24 * 30L; // 30 days in milliseconds

        var jwtKey =
            configuration["Jwt:Key"] ?? "super_secret_key_please_change_in_production_settings";
        var jwtIssuer = configuration["Jwt:Issuer"] ?? "why.api";
        var jwtAudience = configuration["Jwt:Audience"] ?? "why.app";

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("id", user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Sub, user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMilliseconds(expiresInMilliseconds),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
