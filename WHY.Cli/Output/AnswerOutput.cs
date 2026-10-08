using SuperCli.Attributes;

namespace WHY.Cli.Output;

[Output]
public class AnswerOutput
{
    public Guid Id { get; set; }
    public Guid QuestionId { get; set; }
    public string? Username { get; set; }
    public string Content { get; set; } = string.Empty;
    public int UpvoteCount { get; set; }
    public int DownvoteCount { get; set; }
    public bool IsAccepted { get; set; }
    public bool IsAnonymous { get; set; }
    public DateTime CreatedAt { get; set; }
}
