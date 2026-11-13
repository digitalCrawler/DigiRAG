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
        return client.GetChatClient(_settings.Local.ChatModel).AsIChatClient();
    }

    private IEmbeddingGenerator<string, Embedding<float>> CreateLocalEmbeddingGenerator()
    {
        var endpoint = _settings.Local.Endpoint.TrimEnd('/') + "/v1";
        var clientOptions = new OpenAIClientOptions { Endpoint = new Uri(endpoint) };
        var client = new OpenAIClient(new ApiKeyCredential("ollama"), clientOptions);
        return client.GetEmbeddingClient(_settings.Local.EmbeddingModel).AsIEmbeddingGenerator();
    }

    private IChatClient CreateOpenAiChatClient()
    {
        var apiKey = string.IsNullOrWhiteSpace(_settings.OpenAI.ApiKey) ? "placeholder" : _settings.OpenAI.ApiKey;
        var client = new OpenAIClient(new ApiKeyCredential(apiKey));
        return client.GetChatClient(_settings.OpenAI.ChatModel).AsIChatClient();
    }

    private IEmbeddingGenerator<string, Embedding<float>> CreateOpenAiEmbeddingGenerator()
    {
        var apiKey = string.IsNullOrWhiteSpace(_settings.OpenAI.ApiKey) ? "placeholder" : _settings.OpenAI.ApiKey;
        var client = new OpenAIClient(new ApiKeyCredential(apiKey));
        return client.GetEmbeddingClient(_settings.OpenAI.EmbeddingModel).AsIEmbeddingGenerator();
    }

    private IChatClient CreateAzureOpenAiChatClient()
    {
        var endpoint = _settings.AzureOpenAI.Endpoint;
        var apiKey = string.IsNullOrWhiteSpace(_settings.AzureOpenAI.ApiKey) ? "placeholder" : _settings.AzureOpenAI.ApiKey;
        var clientOptions = new OpenAIClientOptions { Endpoint = string.IsNullOrEmpty(endpoint) ? null : new Uri(endpoint) };
        var client = new OpenAIClient(new ApiKeyCredential(apiKey), clientOptions);
        return client.GetChatClient(_settings.AzureOpenAI.ChatDeploymentName).AsIChatClient();
    }

    private IEmbeddingGenerator<string, Embedding<float>> CreateAzureOpenAiEmbeddingGenerator()
    {
        var endpoint = _settings.AzureOpenAI.Endpoint;
        var apiKey = string.IsNullOrWhiteSpace(_settings.AzureOpenAI.ApiKey) ? "placeholder" : _settings.AzureOpenAI.ApiKey;
        var clientOptions = new OpenAIClientOptions { Endpoint = string.IsNullOrEmpty(endpoint) ? null : new Uri(endpoint) };
        var client = new OpenAIClient(new ApiKeyCredential(apiKey), clientOptions);
        return client.GetEmbeddingClient(_settings.AzureOpenAI.EmbeddingDeploymentName).AsIEmbeddingGenerator();
    }

    private IChatClient CreateGeminiChatClient()
    {
        // Google Gemini supports OpenAI-compatible REST endpoint
        var endpoint = new Uri("https://generativelanguage.googleapis.com/v1beta/openai/");
        var apiKey = string.IsNullOrWhiteSpace(_settings.Gemini.ApiKey) ? "placeholder" : _settings.Gemini.ApiKey;
        var clientOptions = new OpenAIClientOptions { Endpoint = endpoint };
        var client = new OpenAIClient(new ApiKeyCredential(apiKey), clientOptions);
        return client.GetChatClient(_settings.Gemini.ChatModel).AsIChatClient();
    }

    private IEmbeddingGenerator<string, Embedding<float>> CreateGeminiEmbeddingGenerator()
    {
        var endpoint = new Uri("https://generativelanguage.googleapis.com/v1beta/openai/");
        var apiKey = string.IsNullOrWhiteSpace(_settings.Gemini.ApiKey) ? "placeholder" : _settings.Gemini.ApiKey;
        var clientOptions = new OpenAIClientOptions { Endpoint = endpoint };
        var client = new OpenAIClient(new ApiKeyCredential(apiKey), clientOptions);
        return client.GetEmbeddingClient(_settings.Gemini.EmbeddingModel).AsIEmbeddingGenerator();
    }

    #endregion
}
