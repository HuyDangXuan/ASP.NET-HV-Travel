using Microsoft.AspNetCore.Authentication.Cookies;
using HVTravel.Application;
using HVTravel.Infrastructure;
using HVTravel.Infrastructure.Data;
using HVTravel.Web.Hubs;
using HVTravel.Web.Security;
using HVTravel.Web.Services;

using DotNetEnv;

// Load .env from the current directory or upwards (solution root)
Env.TraversePath().Load();
var builder = WebApplication.CreateBuilder(args);



// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();
builder.Services.AddSignalR();

// Add Layered Services
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddScoped<IPublicContentService, PublicContentService>();
builder.Services.AddScoped<ISupportChatService, SupportChatService>();

// Add Authentication
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = AuthSchemes.AppScheme;
        options.DefaultChallengeScheme = AuthSchemes.AppScheme;
        options.DefaultScheme = AuthSchemes.AppScheme;
    })
    .AddPolicyScheme(AuthSchemes.AppScheme, AuthSchemes.AppScheme, options =>
    {
        options.ForwardDefaultSelector = context =>
        {
            var path = context.Request.Path;
            var hasAdminCookie = context.Request.Cookies.ContainsKey(AuthSchemes.AdminCookieName);
            var hasCustomerCookie = context.Request.Cookies.ContainsKey(AuthSchemes.CustomerCookieName);

            if (path.StartsWithSegments("/Admin", StringComparison.OrdinalIgnoreCase))
            {
                return AuthSchemes.AdminScheme;
            }

            if (path.StartsWithSegments("/supportChatHub", StringComparison.OrdinalIgnoreCase))
            {
                if (hasAdminCookie)
                {
                    return AuthSchemes.AdminScheme;
                }

                return AuthSchemes.CustomerScheme;
            }

            if (hasCustomerCookie)
            {
                return AuthSchemes.CustomerScheme;
            }

            return AuthSchemes.CustomerScheme;
        };
    })
    .AddCookie(AuthSchemes.AdminScheme, options =>
    {
        options.Cookie.Name = AuthSchemes.AdminCookieName;
        options.LoginPath = "/Admin/Auth/Login";
        options.AccessDeniedPath = "/Admin/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    })
    .AddCookie(AuthSchemes.CustomerScheme, options =>
    {
        options.Cookie.Name = AuthSchemes.CustomerCookieName;
        options.LoginPath = "/CustomerAuth/Login";
        options.AccessDeniedPath = "/CustomerAuth/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<SupportChatHub>("/supportChatHub");

// Seed Data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await DbInitializer.SeedAsync(services);
    }
    catch (Exception ex)
    {
        var lines = new System.Collections.Generic.List<string> { "=== SEED ERROR ===" };
        var current = ex;
        while (current != null)
        {
            lines.Add($"[{current.GetType().FullName}] {current.Message}");
            current = current.InnerException;
        }
        lines.Add("=== END SEED ERROR ===");
        System.IO.File.WriteAllLines("seed_error_log.txt", lines);
        Console.WriteLine("SEED ERROR - see seed_error_log.txt for details");
    }
}

app.Run();
