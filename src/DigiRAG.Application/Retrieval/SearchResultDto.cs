namespace DigiRAG.Application.Retrieval;

public class SearchResultDto
{
    public Guid ChunkId { get; set; }
    public Guid DocumentId { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public int? PageNumber { get; set; }
    public string? HeadingPath { get; set; }
    public string Content { get; set; } = string.Empty;
    /// <summary>Fusion score. Rewritten by Reciprocal Rank Fusion, so it ranks but does not measure.</summary>
    public double Score { get; set; }

    /// <summary>
    /// Raw cosine similarity against the query, kept separately because RRF overwrites
    /// Score with a rank-derived value (~0.016) that cannot answer "is this relevant at all".
    /// </summary>
    public double VectorScore { get; set; }

    public double? RerankScore { get; set; }
}

public class CitationDto
{
    public Guid DocumentId { get; set; }
    public Guid ChunkId { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public int? PageNumber { get; set; }
    public string? HeadingPath { get; set; }
    public string Excerpt { get; set; } = string.Empty;
}
