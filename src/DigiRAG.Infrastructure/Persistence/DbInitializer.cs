using DigiRAG.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace DigiRAG.Infrastructure.Persistence;

public static class DbInitializer
{
    /// <summary>
    /// Applies EF Core migrations and seeds the default tenant.
    /// </summary>
    /// <param name="throwOnFailure">
    /// When true (the default for Production) a failure is rethrown so the process exits
    /// instead of serving a broken API. A swallowed migration error is what makes a deploy
    /// look healthy while every endpoint returns 500.
    /// </param>
    public static async Task InitializeDatabaseAsync(
        ApplicationDbContext dbContext,
        ILogger logger,
        bool throwOnFailure = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await WaitForDatabaseAsync(dbContext, logger, cancellationToken);

            // 1. pgvector must exist before the migrations that create vector columns run.
            logger.LogInformation("Ensuring the pgvector extension is installed...");
            await dbContext.Database.ExecuteSqlRawAsync(
                "CREATE EXTENSION IF NOT EXISTS vector;", cancellationToken);

            // 2. Report what is about to happen, then apply migrations.
            var applied = (await dbContext.Database.GetAppliedMigrationsAsync(cancellationToken)).ToList();
            var pending = (await dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();

            logger.LogInformation(
                "EF Core migrations: {AppliedCount} already applied, {PendingCount} pending.",
                applied.Count, pending.Count);

            if (pending.Count > 0)
            {
                logger.LogInformation("Applying pending migrations: {Pending}", string.Join(", ", pending));
                await dbContext.Database.MigrateAsync(cancellationToken);
                logger.LogInformation("All pending migrations applied successfully.");
            }
            else
            {
                logger.LogInformation("Database schema is already up to date.");
            }
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex,
                "Database migration FAILED. The schema is not usable. " +
                "Check ConnectionStrings:DefaultConnection, that the database exists, " +
                "and that the login may run CREATE EXTENSION vector.");

            if (throwOnFailure)
            {
                throw;
            }
            return;
        }

        // Seeding is best-effort: a seed clash must never take the API down.
        try
        {
            await SeedDefaultTenantAsync(dbContext, logger, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Default tenant seeding was skipped because it failed.");
        }
    }

    /// <summary>
    /// Containers and managed Postgres instances are often still accepting connections late.
    /// Retry briefly instead of crash-looping the whole service on a cold start.
    /// </summary>
    private static async Task WaitForDatabaseAsync(
        ApplicationDbContext dbContext,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        const int maxAttempts = 10;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                if (await dbContext.Database.CanConnectAsync(cancellationToken))
                {
                    return;
                }
                throw new NpgsqlException("The database refused the connection.");
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                var delay = TimeSpan.FromSeconds(Math.Min(attempt * 2, 10));
                logger.LogWarning(
                    "Database not reachable (attempt {Attempt}/{MaxAttempts}): {Message}. Retrying in {Delay}s...",
                    attempt, maxAttempts, ex.Message, delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken);
            }
        }

        // Final attempt: let the real exception escape to the caller.
        await dbContext.Database.OpenConnectionAsync(cancellationToken);
        await dbContext.Database.CloseConnectionAsync();
    }

    private static async Task SeedDefaultTenantAsync(
        ApplicationDbContext dbContext,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (await dbContext.Tenants.IgnoreQueryFilters().AnyAsync(cancellationToken))
        {
            return;
        }

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

        logger.LogInformation(
            "Seeded tenant {TenantId} ({Slug}) and knowledge base {KnowledgeBaseId}.",
            defaultTenant.Id, defaultTenant.Slug, defaultKb.Id);
    }
}
