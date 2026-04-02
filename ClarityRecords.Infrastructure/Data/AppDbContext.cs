using ClarityRecords.Domain.Entities;
using ClarityRecords.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ClarityRecords.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<ArticleTag> ArticleTags => Set<ArticleTag>();
    public DbSet<ArticleManualLink> ArticleManualLinks => Set<ArticleManualLink>();
    public DbSet<ReadingTrace> ReadingTraces => Set<ReadingTrace>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Article>(e =>
        {
            e.ToTable("articles");
            e.HasKey(a => a.Id);
            e.Property(a => a.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(a => a.Slug).HasMaxLength(200).IsRequired();
            e.Property(a => a.Title).HasMaxLength(500).IsRequired();
            e.Property(a => a.Body).IsRequired();
            e.Property(a => a.Summary).HasMaxLength(500);
            e.Property(a => a.CreatedAt).HasDefaultValueSql("NOW()");
            e.Property(a => a.UpdatedAt).HasDefaultValueSql("NOW()");
            e.HasIndex(a => a.Slug).IsUnique();
            e.Property(a => a.AuthorId).HasMaxLength(450);
            e.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(a => a.AuthorId)
                .OnDelete(DeleteBehavior.SetNull);
            // Embedding column added in Phase 3 migration
        });

        modelBuilder.Entity<Tag>(e =>
        {
            e.ToTable("tags");
            e.HasKey(t => t.Id);
            e.Property(t => t.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(t => t.Name).HasMaxLength(100).IsRequired();
            e.Property(t => t.Slug).HasMaxLength(100).IsRequired();
            e.HasIndex(t => t.Name).IsUnique();
            e.HasIndex(t => t.Slug).IsUnique();
        });

        modelBuilder.Entity<ArticleTag>(e =>
        {
            e.ToTable("article_tags");
            e.HasKey(at => new { at.ArticleId, at.TagId });
            e.HasOne(at => at.Article)
                .WithMany(a => a.ArticleTags)
                .HasForeignKey(at => at.ArticleId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(at => at.Tag)
                .WithMany(t => t.ArticleTags)
                .HasForeignKey(at => at.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ArticleManualLink>(e =>
        {
            e.ToTable("article_manual_links");
            e.HasKey(l => l.Id);
            e.Property(l => l.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(l => l.RelationNote).HasMaxLength(200);
            e.Property(l => l.CreatedAt).HasDefaultValueSql("NOW()");
            e.HasOne(l => l.FromArticle)
                .WithMany(a => a.OutgoingLinks)
                .HasForeignKey(l => l.FromArticleId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(l => l.ToArticle)
                .WithMany(a => a.IncomingLinks)
                .HasForeignKey(l => l.ToArticleId)
                .OnDelete(DeleteBehavior.Cascade);
            // Undirected uniqueness index and self-loop check are applied in the migration SQL directly
        });

        modelBuilder.Entity<ReadingTrace>(e =>
        {
            e.ToTable("reading_trace");
            e.HasKey(r => r.Id);
            e.Property(r => r.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(r => r.EventType).HasMaxLength(50).IsRequired();
            e.Property(r => r.OccurredAt).HasDefaultValueSql("NOW()");
            e.HasOne(r => r.Article)
                .WithMany()
                .HasForeignKey(r => r.ArticleId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(r => r.RelatedArticle)
                .WithMany()
                .HasForeignKey(r => r.RelatedArticleId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(r => r.ManualLink)
                .WithMany()
                .HasForeignKey(r => r.ManualLinkId)
                .OnDelete(DeleteBehavior.SetNull);
            // CHECK constraint applied in migration SQL directly
        });
    }
}
