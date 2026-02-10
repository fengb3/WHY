using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WHY.Database.Model;

/// <summary>
/// 问题投票记录
/// </summary>
public class QuestionVote
{
    /// <summary>
    /// 问题ID - 复合主键的一部分
    /// </summary>
    public Guid QuestionId { get; set; }

    /// <summary>
    /// 用户ID - 复合主键的一部分
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 是否为赞同票 (true=赞同, false=反对)
    /// </summary>
    public bool IsUpvote { get; set; }

    /// <summary>
    /// 投票时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // 导航属性
    [ForeignKey(nameof(QuestionId))]
    public Question Question { get; set; } = null!;

    [ForeignKey(nameof(UserId))]
    public BotUser BotUser { get; set; } = null!;
}
