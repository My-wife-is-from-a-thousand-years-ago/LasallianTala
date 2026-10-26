using BenildeTrack.Data;
using BenildeTrack.Models;
using BenildeTrack.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Database: SQLite file. The connection string comes from appsettings.json.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connectionString));

// Login system (Identity). AddRoles lets us use Student / Teacher / Admin roles.
builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// MVC. The filter makes every POST need an anti-forgery token (protects forms from forged requests).
builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

// Our own classes. Controllers receive them through their constructors.
builder.Services.AddScoped<StandingService>();
builder.Services.AddScoped<HandbookService>();
builder.Services.AddScoped<TalaService>();
builder.Services.AddHttpClient<GeminiClient>(client => client.Timeout = TimeSpan.FromSeconds(30));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages(); // the login/register pages that come with --auth Individual

// Apply migrations, create the roles, and (development only) add demo accounts and data.
using (var scope = app.Services.CreateScope())
{
    await SeedData.InitializeAsync(scope.ServiceProvider, addDemoData: app.Environment.IsDevelopment());
}

app.Run();
