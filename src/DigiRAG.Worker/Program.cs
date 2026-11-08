using DigiRAG.Infrastructure;
using Hangfire;

var builder = Host.CreateApplicationBuilder(args);

// Add Infrastructure & Hangfire
builder.Services.AddInfrastructure(builder.Configuration);

// Add Hangfire Processing Server
builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = Math.Max(2, Environment.ProcessorCount);
    options.Queues = new[] { "default", "ingestion" };
});

var host = builder.Build();
host.Run();
