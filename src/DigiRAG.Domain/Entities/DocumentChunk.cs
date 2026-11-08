using Pgvector;

namespace DigiRAG.Domain.Entities;

public class DocumentChunk
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid KnowledgeBaseId { get; set; }
    public Guid DocumentId { get; set; }
    public int ChunkIndex { get; set; }
    public string Content { get; set; } = string.Empty;
    public int? PageNumber { get; set; }
    public string? HeadingPath { get; set; }
    public string MetadataJson { get; set; } = "{}";
    public Vector? Embedding { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Tenant? Tenant { get; set; }
    public KnowledgeBase? KnowledgeBase { get; set; }
    public Document? Document { get; set; }
}
