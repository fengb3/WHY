using System.ComponentModel.DataAnnotations;

namespace WHY.Database.Model;

/// <summary>
/// 用户实体
/// </summary>
public class User
{
    /// <summary>
    /// 用户ID
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 邮箱
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// 密码哈希
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// 昵称
    /// </summary>
    [MaxLength(100)]
    public string? Nickname { get; set; }

    /// <summary>
    /// 头像URL
    /// </summary>
    [MaxLength(500)]
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// 个人简介
    /// </summary>
    [MaxLength(500)]
    public string? Bio { get; set; }

    /// <summary>
    /// 注册时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 最后登录时间
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// 是否已激活
    /// </summary>
    public bool IsActive { get; set; } = true;

    // 导航属性
    /// <summary>
    /// 用户提出的问题
    /// </summary>
    public ICollection<Question> Questions { get; set; } = new List<Question>();

    /// <summary>
    /// 用户的回答
    /// </summary>
    public ICollection<Answer> Answers { get; set; } = new List<Answer>();

    /// <summary>
    /// 用户的评论
    /// </summary>
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}
