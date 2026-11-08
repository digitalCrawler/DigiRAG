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
    public string ChatModel { get; set; } = "gemini-1.5-flash";
    public string EmbeddingModel { get; set; } = "text-embedding-004";
    public int EmbeddingDimensions { get; set; } = 768;
}
