using ClarityRecords.Domain.Authorization;
using ClarityRecords.Infrastructure.Data;
using ClarityRecords.Infrastructure.Extensions;
using ClarityRecords.Infrastructure.Identity;
using ClarityRecords.Web.Components;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 8;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.Cookie.Name = "clarity_auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
});

builder.Services.Configure<SecurityStampValidatorOptions>(opts =>
    opts.ValidationInterval = TimeSpan.FromMinutes(5));

builder.Services.AddAuthorization(options =>
{
    foreach (var permission in Permissions.All)
    {
        options.AddPolicy(permission, policy =>
            policy.RequireClaim("permission", permission));
    }
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddInfrastructure(connectionString);

var app = builder.Build();

await SeedAsync(app);

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapPost("/account/login", async (
    HttpContext ctx,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    RoleManager<IdentityRole> roleManager,
    IAntiforgery antiforgery) =>
{
    try { await antiforgery.ValidateRequestAsync(ctx); }
    catch { return Results.Redirect("/login?error=1"); }

    var form = await ctx.Request.ReadFormAsync();
    var username = form["username"].ToString();
    var password = form["password"].ToString();
    var rememberMe = form["rememberMe"].ToString() == "true";

    var user = await userManager.FindByNameAsync(username);
    if (user == null || !await userManager.CheckPasswordAsync(user, password))
        return Results.Redirect("/login?error=1");

    if (user.RequirePasswordChange)
        return Results.Redirect($"/change-password?userId={user.Id}");

    var roles = await userManager.GetRolesAsync(user);
    var permClaims = new List<Claim>();
    foreach (var roleName in roles)
    {
        var role = await roleManager.FindByNameAsync(roleName);
        if (role != null)
        {
            var rc = await roleManager.GetClaimsAsync(role);
            permClaims.AddRange(rc.Where(c => c.Type == "permission"));
        }
    }

    user.LastLoginAt = DateTimeOffset.UtcNow;
    await userManager.UpdateAsync(user);

    var props = new AuthenticationProperties { IsPersistent = rememberMe };
    await signInManager.SignInWithClaimsAsync(user, props, permClaims);
    return Results.Redirect("/admin");
});

app.MapPost("/account/logout", async (
    SignInManager<ApplicationUser> signInManager,
    IAntiforgery antiforgery,
    HttpContext ctx) =>
{
    try { await antiforgery.ValidateRequestAsync(ctx); }
    catch { }
    await signInManager.SignOutAsync();
    return Results.Redirect("/login");
});

app.MapPost("/account/change-password", async (
    HttpContext ctx,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    RoleManager<IdentityRole> roleManager,
    IAntiforgery antiforgery) =>
{
    try { await antiforgery.ValidateRequestAsync(ctx); }
    catch { return Results.Redirect("/login"); }

    var form = await ctx.Request.ReadFormAsync();
    var userId = form["userId"].ToString();
    var newPassword = form["newPassword"].ToString();
    var confirmPassword = form["confirmPassword"].ToString();

    if (newPassword != confirmPassword)
        return Results.Redirect($"/change-password?userId={userId}&error=mismatch");
    if (newPassword.Length < 8)
        return Results.Redirect($"/change-password?userId={userId}&error=weak");

    var user = await userManager.FindByIdAsync(userId);
    if (user == null)
        return Results.Redirect("/login?error=1");

    var token = await userManager.GeneratePasswordResetTokenAsync(user);
    var result = await userManager.ResetPasswordAsync(user, token, newPassword);
    if (!result.Succeeded)
        return Results.Redirect($"/change-password?userId={userId}&error=failed");

    user.RequirePasswordChange = false;
    await userManager.UpdateAsync(user);

    var roles = await userManager.GetRolesAsync(user);
    var permClaims = new List<Claim>();
    foreach (var roleName in roles)
    {
        var role = await roleManager.FindByNameAsync(roleName);
        if (role != null)
        {
            var rc = await roleManager.GetClaimsAsync(role);
            permClaims.AddRange(rc.Where(c => c.Type == "permission"));
        }
    }

    user.LastLoginAt = DateTimeOffset.UtcNow;
    await userManager.UpdateAsync(user);

    await signInManager.SignInWithClaimsAsync(user, isPersistent: false, permClaims);
    return Results.Redirect("/admin");
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

static async Task SeedAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    // 初始化角色及其权限
    var rolePermissions = new Dictionary<string, string[]>
    {
        ["Admin"] = Permissions.All,
        ["Editor"] =
        [
            Permissions.ArticlesCreate, Permissions.ArticlesEditOwn, Permissions.ArticlesEditAll,
            Permissions.ArticlesPublish, Permissions.ArticlesDelete,
            Permissions.TagsManage, Permissions.KnowledgeLinksManage,
            Permissions.UsersView
        ],
        ["Author"] = [Permissions.ArticlesCreate, Permissions.ArticlesEditOwn]
    };

    foreach (var (roleName, perms) in rolePermissions)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
            await roleManager.CreateAsync(new IdentityRole(roleName));

        var role = await roleManager.FindByNameAsync(roleName);
        var existingClaims = await roleManager.GetClaimsAsync(role!);

        foreach (var perm in perms)
        {
            if (!existingClaims.Any(c => c.Type == "permission" && c.Value == perm))
                await roleManager.AddClaimAsync(role!, new Claim("permission", perm));
        }
    }

    // 若无任何用户则创建初始管理员账户
    if (!userManager.Users.Any())
    {
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var username = config["AdminCredentials:Username"] ?? "admin";
        var password = config["AdminCredentials:Password"] ?? string.Empty;

        var admin = new ApplicationUser { UserName = username, Email = $"{username}@localhost" };
        var result = await userManager.CreateAsync(admin, password);
        if (result.Succeeded)
            await userManager.AddToRoleAsync(admin, "Admin");
    }
}
