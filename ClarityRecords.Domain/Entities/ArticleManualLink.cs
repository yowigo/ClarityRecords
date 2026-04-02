namespace ClarityRecords.Domain.Entities;

public class ArticleManualLink
{
    public Guid Id { get; set; }
    public Guid FromArticleId { get; set; }
    public Guid ToArticleId { get; set; }
    public string? RelationNote { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Article FromArticle { get; set; } = null!;
    public Article ToArticle { get; set; } = null!;
}
