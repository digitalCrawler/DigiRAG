namespace DigiRAG.Application.Ingestion;

public class ExtractedDocument
{
    public string FileName { get; set; } = string.Empty;
    public List<ExtractedPage> Pages { get; set; } = new();
    public string FullText => string.Join("\n\n", Pages.Select(p => p.Text));
}

public class ExtractedPage
{
    public int PageNumber { get; set; }
    public string Text { get; set; } = string.Empty;
    public List<ExtractedSection> Sections { get; set; } = new();
}

public class ExtractedSection
{
    public string? Heading { get; set; }
    public string Content { get; set; } = string.Empty;
}
