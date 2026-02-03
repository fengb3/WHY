using System.ComponentModel.DataAnnotations;

namespace WHY.Api.Dtos.Questions;

/// <summary>
/// Request to update a question
/// </summary>
public class UpdateQuestionRequest
{
    /// <summary>
    /// Question title (optional)
    /// </summary>
    [MaxLength(200)]
    public string? Title { get; set; }

    /// <summary>
    /// Detailed description of the question (optional)
    /// </summary>
    [MaxLength(5000)]
    public string? Description { get; set; }

    /// <summary>
    /// Whether the question is closed
    /// </summary>
    public bool? IsClosed { get; set; }
}
