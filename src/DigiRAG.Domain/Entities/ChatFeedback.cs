namespace DigiRAG.Domain.Entities;

public class ChatFeedback
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid ChatSessionId { get; set; }
    public Guid? ChatMessageId { get; set; }
    public bool IsPositive { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ChatSession? ChatSession { get; set; }
    public ChatMessage? ChatMessage { get; set; }
}
