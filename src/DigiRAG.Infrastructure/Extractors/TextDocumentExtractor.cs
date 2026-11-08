using DigiRAG.Application.Ingestion;

namespace DigiRAG.Infrastructure.Extractors;

public class TextDocumentExtractor : IDocumentExtractor
{
    public bool CanHandle(string contentType, string fileName)
    {
        return contentType.StartsWith("text/") ||
               fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) ||
               fileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ||
               fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) ||
               fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<ExtractedDocument> ExtractAsync(Stream stream, string fileName, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(stream);
        var text = await reader.ReadToEndAsync(cancellationToken);

        var result = new ExtractedDocument { FileName = fileName };
        result.Pages.Add(new ExtractedPage
        {
            PageNumber = 1,
            Text = text
        });

        return result;
    }
}
