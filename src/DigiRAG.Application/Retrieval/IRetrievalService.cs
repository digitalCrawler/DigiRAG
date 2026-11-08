namespace DigiRAG.Application.Retrieval;

public interface IRetrievalService
{
    Task<IReadOnlyList<SearchResultDto>> HybridSearchAsync(
        Guid tenantId,
        Guid? knowledgeBaseId,
        string query,
        int limit = 10,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SearchResultDto>> VectorSearchAsync(
        Guid tenantId,
        Guid? knowledgeBaseId,
        float[] queryEmbedding,
        int limit = 10,
        CancellationToken cancellationToken = default);
}
