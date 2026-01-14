using Microsoft.AspNetCore.Authentication.Cookies; // Make sure you have this
using Microsoft.EntityFrameworkCore;
using YourApp.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// 1. ADD AUTHENTICATION SERVICES
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
    });

// Connect to SQL Server
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting(); // <--- EXISTING CODE

// 2. ADD THESE TWO LINES HERE (ORDER MATTERS!)
app.UseAuthentication();
app.UseAuthorization();

// 3. YOUR ROUTE CONFIGURATION
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Prizes}/{action=Search}/{id?}"); // Start page = Prizes/Search

app.Run();