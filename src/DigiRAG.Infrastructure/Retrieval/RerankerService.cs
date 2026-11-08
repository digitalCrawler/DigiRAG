using DigiRAG.Application.Retrieval;

namespace DigiRAG.Infrastructure.Retrieval;

public class RerankerService : IRerankerService
{
    public Task<IReadOnlyList<SearchResultDto>> RerankAsync(
        string query,
        IReadOnlyList<SearchResultDto> candidates,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        if (candidates.Count == 0)
        {
            return Task.FromResult<IReadOnlyList<SearchResultDto>>(Array.Empty<SearchResultDto>());
        }

        var terms = query.ToLowerInvariant()
            .Split(new[] { ' ', ',', '.', '?', '!', ';', ':' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length > 2)
            .ToHashSet();

        // Cross-scoring boost combining original dense score with exact query term frequency
        foreach (var item in candidates)
        {
            var contentLower = item.Content.ToLowerInvariant();
            var matches = terms.Count(t => contentLower.Contains(t));
            var termOverlapRatio = terms.Count > 0 ? (double)matches / terms.Count : 0.0;

            // Combine vector score with keyword density boost
            item.RerankScore = (item.Score * 0.7) + (termOverlapRatio * 0.3);
        }

        var reranked = candidates
            .OrderByDescending(c => c.RerankScore ?? c.Score)
            .Take(topK)
            .ToList();

        return Task.FromResult<IReadOnlyList<SearchResultDto>>(reranked);
    }
}
