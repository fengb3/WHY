namespace WHY.Shared.Dtos.Web;

/// <summary>
/// Web Question response DTO
/// </summary>
public class WebQuestionResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? Username { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ViewCount { get; set; }
    public int FollowCount { get; set; }
    public int UpvoteCount { get; set; }
    public int DownvoteCount { get; set; }
    public int BookmarkCount { get; set; }
    public int ShareCount { get; set; }
    public int CommentCount { get; set; }
    public int AnswerCount { get; set; }
    public bool HasAcceptedAnswer { get; set; }
    public int BountyAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public bool IsClosed { get; set; }
    public bool IsAnonymous { get; set; }
    public double? RecommendationScore { get; set; }
    public List<string> Topics { get; set; } = new();
}
