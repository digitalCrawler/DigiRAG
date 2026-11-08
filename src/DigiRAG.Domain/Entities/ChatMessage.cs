using DigiRAG.Domain.Enums;

namespace DigiRAG.Domain.Entities;

public class ChatMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ChatSessionId { get; set; }
    public MessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ChatSession? ChatSession { get; set; }
    public ICollection<ChatCitation> Citations { get; set; } = new List<ChatCitation>();
}
