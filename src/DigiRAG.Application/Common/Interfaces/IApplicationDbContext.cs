using DigiRAG.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DigiRAG.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<KnowledgeBase> KnowledgeBases { get; }
    DbSet<Document> Documents { get; }
    DbSet<DocumentChunk> DocumentChunks { get; }
    DbSet<IngestionJob> IngestionJobs { get; }
    DbSet<ChatSession> ChatSessions { get; }
    DbSet<ChatMessage> ChatMessages { get; }
    DbSet<ChatCitation> ChatCitations { get; }
    DbSet<ChatFeedback> ChatFeedbacks { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
