namespace DigiRAG.Application.Common.Interfaces;

public interface ITenantContext
{
    Guid? TenantId { get; set; }
    bool HasTenant => TenantId.HasValue && TenantId.Value != Guid.Empty;
}
