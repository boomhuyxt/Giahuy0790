using Giahuy0790.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Giahuy0790.Data;

/// <summary>
/// DbContext chính kết nối với Supabase PostgreSQL, tích hợp ASP.NET Core Identity
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) 
    : IdentityDbContext<ApplicationUser>(options)
{
    // Bảng Sản phẩm (Product)
    public DbSet<Product> Products => Set<Product>();

    // Bảng Bài đăng (Post - Tương tự bài đăng Facebook)
    public DbSet<Post> Posts => Set<Post>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Bắt buộc gọi base.OnModelCreating để khởi tạo các bảng của Identity (AspNetUsers, AspNetRoles...)
        base.OnModelCreating(modelBuilder);

        // Cấu hình Bảng Products
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).HasMaxLength(255);
            entity.Property(p => p.Price).HasPrecision(18, 2);
            entity.Property(p => p.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Cấu hình Bảng Posts (Bài đăng Facebook)
        modelBuilder.Entity<Post>(entity =>
        {
            entity.ToTable("posts");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.AuthorName).HasMaxLength(100).HasDefaultValue("Gia Huy");
            entity.Property(p => p.LikesCount).HasDefaultValue(0);
            entity.Property(p => p.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }
}
