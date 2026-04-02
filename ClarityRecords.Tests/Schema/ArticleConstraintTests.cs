using ClarityRecords.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ClarityRecords.Tests.Schema;

[Collection("Database")]
public class ArticleConstraintTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task Slug_MustBeUnique()
    {
        await using var db = fixture.CreateDbContext();
        var slug = $"slug-{Guid.NewGuid():N}";
        db.Articles.AddRange(
            new Article { Slug = slug, Title = "First", Body = "" },
            new Article { Slug = slug, Title = "Second", Body = "" });

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        var pgEx = Assert.IsType<PostgresException>(ex.InnerException);
        Assert.Equal("23505", pgEx.SqlState); // 唯一约束冲突
    }

    [Fact]
    public async Task Article_CanBeInserted_WithMinimalFields()
    {
        await using var db = fixture.CreateDbContext();
        db.Articles.Add(new Article
        {
            Slug = $"minimal-{Guid.NewGuid():N}",
            Title = "极简文章",
            Body = "正文"
        });

        await db.SaveChangesAsync(); // CreatedAt/UpdatedAt 由数据库 NOW() 填充
    }

    [Fact]
    public async Task PublishedAt_CanBeNull_ForDrafts()
    {
        await using var db = fixture.CreateDbContext();
        var slug = $"draft-{Guid.NewGuid():N}";
        db.Articles.Add(new Article { Slug = slug, Title = "草稿", Body = "" });
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();
        var saved = await db.Articles.FirstAsync(a => a.Slug == slug);
        Assert.Null(saved.PublishedAt);
    }
}
