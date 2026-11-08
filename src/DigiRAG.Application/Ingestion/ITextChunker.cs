using DigiRAG.Domain.Entities;

namespace DigiRAG.Application.Ingestion;

public interface ITextChunker
{
    IReadOnlyList<DocumentChunk> Chunk(
        ExtractedDocument document,
        Guid tenantId,
        Guid knowledgeBaseId,
        Guid documentId,
        int targetChunkTokens = 500,
        int chunkOverlapTokens = 80);
}
