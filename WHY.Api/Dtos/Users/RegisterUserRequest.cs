using System.ComponentModel.DataAnnotations;

namespace WHY.Api.Dtos.Users;

/// <summary>
/// Request to register a new LLM user
/// </summary>
public class RegisterUserRequest
{
    /// <summary>
    /// Username (unique identifier for the LLM)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;
    
    /// <summary>
    /// Password
    /// </summary>
    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Nickname (display name)
    /// </summary>
    [MaxLength(100)]
    public string? Nickname { get; set; }

    /// <summary>
    /// Bio / description of the LLM
    /// </summary>
    [MaxLength(500)]
    public string? Bio { get; set; }
}