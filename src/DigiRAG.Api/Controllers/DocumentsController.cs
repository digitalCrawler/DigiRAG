using System.Security.Cryptography;
using DigiRAG.Application.Common.Exceptions;
using DigiRAG.Application.Common.Interfaces;
using DigiRAG.Application.Ingestion;
using DigiRAG.Domain.Entities;
using DigiRAG.Domain.Enums;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DigiRAG.Api.Controllers;

[ApiController]
[Route("api/v1")]
public class DocumentsController : ControllerBase
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly IFileStorageService _fileStorage;

    public DocumentsController(
        IApplicationDbContext dbContext,
        ITenantContext tenantContext,
        IFileStorageService fileStorage)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
        _fileStorage = fileStorage;
    }

    private Guid RequireTenantId()
    {
        if (!_tenantContext.HasTenant)
        {
            throw new BadHttpRequestException("Missing or invalid X-Tenant-Id header.");
        }
        return _tenantContext.TenantId!.Value;
    }

    [HttpGet("knowledge-bases/{kbId}/documents")]
    public async Task<ActionResult<List<Document>>> GetDocumentsByKnowledgeBase(Guid kbId, CancellationToken ct)
    {
        var tenantId = RequireTenantId();
        var docs = await _dbContext.Documents
            .Where(d => d.TenantId == tenantId && d.KnowledgeBaseId == kbId)
            .OrderByDescending(d => d.CreatedAtUtc)
            .ToListAsync(ct);

        return Ok(docs);
    }

    [HttpGet("documents/{id}")]
    public async Task<ActionResult<Document>> GetDocument(Guid id, CancellationToken ct)
    {
        var tenantId = RequireTenantId();
        var doc = await _dbContext.Documents
            .FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId, ct);

        if (doc == null)
        {
            return NotFound();
        }

        return Ok(doc);
    }

    [HttpPost("knowledge-bases/{kbId}/documents/upload")]
    [RequestSizeLimit(100 * 1024 * 1024)] // 100 MB max
    public async Task<ActionResult<Document>> UploadDocument(Guid kbId, IFormFile file, CancellationToken ct)
    {
        var tenantId = RequireTenantId();

        if (file == null || file.Length == 0)
        {
            return BadRequest("No file provided.");
        }

        // Verify Knowledge Base exists for tenant
        var kbExists = await _dbContext.KnowledgeBases
            .AnyAsync(kb => kb.Id == kbId && kb.TenantId == tenantId, ct);
        if (!kbExists)
        {
            return NotFound($"Knowledge Base {kbId} not found for this tenant.");
        }

        // Calculate SHA-256 Hash for deduplication
        string contentHash;
        await using (var hashStream = file.OpenReadStream())
        {
            using var sha256 = SHA256.Create();
            var hashBytes = await sha256.ComputeHashAsync(hashStream, ct);
            contentHash = Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        // Check for existing document with same content in this KB
        var existingDoc = await _dbContext.Documents
            .FirstOrDefaultAsync(d => d.TenantId == tenantId && d.KnowledgeBaseId == kbId && d.ContentHash == contentHash, ct);

        if (existingDoc != null && existingDoc.Status == DocumentStatus.Indexed)
        {
            return Ok(existingDoc); // Already indexed
        }

        // Upload to storage
        string storagePath;
        try
        {
            await using var uploadStream = file.OpenReadStream();
            storagePath = await _fileStorage.UploadAsync(uploadStream, file.FileName, file.ContentType, ct);
        }
        catch (FileStorageException ex)
        {
            // An unwritable upload directory is a deployment problem, not a bad request.
            // Report it explicitly instead of letting it become an empty HTTP 500.
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "File storage is not writable.",
                storageRoot = ex.StorageRoot,
                detail = ex.Message,
                hint = "Create the directory and grant the service account write access, or set Storage:LocalPath to a writable location."
            });
        }

        // Create Document record
        var document = new Document
        {
            TenantId = tenantId,
            KnowledgeBaseId = kbId,
            FileName = file.FileName,
            ContentType = file.ContentType,
            FileSizeBytes = file.Length,
            ContentHash = contentHash,
            StoragePath = storagePath,
            Status = DocumentStatus.Queued,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Documents.Add(document);
        await _dbContext.SaveChangesAsync(ct);

        // Enqueue durable Hangfire background job
        BackgroundJob.Enqueue<IIngestionService>(svc =>
            svc.ProcessDocumentAsync(tenantId, document.Id, CancellationToken.None));

        return AcceptedAtAction(nameof(GetDocument), new { id = document.Id }, document);
    }

    [HttpDelete("documents/{id}")]
    public async Task<IActionResult> DeleteDocument(Guid id, CancellationToken ct)
    {
        var tenantId = RequireTenantId();
        var doc = await _dbContext.Documents
            .FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId, ct);

        if (doc == null)
        {
            return NotFound();
        }

        // Remove chunks
        var chunks = await _dbContext.DocumentChunks
            .Where(c => c.DocumentId == id)
            .ToListAsync(ct);
        _dbContext.DocumentChunks.RemoveRange(chunks);

        // Delete stored file
        await _fileStorage.DeleteAsync(doc.StoragePath, ct);

        _dbContext.Documents.Remove(doc);
        await _dbContext.SaveChangesAsync(ct);

        return NoContent();
    }
}
