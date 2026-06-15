using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PharmaSkincare.Data;
using PharmaSkincare.Models;
using PharmaSkincare.Services;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// OAuth providers (fill keys in appsettings.json)
var googleClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "";
var googleSecret   = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";
var fbAppId        = builder.Configuration["Authentication:Facebook:AppId"] ?? "";
var fbSecret       = builder.Configuration["Authentication:Facebook:AppSecret"] ?? "";

var authBuilder = builder.Services.AddAuthentication();
if (!string.IsNullOrWhiteSpace(googleClientId) && !googleClientId.StartsWith("YOUR_"))
    authBuilder.AddGoogle(o => { o.ClientId = googleClientId; o.ClientSecret = googleSecret; });
if (!string.IsNullOrWhiteSpace(fbAppId) && !fbAppId.StartsWith("YOUR_"))
    authBuilder.AddFacebook(o => { o.AppId = fbAppId; o.AppSecret = fbSecret; });

// Cookie config
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

// Session (for cart)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(4);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".PharmaSkincare.Session";
});
builder.Services.AddHttpContextAccessor();

// Stripe
var stripeSecret = builder.Configuration["Stripe:SecretKey"] ?? "";
if (!string.IsNullOrWhiteSpace(stripeSecret) && !stripeSecret.StartsWith("sk_test_YOUR"))
    StripeConfiguration.ApiKey = stripeSecret;

// Application services
builder.Services.AddScoped<IProductService, PharmaSkincare.Services.ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IActivityLogService, ActivityLogService>();
builder.Services.AddScoped<IFileService, PharmaSkincare.Services.FileService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IWishlistService, WishlistService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// MVC
builder.Services.AddControllersWithViews();

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Shop}/{action=Index}/{id?}");

await DbInitializer.SeedAsync(app.Services);

app.Run();
