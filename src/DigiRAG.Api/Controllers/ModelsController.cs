using DigiRAG.Application.AI;
using DigiRAG.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DigiRAG.Api.Controllers;

[ApiController]
[Route("api/v1/models")]
public class ModelsController : ControllerBase
{
    private readonly AiSettings _settings;
    private readonly IAiModelFactory _modelFactory;

    public ModelsController(IOptions<AiSettings> settings, IAiModelFactory modelFactory)
    {
        _settings = settings.Value;
        _modelFactory = modelFactory;
    }

    [HttpGet]
    public ActionResult<object> GetConfiguredModels()
    {
        return Ok(new
        {
            ActiveProvider = _settings.ActiveProvider.ToString(),
            ActiveEmbeddingDimensions = _modelFactory.GetCurrentEmbeddingDimensions(),
            Providers = new
            {
                Local = new
                {
                    _settings.Local.Endpoint,
                    _settings.Local.ChatModel,
                    _settings.Local.EmbeddingModel,
                    _settings.Local.EmbeddingDimensions
                },
                Gemini = new
                {
                    _settings.Gemini.ChatModel,
                    _settings.Gemini.EmbeddingModel,
                    _settings.Gemini.EmbeddingDimensions,
                    IsConfigured = !string.IsNullOrWhiteSpace(_settings.Gemini.ApiKey)
                },
                OpenAI = new
                {
                    _settings.OpenAI.ChatModel,
                    _settings.OpenAI.EmbeddingModel,
                    _settings.OpenAI.EmbeddingDimensions,
                    IsConfigured = !string.IsNullOrWhiteSpace(_settings.OpenAI.ApiKey)
                },
                AzureOpenAI = new
                {
                    _settings.AzureOpenAI.Endpoint,
                    _settings.AzureOpenAI.ChatDeploymentName,
                    _settings.AzureOpenAI.EmbeddingDeploymentName,
                    _settings.AzureOpenAI.EmbeddingDimensions,
                    IsConfigured = !string.IsNullOrWhiteSpace(_settings.AzureOpenAI.ApiKey)
                }
            }
        });
    }
}
