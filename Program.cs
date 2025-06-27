using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using truyenchu.Data;
using truyenchu.ExtendMethods;
using truyenchu.Models;
using truyenchu.Service;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
                .AddNewtonsoftJson(options =>
                    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
                );

// Thêm Razor Pages
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Thêm middleware cho CSRF
builder.Services.AddAntiforgery();

builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.FromSeconds(30); // Kiểm tra cookie mỗi 30 giây
});

// Identity
builder.Services.Configure<IdentityOptions>(options =>
{
    // Thiết lập về Password
    options.Password.RequireDigit = false; // Không bắt phải có số
    options.Password.RequireLowercase = false; // Không bắt phải có chữ thường
    options.Password.RequireNonAlphanumeric = false; // Không bắt ký tự đặc biệt
    options.Password.RequireUppercase = false; // Không bắt buộc chữ in
    options.Password.RequiredLength = 3; // Số ký tự tối thiểu của password
    options.Password.RequiredUniqueChars = 1; // Số ký tự riêng biệt

    // Cấu hình Lockout - khóa user
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5); // Khóa 5 phút
    options.Lockout.MaxFailedAccessAttempts = 5; // Thất bại 5 lầ thì khóa
    options.Lockout.AllowedForNewUsers = true;

    // Cấu hình về User.
    options.User.AllowedUserNameCharacters = // các ký tự đặt tên user
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = false;  // Email là duy nhất

    // Cấu hình đăng nhập.
    options.SignIn.RequireConfirmedEmail = false;            // Cấu hình xác thực địa chỉ email (email phải tồn tại, xác thực rồi mới cho login)
    options.SignIn.RequireConfirmedPhoneNumber = false;     // Xác thực số điện thoại
    options.SignIn.RequireConfirmedAccount = false;  // Yêu cầu xác thực email trước khi login, xem trang register để rõ hơn

});

builder.Services.AddIdentity<AppUser, IdentityRole>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

/* ================ Authorization option ================ */
//builder.Services.ConfigureApplicationCookie(option =>
//{
//    option.LoginPath = "/login/";
//    option.LogoutPath = "/logout/";
//    option.AccessDeniedPath = "/khongduoctruycap.html";
//});
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true; // Cookie chỉ được sử dụng qua HTTP
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // Cookie hết hạn sau 30 phút
    options.SlidingExpiration = true; // Gia hạn cookie nếu người dùng hoạt động
    options.LoginPath = "/Identity/Account/Login"; // Đường dẫn cho đăng nhập
    options.LogoutPath = "/Account/LogOff"; // Đường dẫn cho đăng xuất
    options.AccessDeniedPath = "/khongduoctruycap.html"; // Đường dẫn khi bị từ chối quyền truy cập
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true; // Cookie chỉ sử dụng qua HTTP, không qua JavaScript
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // Cookie hết hạn sau 30 phút
    options.SlidingExpiration = true; // Gia hạn cookie nếu người dùng hoạt động

    options.Events = new CookieAuthenticationEvents
    {
        // Kiểm tra cookie mỗi lần tải trang
        OnValidatePrincipal = context =>
        {
            // Kiểm tra xem cookie có còn hợp lệ không
            if (context.Principal.Identity.IsAuthenticated)
            {
                // Nếu cần, kiểm tra thêm logic (ví dụ: khóa tài khoản)
                var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<AppUser>>();
                var user = userManager.GetUserAsync(context.Principal).Result;
                if (user == null || !user.LockoutEnabled) // Hoặc logic khác
                {
                    context.RejectPrincipal(); // Hủy cookie
                }
            }
            return Task.CompletedTask;
        }
    };
});

/* ================ DbContext ================ */
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("TruyenChu"));
});

/* ================ Service ================ */
builder.Services.AddTransient(typeof(StoryService), typeof(StoryService));

var app = builder.Build();
var configuration = builder.Configuration;

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// app.UseHttpsRedirection();
app.UseStaticFiles();
// file tĩnh trong /Uploads
app.UseStaticFiles(new StaticFileOptions()
{
   FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "Uploads")),
   RequestPath = "/contents"
});
// app.UseStaticFiles(new StaticFileOptions
// {
//     FileProvider = new PhysicalFileProvider(configuration["StaticFiles:UploadsPath"]),
//     RequestPath = "/contents"
// });

Console.WriteLine(Path.Combine(Directory.GetCurrentDirectory(), "Uploads\\story_thumb"));

app.UseCookiePolicy();

app.UseRouting();
app.AddStatusCodePage();

app.UseAuthentication();
// Thêm middleware vào pipeline
app.UseMiddleware<RoleMiddleware>();
app.UseAuthorization();

app.MapControllerRoute(
    name: "storyDetails",
    pattern: "{slug}",
    defaults: new { controller = "BookStory", action = "Index" });
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapAreaControllerRoute(
    name: "default2",
    pattern: "{controller=ViewStory}/{action=Index}/{id?}", areaName: "ViewStory");


app.MapRazorPages();

app.Run();
