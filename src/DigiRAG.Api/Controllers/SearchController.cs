using DigiRAG.Application.Common.Interfaces;
using DigiRAG.Application.Retrieval;
using Microsoft.AspNetCore.Mvc;

namespace DigiRAG.Api.Controllers;

[ApiController]
[Route("api/v1/search")]
public class SearchController : ControllerBase
{
    private readonly IRetrievalService _retrievalService;
    private readonly IRerankerService _rerankerService;
    private readonly ITenantContext _tenantContext;

    public SearchController(
        IRetrievalService retrievalService,
        IRerankerService rerankerService,
        ITenantContext tenantContext)
    {
        _retrievalService = retrievalService;
        _rerankerService = rerankerService;
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

    public record SearchRequest(string Query, Guid? KnowledgeBaseId, int Limit = 10);

    [HttpPost]
    public async Task<ActionResult<List<SearchResultDto>>> Search([FromBody] SearchRequest request, CancellationToken ct)
    {
        var tenantId = RequireTenantId();

        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return BadRequest("Query cannot be empty.");
        }

        var candidates = await _retrievalService.HybridSearchAsync(
            tenantId,
            request.KnowledgeBaseId,
            request.Query,
            request.Limit * 2,
            ct);

        var reranked = await _rerankerService.RerankAsync(
            request.Query,
            candidates,
            request.Limit,
            ct);

        return Ok(reranked);
    }
}
