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
                    Title = "KidCare API - Hệ thống Quản lý Trẻ em",
                    Version = "v1",
                    Description = "API Backend Quản lý Hồ sơ Trẻ em, Nhật ký hoạt động, Nhiệm vụ sinh hoạt & Phân quyền (Admin, Parents, Kid)"
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
            // Đăng ký Repository Pattern Quản lý Trẻ vào DI Container
            // ==========================================
            builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            builder.Services.AddScoped<IKidRepository, KidRepository>();
            builder.Services.AddScoped<IKidActivityRepository, KidActivityRepository>();

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

            // Khởi tạo 3 nhóm quyền chính: Admin, Parents, Kid và các tài khoản mẫu
            try
            {
                using var scope = app.Services.CreateScope();
                var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                string[] roles = ["Admin", "Parents", "Kid"];
                foreach (var r in roles)
                {
                    if (!await roleMgr.RoleExistsAsync(r))
                    {
                        await roleMgr.CreateAsync(new IdentityRole(r));
                    }
                }

                // 1. Tài khoản Quản trị viên (Admin)
                if (await userMgr.FindByNameAsync("admin") == null)
                {
                    var admin = new ApplicationUser
                    {
                        UserName = "admin",
                        Email = "admin@kidcare.com",
                        FullName = "Quản Trị Viên Hệ Thống",
                        AvatarUrl = "https://api.dicebear.com/7.x/bottts/svg?seed=Admin"
                    };
                    var res = await userMgr.CreateAsync(admin, "Admin@123");
                    if (res.Succeeded) await userMgr.AddToRoleAsync(admin, "Admin");
                }

                // 2. Tài khoản Phụ huynh (Parents)
                ApplicationUser? parentUser = await userMgr.FindByNameAsync("parents");
                if (parentUser == null)
                {
                    parentUser = new ApplicationUser
                    {
                        UserName = "parents",
                        Email = "parents@kidcare.com",
                        FullName = "Phụ Huynh Nguyễn Văn A",
                        AvatarUrl = "https://api.dicebear.com/7.x/bottts/svg?seed=Parents"
                    };
                    var res = await userMgr.CreateAsync(parentUser, "Parents@123");
                    if (res.Succeeded) await userMgr.AddToRoleAsync(parentUser, "Parents");
                }

                // 3. Tài khoản Trẻ em (Kid)
                ApplicationUser? kidUser = await userMgr.FindByNameAsync("kid");
                if (kidUser == null)
                {
                    kidUser = new ApplicationUser
                    {
                        UserName = "kid",
                        Email = "kid@kidcare.com",
                        FullName = "Bé Nguyễn Gia Huy",
                        AvatarUrl = "https://api.dicebear.com/7.x/bottts/svg?seed=GiaHuy"
                    };
                    var res = await userMgr.CreateAsync(kidUser, "Kid@123");
                    if (res.Succeeded) await userMgr.AddToRoleAsync(kidUser, "Kid");
                }

                // Khởi tạo dữ liệu mẫu hồ sơ trẻ em nếu chưa có
                if (!await dbContext.Kids.AnyAsync())
                {
                    var sampleKid1 = new Kid
                    {
                        FullName = "Nguyễn Gia Huy",
                        Nickname = "Bé Bơ",
                        DateOfBirth = DateTime.UtcNow.AddYears(-7),
                        Gender = "Nam",
                        AvatarUrl = "https://api.dicebear.com/7.x/bottts/svg?seed=GiaHuyKid",
                        SchoolName = "Trường Tiểu Học Kim Đồng - Lớp 2A",
                        HealthNotes = "Sức khỏe tốt, không có tiền sử dị ứng thực phẩm",
                        Interests = "Vẽ tranh, xếp hình lego, bóng đá",
                        ParentId = parentUser?.Id,
                        KidUserId = kidUser?.Id,
                        TotalPoints = 45,
                        CreatedAt = DateTime.UtcNow
                    };

                    var sampleKid2 = new Kid
                    {
                        FullName = "Nguyễn Bảo An",
                        Nickname = "Bé Na",
                        DateOfBirth = DateTime.UtcNow.AddYears(-5),
                        Gender = "Nữ",
                        AvatarUrl = "https://api.dicebear.com/7.x/bottts/svg?seed=BaoAn",
                        SchoolName = "Trường Mầm Non Họa Mi",
                        HealthNotes = "Dị ứng hải sản nhẹ, cần nhắc uống nước thường xuyên",
                        Interests = "Hát múa, nghe kể chuyện cổ tích",
                        ParentId = parentUser?.Id,
                        TotalPoints = 30,
                        CreatedAt = DateTime.UtcNow
                    };

                    await dbContext.Kids.AddRangeAsync(sampleKid1, sampleKid2);
                    await dbContext.SaveChangesAsync();

                    // Thêm hoạt động mẫu cho bé
                    var sampleActivity1 = new KidActivity
                    {
                        KidId = sampleKid1.Id,
                        Title = "Đọc 1 mẩu truyện tiếng Anh",
                        Description = "Đọc và phát âm truyện tranh song ngữ 15 phút",
                        Category = "Học tập",
                        ActivityTime = DateTime.UtcNow.AddHours(2),
                        Status = "Completed",
                        RewardPoints = 10,
                        Notes = "Bé đọc rất chuẩn và tự giác!",
                        CreatedAt = DateTime.UtcNow
                    };

                    var sampleActivity2 = new KidActivity
                    {
                        KidId = sampleKid1.Id,
                        Title = "Tự giác dọn dẹp góc học tập",
                        Description = "Xếp gọn sách vở và để bút vào hộp",
                        Category = "Nhiệm vụ",
                        ActivityTime = DateTime.UtcNow.AddHours(4),
                        Status = "Pending",
                        RewardPoints = 5,
                        Notes = "Nhắc bé trước khi đi ngủ",
                        CreatedAt = DateTime.UtcNow
                    };

                    await dbContext.KidActivities.AddRangeAsync(sampleActivity1, sampleActivity2);
                    await dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                app.Logger.LogWarning("Không thể khởi tạo seed users hoặc dữ liệu trẻ em mẫu: {Msg}", ex.Message);
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
