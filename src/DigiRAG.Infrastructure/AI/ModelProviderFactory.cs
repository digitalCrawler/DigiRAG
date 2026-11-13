using System.ClientModel;
using DigiRAG.Application.AI;
using DigiRAG.Domain.Enums;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAI;

namespace DigiRAG.Infrastructure.AI;

public class ModelProviderFactory : IAiModelFactory
{
    private readonly AiSettings _settings;

    public ModelProviderFactory(IOptions<AiSettings> settings)
    {
        _settings = settings.Value;
    }

    public IChatClient GetChatClient()
    {
        return _settings.ActiveProvider switch
        {
            AiProviderType.Local => CreateLocalChatClient(),
            AiProviderType.OpenAI => CreateOpenAiChatClient(),
            AiProviderType.AzureOpenAI => CreateAzureOpenAiChatClient(),
            AiProviderType.Gemini => CreateGeminiChatClient(),
            _ => CreateLocalChatClient()
        };
    }

    public IEmbeddingGenerator<string, Embedding<float>> GetEmbeddingGenerator()
    {
        return _settings.ActiveProvider switch
        {
            AiProviderType.Local => CreateLocalEmbeddingGenerator(),
            AiProviderType.OpenAI => CreateOpenAiEmbeddingGenerator(),
            AiProviderType.AzureOpenAI => CreateAzureOpenAiEmbeddingGenerator(),
            AiProviderType.Gemini => CreateGeminiEmbeddingGenerator(),
            _ => CreateLocalEmbeddingGenerator()
        };
    }

    public int GetCurrentEmbeddingDimensions()
    {
        return _settings.ActiveProvider switch
        {
            AiProviderType.Local => _settings.Local.EmbeddingDimensions,
            AiProviderType.OpenAI => _settings.OpenAI.EmbeddingDimensions,
            AiProviderType.AzureOpenAI => _settings.AzureOpenAI.EmbeddingDimensions,
            AiProviderType.Gemini => _settings.Gemini.EmbeddingDimensions,
            _ => 768
        };
    }

    #region Provider Implementations

    private IChatClient CreateLocalChatClient()
    {
        var endpoint = _settings.Local.Endpoint.TrimEnd('/') + "/v1";
        var clientOptions = new OpenAIClientOptions { Endpoint = new Uri(endpoint) };
        var client = new OpenAIClient(new ApiKeyCredential("ollama"), clientOptions);
        return client.AsChatClient(_settings.Local.ChatModel);
    }

    private IEmbeddingGenerator<string, Embedding<float>> CreateLocalEmbeddingGenerator()
    {
        var endpoint = _settings.Local.Endpoint.TrimEnd('/') + "/v1";
        var clientOptions = new OpenAIClientOptions { Endpoint = new Uri(endpoint) };
        var client = new OpenAIClient(new ApiKeyCredential("ollama"), clientOptions);
        return client.AsEmbeddingGenerator(_settings.Local.EmbeddingModel);
    }

    private IChatClient CreateOpenAiChatClient()
    {
        var apiKey = string.IsNullOrWhiteSpace(_settings.OpenAI.ApiKey) ? "placeholder" : _settings.OpenAI.ApiKey;
        var client = new OpenAIClient(new ApiKeyCredential(apiKey));
        return client.AsChatClient(_settings.OpenAI.ChatModel);
    }

    private IEmbeddingGenerator<string, Embedding<float>> CreateOpenAiEmbeddingGenerator()
    {
        var apiKey = string.IsNullOrWhiteSpace(_settings.OpenAI.ApiKey) ? "placeholder" : _settings.OpenAI.ApiKey;
        var client = new OpenAIClient(new ApiKeyCredential(apiKey));
        return client.AsEmbeddingGenerator(_settings.OpenAI.EmbeddingModel);
    }

    private IChatClient CreateAzureOpenAiChatClient()
    {
        var endpoint = _settings.AzureOpenAI.Endpoint;
        var apiKey = string.IsNullOrWhiteSpace(_settings.AzureOpenAI.ApiKey) ? "placeholder" : _settings.AzureOpenAI.ApiKey;
        var clientOptions = new OpenAIClientOptions { Endpoint = string.IsNullOrEmpty(endpoint) ? null : new Uri(endpoint) };
        var client = new OpenAIClient(new ApiKeyCredential(apiKey), clientOptions);
        return client.AsChatClient(_settings.AzureOpenAI.ChatDeploymentName);
    }

    private IEmbeddingGenerator<string, Embedding<float>> CreateAzureOpenAiEmbeddingGenerator()
    {
        var endpoint = _settings.AzureOpenAI.Endpoint;
        var apiKey = string.IsNullOrWhiteSpace(_settings.AzureOpenAI.ApiKey) ? "placeholder" : _settings.AzureOpenAI.ApiKey;
        var clientOptions = new OpenAIClientOptions { Endpoint = string.IsNullOrEmpty(endpoint) ? null : new Uri(endpoint) };
        var client = new OpenAIClient(new ApiKeyCredential(apiKey), clientOptions);
        return client.AsEmbeddingGenerator(_settings.AzureOpenAI.EmbeddingDeploymentName);
    }

    // Google exposes an OpenAI-compatible REST surface for the Gemini API,
    // which lets the OpenAI client be reused for both chat and embeddings.
    private static readonly Uri GeminiOpenAiEndpoint =
        new("https://generativelanguage.googleapis.com/v1beta/openai/");

    private OpenAIClient CreateGeminiClient()
    {
        var apiKey = _settings.Gemini.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "Gemini is the active AI provider but no API key is configured. " +
                "Set AiSettings:Gemini:ApiKey (environment variable " +
                "AiSettings__Gemini__ApiKey) to a Google AI Studio key.");
        }

        var clientOptions = new OpenAIClientOptions { Endpoint = GeminiOpenAiEndpoint };
        return new OpenAIClient(new ApiKeyCredential(apiKey), clientOptions);
    }

    private IChatClient CreateGeminiChatClient()
    {
        return CreateGeminiClient().AsChatClient(_settings.Gemini.ChatModel);
    }

    private IEmbeddingGenerator<string, Embedding<float>> CreateGeminiEmbeddingGenerator()
    {
        // The dimension count is passed explicitly: Gemini embedding models
        // return 3072-dimension vectors unless a smaller output size is
        // requested, which would not fit the vector(768) column.
        return CreateGeminiClient()
            .AsEmbeddingGenerator(_settings.Gemini.EmbeddingModel, _settings.Gemini.EmbeddingDimensions);
    }

    #endregion
}
