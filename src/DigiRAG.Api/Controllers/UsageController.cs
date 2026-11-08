using DigiRAG.Application.Common.Interfaces;
using DigiRAG.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DigiRAG.Api.Controllers;

[ApiController]
[Route("api/v1/usage")]
public class UsageController : ControllerBase
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public UsageController(IApplicationDbContext dbContext, ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    private Guid RequireTenantId()
    {
        if (!_tenantContext.HasTenant)
        {
            throw new BadHttpRequestException("Missing or invalid X-Tenant-Id header.");
        }
        return _tenantContext.TenantId!.Value;
    }

    [HttpGet]
    public async Task<ActionResult<object>> GetTenantUsage(CancellationToken ct)
    {
        var tenantId = RequireTenantId();

        var kbCount = await _dbContext.KnowledgeBases.CountAsync(kb => kb.TenantId == tenantId, ct);
        var docCount = await _dbContext.Documents.CountAsync(d => d.TenantId == tenantId, ct);
        var chunkCount = await _dbContext.DocumentChunks.CountAsync(c => c.TenantId == tenantId, ct);
        var totalBytes = await _dbContext.Documents.Where(d => d.TenantId == tenantId).SumAsync(d => d.FileSizeBytes, ct);
        
        var queuedJobs = await _dbContext.IngestionJobs.CountAsync(j => j.TenantId == tenantId && j.Status == DocumentStatus.Queued, ct);
        var processingJobs = await _dbContext.IngestionJobs.CountAsync(j => j.TenantId == tenantId && j.Status == DocumentStatus.Processing, ct);
        var failedJobs = await _dbContext.IngestionJobs.CountAsync(j => j.TenantId == tenantId && j.Status == DocumentStatus.Failed, ct);

        var chatSessions = await _dbContext.ChatSessions.CountAsync(s => s.TenantId == tenantId, ct);

        return Ok(new
        {
            TenantId = tenantId,
            KnowledgeBasesCount = kbCount,
            DocumentsCount = docCount,
            TotalChunksIndexed = chunkCount,
            TotalStorageBytes = totalBytes,
            Jobs = new
            {
                Queued = queuedJobs,
                Processing = processingJobs,
                Failed = failedJobs
            },
            ChatSessionsCount = chatSessions
        });
    }
}
