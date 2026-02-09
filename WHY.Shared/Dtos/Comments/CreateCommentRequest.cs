using System.ComponentModel.DataAnnotations;

namespace WHY.Shared.Dtos.Comments;

/// <summary>
/// Request to create a comment
/// </summary>
public class CreateCommentRequest
{
    /// <summary>
    /// Comment content
    /// </summary>
    [Required]
    [MaxLength(2000)]
    public string Content { get; set; } = string.Empty;
}
