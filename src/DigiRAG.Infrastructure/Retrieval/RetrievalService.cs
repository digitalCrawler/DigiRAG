using DigiRAG.Application.AI;
using DigiRAG.Application.Common.Interfaces;
using DigiRAG.Application.Retrieval;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace DigiRAG.Infrastructure.Retrieval;

public class RetrievalService : IRetrievalService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAiModelFactory _aiModelFactory;

    public RetrievalService(IApplicationDbContext dbContext, IAiModelFactory aiModelFactory)
    {
        _dbContext = dbContext;
        _aiModelFactory = aiModelFactory;
    }

    public async Task<IReadOnlyList<SearchResultDto>> VectorSearchAsync(
        Guid tenantId,
        Guid? knowledgeBaseId,
        float[] queryEmbedding,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var targetVector = new Vector(queryEmbedding);

        var query = _dbContext.DocumentChunks
            .Include(c => c.Document)
            .Where(c => c.TenantId == tenantId && c.Embedding != null);

        if (knowledgeBaseId.HasValue && knowledgeBaseId.Value != Guid.Empty)
        {
            query = query.Where(c => c.KnowledgeBaseId == knowledgeBaseId.Value);
        }

        // Cosine distance sorting: lower distance <=> means higher similarity
        var results = await query
            .OrderBy(c => c.Embedding!.CosineDistance(targetVector))
            .Take(limit)
            .Select(c => new SearchResultDto
            {
                ChunkId = c.Id,
                DocumentId = c.DocumentId,
                DocumentName = c.Document != null ? c.Document.FileName : string.Empty,
                PageNumber = c.PageNumber,
                HeadingPath = c.HeadingPath,
                Content = c.Content,
                Score = 1.0 - (double)c.Embedding!.CosineDistance(targetVector)
            })
            .ToListAsync(cancellationToken);

        return results;
    }

    public async Task<IReadOnlyList<SearchResultDto>> HybridSearchAsync(
        Guid tenantId,
        Guid? knowledgeBaseId,
        string userQuery,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        // 1. Generate query embedding.
        // Dimensions must be requested explicitly: gemini-embedding-001 defaults to 3072,
        // while document_chunks.Embedding is vector(768). A mismatch fails at query time.
        var generator = _aiModelFactory.GetEmbeddingGenerator();
        var embeddingOptions = new EmbeddingGenerationOptions
        {
            Dimensions = _aiModelFactory.GetCurrentEmbeddingDimensions()
        };
        var embeddingResult = await generator.GenerateAsync(new[] { userQuery }, embeddingOptions, cancellationToken);
        var queryVector = embeddingResult[0].Vector.ToArray();

        // 2. Vector candidates (Top 30)
        var vectorCandidates = await VectorSearchAsync(tenantId, knowledgeBaseId, queryVector, 30, cancellationToken);

        // 3. Keyword / Lexical candidates using ILIKE / Full-text (Top 30)
        var keywordQuery = _dbContext.DocumentChunks
            .Include(c => c.Document)
            .Where(c => c.TenantId == tenantId);

        if (knowledgeBaseId.HasValue && knowledgeBaseId.Value != Guid.Empty)
        {
            keywordQuery = keywordQuery.Where(c => c.KnowledgeBaseId == knowledgeBaseId.Value);
        }

        var keywordCandidates = await keywordQuery
            .Where(c => EF.Functions.ILike(c.Content, $"%{userQuery}%"))
            .Take(30)
            .Select(c => new SearchResultDto
            {
                ChunkId = c.Id,
                DocumentId = c.DocumentId,
                DocumentName = c.Document != null ? c.Document.FileName : string.Empty,
                PageNumber = c.PageNumber,
                HeadingPath = c.HeadingPath,
                Content = c.Content,
                Score = 1.0
            })
            .ToListAsync(cancellationToken);

        // 4. Reciprocal Rank Fusion (RRF)
        const double k = 60.0;
        var rrfScores = new Dictionary<Guid, (SearchResultDto Item, double Score)>();

        for (var i = 0; i < vectorCandidates.Count; i++)
        {
            var item = vectorCandidates[i];
            var rrf = 1.0 / (k + (i + 1));
            rrfScores[item.ChunkId] = (item, rrf);
        }

        for (var i = 0; i < keywordCandidates.Count; i++)
        {
            var item = keywordCandidates[i];
            var rrf = 1.0 / (k + (i + 1));
            if (rrfScores.TryGetValue(item.ChunkId, out var existing))
            {
                rrfScores[item.ChunkId] = (existing.Item, existing.Score + rrf);
            }
            else
            {
                rrfScores[item.ChunkId] = (item, rrf);
            }
        }

        var rankedList = rrfScores.Values
            .OrderByDescending(x => x.Score)
            .Take(limit)
            .Select(x =>
            {
                x.Item.Score = x.Score;
                return x.Item;
            })
            .ToList();

        return rankedList;
    }
}
