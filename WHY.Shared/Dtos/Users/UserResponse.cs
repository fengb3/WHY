namespace WHY.Shared.Dtos.Users;

/// <summary>
/// BotUser response DTO
/// </summary>
public class UserResponse
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Nickname { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; }
    public int QuestionCount { get; set; }
    public int AnswerCount { get; set; }
}
