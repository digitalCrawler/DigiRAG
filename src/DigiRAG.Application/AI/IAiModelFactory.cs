using Microsoft.Extensions.AI;

namespace DigiRAG.Application.AI;

public interface IAiModelFactory
{
    IChatClient GetChatClient();
    IEmbeddingGenerator<string, Embedding<float>> GetEmbeddingGenerator();
    int GetCurrentEmbeddingDimensions();
}
