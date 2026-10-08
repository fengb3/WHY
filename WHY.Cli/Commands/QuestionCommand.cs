using SuperCli;
using SuperCli.Attributes;
using WHY.Cli.Output;
using WHY.Shared.Dtos.Common;
using WHY.Shared.Dtos.Questions;

namespace WHY.Cli.Commands;

[Command("question")]
[CommandFilter<CommandExceptionFilter>]
internal partial class QuestionCommand
{
    private readonly WhyApiClient _api;
    private readonly TokenStore _tokenStore;
    private readonly IOutputFormatter _formatter;

    public QuestionCommand(WhyApiClient api, TokenStore tokenStore, IOutputFormatter formatter)
    {
        _api = api;
        _tokenStore = tokenStore;
        _formatter = formatter;
    }

    [Command("list")]
    public async Task<int> List(int page = 1, int pageSize = 20)
    {
        var result = await _api.GetRecommendedQuestionsAsync(new PagedRequest
        {
            Page = page,
            PageSize = pageSize,
        });

        if (result.StatusCode >= 400 || result.Data == null)
        {
            await Console.Error.WriteLineAsync($"Failed to list questions: {result.Message}");
            return 1;
        }

        var output = result.Data.Items.Select(q => new QuestionOutput
        {
            Id = q.Id,
            Username = q.Username,
            Title = q.Title,
            UpvoteCount = q.UpvoteCount,
            DownvoteCount = q.DownvoteCount,
            AnswerCount = q.AnswerCount,
            HasAcceptedAnswer = q.HasAcceptedAnswer,
            CreatedAt = q.CreatedAt,
            IsAnonymous = q.IsAnonymous,
        }).ToList();

        Console.WriteLine(_formatter.Serialize(output));
        return 0;
    }

    [Command("get")]
    public async Task<int> Get(string id)
    {
        if (!Guid.TryParse(id, out var questionId))
        {
            await Console.Error.WriteLineAsync("Invalid question ID. Must be a valid GUID.");
            return 1;
        }

        var result = await _api.GetQuestionAsync(questionId);
        if (result.StatusCode >= 400 || result.Data == null)
        {
            await Console.Error.WriteLineAsync($"Failed to get question: {result.Message}");
            return 1;
        }

        var q = result.Data;
        var output = new QuestionOutput
        {
            Id = q.Id,
            Username = q.Username,
            Title = q.Title,
            UpvoteCount = q.UpvoteCount,
            DownvoteCount = q.DownvoteCount,
            AnswerCount = q.AnswerCount,
            HasAcceptedAnswer = q.HasAcceptedAnswer,
            CreatedAt = q.CreatedAt,
            IsAnonymous = q.IsAnonymous,
        };

        Console.WriteLine(_formatter.Serialize(output));
        return 0;
    }

    [Command("create")]
    public async Task<int> Create(string title, string description, bool isAnonymous = false)
    {
        if (!_tokenStore.IsLoggedIn)
        {
            await Console.Error.WriteLineAsync("You must be logged in. Run 'why auth login' first.");
            return 1;
        }

        var result = await _api.CreateQuestionAsync(new CreateQuestionRequest
        {
            Title = title,
            Description = description,
            IsAnonymous = isAnonymous,
        });

        if (result.StatusCode >= 400 || result.Data == null)
        {
            await Console.Error.WriteLineAsync($"Failed to create question: {result.Message}");
            return 1;
        }

        Console.WriteLine($"Created question {result.Data.Id}.");
        return 0;
    }
}
