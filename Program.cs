using Giahuy0790.Data;
using Microsoft.EntityFrameworkCore;

namespace Giahuy0790
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Đăng ký API Controllers (cho Swagger scan được)
            builder.Services.AddControllers();

            // Cấu hình Swagger / OpenAPI
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
                {
                    Title = "Giahuy0790 API",
                    Version = "v1",
                    Description = "API Backend Quản lý Sản phẩm, Bài đăng & Upload File"
                });
            });

            // Cấu hình Entity Framework Core + Supabase PostgreSQL
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            var app = builder.Build();

            // Tự động áp dụng Migrations lên Supabase PostgreSQL khi khởi động
            if (app.Configuration.GetValue<bool>("DatabaseSettings:AutoMigrateOnStartup", true))
            {
                using var scope = app.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                try
                {
                    var pendingMigrations = (await db.Database.GetPendingMigrationsAsync()).ToList();
                    if (pendingMigrations.Count != 0)
                    {
                        app.Logger.LogInformation("Phát hiện {Count} migrations mới. Đang tự động cập nhật Supabase Database...", pendingMigrations.Count);
                        await db.Database.MigrateAsync();
                        app.Logger.LogInformation("✅ Cập nhật cấu trúc Database Supabase thành công!");
                    }
                    else
                    {
                        app.Logger.LogInformation("✅ Database Supabase đã đồng bộ đầy đủ các Models.");
                    }
                }
                catch (Exception ex)
                {
                    app.Logger.LogError(ex, "⚠️ Không thể tự động chạy migrations khi khởi động: {Message}", ex.Message);
                }
            }

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            // Bật Swagger UI
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Giahuy0790 API v1");
                options.RoutePrefix = "swagger"; // Truy cập tại /swagger
            });

            app.UseHttpsRedirection();

            // Phục vụ các file tĩnh trong wwwroot (bao gồm wwwroot/uploads/products và wwwroot/uploads/posts)
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapStaticAssets();

            // Map API Controllers
            app.MapControllers();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            await app.RunAsync();
        }
    }
}
