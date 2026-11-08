namespace DigiRAG.Application.Retrieval;

public class ChatPromptRequest
{
    public Guid? SessionId { get; set; }
    public Guid? KnowledgeBaseId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class ChatStreamEvent
{
    public string Type { get; set; } = string.Empty; // "token", "citations", "done", "error"
    public string? Content { get; set; }
    public IReadOnlyList<CitationDto>? Citations { get; set; }
}

public interface IChatService
{
    IAsyncEnumerable<ChatStreamEvent> StreamChatResponseAsync(
        Guid tenantId,
        ChatPromptRequest request,
        CancellationToken cancellationToken = default);
}
