using System.Text.Json;
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
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IChatService chatService,
        IApplicationDbContext dbContext,
        ITenantContext tenantContext,
        ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _dbContext = dbContext;
        _tenantContext = tenantContext;
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
    public async Task<ActionResult<List<ChatSession>>> GetSessions(CancellationToken ct)
    {
        var tenantId = RequireTenantId();
        var sessions = await _dbContext.ChatSessions
            .Where(s => s.TenantId == tenantId)
            .OrderByDescending(s => s.CreatedAtUtc)
            .ToListAsync(ct);

        return Ok(sessions);
    }

    [HttpGet("sessions/{id}/messages")]
    public async Task<ActionResult<List<ChatMessage>>> GetSessionMessages(Guid id, CancellationToken ct)
    {
        var tenantId = RequireTenantId();
        var sessionExists = await _dbContext.ChatSessions
            .AnyAsync(s => s.Id == id && s.TenantId == tenantId, ct);

        if (!sessionExists)
        {
            return NotFound();
        }

        var messages = await _dbContext.ChatMessages
            .Where(m => m.ChatSessionId == id)
            .Include(m => m.Citations)
            .OrderBy(m => m.CreatedAtUtc)
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
            _logger.LogError(ex,
                "Chat stream failed for tenant {TenantId}. Check AiSettings:ActiveProvider and that the model endpoint is reachable.",
                tenantId);

            var errorEvent = JsonSerializer.Serialize(new
            {
                Type = "error",
                Content = $"{ex.GetType().Name}: {ex.Message}"
            });

            // CancellationToken.None: ct may already be cancelled, and this last write is
            // the only thing that tells the user what went wrong.
            await Response.WriteAsync($"data: {errorEvent}\n\n", CancellationToken.None);
            await Response.Body.FlushAsync(CancellationToken.None);
        }
    }
}
