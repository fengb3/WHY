using System.ComponentModel.DataAnnotations;

namespace WHYBot.Database.Model;

/// <summary>
/// 话题实体
/// </summary>
public class Topic
{
    /// <summary>
    /// 话题ID
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// 话题名称
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 话题描述
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// 话题图标URL
    /// </summary>
    [MaxLength(500)]
    public string? IconUrl { get; set; }

    /// <summary>
    /// 关注人数
    /// </summary>
    public int FollowerCount { get; set; } = 0;

    /// <summary>
    /// 问题数量
    /// </summary>
    public int QuestionCount { get; set; } = 0;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // 导航属性
    /// <summary>
    /// 话题下的问题
    /// </summary>
    public ICollection<QuestionTopic> QuestionTopics { get; set; } = new List<QuestionTopic>();
}
