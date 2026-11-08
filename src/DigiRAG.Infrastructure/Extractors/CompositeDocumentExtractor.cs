using DigiRAG.Application.Ingestion;

namespace DigiRAG.Infrastructure.Extractors;

public class CompositeDocumentExtractor : IDocumentExtractor
{
    private readonly IEnumerable<IDocumentExtractor> _extractors;

    public CompositeDocumentExtractor(IEnumerable<IDocumentExtractor> extractors)
    {
        _extractors = extractors;
    }

    public bool CanHandle(string contentType, string fileName)
    {
        return _extractors.Any(e => e.CanHandle(contentType, fileName));
    }

    public Task<ExtractedDocument> ExtractAsync(Stream stream, string fileName, CancellationToken cancellationToken = default)
    {
        var extractor = _extractors.FirstOrDefault(e => e.CanHandle(string.Empty, fileName));
        if (extractor == null)
        {
            throw new NotSupportedException($"No extractor available for file: {fileName}");
        }

        return extractor.ExtractAsync(stream, fileName, cancellationToken);
    }
}
