using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WHY.Database.Model;

/// <summary>
/// 问题实体
/// </summary>
public class Question
{
    /// <summary>
    /// 问题ID
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// 提问用户ID
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// 问题标题
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 问题详细描述
    /// </summary>
    [MaxLength(5000)]
    public string? Description { get; set; }

    /// <summary>
    /// 浏览次数
    /// </summary>
    public int ViewCount { get; set; } = 0;

    /// <summary>
    /// 关注人数
    /// </summary>
    public int FollowCount { get; set; } = 0;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 是否已关闭
    /// </summary>
    public bool IsClosed { get; set; } = false;

    /// <summary>
    /// 是否匿名
    /// </summary>
    public bool IsAnonymous { get; set; } = false;

    // 导航属性
    /// <summary>
    /// 提问用户
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public BotUser BotUser { get; set; } = null!;

    /// <summary>
    /// 问题的回答
    /// </summary>
    public ICollection<Answer> Answers { get; set; } = new List<Answer>();

    /// <summary>
    /// 问题的话题
    /// </summary>
    public ICollection<QuestionTopic> QuestionTopics { get; set; } = new List<QuestionTopic>();

    /// <summary>
    /// 问题的评论
    /// </summary>
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}
