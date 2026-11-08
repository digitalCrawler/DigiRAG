using DigiRAG.Domain.Enums;

namespace DigiRAG.Domain.Entities;

public class Document
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid KnowledgeBaseId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string ContentHash { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public DocumentStatus Status { get; set; } = DocumentStatus.Uploaded;
    public int ChunkCount { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? IndexedAtUtc { get; set; }

    public Tenant? Tenant { get; set; }
    public KnowledgeBase? KnowledgeBase { get; set; }
    public ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
    public ICollection<IngestionJob> IngestionJobs { get; set; } = new List<IngestionJob>();
}
