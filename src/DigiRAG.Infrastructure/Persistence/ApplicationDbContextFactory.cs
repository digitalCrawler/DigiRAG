using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Pgvector.EntityFrameworkCore;

namespace DigiRAG.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by the EF Core CLI (<c>dotnet ef migrations add</c>, <c>dotnet ef database update</c>).
/// Without this, the tools have to boot the whole API host just to discover the DbContext.
///
/// Usage from the repository root:
///   dotnet ef migrations add &lt;Name&gt; -p src/DigiRAG.Infrastructure
///   dotnet ef database update      -p src/DigiRAG.Infrastructure
///   dotnet ef migrations script --idempotent -p src/DigiRAG.Infrastructure -o database/migrate.sql
///
/// The connection string is read from (highest priority first):
///   1. the --connection argument passed to dotnet ef
///   2. the ConnectionStrings__DefaultConnection environment variable
///   3. src/DigiRAG.Api/appsettings[.Environment].json
///   4. a localhost fallback
///
/// IMPORTANT: no ITenantContext is supplied here on purpose. ApplicationDbContext only
/// applies its tenant query filters when a tenant is present, and query filters must never
/// leak into the migration model snapshot.
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    private const string LocalFallback =
        "Host=localhost;Port=5432;Database=digirag_db;Username=postgres;Password=postgres;";

    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var environmentName =
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ??
            Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ??
            "Development";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(ResolveApiProjectPath())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString =
            GetConnectionFromArgs(args) ??
            configuration.GetConnectionString("DefaultConnection") ??
            LocalFallback;

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString, npgsql =>
            {
                npgsql.UseVector();
                npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
            })
            .Options;

        return new ApplicationDbContext(options, tenantContext: null);
    }

    private static string? GetConnectionFromArgs(string[] args)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], "--connection", StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }
        return null;
    }

    /// <summary>
    /// Walks up from the Infrastructure project to find src/DigiRAG.Api so appsettings.json
    /// is picked up no matter which directory dotnet ef was invoked from.
    /// </summary>
    private static string ResolveApiProjectPath()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "src", "DigiRAG.Api");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
            directory = directory.Parent;
        }

        return Directory.GetCurrentDirectory();
    }
}
