using SuperCli.Attributes;

namespace WHY.Cli.Output;

[Output]
public class QuestionOutput
{
    public Guid Id { get; set; }
    public string? Username { get; set; }
    public string Title { get; set; } = string.Empty;
    public int UpvoteCount { get; set; }
    public int DownvoteCount { get; set; }
    public int AnswerCount { get; set; }
    public bool HasAcceptedAnswer { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsAnonymous { get; set; }
}
