using System.Text.Json;
using DigiRAG.Application.AI;
using DigiRAG.Application.Common.Interfaces;
using DigiRAG.Application.Retrieval;
using DigiRAG.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DigiRAG.Api.Controllers;

[ApiController]
[Route("api/v1/chat")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly IApplicationDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly IAiModelFactory _aiModelFactory;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IChatService chatService,
        IApplicationDbContext dbContext,
        ITenantContext tenantContext,
        IAiModelFactory aiModelFactory,
        ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _dbContext = dbContext;
        _tenantContext = tenantContext;
        _aiModelFactory = aiModelFactory;
        _logger = logger;
    }

    private Guid RequireTenantId()
    {
        if (!_tenantContext.HasTenant)
        {
            throw new BadHttpRequestException("Missing or invalid X-Tenant-Id header.");
        }
        return _tenantContext.TenantId!.Value;
    }

    [HttpPost("sessions")]
    public async Task<ActionResult<ChatSession>> CreateSession([FromBody] string? title, CancellationToken ct)
    {
        var tenantId = RequireTenantId();
        var session = new ChatSession
        {
            TenantId = tenantId,
            Title = string.IsNullOrWhiteSpace(title) ? "New Conversation" : title,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.ChatSessions.Add(session);
        await _dbContext.SaveChangesAsync(ct);

        return Ok(session);
    }

    [HttpGet("sessions")]
    public async Task<ActionResult<object>> GetSessions(CancellationToken ct)
    {
        var tenantId = RequireTenantId();

        // Projected rather than returned as entities: the navigation properties form
        // cycles (session -> messages -> session) that the serializer cannot follow.
        var sessions = await _dbContext.ChatSessions
            .Where(s => s.TenantId == tenantId)
            .OrderByDescending(s => s.CreatedAtUtc)
            .Select(s => new
            {
                s.Id,
                s.Title,
                s.CreatedAtUtc,
                s.KnowledgeBaseId
            })
            .ToListAsync(ct);

        return Ok(sessions);
    }

    [HttpGet("sessions/{id}/messages")]
    public async Task<ActionResult<object>> GetSessionMessages(Guid id, CancellationToken ct)
    {
        var tenantId = RequireTenantId();
        var sessionExists = await _dbContext.ChatSessions
            .AnyAsync(s => s.Id == id && s.TenantId == tenantId, ct);

        if (!sessionExists)
        {
            return NotFound();
        }

        // Returning the entities threw "A possible object cycle was detected" because
        // ChatCitation has a ChatMessage back-reference, so this endpoint never worked.
        var messages = await _dbContext.ChatMessages
            .Where(m => m.ChatSessionId == id)
            .OrderBy(m => m.CreatedAtUtc)
            .Select(m => new
            {
                m.Id,
                m.ChatSessionId,
                Role = (int)m.Role,
                m.Content,
                m.CreatedAtUtc,
                Citations = m.Citations.Select(c => new
                {
                    c.DocumentId,
                    c.ChunkId,
                    c.DocumentName,
                    c.PageNumber,
                    c.HeadingPath,
                    c.Excerpt
                }).ToList()
            })
            .ToListAsync(ct);

        return Ok(messages);
    }

    [HttpPost("stream")]
    public async Task StreamChat([FromBody] ChatPromptRequest request, CancellationToken ct)
    {
        var tenantId = RequireTenantId();

        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        // Tells nginx/Caddy-style reverse proxies not to buffer the response, which would
        // otherwise hold the whole stream until completion and look like a hang.
        Response.Headers["X-Accel-Buffering"] = "no";

        try
        {
            await foreach (var evt in _chatService.StreamChatResponseAsync(tenantId, request, ct))
            {
                var json = JsonSerializer.Serialize(evt);
                await Response.WriteAsync($"data: {json}\n\n", ct);
                await Response.Body.FlushAsync(ct);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // The browser navigated away or hit Stop. Not an error.
        }
        catch (Exception ex)
        {
            // Retrieval needs an embedding from the configured AI provider before it touches
            // the database, so an unreachable model endpoint surfaces here. Without this the
            // client gets a bare HTTP 500 with an empty body and no way to tell why.
            var chatModel = _aiModelFactory.GetCurrentChatModel();
            var embeddingModel = _aiModelFactory.GetCurrentEmbeddingModel();

            _logger.LogError(ex,
                "Chat stream failed for tenant {TenantId} (chatModel={ChatModel}, embeddingModel={EmbeddingModel}).",
                tenantId, chatModel, embeddingModel);

            // Retrieval runs before generation, so citations arriving followed by a failure here
            // means embeddings worked and only the chat model is at fault - almost always a model
            // name the API key cannot serve.
            var hint = ex.Message.Contains("404", StringComparison.Ordinal)
                ? $" The chat model '{chatModel}' was not found. The embedding model is resolving fine, so the key is valid - this model name is likely unavailable to it. List what the key can use: GET https://generativelanguage.googleapis.com/v1beta/models?key=YOUR_KEY"
                : string.Empty;

            var errorEvent = JsonSerializer.Serialize(new
            {
                Type = "error",
                Content = $"{ex.GetType().Name}: {ex.Message} [chatModel={chatModel}]{hint}"
            });

            // CancellationToken.None: ct may already be cancelled, and this last write is
            // the only thing that tells the user what went wrong.
            await Response.WriteAsync($"data: {errorEvent}\n\n", CancellationToken.None);
            await Response.Body.FlushAsync(CancellationToken.None);
        }
    }
}
