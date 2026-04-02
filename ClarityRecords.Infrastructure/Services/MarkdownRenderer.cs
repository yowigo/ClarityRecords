using ClarityRecords.Domain.Services;
using Ganss.Xss;
using Markdig;

namespace ClarityRecords.Infrastructure.Services;

public sealed class MarkdownRenderer : IMarkdownRenderer
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .DisableHtml()          // 禁止原始 HTML 输入，防止 XSS
        .Build();

    private static readonly HtmlSanitizer Sanitizer = new();

    public string Render(string markdown)
    {
        var rawHtml = Markdig.Markdown.ToHtml(markdown, Pipeline);
        return Sanitizer.Sanitize(rawHtml);
    }
}
