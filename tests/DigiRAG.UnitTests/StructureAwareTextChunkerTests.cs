using DigiRAG.Application.Ingestion;
using DigiRAG.Infrastructure.Chunking;

namespace DigiRAG.UnitTests;

public class StructureAwareTextChunkerTests
{
    [Fact]
    public void Chunk_ShouldPreservePageNumbersAndHeadings()
    {
        // Arrange
        var chunker = new StructureAwareTextChunker();
        var doc = new ExtractedDocument
        {
            FileName = "EmployeeHandbook.pdf",
            Pages = new List<ExtractedPage>
            {
                new()
                {
                    PageNumber = 1,
                    Text = "# Chapter 1: Introduction\n\nWelcome to our company. We value excellence and integrity.\n\nAll employees must read this carefully."
                },
                new()
                {
                    PageNumber = 2,
                    Text = "## Section 2: Paid Time Off (PTO)\n\nEmployees accrue 20 days of paid vacation per calendar year.\n\nUnused days roll over up to a maximum of 5 days."
                }
            }
        };

        var tenantId = Guid.NewGuid();
        var kbId = Guid.NewGuid();
        var docId = Guid.NewGuid();

        // Act
        var chunks = chunker.Chunk(doc, tenantId, kbId, docId, targetChunkTokens: 200, chunkOverlapTokens: 30);

        // Assert
        Assert.NotEmpty(chunks);
        Assert.All(chunks, c =>
        {
            Assert.Equal(tenantId, c.TenantId);
            Assert.Equal(kbId, c.KnowledgeBaseId);
            Assert.Equal(docId, c.DocumentId);
            Assert.NotNull(c.PageNumber);
        });

        // Verify page 1 and page 2 chunk provenance
        Assert.Contains(chunks, c => c.PageNumber == 1 && c.Content.Contains("Welcome to our company"));
        Assert.Contains(chunks, c => c.PageNumber == 2 && c.Content.Contains("Paid Time Off"));
    }
}
