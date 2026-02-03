using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using WHY.Database.Model;

namespace WHY.Database;

public class WHYBotDbContext(DbContextOptions<WHYBotDbContext> options) : DbContext(options)
{
    // 知乎功能相关实体
    public DbSet<User> Users { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<Answer> Answers { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<Topic> Topics { get; set; }
    public DbSet<QuestionTopic> QuestionTopics { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // 配置 User 实体
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // 配置 Question 实体
        modelBuilder.Entity<Question>(entity =>
        {
            entity.HasOne(q => q.User)
                .WithMany(u => u.Questions)
                .HasForeignKey(q => q.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.UserId);
        });

        // 配置 Answer 实体
        modelBuilder.Entity<Answer>(entity =>
        {
            entity.HasOne(a => a.Question)
                .WithMany(q => q.Answers)
                .HasForeignKey(a => a.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.User)
                .WithMany(u => u.Answers)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.QuestionId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CreatedAt);
        });

        // 配置 Comment 实体
        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasOne(c => c.User)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.Question)
                .WithMany(q => q.Comments)
                .HasForeignKey(c => c.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(c => c.Answer)
                .WithMany(a => a.Comments)
                .HasForeignKey(c => c.AnswerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(c => c.ParentComment)
                .WithMany(c => c.Replies)
                .HasForeignKey(c => c.ParentCommentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.QuestionId);
            entity.HasIndex(e => e.AnswerId);
            entity.HasIndex(e => e.ParentCommentId);
            entity.HasIndex(e => e.CreatedAt);
        });

        // 配置 Topic 实体
        modelBuilder.Entity<Topic>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // 配置 QuestionTopic 实体（多对多关系）
        modelBuilder.Entity<QuestionTopic>(entity =>
        {
            entity.HasKey(qt => new { qt.QuestionId, qt.TopicId });

            entity.HasOne(qt => qt.Question)
                .WithMany(q => q.QuestionTopics)
                .HasForeignKey(qt => qt.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(qt => qt.Topic)
                .WithMany(t => t.QuestionTopics)
                .HasForeignKey(qt => qt.TopicId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.TopicId);
        });
    }

}

public class WHYBotDbContextFactory : IDesignTimeDbContextFactory<WHYBotDbContext>
{
    public WHYBotDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<WHYBotDbContext>();
        // 请根据实际情况修改连接字符串
        optionsBuilder.UseSqlite("Data Source=whybott.db");

        return new WHYBotDbContext(optionsBuilder.Options);
    }
}