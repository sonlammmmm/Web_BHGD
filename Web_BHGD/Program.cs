using Microsoft.EntityFrameworkCore;
using Web_BHGD.Models;
using Web_BHGD.Models.Repositories;
using Microsoft.AspNetCore.Identity; // <-- THÊM DÒNG NÀY

var builder = WebApplication.CreateBuilder(args);

// Cấu hình DbContext
builder.Services.AddDbContext<ApplicationDbContext>(
    options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Cấu hình Identity - Đảm bảo dòng này chính xác và ở đây
// Bạn có thể đặt RequireConfirmedAccount = true nếu bạn muốn yêu cầu xác nhận email trong môi trường thực tế
builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();

// Thêm các dịch vụ vào vùng chứa.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Cấu hình pipeline yêu cầu HTTP.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// <-- THÊM HAI DÒNG NÀY THEO ĐÚNG THỨ TỰ
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    // Tạo vai trò "Admin" nếu chưa có
    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    }

    // Tạo người dùng Admin mặc định nếu chưa có
    string adminEmail = "admin@example.com"; // Đổi thành email bạn muốn cho admin
    string adminPassword = "AdminPassword123!"; // Đổi thành mật khẩu mạnh hơn và bảo mật
    if (await userManager.FindByEmailAsync(adminEmail) == null)
    {
        var adminUser = new ApplicationUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
        var result = await userManager.CreateAsync(adminUser, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
}

app.Run();