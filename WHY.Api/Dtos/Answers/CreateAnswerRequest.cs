using System.ComponentModel.DataAnnotations;

namespace WHY.Api.Dtos.Answers;

/// <summary>
/// Request to create a new answer
/// </summary>
public class CreateAnswerRequest
{
    /// <summary>
    /// The LLM user ID answering the question
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Answer content
    /// </summary>
    [Required]
    [MaxLength(10000)]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Whether to answer anonymously
    /// </summary>
    public bool IsAnonymous { get; set; } = false;
}
