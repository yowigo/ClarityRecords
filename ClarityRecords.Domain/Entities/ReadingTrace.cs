namespace ClarityRecords.Domain.Entities;

public class ReadingTrace
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public Guid? ArticleId { get; set; }
    public Guid? RelatedArticleId { get; set; }
    public Guid? ManualLinkId { get; set; }
    public string? Note { get; set; }
    public DateTimeOffset OccurredAt { get; set; }

    public Article? Article { get; set; }
    public Article? RelatedArticle { get; set; }
    public ArticleManualLink? ManualLink { get; set; }
}

public static class ReadingTraceEventType
{
    public const string NewArticle = "new_article";
    public const string ManualLink = "manual_link";
    public const string Insight = "insight";
}
