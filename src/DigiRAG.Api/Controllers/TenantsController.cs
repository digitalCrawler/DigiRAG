using DigiRAG.Application.Common.Interfaces;
using DigiRAG.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DigiRAG.Api.Controllers;

[ApiController]
[Route("api/v1/tenants")]
public class TenantsController : ControllerBase
{
    private readonly IApplicationDbContext _dbContext;

    public TenantsController(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<List<Tenant>>> GetTenants(CancellationToken ct)
    {
        var tenants = await _dbContext.Tenants.OrderBy(t => t.Name).ToListAsync(ct);
        return Ok(tenants);
    }

    public record CreateTenantRequest(string Name, string Slug);

    [HttpPost]
    public async Task<ActionResult<Tenant>> CreateTenant([FromBody] CreateTenantRequest request, CancellationToken ct)
    {
        var tenant = new Tenant
        {
            Name = request.Name,
            Slug = request.Slug.ToLowerInvariant().Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetTenants), new { id = tenant.Id }, tenant);
    }
}
