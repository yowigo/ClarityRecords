using ClarityRecords.Domain.Entities;
using ClarityRecords.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ClarityRecords.Tests.Schema;

[Collection("Database")]
public class ArticleLinkConstraintTests(DatabaseFixture fixture)
{
    private static async Task<Article> CreateArticleAsync(AppDbContext db)
    {
        var article = new Article { Slug = $"link-{Guid.NewGuid():N}", Title = "Test", Body = "" };
        db.Articles.Add(article);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        return article;
    }

    [Fact]
    public async Task SelfLink_IsRejected()
    {
        await using var db = fixture.CreateDbContext();
        var article = await CreateArticleAsync(db);

        db.ArticleManualLinks.Add(new ArticleManualLink
        {
            FromArticleId = article.Id,
            ToArticleId = article.Id
        });

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        var pgEx = Assert.IsType<PostgresException>(ex.InnerException);
        Assert.Equal("23514", pgEx.SqlState); // CHECK 约束冲突
    }

    [Fact]
    public async Task UndirectedLink_ReverseDuplicate_IsRejected()
    {
        await using var db = fixture.CreateDbContext();
        var a1 = await CreateArticleAsync(db);
        var a2 = await CreateArticleAsync(db);

        // 正向链接 A→B
        db.ArticleManualLinks.Add(new ArticleManualLink { FromArticleId = a1.Id, ToArticleId = a2.Id });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        // 反向链接 B→A 应被无向唯一索引拒绝
        db.ArticleManualLinks.Add(new ArticleManualLink { FromArticleId = a2.Id, ToArticleId = a1.Id });
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        var pgEx = Assert.IsType<PostgresException>(ex.InnerException);
        Assert.Equal("23505", pgEx.SqlState); // 唯一约束冲突
    }

    [Fact]
    public async Task TwoDistinctLinks_AreAllowed()
    {
        await using var db = fixture.CreateDbContext();
        var a1 = await CreateArticleAsync(db);
        var a2 = await CreateArticleAsync(db);
        var a3 = await CreateArticleAsync(db);

        db.ArticleManualLinks.AddRange(
            new ArticleManualLink { FromArticleId = a1.Id, ToArticleId = a2.Id },
            new ArticleManualLink { FromArticleId = a1.Id, ToArticleId = a3.Id });

        await db.SaveChangesAsync(); // 不同目标，应成功
    }
}
