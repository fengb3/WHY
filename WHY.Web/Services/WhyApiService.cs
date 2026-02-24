using System.Net.Http.Json;
using WHY.Shared.Dtos.Common;
using WHY.Shared.Dtos.Web;

namespace WHY.Web.Services;

/// <summary>
/// Service for calling the WHY API
/// </summary>
public class WhyApiService(HttpClient httpClient)
{
    /// <summary>
    /// Get recommended questions ordered by trending score
    /// </summary>
    public async Task<PagedResponse<WebQuestionResponse>?> GetRecommendedQuestionsAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<PagedResponse<WebQuestionResponse>>(
                $"api/web/questions/recommended?page={page}&pageSize={pageSize}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error fetching recommended questions: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Get a specific question by ID
    /// </summary>
    public async Task<WebQuestionResponse?> GetQuestionAsync(Guid questionId)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<WebQuestionResponse>(
                $"api/web/questions/{questionId}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error fetching question: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Get paginated answers for a question
    /// </summary>
    public async Task<PagedResponse<WebAnswerResponse>?> GetAnswersAsync(Guid questionId, int page = 1, int pageSize = 20)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<PagedResponse<WebAnswerResponse>>(
                $"api/web/questions/{questionId}/answers?page={page}&pageSize={pageSize}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error fetching answers: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Get paginated comments for an answer
    /// </summary>
    public async Task<PagedResponse<WebCommentResponse>?> GetAnswerCommentsAsync(Guid questionId, Guid answerId, int page = 1, int pageSize = 10)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<PagedResponse<WebCommentResponse>>(
                $"api/web/questions/{questionId}/answers/{answerId}/comments?page={page}&pageSize={pageSize}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error fetching answer comments: {ex.Message}");
            return null;
        }
    }
}
