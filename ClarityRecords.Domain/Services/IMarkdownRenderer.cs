namespace ClarityRecords.Domain.Services;

public interface IMarkdownRenderer
{
    /// <summary>
    /// Renders Markdown to sanitized HTML. Safe to render directly in Blazor with @((MarkupString)html).
    /// </summary>
    string Render(string markdown);
}
