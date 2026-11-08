using DigiRAG.Application.Retrieval;
using DigiRAG.Infrastructure.Retrieval;

namespace DigiRAG.UnitTests.Evaluation;

public class EvaluationRunnerTests
{
    private readonly IRerankerService _reranker = new RerankerService();

    [Fact]
    public async Task Evaluate_FactRetrievalQueries_ShouldRankCorrectEvidenceAtTop()
    {
        // Sample extracted chunks from Acme Employee Handbook
        var handbookChunks = new List<SearchResultDto>
        {
            new()
            {
                ChunkId = Guid.NewGuid(),
                DocumentName = "Acme_Employee_Handbook.md",
                PageNumber = 1,
                HeadingPath = "2.1 Annual Vacation",
                Content = "Full-time employees receive 25 days of paid annual vacation per calendar year.",
                Score = 0.88
            },
            new()
            {
                ChunkId = Guid.NewGuid(),
                DocumentName = "Acme_Employee_Handbook.md",
                PageNumber = 1,
                HeadingPath = "2.2 Parental Leave",
                Content = "Acme provides up to 16 weeks of fully paid parental leave for all eligible parents following the birth, adoption, or foster placement.",
                Score = 0.85
            },
            new()
            {
                ChunkId = Guid.NewGuid(),
                DocumentName = "Acme_Employee_Handbook.md",
                PageNumber = 2,
                HeadingPath = "3. Remote Work & Equipment",
                Content = "Every full-time remote employee receives a one-time home office setup reimbursement of up to $1,500.",
                Score = 0.82
            }
        };

        var factQueries = EvalDataset.StarterQueries.Where(q => q.Category == "FactRetrieval").ToList();

        foreach (var query in factQueries)
        {
            var ranked = await _reranker.RerankAsync(query.Question, handbookChunks, topK: 1);

            Assert.NotEmpty(ranked);
            var topResult = ranked[0];

            // Verify evidence contains the expected fact keyword
            if (!string.IsNullOrEmpty(query.ExpectedKeyword))
            {
                Assert.Contains(query.ExpectedKeyword, topResult.Content, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public async Task Evaluate_SafeAbstention_WhenEvidenceIsIrrelevant()
    {
        // Unrelated evidence
        var irrelevantEvidence = new List<SearchResultDto>
        {
            new()
            {
                ChunkId = Guid.NewGuid(),
                DocumentName = "OfficeRules.md",
                Content = "Office desks must be kept tidy at the end of each working day.",
                Score = 0.15
            }
        };

        var abstentionQueries = EvalDataset.StarterQueries.Where(q => q.ShouldAbstain).ToList();

        foreach (var query in abstentionQueries)
        {
            var ranked = await _reranker.RerankAsync(query.Question, irrelevantEvidence, topK: 1);
            
            // The score should remain low
            Assert.True((ranked[0].RerankScore ?? ranked[0].Score) < 0.6, 
                $"Query '{query.Question}' scored too high on irrelevant context.");
        }
    }
}
