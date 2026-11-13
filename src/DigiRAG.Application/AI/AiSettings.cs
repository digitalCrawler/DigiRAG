using DigiRAG.Domain.Enums;

namespace DigiRAG.Application.AI;

public class AiSettings
{
    public const string SectionName = "AiSettings";

    public AiProviderType ActiveProvider { get; set; } = AiProviderType.Local;
    
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
    public string ChatModel { get; set; } = "gemini-3.8-flash";

    /// <summary>
    /// Gemini embedding model. Produces 3072-dimension vectors by default; the
    /// value of <see cref="EmbeddingDimensions"/> is sent as an explicit output
    /// size so the result matches the pgvector column width.
    /// </summary>
    public string EmbeddingModel { get; set; } = "gemini-embedding-2";

    /// <summary>
    /// Must match the <c>vector(n)</c> column width on DocumentChunk.Embedding
    /// (currently 768). gemini-embedding-2 supports 128-3072 and L2-normalizes
    /// truncated outputs automatically.
    /// </summary>
    public int EmbeddingDimensions { get; set; } = 768;
}
