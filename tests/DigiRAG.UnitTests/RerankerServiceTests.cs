using DigiRAG.Application.Retrieval;
using DigiRAG.Infrastructure.Retrieval;

namespace DigiRAG.UnitTests;

public class RerankerServiceTests
{
    [Fact]
    public async Task RerankAsync_ShouldPrioritizeMostRelevantPassage()
    {
        // Arrange
        var reranker = new RerankerService();
        var candidates = new List<SearchResultDto>
        {
            new()
            {
                ChunkId = Guid.NewGuid(),
                DocumentName = "GeneralPolicies.pdf",
                Content = "This document describes building access rules and parking permits.",
                Score = 0.85
            },
            new()
            {
                ChunkId = Guid.NewGuid(),
                DocumentName = "Benefits.pdf",
                Content = "Parental leave policy provides 12 weeks of paid leave for new parents upon birth or adoption.",
                Score = 0.80
            }
        };

        var query = "How many weeks of parental leave do employees get?";

        // Act
        var results = await reranker.RerankAsync(query, candidates, topK: 2);

        // Assert
        Assert.NotEmpty(results);
        Assert.Equal("Benefits.pdf", results[0].DocumentName);
        Assert.Contains("12 weeks of paid leave", results[0].Content);
    }
}
