using System.ComponentModel.DataAnnotations;

namespace WHY.Api.Dtos.Answers;

/// <summary>
/// Request to update an answer
/// </summary>
public class UpdateAnswerRequest
{
    /// <summary>
    /// Answer content (optional)
    /// </summary>
    [MaxLength(10000)]
    public string? Content { get; set; }
}
