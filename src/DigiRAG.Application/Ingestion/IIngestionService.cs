namespace DigiRAG.Application.Ingestion;

public interface IIngestionService
{
    Task ProcessDocumentAsync(Guid tenantId, Guid documentId, CancellationToken cancellationToken = default);
}
