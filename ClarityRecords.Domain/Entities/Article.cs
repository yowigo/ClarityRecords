namespace ClarityRecords.Domain.Entities;

public class Article
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public float[]? Embedding { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public string? AuthorId { get; set; }

    public ICollection<ArticleTag> ArticleTags { get; set; } = [];
    public ICollection<ArticleManualLink> OutgoingLinks { get; set; } = [];
    public ICollection<ArticleManualLink> IncomingLinks { get; set; } = [];

    public bool IsPublished => PublishedAt.HasValue && PublishedAt <= DateTimeOffset.UtcNow;
}
