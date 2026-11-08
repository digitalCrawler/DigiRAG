using DigiRAG.Application.Common.Interfaces;
using DigiRAG.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace DigiRAG.Api.Controllers;

[ApiController]
[Route("api/v1/chat/feedback")]
public class FeedbackController : ControllerBase
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public FeedbackController(IApplicationDbContext dbContext, ITenantContext tenantContext)
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

    public record SubmitFeedbackRequest(Guid SessionId, Guid? MessageId, bool IsPositive, string? Comment);

    [HttpPost]
    public async Task<IActionResult> SubmitFeedback([FromBody] SubmitFeedbackRequest request, CancellationToken ct)
    {
        var tenantId = RequireTenantId();

        var feedback = new ChatFeedback
        {
            TenantId = tenantId,
            ChatSessionId = request.SessionId,
            ChatMessageId = request.MessageId,
            IsPositive = request.IsPositive,
            Comment = request.Comment,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.ChatFeedbacks.Add(feedback);
        await _dbContext.SaveChangesAsync(ct);

        return Ok(new { Message = "Feedback recorded successfully.", FeedbackId = feedback.Id });
    }
}
