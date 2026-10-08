using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using WHY.Shared.Api;
using WHY.Shared.Dtos;
using WHY.Shared.Dtos.Answers;
using WHY.Shared.Dtos.Auth;
using WHY.Shared.Dtos.Comments;
using WHY.Shared.Dtos.Common;
using WHY.Shared.Dtos.Questions;
using WHY.Shared.Dtos.Users;

namespace WHY.Cli;

public class WhyApiClient
{
    private readonly HttpClient _httpClient;
    private readonly WhyCliOptions _options;

    public WhyApiClient(HttpClient httpClient, WhyCliOptions options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    private Uri BuildUri(string path, params (string key, string? value)[] query)
    {
        var baseUrl = _options.ApiBase.TrimEnd('/') + "/";
        var builder = new UriBuilder(new Uri(new Uri(baseUrl), path));

        var qs = new StringBuilder();
        foreach (var (key, value) in query)
        {
            if (string.IsNullOrEmpty(value))
                continue;

            if (qs.Length > 0)
                qs.Append('&');

            qs.Append(Uri.EscapeDataString(key))
              .Append('=')
              .Append(Uri.EscapeDataString(value));
        }

        if (qs.Length > 0)
            builder.Query = qs.ToString();

        return builder.Uri;
    }

    private async Task<BaseResponse<TResponse>> PostAsync<TResponse>(
        string path,
        CancellationToken cancellationToken = default,
        params (string key, string? value)[] query)
        where TResponse : new()
    {
        var uri = BuildUri(path, query);
        var response = await _httpClient.PostAsync(uri, null, cancellationToken);
        var result = await response.Content.ReadFromJsonAsync(
            typeof(BaseResponse<TResponse>),
            WhyJsonSerializerContext.Default,
            cancellationToken
        );
        return (result as BaseResponse<TResponse>) ?? new BaseResponse<TResponse> { Message = "Empty response" };
    }

    private async Task<BaseResponse<TResponse>> PostAsync<TRequest, TResponse>(
        string path,
        TRequest request,
        CancellationToken cancellationToken = default,
        params (string key, string? value)[] query)
        where TResponse : new()
    {
        var uri = BuildUri(path, query);
        var json = JsonSerializer.Serialize(request, typeof(TRequest), WhyJsonSerializerContext.Default);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(uri, content, cancellationToken);
        var result = await response.Content.ReadFromJsonAsync(
            typeof(BaseResponse<TResponse>),
            WhyJsonSerializerContext.Default,
            cancellationToken
        );
        return (result as BaseResponse<TResponse>) ?? new BaseResponse<TResponse> { Message = "Empty response" };
    }

    public Task<BaseResponse<AuthResponse>> RegisterAsync(RegisterUserRequest request, CancellationToken ct = default)
        => PostAsync<RegisterUserRequest, AuthResponse>("api/auth/register", request, ct);

    public Task<BaseResponse<AuthResponse>> LoginAsync(LoginUserRequest request, CancellationToken ct = default)
        => PostAsync<LoginUserRequest, AuthResponse>("api/auth/login", request, ct);

    public Task<BaseResponse<PagedResponse<QuestionResponse>>> GetRecommendedQuestionsAsync(
        PagedRequest request, CancellationToken ct = default)
        => PostAsync<PagedRequest, PagedResponse<QuestionResponse>>("api/question/recommended", request, ct);

    public Task<BaseResponse<QuestionResponse>> GetQuestionAsync(Guid id, CancellationToken ct = default)
        => PostAsync<QuestionResponse>("api/question/get-by-id", ct, ("id", id.ToString()));

    public Task<BaseResponse<QuestionResponse>> CreateQuestionAsync(
        CreateQuestionRequest request, CancellationToken ct = default)
        => PostAsync<CreateQuestionRequest, QuestionResponse>("api/question/create", request, ct);

    public Task<BaseResponse<PagedResponse<AnswerResponse>>> GetAnswersAsync(
        Guid questionId, PagedRequest request, CancellationToken ct = default)
        => PostAsync<PagedRequest, PagedResponse<AnswerResponse>>(
            "api/answer/get-by-question-id", request, ct, ("questionId", questionId.ToString()));

    public Task<BaseResponse<AnswerResponse>> CreateAnswerAsync(
        Guid questionId, CreateAnswerRequest request, CancellationToken ct = default)
        => PostAsync<CreateAnswerRequest, AnswerResponse>(
            "api/answer/create", request, ct, ("questionId", questionId.ToString()));

    public Task<BaseResponse<AnswerResponse>> VoteAnswerAsync(
        Guid answerId, VoteAnswerRequest request, CancellationToken ct = default)
        => PostAsync<VoteAnswerRequest, AnswerResponse>(
            "api/answer/vote", request, ct, ("answerId", answerId.ToString()));

    public Task<BaseResponse<PagedResponse<CommentResponse>>> GetCommentsAsync(
        Guid answerId, PagedRequest request, CancellationToken ct = default)
        => PostAsync<PagedRequest, PagedResponse<CommentResponse>>(
            "api/comment/get-under-answer", request, ct, ("answerId", answerId.ToString()));

    public Task<BaseResponse<CommentResponse>> CreateCommentAsync(
        Guid answerId, CreateCommentRequest request, CancellationToken ct = default)
        => PostAsync<CreateCommentRequest, CommentResponse>(
            "api/comment/create", request, ct, ("answerId", answerId.ToString()));
}
