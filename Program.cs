using System.Text;
using Giahuy0790.Data;
using Giahuy0790.Models;
using Giahuy0790.Repositories;
using Giahuy0790.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Giahuy0790
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Tự động load file .env vào biến môi trường hệ thống để bảo mật thông tin
            try
            {
                DotNetEnv.Env.TraversePath().Load();
            }
            catch
            {
                // Bỏ qua nếu không tìm thấy file .env
            }

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
                    Description = "API Backend Quản lý Sản phẩm, Bài đăng, Identity & JWT Token"
                });
            });

            // Cấu hình Entity Framework Core + Supabase PostgreSQL
            var rawConnection = builder.Configuration.GetConnectionString("DefaultConnection");
            var envConnection = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
            var connectionString = !string.IsNullOrWhiteSpace(envConnection)
                ? envConnection
                : (!string.IsNullOrWhiteSpace(rawConnection)
                    ? rawConnection
                    : "Host=aws-0-ap-southeast-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.fddbvowrgywnlcingnnc;Password=Huydepzai123;SSL Mode=Require;Trust Server Certificate=true;Timeout=60;Command Timeout=60");

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));

            // ==========================================
            // Cấu hình ASP.NET Core Identity
            // ==========================================
            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                // Cấu hình độ phức tạp mật khẩu linh hoạt
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequiredLength = 6;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

            // ==========================================
            // Cấu hình Xác thực JWT Bearer Token
            // ==========================================
            var rawJwtKey = builder.Configuration["Jwt:Key"];
            var envJwtKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
            var jwtKey = !string.IsNullOrWhiteSpace(envJwtKey)
                ? envJwtKey
                : (!string.IsNullOrWhiteSpace(rawJwtKey)
                    ? rawJwtKey
                    : "Giahuy0790_Super_Secret_Key_For_Jwt_Authentication_2026!#@$");

            // Đảm bảo độ dài khóa bí mật luôn đủ chuẩn HMAC-SHA256 (ít nhất 32 ký tự / 256 bits)
            if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
            {
                jwtKey = "Giahuy0790_Super_Secret_Key_For_Jwt_Authentication_2026!#@$";
            }

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "Giahuy0790",
                    ValidAudience = builder.Configuration["Jwt:Audience"] ?? "Giahuy0790_Client",
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ClockSkew = TimeSpan.Zero
                };
            });

            // ==========================================
            // Đăng ký Email Service gửi qua SMTP
            // ==========================================
            builder.Services.AddScoped<IEmailService, EmailService>();

            // ==========================================
            // Đăng ký Repository Pattern vào DI Container
            // ==========================================
            builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            builder.Services.AddScoped<IProductRepository, ProductRepository>();
            builder.Services.AddScoped<IPostRepository, PostRepository>();

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

            // Khởi tạo các Roles mặc định và người dùng mẫu cho từng quyền (Admin, Staff, User)
            try
            {
                using var scope = app.Services.CreateScope();
                var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

                string[] roles = ["Admin", "Staff", "User"];
                foreach (var r in roles)
                {
                    if (!await roleMgr.RoleExistsAsync(r))
                    {
                        await roleMgr.CreateAsync(new IdentityRole(r));
                    }
                }

                // 1. Tài khoản Admin mẫu
                if (await userMgr.FindByNameAsync("admin") == null)
                {
                    var admin = new ApplicationUser
                    {
                        UserName = "admin",
                        Email = "admin@giahuy.com",
                        FullName = "Quản Trị Viên (Admin)",
                        AvatarUrl = "https://api.dicebear.com/7.x/bottts/svg?seed=Admin"
                    };
                    var res = await userMgr.CreateAsync(admin, "Admin@123");
                    if (res.Succeeded) await userMgr.AddToRoleAsync(admin, "Admin");
                }

                // 2. Tài khoản Staff mẫu
                if (await userMgr.FindByNameAsync("staff") == null)
                {
                    var staff = new ApplicationUser
                    {
                        UserName = "staff",
                        Email = "staff@giahuy.com",
                        FullName = "Nhân Viên POS (Staff)",
                        AvatarUrl = "https://api.dicebear.com/7.x/bottts/svg?seed=Staff"
                    };
                    var res = await userMgr.CreateAsync(staff, "Staff@123");
                    if (res.Succeeded) await userMgr.AddToRoleAsync(staff, "Staff");
                }

                // 3. Tài khoản User khách hàng mẫu
                if (await userMgr.FindByNameAsync("user") == null)
                {
                    var user = new ApplicationUser
                    {
                        UserName = "user",
                        Email = "user@giahuy.com",
                        FullName = "Khách Hàng (User)",
                        AvatarUrl = "https://api.dicebear.com/7.x/bottts/svg?seed=Customer"
                    };
                    var res = await userMgr.CreateAsync(user, "User@123");
                    if (res.Succeeded) await userMgr.AddToRoleAsync(user, "User");
                }
            }
            catch (Exception ex)
            {
                app.Logger.LogWarning("Không thể khởi tạo seed users: {Msg}", ex.Message);
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

            // Phục vụ các file tĩnh trong wwwroot (bao gồm uploads)
            app.UseStaticFiles();

            app.UseRouting();

            // Authentication PHẢI nằm TRƯỚC Authorization
            app.UseAuthentication();
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
