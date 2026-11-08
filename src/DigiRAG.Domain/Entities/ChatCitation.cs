namespace DigiRAG.Domain.Entities;

public class ChatCitation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ChatMessageId { get; set; }
    public Guid DocumentId { get; set; }
    public Guid ChunkId { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public int? PageNumber { get; set; }
    public string? HeadingPath { get; set; }
    public string Excerpt { get; set; } = string.Empty;

    public ChatMessage? ChatMessage { get; set; }
    public Document? Document { get; set; }
    public DocumentChunk? Chunk { get; set; }
}
