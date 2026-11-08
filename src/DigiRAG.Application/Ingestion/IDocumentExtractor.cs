namespace DigiRAG.Application.Ingestion;

public interface IDocumentExtractor
{
    bool CanHandle(string contentType, string fileName);
    Task<ExtractedDocument> ExtractAsync(Stream stream, string fileName, CancellationToken cancellationToken = default);
}
