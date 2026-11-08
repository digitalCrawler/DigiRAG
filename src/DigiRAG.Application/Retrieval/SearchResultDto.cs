namespace DigiRAG.Application.Retrieval;

public class SearchResultDto
{
    public Guid ChunkId { get; set; }
    public Guid DocumentId { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public int? PageNumber { get; set; }
    public string? HeadingPath { get; set; }
    public string Content { get; set; } = string.Empty;
    public double Score { get; set; }
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
