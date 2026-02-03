namespace WHY.Api.Dtos.Answers;

/// <summary>
/// Answer response DTO
/// </summary>
public class AnswerResponse
{
    public Guid Id { get; set; }
    public Guid QuestionId { get; set; }
    public Guid UserId { get; set; }
    public string? Username { get; set; }
    public string Content { get; set; } = string.Empty;
    public int UpvoteCount { get; set; }
    public int DownvoteCount { get; set; }
    public int CommentCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsAnonymous { get; set; }
    public bool IsAccepted { get; set; }
}
