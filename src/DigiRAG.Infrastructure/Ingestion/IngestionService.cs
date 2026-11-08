using DigiRAG.Application.AI;
using DigiRAG.Application.Common.Interfaces;
using DigiRAG.Application.Ingestion;
using DigiRAG.Domain.Entities;
using DigiRAG.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pgvector;

namespace DigiRAG.Infrastructure.Ingestion;

public class IngestionService : IIngestionService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IFileStorageService _fileStorage;
    private readonly IDocumentExtractor _extractor;
    private readonly ITextChunker _chunker;
    private readonly IAiModelFactory _aiModelFactory;
    private readonly ILogger<IngestionService> _logger;

    public IngestionService(
        IApplicationDbContext dbContext,
        IFileStorageService fileStorage,
        IDocumentExtractor extractor,
        ITextChunker chunker,
        IAiModelFactory aiModelFactory,
        ILogger<IngestionService> logger)
    {
        _dbContext = dbContext;
        _fileStorage = fileStorage;
        _extractor = extractor;
        _chunker = chunker;
        _aiModelFactory = aiModelFactory;
        _logger = logger;
    }

    public async Task ProcessDocumentAsync(Guid tenantId, Guid documentId, CancellationToken cancellationToken = default)
    {
        var document = await _dbContext.Documents
            .FirstOrDefaultAsync(d => d.Id == documentId && d.TenantId == tenantId, cancellationToken);

        if (document == null)
        {
            _logger.LogError("Document {DocumentId} for tenant {TenantId} not found", documentId, tenantId);
            return;
        }

        try
        {
            document.Status = DocumentStatus.Processing;
            await _dbContext.SaveChangesAsync(cancellationToken);

            // 1. Download file stream
            await using var stream = await _fileStorage.DownloadAsync(document.StoragePath, cancellationToken);

            // 2. Extract content & page structure
            var extractedDoc = await _extractor.ExtractAsync(stream, document.FileName, cancellationToken);

            // 3. Chunk content
            var chunks = _chunker.Chunk(extractedDoc, tenantId, document.KnowledgeBaseId, document.Id);

            if (chunks.Count == 0)
            {
                document.Status = DocumentStatus.Indexed;
                document.ChunkCount = 0;
                document.IndexedAtUtc = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync(cancellationToken);
                return;
            }

            // 4. Batch Embeddings
            var embeddingGenerator = _aiModelFactory.GetEmbeddingGenerator();
            var chunkTexts = chunks.Select(c => c.Content).ToList();

            // Generate embeddings in batches of 16
            const int batchSize = 16;
            for (var i = 0; i < chunks.Count; i += batchSize)
            {
                var batch = chunkTexts.Skip(i).Take(batchSize).ToList();
                var embeddings = await embeddingGenerator.GenerateAsync(batch, cancellationToken: cancellationToken);

                for (var j = 0; j < batch.Count; j++)
                {
                    var chunkIndex = i + j;
                    var rawVector = embeddings[j].Vector.ToArray();
                    chunks[chunkIndex].Embedding = new Vector(rawVector);
                }
            }

            // 5. Remove existing chunks if re-indexing
            var existingChunks = await _dbContext.DocumentChunks
                .Where(c => c.DocumentId == documentId)
                .ToListAsync(cancellationToken);

            if (existingChunks.Count > 0)
            {
                _dbContext.DocumentChunks.RemoveRange(existingChunks);
            }

            // 6. Save new chunks
            await _dbContext.DocumentChunks.AddRangeAsync(chunks, cancellationToken);

            document.Status = DocumentStatus.Indexed;
            document.ChunkCount = chunks.Count;
            document.IndexedAtUtc = DateTime.UtcNow;
            document.ErrorMessage = null;

            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Successfully indexed document {DocumentId} with {ChunkCount} chunks", documentId, chunks.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing document {DocumentId}", documentId);
            document.Status = DocumentStatus.Failed;
            document.ErrorMessage = ex.Message;
            await _dbContext.SaveChangesAsync(cancellationToken);
            throw;
        }
    }
}
