using DigiRAG.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DigiRAG.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task InitializeDatabaseAsync(ApplicationDbContext dbContext, ILogger logger, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Verifying PostgreSQL database and running EF Core migrations...");

            // 1. Ensure pgvector extension exists before table creation
            await dbContext.Database.ExecuteSqlRawAsync(
                "CREATE EXTENSION IF NOT EXISTS vector;", cancellationToken);

            // 2. Apply all pending EF Core Migrations automatically
            await dbContext.Database.MigrateAsync(cancellationToken);

            // 3. Seed default Tenant if empty
            if (!await dbContext.Tenants.AnyAsync(cancellationToken))
            {
                logger.LogInformation("Seeding default Acme Corporation tenant...");

                var defaultTenant = new Tenant
                {
                    Id = Guid.Parse("a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d"),
                    Name = "Acme Corporation",
                    Slug = "acme",
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow
                };

                var defaultKb = new KnowledgeBase
                {
                    Id = Guid.Parse("b2c3d4e5-f6a7-8b9c-0d1e-2f3a4b5c6d7e"),
                    TenantId = defaultTenant.Id,
                    Name = "Company Knowledge Base",
                    Description = "Internal policies, technical documentation and manuals",
                    CreatedAtUtc = DateTime.UtcNow
                };

                dbContext.Tenants.Add(defaultTenant);
                dbContext.KnowledgeBases.Add(defaultKb);
                await dbContext.SaveChangesAsync(cancellationToken);

                logger.LogInformation("Default tenant and knowledge base seeded successfully.");
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Database auto-initialization could not complete. Verify PostgreSQL connection string.");
        }
    }
}
