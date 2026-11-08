using DigiRAG.Application.Common.Interfaces;
using DigiRAG.Infrastructure.Persistence;

namespace DigiRAG.Api.Middleware;

public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext, ApplicationDbContext dbContext)
    {
        if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantHeader) &&
            Guid.TryParse(tenantHeader, out var tenantId))
        {
            tenantContext.TenantId = tenantId;

            // Configure Postgres Session RLS
            try
            {
                await dbContext.SetTenantSessionContextAsync(tenantId, context.RequestAborted);
            }
            catch
            {
                // In dev or before migrations, gracefully continue
            }
        }

        await _next(context);
    }
}
