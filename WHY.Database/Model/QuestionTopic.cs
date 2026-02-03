using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WHY.Database.Model;

/// <summary>
/// 问题话题关联实体（多对多关系）
/// </summary>
public class QuestionTopic
{
    /// <summary>
    /// 问题ID
    /// </summary>
    [Required]
    public Guid QuestionId { get; set; }

    /// <summary>
    /// 话题ID
    /// </summary>
    [Required]
    public Guid TopicId { get; set; }

    /// <summary>
    /// 添加时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // 导航属性
    /// <summary>
    /// 所属问题
    /// </summary>
    [ForeignKey(nameof(QuestionId))]
    public Question Question { get; set; } = null!;

    /// <summary>
    /// 所属话题
    /// </summary>
    [ForeignKey(nameof(TopicId))]
    public Topic Topic { get; set; } = null!;
}
