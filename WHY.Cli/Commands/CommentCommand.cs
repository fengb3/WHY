using SuperCli;
using SuperCli.Attributes;
using WHY.Cli.Output;
using WHY.Shared.Dtos.Comments;
using WHY.Shared.Dtos.Common;

namespace WHY.Cli.Commands;

[Command("comment")]
[CommandFilter<CommandExceptionFilter>]
internal partial class CommentCommand
{
    private readonly WhyApiClient _api;
    private readonly TokenStore _tokenStore;
    private readonly IOutputFormatter _formatter;

    public CommentCommand(WhyApiClient api, TokenStore tokenStore, IOutputFormatter formatter)
    {
        _api = api;
        _tokenStore = tokenStore;
        _formatter = formatter;
    }

    [Command("list")]
    public async Task<int> List(string answerId, int page = 1, int pageSize = 20)
    {
        if (!Guid.TryParse(answerId, out var aid))
        {
            await Console.Error.WriteLineAsync("Invalid answer ID. Must be a valid GUID.");
            return 1;
        }

        var result = await _api.GetCommentsAsync(aid, new PagedRequest
        {
            Page = page,
            PageSize = pageSize,
        });

        if (result.StatusCode >= 400 || result.Data == null)
        {
            await Console.Error.WriteLineAsync($"Failed to list comments: {result.Message}");
            return 1;
        }

        var output = result.Data.Items.Select(c => new CommentOutput
        {
            Id = c.Id,
            Username = c.Username,
            Content = c.Content,
            CreatedAt = c.CreatedAt,
        }).ToList();

        Console.WriteLine(_formatter.Serialize(output));
        return 0;
    }

    [Command("create")]
    public async Task<int> Create(string answerId, string content)
    {
        if (!_tokenStore.IsLoggedIn)
        {
            await Console.Error.WriteLineAsync("You must be logged in. Run 'why auth login' first.");
            return 1;
        }

        if (!Guid.TryParse(answerId, out var aid))
        {
            await Console.Error.WriteLineAsync("Invalid answer ID. Must be a valid GUID.");
            return 1;
        }

        var result = await _api.CreateCommentAsync(aid, new CreateCommentRequest
        {
            Content = content,
        });

        if (result.StatusCode >= 400 || result.Data == null)
        {
            await Console.Error.WriteLineAsync($"Failed to create comment: {result.Message}");
            return 1;
        }

        Console.WriteLine($"Created comment {result.Data.Id}.");
        return 0;
    }
}
