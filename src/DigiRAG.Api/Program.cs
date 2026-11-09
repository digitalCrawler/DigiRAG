using DigiRAG.Api.Middleware;
using DigiRAG.Infrastructure;
using DigiRAG.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Controllers & Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 2. Add Infrastructure Services (Database, AI Providers, Hangfire, Storage)
builder.Services.AddInfrastructure(builder.Configuration);

// 2b. Process Hangfire jobs inside the API unless a dedicated DigiRAG.Worker is deployed.
//     Without this, document uploads sit in the queue at Status=Queued forever.
var runHangfireServerInApi = builder.Configuration.GetValue("Hangfire:RunServerInApi", true);
if (runHangfireServerInApi)
{
    builder.Services.AddHangfireServer(options =>
    {
        options.WorkerCount = Math.Max(2, Environment.ProcessorCount);
        options.Queues = new[] { "default", "ingestion" };
    });
}

// 3. Add CORS for Angular frontend (supports Vercel & local development)
var configuredOrigins = builder.Configuration["Cors:AllowedOrigins"]?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
var defaultOrigins = new[] { "http://localhost:4200", "http://127.0.0.1:4200" };
var allowedOrigins = (configuredOrigins != null && configuredOrigins.Length > 0) ? configuredOrigins : defaultOrigins;

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularClient", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .WithExposedHeaders("Content-Disposition");
    });
});

var app = builder.Build();

// ---------------------------------------------------------------------------
// Migration-only mode: `dotnet DigiRAG.Api.dll --migrate` applies migrations and
// exits with code 0 on success / 1 on failure. Use it as a deploy step when you
// do not want the web process to touch the schema.
// ---------------------------------------------------------------------------
if (args.Contains("--migrate", StringComparer.OrdinalIgnoreCase))
{
    using var migrationScope = app.Services.CreateScope();
    var migrationDb = migrationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var migrationLogger = migrationScope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        await DbInitializer.InitializeDatabaseAsync(migrationDb, migrationLogger, throwOnFailure: true);
        migrationLogger.LogInformation("Migration-only run completed successfully.");
        return 0;
    }
    catch
    {
        migrationLogger.LogCritical("Migration-only run failed.");
        return 1;
    }
}

// Swagger: this API has no authentication, so hiding the schema buys nothing.
// Set Swagger:Enabled=false to turn it off.
if (builder.Configuration.GetValue("Swagger:Enabled", true))
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.RoutePrefix = "swagger");
}

// ---------------------------------------------------------------------------
// Serve the compiled Angular client from wwwroot when it has been built.
// The publish step copies src/DigiRAG.Client/dist/digi-rag-client/browser here.
// ---------------------------------------------------------------------------
var webRoot = app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");
var spaIndexPath = Path.Combine(webRoot, "index.html");
var hasSpa = File.Exists(spaIndexPath);

if (hasSpa)
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
}

app.UseCors("AllowAngularClient");

// Custom Tenant Resolution Middleware
app.UseMiddleware<TenantResolutionMiddleware>();

// Hangfire Dashboard endpoint at /hangfire.
// Hangfire's default authorization filter rejects every non-local request, so on a real
// server /hangfire returns 401 until Hangfire:DashboardPublic is set to true.
var dashboardOptions = new DashboardOptions { DashboardTitle = "DigiRAG Jobs & Ingestion" };
if (builder.Configuration.GetValue("Hangfire:DashboardPublic", false))
{
    dashboardOptions.Authorization = new[] { new AllowAllDashboardAuthorizationFilter() };
}
app.UseHangfireDashboard("/hangfire", dashboardOptions);

app.UseAuthorization();
app.MapControllers();

// ---------------------------------------------------------------------------
// Liveness / readiness: a single URL that answers "is this deploy actually working?"
// ---------------------------------------------------------------------------
app.MapGet("/health", async (ApplicationDbContext db) =>
{
    try
    {
        var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();
        return Results.Ok(new
        {
            status = pending.Count == 0 ? "healthy" : "degraded",
            database = "connected",
            pendingMigrations = pending,
            spa = hasSpa ? "served from wwwroot" : "not deployed",
            utc = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        return Results.Json(new
        {
            status = "unhealthy",
            database = "unreachable",
            error = ex.Message,
            utc = DateTime.UtcNow
        }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

if (hasSpa)
{
    // Angular is a single-page app: every unmatched non-API path must return index.html,
    // otherwise deep links such as /client give a raw 404 from Kestrel.
    app.MapFallback(async context =>
    {
        var path = context.Request.Path;

        if (path.StartsWithSegments("/api") ||
            path.StartsWithSegments("/hangfire") ||
            path.StartsWithSegments("/swagger"))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsJsonAsync(new { error = "Not found", path = path.Value });
            return;
        }

        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.SendFileAsync(spaIndexPath);
    });
}
else
{
    // No UI was published. Without this, "/" returns a bare 404 and the deploy looks dead
    // even though the API is healthy.
    app.MapGet("/", () => Results.Ok(new
    {
        service = "DigiRAG API",
        status = "running",
        ui = "not deployed - build src/DigiRAG.Client into src/DigiRAG.Api/wwwroot",
        endpoints = new[] { "/health", "/swagger", "/hangfire", "/api/v1/tenants" }
    }));
}

// Auto-initialize DB and pgvector on startup.
// Database:AutoMigrate=false disables this when migrations are run as a separate deploy step.
if (builder.Configuration.GetValue("Database:AutoMigrate", true))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    // In Development a missing database should not block `dotnet run`; in Production it must.
    var throwOnFailure = builder.Configuration.GetValue(
        "Database:FailFastOnMigrationError",
        !app.Environment.IsDevelopment());

    await DbInitializer.InitializeDatabaseAsync(dbContext, logger, throwOnFailure);
}

app.Run();
return 0;

/// <summary>Opens the Hangfire dashboard to remote requests when explicitly enabled.</summary>
internal sealed class AllowAllDashboardAuthorizationFilter : Hangfire.Dashboard.IDashboardAuthorizationFilter
{
    public bool Authorize(Hangfire.Dashboard.DashboardContext context) => true;
}
