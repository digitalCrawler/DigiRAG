namespace DigiRAG.Application.Retrieval;

public interface IRerankerService
{
    Task<IReadOnlyList<SearchResultDto>> RerankAsync(
        string query,
        IReadOnlyList<SearchResultDto> candidates,
        int topK = 5,
        CancellationToken cancellationToken = default);
}
