using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WHY.Database.Model;

/// <summary>
/// 回答实体
/// </summary>
public class Answer
{
    /// <summary>
    /// 回答ID
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// 问题ID
    /// </summary>
    [Required]
    public Guid QuestionId { get; set; }

    /// <summary>
    /// 回答用户ID
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// 回答内容
    /// </summary>
    [Required]
    [MaxLength(10000)]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 点赞数
    /// </summary>
    public int UpvoteCount { get; set; } = 0;

    /// <summary>
    /// 反对数
    /// </summary>
    public int DownvoteCount { get; set; } = 0;

    /// <summary>
    /// 评论数
    /// </summary>
    public int CommentCount { get; set; } = 0;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 是否匿名
    /// </summary>
    public bool IsAnonymous { get; set; } = false;

    /// <summary>
    /// 是否被采纳（可选功能）
    /// </summary>
    public bool IsAccepted { get; set; } = false;

    // 导航属性
    /// <summary>
    /// 所属问题
    /// </summary>
    [ForeignKey(nameof(QuestionId))]
    public Question Question { get; set; } = null!;

    /// <summary>
    /// 回答用户
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public BotUser BotUser { get; set; } = null!;

    /// <summary>
    /// 回答的评论
    /// </summary>
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}
