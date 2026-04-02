using ClarityRecords.Domain.Entities;
using ClarityRecords.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ClarityRecords.Tests.Schema;

[Collection("Database")]
public class ReadingTraceConstraintTests(DatabaseFixture fixture)
{
    private static async Task<Article> CreateArticleAsync(AppDbContext db)
    {
        var article = new Article { Slug = $"trace-{Guid.NewGuid():N}", Title = "Test", Body = "" };
        db.Articles.Add(article);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        return article;
    }

    // ── EventType CHECK ──────────────────────────────────────────────────────

    [Fact]
    public async Task InvalidEventType_IsRejected()
    {
        await using var db = fixture.CreateDbContext();
        db.ReadingTraces.Add(new ReadingTrace { EventType = "not_valid" });

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        var pgEx = Assert.IsType<PostgresException>(ex.InnerException);
        Assert.Equal("23514", pgEx.SqlState); // CHECK 约束冲突
    }

    // ── 跨字段 CHECK ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ManualLink_MissingRelatedArticle_IsRejected()
    {
        await using var db = fixture.CreateDbContext();
        var article = await CreateArticleAsync(db);

        // manual_link 必须同时有 ArticleId 和 RelatedArticleId
        db.ReadingTraces.Add(new ReadingTrace
        {
            EventType = ReadingTraceEventType.ManualLink,
            ArticleId = article.Id
            // RelatedArticleId 缺失
        });

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        var pgEx = Assert.IsType<PostgresException>(ex.InnerException);
        Assert.Equal("23514", pgEx.SqlState);
    }

    [Fact]
    public async Task ManualLink_MissingBothArticles_IsRejected()
    {
        await using var db = fixture.CreateDbContext();
        db.ReadingTraces.Add(new ReadingTrace
        {
            EventType = ReadingTraceEventType.ManualLink
            // ArticleId 和 RelatedArticleId 均缺失
        });

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        var pgEx = Assert.IsType<PostgresException>(ex.InnerException);
        Assert.Equal("23514", pgEx.SqlState);
    }

    [Fact]
    public async Task NewArticle_MissingArticleId_IsRejected()
    {
        await using var db = fixture.CreateDbContext();
        db.ReadingTraces.Add(new ReadingTrace
        {
            EventType = ReadingTraceEventType.NewArticle
            // ArticleId 缺失
        });

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        var pgEx = Assert.IsType<PostgresException>(ex.InnerException);
        Assert.Equal("23514", pgEx.SqlState);
    }

    // ── 合法场景 ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Insight_WithNoArticle_IsAllowed()
    {
        await using var db = fixture.CreateDbContext();
        db.ReadingTraces.Add(new ReadingTrace
        {
            EventType = ReadingTraceEventType.Insight,
            Note = "一个洞见，不关联任何文章"
        });

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task NewArticle_WithArticleId_IsAllowed()
    {
        await using var db = fixture.CreateDbContext();
        var article = await CreateArticleAsync(db);

        db.ReadingTraces.Add(new ReadingTrace
        {
            EventType = ReadingTraceEventType.NewArticle,
            ArticleId = article.Id
        });

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task ManualLink_WithBothArticles_IsAllowed()
    {
        await using var db = fixture.CreateDbContext();
        var a1 = await CreateArticleAsync(db);
        var a2 = await CreateArticleAsync(db);

        db.ReadingTraces.Add(new ReadingTrace
        {
            EventType = ReadingTraceEventType.ManualLink,
            ArticleId = a1.Id,
            RelatedArticleId = a2.Id
        });

        await db.SaveChangesAsync();
    }
}
