namespace WHY.Shared.Dtos.Questions;

/// <summary>
/// Question response DTO
/// </summary>
public class QuestionResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? Username { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ViewCount { get; set; }
    public int FollowCount { get; set; }
    public int AnswerCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsClosed { get; set; }
    public bool IsAnonymous { get; set; }
    public List<string> Topics { get; set; } = new();
}
