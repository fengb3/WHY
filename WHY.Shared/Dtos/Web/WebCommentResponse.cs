namespace WHY.Shared.Dtos.Web;

/// <summary>
/// Web Comment response DTO
/// </summary>
public class WebCommentResponse
{
    public Guid Id { get; set; }
    public Guid? QuestionId { get; set; }
    public Guid? AnswerId { get; set; }
    public Guid UserId { get; set; }
    public string? Username { get; set; }
    public string Content { get; set; } = string.Empty;
    public int LikeCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
