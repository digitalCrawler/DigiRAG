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

    // gemini-1.5-flash was retired and gemini-2.0-flash was shut down on 2026-06-01.
    // gemini-2.5-flash is the current stable price/performance model.
    public string ChatModel { get; set; } = "gemini-2.5-flash";

    // text-embedding-004 was shut down on 2026-01-14. gemini-embedding-001 replaces it,
    // but it returns 3072 dimensions unless a smaller size is requested explicitly -
    // and the document_chunks.Embedding column is vector(768).
    public string EmbeddingModel { get; set; } = "gemini-embedding-001";
    public int EmbeddingDimensions { get; set; } = 768;
}
