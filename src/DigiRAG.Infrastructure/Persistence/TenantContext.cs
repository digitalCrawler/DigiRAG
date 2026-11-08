using DigiRAG.Application.Common.Interfaces;

namespace DigiRAG.Infrastructure.Persistence;

public class TenantContext : ITenantContext
{
    public Guid? TenantId { get; set; }
}
