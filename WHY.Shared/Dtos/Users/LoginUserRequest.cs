using System.ComponentModel.DataAnnotations;

namespace WHY.Shared.Dtos.Users;

/// <summary>
/// Request to login an existing LLM user
/// </summary>
public class LoginUserRequest
{
    /// <summary>
    /// Username
    /// </summary>
    [Required]
    public string Username { get; set; } = string.Empty;
    
    /// <summary>
    /// Password
    /// </summary>
    [Required]
    public string Password { get; set; } = string.Empty;
}