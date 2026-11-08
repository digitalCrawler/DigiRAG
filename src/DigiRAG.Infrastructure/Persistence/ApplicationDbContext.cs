using DigiRAG.Application.Common.Interfaces;
using DigiRAG.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Pgvector.EntityFrameworkCore;

namespace DigiRAG.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly ITenantContext? _tenantContext;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantContext? tenantContext = null)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<KnowledgeBase> KnowledgeBases => Set<KnowledgeBase>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();
    public DbSet<IngestionJob> IngestionJobs => Set<IngestionJob>();
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<ChatCitation> ChatCitations => Set<ChatCitation>();
    public DbSet<ChatFeedback> ChatFeedbacks => Set<ChatFeedback>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Enable pgvector extension
        modelBuilder.HasPostgresExtension("vector");

        // Tenant Configuration
        modelBuilder.Entity<Tenant>(b =>
        {
            b.ToTable("tenants");
            b.HasKey(t => t.Id);
            b.HasIndex(t => t.Slug).IsUnique();
        });

        // KnowledgeBase Configuration
        modelBuilder.Entity<KnowledgeBase>(b =>
        {
            b.ToTable("knowledge_bases");
            b.HasKey(kb => kb.Id);
            b.HasIndex(kb => new { kb.TenantId, kb.Name });

            if (_tenantContext?.HasTenant == true)
            {
                b.HasQueryFilter(kb => kb.TenantId == _tenantContext.TenantId);
            }
        });

        // Document Configuration
        modelBuilder.Entity<Document>(b =>
        {
            b.ToTable("documents");
            b.HasKey(d => d.Id);
            b.HasIndex(d => new { d.TenantId, d.KnowledgeBaseId });
            b.HasIndex(d => new { d.TenantId, d.ContentHash });

            if (_tenantContext?.HasTenant == true)
            {
                b.HasQueryFilter(d => d.TenantId == _tenantContext.TenantId);
            }
        });

        // DocumentChunk Configuration (Vectors + FullText + Metadata)
        modelBuilder.Entity<DocumentChunk>(b =>
        {
            b.ToTable("document_chunks");
            b.HasKey(c => c.Id);

            b.HasIndex(c => new { c.TenantId, c.KnowledgeBaseId });
            b.HasIndex(c => new { c.DocumentId, c.ChunkIndex }).IsUnique();

            b.Property(c => c.Embedding)
             .HasColumnType("vector(768)");

            // Vector index: HNSW with Cosine distance ops
            b.HasIndex(c => c.Embedding)
             .HasMethod("hnsw")
             .HasOperators("vector_cosine_ops");

            if (_tenantContext?.HasTenant == true)
            {
                b.HasQueryFilter(c => c.TenantId == _tenantContext.TenantId);
            }
        });

        // IngestionJob Configuration
        modelBuilder.Entity<IngestionJob>(b =>
        {
            b.ToTable("ingestion_jobs");
            b.HasKey(j => j.Id);
            b.HasIndex(j => new { j.TenantId, j.DocumentId });
        });

        // Chat Sessions & Messages
        modelBuilder.Entity<ChatSession>(b =>
        {
            b.ToTable("chat_sessions");
            b.HasKey(s => s.Id);
            b.HasIndex(s => s.TenantId);

            if (_tenantContext?.HasTenant == true)
            {
                b.HasQueryFilter(s => s.TenantId == _tenantContext.TenantId);
            }
        });

        modelBuilder.Entity<ChatMessage>(b =>
        {
            b.ToTable("chat_messages");
            b.HasKey(m => m.Id);
            b.HasIndex(m => m.ChatSessionId);
        });

        modelBuilder.Entity<ChatCitation>(b =>
        {
            b.ToTable("chat_citations");
            b.HasKey(c => c.Id);
            b.HasIndex(c => c.ChatMessageId);
        });

        modelBuilder.Entity<ChatFeedback>(b =>
        {
            b.ToTable("chat_feedbacks");
            b.HasKey(f => f.Id);
            b.HasIndex(f => new { f.TenantId, f.ChatSessionId });
        });
    }

    public async Task SetTenantSessionContextAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        // Sets PostgreSQL session variable for Row-Level Security
        await Database.ExecuteSqlRawAsync(
            "SELECT set_config('app.tenant_id', {0}, false);",
            new object[] { tenantId.ToString() },
            cancellationToken);
    }
}
