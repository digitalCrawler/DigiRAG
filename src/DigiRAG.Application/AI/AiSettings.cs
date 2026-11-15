using DigiRAG.Domain.Enums;

namespace DigiRAG.Application.AI;

public class AiSettings
{
    public const string SectionName = "AiSettings";

    public AiProviderType ActiveProvider { get; set; } = AiProviderType.Local;

    /// <summary>
    /// How many retrieved chunks are pasted into the grounded prompt. This is the single
    /// biggest driver of tokens per question (roughly 500 tokens per chunk), so it is
    /// configurable to let a rate-limited or free-tier provider be tuned without a redeploy.
    /// </summary>
    public int MaxContextChunks { get; set; } = 3;

    /// <summary>
    /// Minimum cosine similarity for a retrieved chunk to count as relevant. Vector search
    /// always returns its nearest neighbours, however far away they are, so without a floor
    /// a greeting comes back with the three least-unrelated chunks presented as sources.
    /// The useful range depends on the embedding model; 0.55 suits gemini-embedding-001
    /// and nomic-embed-text. Lower it if good answers are being refused.
    /// </summary>
    public double MinRelevanceScore { get; set; } = 0.55;

    public LocalModelOptions Local { get; set; } = new();
    public OpenAiOptions OpenAI { get; set; } = new();
    public AzureOpenAiOptions AzureOpenAI { get; set; } = new();
    public GeminiOptions Gemini { get; set; } = new();
}

public class LocalModelOptions
{
    public string Endpoint { get; set; } = "http://localhost:11434";
    public string ChatModel { get; set; } = "llama3.2";
    public string EmbeddingModel { get; set; } = "nomic-embed-text";
    public int EmbeddingDimensions { get; set; } = 768;
}

public class OpenAiOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string ChatModel { get; set; } = "gpt-4o-mini";
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";
    public int EmbeddingDimensions { get; set; } = 1536;
}

public class AzureOpenAiOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ChatDeploymentName { get; set; } = "gpt-4o-mini";
    public string EmbeddingDeploymentName { get; set; } = "text-embedding-3-small";
    public int EmbeddingDimensions { get; set; } = 1536;
}

public class GeminiOptions
{
    public string ApiKey { get; set; } = string.Empty;

    // A rolling alias rather than a pinned version. gemini-1.5-flash was retired,
    // gemini-2.0-flash was shut down on 2026-06-01, and 2.5-series models return 404 for
    // API keys whose project never used them - which is what broke chat here. The alias
    // always resolves to the current Flash-Lite (the cheapest tier, which suits free-tier
    // quotas) and Google gives two weeks' notice before changing what sits behind it.
    // Pin an explicit version here if reproducible behaviour ever matters more than currency.
    public string ChatModel { get; set; } = "gemini-flash-lite-latest";

    // text-embedding-004 was shut down on 2026-01-14. gemini-embedding-001 replaces it,
    // but it returns 3072 dimensions unless a smaller size is requested explicitly -
    // and the document_chunks.Embedding column is vector(768).
    public string EmbeddingModel { get; set; } = "gemini-embedding-001";
    public int EmbeddingDimensions { get; set; } = 768;
}
