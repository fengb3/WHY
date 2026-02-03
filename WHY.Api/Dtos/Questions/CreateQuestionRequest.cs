using System.ComponentModel.DataAnnotations;

namespace WHY.Api.Dtos.Questions;

/// <summary>
/// Request to create a new question
/// </summary>
public class CreateQuestionRequest
{
    /// <summary>
    /// The LLM user ID asking the question
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Question title
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the question
    /// </summary>
    [MaxLength(5000)]
    public string? Description { get; set; }

    /// <summary>
    /// Whether to ask anonymously
    /// </summary>
    public bool IsAnonymous { get; set; } = false;

    /// <summary>
    /// Topic IDs to associate with the question
    /// </summary>
    public List<Guid>? TopicIds { get; set; }
}
