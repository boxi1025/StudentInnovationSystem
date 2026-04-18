using Microsoft.EntityFrameworkCore;
using StudentInnovation.Shared.Models;

namespace StudentInnovation.WebApi.Data;

public class StudentInnovationDbContext : DbContext
{
    public StudentInnovationDbContext(DbContextOptions<StudentInnovationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Achievement> Achievements => Set<Achievement>();
    public DbSet<AchievementAttachment> AchievementAttachments => Set<AchievementAttachment>();
    public DbSet<AchievementAuditLog> AchievementAuditLogs => Set<AchievementAuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Username).HasMaxLength(64).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(256).IsRequired();
            entity.Property(x => x.Role).HasMaxLength(32).IsRequired();
            entity.Property(x => x.FullName).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Department).HasMaxLength(128);
            entity.Property(x => x.StudentNo).HasMaxLength(32);
            entity.Property(x => x.EmployeeNo).HasMaxLength(32);
            entity.HasIndex(x => x.Username).IsUnique();
        });

        modelBuilder.Entity<Achievement>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Level).HasMaxLength(32).IsRequired();
            entity.Property(x => x.StudentName).HasMaxLength(64).IsRequired();
            entity.Property(x => x.StudentId).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Advisor).HasMaxLength(64).IsRequired();
            entity.Property(x => x.OwnerUsername).HasMaxLength(64).IsRequired();
            entity.Property(x => x.TeamName).HasMaxLength(128);
            entity.Property(x => x.Department).HasMaxLength(128);
            entity.Property(x => x.Status).HasMaxLength(32).IsRequired();
            entity.Property(x => x.CreditScore).HasPrecision(8, 2);
            entity.Property(x => x.ExtraJson).HasMaxLength(2000);
            entity.Property(x => x.ProjectNumber).HasMaxLength(64);
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.HasMany(x => x.Attachments).WithOne().HasForeignKey(x => x.AchievementId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(x => x.AuditLogs).WithOne().HasForeignKey(x => x.AchievementId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AchievementAttachment>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FileName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.FileUrl).HasMaxLength(500).IsRequired();
            entity.Property(x => x.FileType).HasMaxLength(64).IsRequired();
        });

        modelBuilder.Entity<AchievementAuditLog>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Action).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Reviewer).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Comment).HasMaxLength(1000);
        });
    }
}
