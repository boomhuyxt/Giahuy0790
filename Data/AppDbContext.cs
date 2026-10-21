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

    // Bảng Nhóm gia đình (FamilyGroups)
    public DbSet<FamilyGroup> FamilyGroups => Set<FamilyGroup>();

    // Bảng Thành viên nhóm gia đình (FamilyMembers)
    public DbSet<FamilyMember> FamilyMembers => Set<FamilyMember>();

    // Bảng Danh sách chặn Web & Ứng dụng của Trẻ (BlockedItems)
    public DbSet<BlockedItem> BlockedItems => Set<BlockedItem>();

    // Bảng Cấu hình Lịch & Giới hạn sử dụng thiết bị của Trẻ (Screen Time & Device Schedule)
    public DbSet<KidScreenTime> KidScreenTimes => Set<KidScreenTime>();

    // Bảng Mã QR Mời Gia Đình Tạm Thời (Thời hạn 1 phút)
    public DbSet<FamilyQrInvite> FamilyQrInvites => Set<FamilyQrInvite>();

    // Bảng Tọa độ GPS & Lịch sử vị trí của Trẻ (Location Tracking)
    public DbSet<KidLocation> KidLocations => Set<KidLocation>();

    // Bảng Vùng an toàn (Geofences)
    public DbSet<KidGeofence> KidGeofences => Set<KidGeofence>();

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

            // Quan hệ với Nhóm Gia Đình (FamilyGroup)
            entity.HasOne(k => k.FamilyGroup)
                  .WithMany(g => g.Kids)
                  .HasForeignKey(k => k.FamilyGroupId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Cấu hình Bảng FamilyGroups
        modelBuilder.Entity<FamilyGroup>(entity =>
        {
            entity.ToTable("family_groups");
            entity.HasKey(g => g.Id);
            entity.Property(g => g.Name).HasMaxLength(150).IsRequired();
            entity.Property(g => g.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(g => g.Owner)
                  .WithMany()
                  .HasForeignKey(g => g.OwnerId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Cấu hình Bảng FamilyMembers
        modelBuilder.Entity<FamilyMember>(entity =>
        {
            entity.ToTable("family_members");
            entity.HasKey(m => m.Id);
            entity.Property(m => m.RoleInFamily).HasMaxLength(50).HasDefaultValue("Parent");
            entity.Property(m => m.JoinedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(m => m.FamilyGroup)
                  .WithMany(g => g.Members)
                  .HasForeignKey(m => m.FamilyGroupId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.User)
                  .WithMany()
                  .HasForeignKey(m => m.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
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

        // Cấu hình Bảng BlockedItems (Danh sách chặn web và app của trẻ)
        modelBuilder.Entity<BlockedItem>(entity =>
        {
            entity.ToTable("blocked_items");
            entity.HasKey(b => b.Id);
            entity.Property(b => b.ItemType).HasMaxLength(20).IsRequired().HasDefaultValue("Website");
            entity.Property(b => b.Target).HasMaxLength(255).IsRequired();
            entity.Property(b => b.Title).HasMaxLength(150).IsRequired();
            entity.Property(b => b.Category).HasMaxLength(50).HasDefaultValue("Khác");
            entity.Property(b => b.Reason).HasMaxLength(255);
            entity.Property(b => b.IsBlocked).HasDefaultValue(true);
            entity.Property(b => b.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(b => b.KidId);
            entity.HasIndex(b => new { b.KidId, b.Target });

            entity.HasOne(b => b.Kid)
                  .WithMany(k => k.BlockedItems)
                  .HasForeignKey(b => b.KidId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(b => b.CreatedByUser)
                  .WithMany()
                  .HasForeignKey(b => b.CreatedByUserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Cấu hình Bảng KidScreenTimes (Lịch và giới hạn giờ dùng máy của trẻ)
        modelBuilder.Entity<KidScreenTime>(entity =>
        {
            entity.ToTable("kid_screen_times");
            entity.HasKey(s => s.Id);
            entity.Property(s => s.DeviceName).HasMaxLength(100).HasDefaultValue("Máy tính / Điện thoại của bé");
            entity.Property(s => s.DailyLimitMinutes).HasDefaultValue(120);
            entity.Property(s => s.WeekendLimitMinutes).HasDefaultValue(180);
            entity.Property(s => s.BedtimeStart).HasMaxLength(10).HasDefaultValue("21:30");
            entity.Property(s => s.BedtimeEnd).HasMaxLength(10).HasDefaultValue("06:30");
            entity.Property(s => s.AllowedStartTime).HasMaxLength(10).HasDefaultValue("07:00");
            entity.Property(s => s.AllowedEndTime).HasMaxLength(10).HasDefaultValue("21:00");
            entity.Property(s => s.LockMessage).HasMaxLength(500);
            entity.Property(s => s.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(s => s.KidId).IsUnique();

            entity.HasOne(s => s.Kid)
                  .WithOne(k => k.ScreenTime)
                  .HasForeignKey<KidScreenTime>(s => s.KidId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Cấu hình Bảng FamilyQrInvites (Mã QR mời vào gia đình trong 1 phút)
        modelBuilder.Entity<FamilyQrInvite>(entity =>
        {
            entity.ToTable("family_qr_invites");
            entity.HasKey(q => q.Id);
            entity.Property(q => q.InviteToken).HasMaxLength(64).IsRequired();
            entity.Property(q => q.ShortCode).HasMaxLength(10).IsRequired();
            entity.Property(q => q.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(q => q.InviteToken).IsUnique();
            entity.HasIndex(q => q.ShortCode);

            entity.HasOne(q => q.FamilyGroup)
                  .WithMany()
                  .HasForeignKey(q => q.FamilyGroupId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(q => q.CreatedByUser)
                  .WithMany()
                  .HasForeignKey(q => q.CreatedByUserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(q => q.UsedByUser)
                  .WithMany()
                  .HasForeignKey(q => q.UsedByUserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Cấu hình Bảng KidLocations (Lịch sử tọa độ GPS của trẻ)
        modelBuilder.Entity<KidLocation>(entity =>
        {
            entity.ToTable("kid_locations");
            entity.HasKey(l => l.Id);
            entity.Property(l => l.Address).HasMaxLength(255);
            entity.Property(l => l.RecordedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(l => l.KidId);
            entity.HasIndex(l => new { l.KidId, l.RecordedAt });

            entity.HasOne(l => l.Kid)
                  .WithMany(k => k.Locations)
                  .HasForeignKey(l => l.KidId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Cấu hình Bảng KidGeofences (Vùng an toàn cho trẻ)
        modelBuilder.Entity<KidGeofence>(entity =>
        {
            entity.ToTable("kid_geofences");
            entity.HasKey(g => g.Id);
            entity.Property(g => g.Name).HasMaxLength(100).IsRequired();
            entity.Property(g => g.RadiusMeters).HasDefaultValue(200);
            entity.Property(g => g.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(g => g.KidId);

            entity.HasOne(g => g.Kid)
                  .WithMany(k => k.Geofences)
                  .HasForeignKey(g => g.KidId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
