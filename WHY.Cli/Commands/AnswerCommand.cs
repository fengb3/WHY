using SuperCli;
using SuperCli.Attributes;
using WHY.Cli.Output;
using WHY.Shared.Dtos.Answers;
using WHY.Shared.Dtos.Common;

namespace WHY.Cli.Commands;

[Command("answer")]
[CommandFilter<CommandExceptionFilter>]
internal partial class AnswerCommand
{
    private readonly WhyApiClient _api;
    private readonly TokenStore _tokenStore;
    private readonly IOutputFormatter _formatter;

    public AnswerCommand(WhyApiClient api, TokenStore tokenStore, IOutputFormatter formatter)
    {
        _api = api;
        _tokenStore = tokenStore;
        _formatter = formatter;
    }

    [Command("list")]
    public async Task<int> List(string questionId, int page = 1, int pageSize = 20)
    {
        if (!Guid.TryParse(questionId, out var qid))
        {
            await Console.Error.WriteLineAsync("Invalid question ID. Must be a valid GUID.");
            return 1;
        }

        var result = await _api.GetAnswersAsync(qid, new PagedRequest
        {
            Page = page,
            PageSize = pageSize,
        });

        if (result.StatusCode >= 400 || result.Data == null)
        {
            await Console.Error.WriteLineAsync($"Failed to list answers: {result.Message}");
            return 1;
        }

        var output = result.Data.Items.Select(a => new AnswerOutput
        {
            Id = a.Id,
            QuestionId = a.QuestionId,
            Username = a.Username,
            Content = a.Content,
            UpvoteCount = a.UpvoteCount,
            DownvoteCount = a.DownvoteCount,
            IsAccepted = a.IsAccepted,
            IsAnonymous = a.IsAnonymous,
            CreatedAt = a.CreatedAt,
        }).ToList();

        Console.WriteLine(_formatter.Serialize(output));
        return 0;
    }

    [Command("create")]
    public async Task<int> Create(string questionId, string content, bool isAnonymous = false)
    {
        if (!_tokenStore.IsLoggedIn)
        {
            await Console.Error.WriteLineAsync("You must be logged in. Run 'why auth login' first.");
            return 1;
        }

        if (!Guid.TryParse(questionId, out var qid))
        {
            await Console.Error.WriteLineAsync("Invalid question ID. Must be a valid GUID.");
            return 1;
        }

        var result = await _api.CreateAnswerAsync(qid, new CreateAnswerRequest
        {
            Content = content,
            IsAnonymous = isAnonymous,
        });

        if (result.StatusCode >= 400 || result.Data == null)
        {
            await Console.Error.WriteLineAsync($"Failed to create answer: {result.Message}");
            return 1;
        }

        Console.WriteLine($"Created answer {result.Data.Id}.");
        return 0;
    }

    [Command("vote")]
    public async Task<int> Vote(string answerId, string voteType)
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

        if (!Enum.TryParse<VoteType>(voteType, true, out var vt))
        {
            await Console.Error.WriteLineAsync("Invalid vote type. Must be Upvote, Downvote, or None.");
            return 1;
        }

        var result = await _api.VoteAnswerAsync(aid, new VoteAnswerRequest
        {
            VoteType = vt,
        });

        if (result.StatusCode >= 400 || result.Data == null)
        {
            await Console.Error.WriteLineAsync($"Failed to vote: {result.Message}");
            return 1;
        }

        Console.WriteLine($"Voted {voteType} on answer {answerId}.");
        return 0;
    }
}
