using Giahuy0790.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Giahuy0790.Data;

/// <summary>
/// DbContext chính kết nối với Supabase PostgreSQL, quản lý Hồ sơ trẻ (Kid), Hoạt động (KidActivity) và ASP.NET Core Identity
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) 
    : IdentityDbContext<ApplicationUser>(options)
{
    // Bảng Hồ sơ trẻ em (Kids)
    public DbSet<Kid> Kids => Set<Kid>();

    // Bảng Nhật ký hoạt động & Nhiệm vụ của trẻ (KidActivities)
    public DbSet<KidActivity> KidActivities => Set<KidActivity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Bắt buộc gọi base.OnModelCreating để khởi tạo các bảng của Identity (AspNetUsers, AspNetRoles...)
        base.OnModelCreating(modelBuilder);

        // Cấu hình Bảng Kids
        modelBuilder.Entity<Kid>(entity =>
        {
            entity.ToTable("kids");
            entity.HasKey(k => k.Id);
            entity.Property(k => k.FullName).HasMaxLength(150).IsRequired();
            entity.Property(k => k.Nickname).HasMaxLength(50);
            entity.Property(k => k.TotalPoints).HasDefaultValue(0);
            entity.Property(k => k.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Quan hệ với Phụ huynh (Parent)
            entity.HasOne(k => k.Parent)
                  .WithMany()
                  .HasForeignKey(k => k.ParentId)
                  .OnDelete(DeleteBehavior.SetNull);

            // Quan hệ với Tài khoản riêng của Trẻ (KidUser)
            entity.HasOne(k => k.KidUser)
                  .WithMany()
                  .HasForeignKey(k => k.KidUserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Cấu hình Bảng KidActivities
        modelBuilder.Entity<KidActivity>(entity =>
        {
            entity.ToTable("kid_activities");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Title).HasMaxLength(200).IsRequired();
            entity.Property(a => a.Category).HasMaxLength(50).HasDefaultValue("Sinh hoạt");
            entity.Property(a => a.Status).HasMaxLength(20).HasDefaultValue("Pending");
            entity.Property(a => a.RewardPoints).HasDefaultValue(5);
            entity.Property(a => a.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Quan hệ với Kid
            entity.HasOne(a => a.Kid)
                  .WithMany(k => k.Activities)
                  .HasForeignKey(a => a.KidId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
