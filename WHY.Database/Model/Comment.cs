using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WHY.Database.Model;

/// <summary>
/// 评论实体
/// </summary>
public class Comment
{
    /// <summary>
    /// 评论ID
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// 评论用户ID
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// 评论内容
    /// </summary>
    [Required]
    [MaxLength(2000)]
    public string Content { get; set; } = string.Empty;

    // / <summary>
    // / 问题ID（如果是对问题的评论）
    // / </summary>
    // public Guid? QuestionId { get; set; }

    /// <summary>
    /// 回答ID（如果是对回答的评论）
    /// </summary>
    public Guid? AnswerId { get; set; }

    /// <summary>
    /// 父评论ID（如果是对评论的回复）
    /// </summary>
    public Guid? ParentCommentId { get; set; }

    /// <summary>
    /// 点赞数
    /// </summary>
    public int LikeCount { get; set; } = 0;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 是否已删除
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    // 导航属性
    /// <summary>
    /// 评论用户
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public BotUser BotUser { get; set; } = null!;

    // /// <summary>
    // /// 所属问题
    // /// </summary>
    // [ForeignKey(nameof(QuestionId))]
    // public Question? Question { get; set; }

    /// <summary>
    /// 所属回答
    /// </summary>
    [ForeignKey(nameof(AnswerId))]
    public Answer? Answer { get; set; }

    /// <summary>
    /// 父评论
    /// </summary>
    [ForeignKey(nameof(ParentCommentId))]
    public Comment? ParentComment { get; set; }

    /// <summary>
    /// 子评论（回复）
    /// </summary>
    public ICollection<Comment> Replies { get; set; } = new List<Comment>();
}
