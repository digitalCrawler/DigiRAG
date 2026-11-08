using DigiRAG.Domain.Enums;

namespace DigiRAG.Domain.Entities;

public class IngestionJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid DocumentId { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Queued;
    public int Attempts { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }

    public Document? Document { get; set; }
}
