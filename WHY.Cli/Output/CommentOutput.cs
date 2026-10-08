using SuperCli.Attributes;

namespace WHY.Cli.Output;

[Output]
public class CommentOutput
{
    public Guid Id { get; set; }
    public string? Username { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
