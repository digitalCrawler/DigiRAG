using System.Text.RegularExpressions;
using DigiRAG.Application.Ingestion;
using DigiRAG.Domain.Entities;

namespace DigiRAG.Infrastructure.Chunking;

public class StructureAwareTextChunker : ITextChunker
{
    // Heuristic estimation: 1 token ≈ 4 characters
    private const int CharsPerToken = 4;

    public IReadOnlyList<DocumentChunk> Chunk(
        ExtractedDocument document,
        Guid tenantId,
        Guid knowledgeBaseId,
        Guid documentId,
        int targetChunkTokens = 500,
        int chunkOverlapTokens = 80)
    {
        var chunks = new List<DocumentChunk>();
        var chunkIndex = 0;
        var maxChars = targetChunkTokens * CharsPerToken;
        var overlapChars = chunkOverlapTokens * CharsPerToken;

        foreach (var page in document.Pages)
        {
            var text = page.Text;
            if (string.IsNullOrWhiteSpace(text)) continue;

            // Split into paragraph blocks
            var paragraphs = Regex.Split(text, @"\r?\n\s*\r?\n")
                                  .Select(p => p.Trim())
                                  .Where(p => !string.IsNullOrEmpty(p))
                                  .ToList();

            var currentChunk = new System.Text.StringBuilder();
            string? currentHeading = null;

            foreach (var paragraph in paragraphs)
            {
                // Detect markdown heading or short heading line
                if (paragraph.StartsWith("#") || (paragraph.Length < 60 && !paragraph.EndsWith('.')))
                {
                    currentHeading = paragraph.Trim('#', ' ');
                }

                if (currentChunk.Length + paragraph.Length > maxChars && currentChunk.Length > 0)
                {
                    // Emit chunk
                    var chunkContent = currentChunk.ToString().Trim();
                    chunks.Add(new DocumentChunk
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        KnowledgeBaseId = knowledgeBaseId,
                        DocumentId = documentId,
                        ChunkIndex = chunkIndex++,
                        Content = chunkContent,
                        PageNumber = page.PageNumber,
                        HeadingPath = currentHeading,
                        CreatedAtUtc = DateTime.UtcNow
                    });

                    // Start next chunk with overlap
                    currentChunk.Clear();
                    if (chunkContent.Length > overlapChars)
                    {
                        var overlapText = chunkContent[^overlapChars..];
                        currentChunk.Append(overlapText).Append("\n\n");
                    }
                }

                currentChunk.AppendLine(paragraph);
            }

            if (currentChunk.Length > 0)
            {
                chunks.Add(new DocumentChunk
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    KnowledgeBaseId = knowledgeBaseId,
                    DocumentId = documentId,
                    ChunkIndex = chunkIndex++,
                    Content = currentChunk.ToString().Trim(),
                    PageNumber = page.PageNumber,
                    HeadingPath = currentHeading,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
        }

        return chunks;
    }
}
