using Microsoft.Extensions.AI;

namespace DigiRAG.Application.AI;

public interface IAiModelFactory
{
    IChatClient GetChatClient();
    IEmbeddingGenerator<string, Embedding<float>> GetEmbeddingGenerator();
    int GetCurrentEmbeddingDimensions();

    /// <summary>
    /// The model names currently in use, for error reporting. A provider returning 404 for one
    /// model and 200 for another is the usual sign that a model name is not available to the
    /// API key, so the names belong in the error rather than only in configuration.
    /// </summary>
    string GetCurrentChatModel();

    string GetCurrentEmbeddingModel();
}
