using DigiRAG.Application.Common.Interfaces;
using DigiRAG.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DigiRAG.Api.Controllers;

[ApiController]
[Route("api/v1/knowledge-bases")]
public class KnowledgeBasesController : ControllerBase
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public KnowledgeBasesController(IApplicationDbContext dbContext, ITenantContext tenantContext)
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
    public async Task<ActionResult<List<KnowledgeBase>>> GetKnowledgeBases(CancellationToken ct)
    {
        var tenantId = RequireTenantId();
        var kbs = await _dbContext.KnowledgeBases
            .Where(kb => kb.TenantId == tenantId)
            .OrderBy(kb => kb.Name)
            .ToListAsync(ct);

        return Ok(kbs);
    }

    public record CreateKbRequest(string Name, string? Description);

    [HttpPost]
    public async Task<ActionResult<KnowledgeBase>> CreateKnowledgeBase([FromBody] CreateKbRequest request, CancellationToken ct)
    {
        var tenantId = RequireTenantId();

        var kb = new KnowledgeBase
        {
            TenantId = tenantId,
            Name = request.Name,
            Description = request.Description,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.KnowledgeBases.Add(kb);
        await _dbContext.SaveChangesAsync(ct);

        return Ok(kb);
    }
}
