using ClarityRecords.Infrastructure.Data;
using ClarityRecords.Infrastructure.Extensions;
using ClarityRecords.Web.Components;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.Cookie.Name = "clarity_auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddInfrastructure(connectionString);

var app = builder.Build();

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapPost("/account/login", async (HttpContext httpContext, IConfiguration config, IAntiforgery antiforgery) =>
{
    try { await antiforgery.ValidateRequestAsync(httpContext); }
    catch { return Results.Redirect("/login?error=1"); }

    var form = await httpContext.Request.ReadFormAsync();
    var username = form["username"].ToString();
    var password = form["password"].ToString();
    var rememberMe = form["rememberMe"].ToString() == "true";

    var expectedUsername = config["AdminCredentials:Username"] ?? "admin";
    var expectedPassword = config["AdminCredentials:Password"] ?? string.Empty;

    if (username == expectedUsername && password == expectedPassword)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var props = new AuthenticationProperties { IsPersistent = rememberMe };
        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity), props);
        return Results.Redirect("/admin");
    }

    return Results.Redirect("/login?error=1");
});

app.MapGet("/api/search", async (string? q, IDbContextFactory<AppDbContext> dbFactory) =>
{
    if (string.IsNullOrWhiteSpace(q))
        return Results.Ok(Array.Empty<object>());

    await using var db = await dbFactory.CreateDbContextAsync();
    var term = q.Trim().ToLower();

    var articles = await db.Articles
        .Where(a => a.PublishedAt != null && a.PublishedAt <= DateTimeOffset.UtcNow)
        .Where(a => a.Title.ToLower().Contains(term) ||
                    (a.Summary != null && a.Summary.ToLower().Contains(term)))
        .OrderByDescending(a => a.PublishedAt)
        .Take(8)
        .Select(a => new { a.Id, a.Title, a.Slug, a.Summary })
        .AsNoTracking()
        .ToListAsync();

    return Results.Ok(articles);
});

app.MapGet("/api/graph-data", async (IDbContextFactory<AppDbContext> dbFactory) =>
{
    await using var db = await dbFactory.CreateDbContextAsync();

    var nodes = await db.Articles
        .Where(a => a.PublishedAt != null && a.PublishedAt <= DateTimeOffset.UtcNow)
        .Select(a => new { id = a.Id, title = a.Title, slug = a.Slug })
        .AsNoTracking()
        .ToListAsync();

    var links = await db.ArticleManualLinks
        .Where(l => l.FromArticle.PublishedAt != null && l.ToArticle.PublishedAt != null)
        .Select(l => new { source = l.FromArticleId, target = l.ToArticleId })
        .AsNoTracking()
        .ToListAsync();

    return Results.Ok(new { nodes, links });
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
