using DigiRAG.Api.Middleware;
using DigiRAG.Infrastructure;
using Hangfire;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Controllers & Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 2. Add Infrastructure Services (Database, AI Providers, Hangfire, Storage)
builder.Services.AddInfrastructure(builder.Configuration);

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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAngularClient");

// Custom Tenant Resolution Middleware
app.UseMiddleware<TenantResolutionMiddleware>();

// Hangfire Dashboard endpoint at /hangfire
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    DashboardTitle = "DigiRAG Jobs & Ingestion"
});

app.UseAuthorization();
app.MapControllers();

// Auto-initialize DB and pgvector on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<DigiRAG.Infrastructure.Persistence.ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await DigiRAG.Infrastructure.Persistence.DbInitializer.InitializeDatabaseAsync(dbContext, logger);
}

app.Run();
