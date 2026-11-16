using BenildeTrack.Data;
using BenildeTrack.Models;
using BenildeTrack.Services;
using BenildeTrack.Services.Tala;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Database: SQLite file. The connection string comes from appsettings.json.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connectionString));

// Login system (Identity). AddRoles lets us use Student / Professor / Admin roles.
builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

    // FIX: log users out right away when an admin changes or deletes their account
builder.Services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.Zero);

// FIX: send relative login redirects, so logout/Back don't go to http://localhost:5246 (unreachable in Codespaces)
builder.Services.ConfigureApplicationCookie(options =>
{
    static string Relative(string uri) =>
        Uri.TryCreate(uri, UriKind.Absolute, out var absolute) ? absolute.PathAndQuery : uri;

    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.Redirect(Relative(context.RedirectUri));
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.Redirect(Relative(context.RedirectUri));
        return Task.CompletedTask;
    };
});

// FIX: trust the address headers Codespaces adds, so the app knows the real URL
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// MVC. The filter makes every POST need an anti-forgery token (protects forms from forged requests).
builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

// Our own classes. Controllers receive them through their constructors.
builder.Services.AddScoped<StandingService>();
builder.Services.AddScoped<HandbookService>();
builder.Services.AddScoped<ExcuseService>();
builder.Services.AddScoped<FileStorage>();
builder.Services.AddScoped<TalaService>();
builder.Services.AddHttpClient<GeminiClient>(client => client.Timeout = TimeSpan.FromSeconds(30));

var app = builder.Build();

// FIX: read the Codespaces address headers (development only)
if (app.Environment.IsDevelopment())
{
    app.UseForwardedHeaders();
}

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
