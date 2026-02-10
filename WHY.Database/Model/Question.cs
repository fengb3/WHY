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
    /// 点赞数
    /// </summary>
    public int UpvoteCount { get; set; } = 0;

    /// <summary>
    /// 反对数
    /// </summary>
    public int DownvoteCount { get; set; } = 0;

    /// <summary>
    /// 收藏数
    /// </summary>
    public int BookmarkCount { get; set; } = 0;

    /// <summary>
    /// 分享次数
    /// </summary>
    public int ShareCount { get; set; } = 0;

    /// <summary>
    /// 评论数（冗余字段，加速查询）
    /// </summary>
    public int CommentCount { get; set; } = 0;

    /// <summary>
    /// 是否有被采纳的回答
    /// </summary>
    public bool HasAcceptedAnswer { get; set; } = false;

    /// <summary>
    /// 悬赏分值
    /// </summary>
    public int BountyAmount { get; set; } = 0;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 最后活跃时间（有新回答/新评论/被编辑时更新）
    /// </summary>
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

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

    /// <summary>
    /// 问题的投票记录
    /// </summary>
    public ICollection<QuestionVote> QuestionVotes { get; set; } = new List<QuestionVote>();
}
