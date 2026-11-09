using DigiRAG.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace DigiRAG.Infrastructure.Storage;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _storageRoot;
    private readonly ILogger<LocalFileStorageService>? _logger;

    public LocalFileStorageService(string? customRoot = null, ILogger<LocalFileStorageService>? logger = null)
    {
        _logger = logger;
        _storageRoot = string.IsNullOrWhiteSpace(customRoot)
            ? Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "Storage")
            : customRoot;

        try
        {
            _storageRoot = Path.GetFullPath(_storageRoot);
            TryEnsureDirectoryExists();
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to resolve or initialize storage path '{StorageRoot}'", _storageRoot);
            Console.Error.WriteLine($"[Storage Warning] Cannot initialize storage root '{_storageRoot}': {ex.Message}");
        }
    }

    private void TryEnsureDirectoryExists()
    {
        try
        {
            if (!Directory.Exists(_storageRoot))
            {
                Directory.CreateDirectory(_storageRoot);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Storage directory '{StorageRoot}' cannot be accessed or created: {Message}", _storageRoot, ex.Message);
            Console.Error.WriteLine($"[Storage Warning] Cannot create or access '{_storageRoot}': {ex.Message}. Uploads will fail if the directory does not have write permissions.");
        }
    }

    public async Task<string> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_storageRoot))
        {
            Directory.CreateDirectory(_storageRoot);
        }

        var safeFileName = $"{Guid.NewGuid():N}_{Path.GetFileName(fileName)}";
        var fullPath = Path.Combine(_storageRoot, safeFileName);

        await using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await stream.CopyToAsync(fileStream, cancellationToken);

        return safeFileName;
    }

    public Task<Stream> DownloadAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_storageRoot, storagePath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Stored file not found: {storagePath}");
        }

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_storageRoot, storagePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
        return Task.CompletedTask;
    }
}
