namespace DigiRAG.Domain.Entities;

public class KnowledgeBase
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Tenant? Tenant { get; set; }
    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
}
