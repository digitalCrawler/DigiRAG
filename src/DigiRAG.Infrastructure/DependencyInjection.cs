using DigiRAG.Application.AI;
using DigiRAG.Application.Common.Interfaces;
using DigiRAG.Application.Ingestion;
using DigiRAG.Application.Retrieval;
using DigiRAG.Infrastructure.AI;
using DigiRAG.Infrastructure.Chunking;
using DigiRAG.Infrastructure.Extractors;
using DigiRAG.Infrastructure.Ingestion;
using DigiRAG.Infrastructure.Persistence;
using DigiRAG.Infrastructure.Retrieval;
using DigiRAG.Infrastructure.Storage;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DigiRAG.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. AI Settings
        services.Configure<AiSettings>(configuration.GetSection(AiSettings.SectionName));
        services.AddSingleton<IAiModelFactory, ModelProviderFactory>();

        // 2. Database with Npgsql and pgvector
        var connectionString = configuration.GetConnectionString("DefaultConnection") ??
            "Host=localhost;Port=5432;Database=digirag_db;Username=postgres;Password=postgres;";

        services.AddScoped<ITenantContext, TenantContext>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString, o =>
            {
                o.UseVector();
                o.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
            });
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        // 3. File Storage
        var storagePath = configuration["Storage:LocalPath"];
        services.AddSingleton<IFileStorageService>(new LocalFileStorageService(storagePath));

        // 4. Document Extractors
        services.AddSingleton<PdfDocumentExtractor>();
        services.AddSingleton<DocxDocumentExtractor>();
        services.AddSingleton<TextDocumentExtractor>();
        services.AddSingleton<IDocumentExtractor>(sp => new CompositeDocumentExtractor(new IDocumentExtractor[]
        {
            sp.GetRequiredService<PdfDocumentExtractor>(),
            sp.GetRequiredService<DocxDocumentExtractor>(),
            sp.GetRequiredService<TextDocumentExtractor>()
        }));

        // 5. Chunking & Ingestion
        services.AddSingleton<ITextChunker, StructureAwareTextChunker>();
        services.AddScoped<IIngestionService, IngestionService>();

        // 6. Retrieval & Chat
        services.AddScoped<IRetrievalService, RetrievalService>();
        services.AddSingleton<IRerankerService, RerankerService>();
        services.AddScoped<IChatService, ChatService>();

        // 7. Hangfire with PostgreSQL storage
        services.AddHangfire(config =>
        {
            config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                  .UseSimpleAssemblyNameTypeSerializer()
                  .UseRecommendedSerializerSettings()
                  .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(connectionString), new PostgreSqlStorageOptions
                  {
                      QueuePollInterval = TimeSpan.FromSeconds(5),
                      InvisibilityTimeout = TimeSpan.FromMinutes(30)
                  });
        });

        return services;
    }
}
