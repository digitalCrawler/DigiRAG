using DigiRAG.Application.Common.Exceptions;
using DigiRAG.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace DigiRAG.Infrastructure.Storage;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _storageRoot;
    private readonly ILogger<LocalFileStorageService>? _logger;

    public string StorageRoot => _storageRoot;

    public LocalFileStorageService(string? customRoot = null, ILogger<LocalFileStorageService>? logger = null)
    {
        _logger = logger;
        var root = string.IsNullOrWhiteSpace(customRoot)
            ? Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "Storage")
            : customRoot;

        try
        {
            root = Path.GetFullPath(root);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Could not resolve storage path '{StorageRoot}'", root);
        }

        _storageRoot = root;

        // Report writability once at startup rather than failing on the first upload.
        if (TryVerifyWritable(out var error))
        {
            _logger?.LogInformation("File storage ready at '{StorageRoot}'.", _storageRoot);
        }
        else
        {
            _logger?.LogError(
                "File storage at '{StorageRoot}' is NOT writable: {Error}. Uploads will fail until this is fixed " +
                "(create the directory and grant the service account write access, or set Storage:LocalPath).",
                _storageRoot, error);
        }
    }

    public bool TryVerifyWritable(out string? error)
    {
        try
        {
            Directory.CreateDirectory(_storageRoot);

            // Creating the directory can succeed where writing a file does not, so probe for real.
            var probe = Path.Combine(_storageRoot, $".write-probe-{Guid.NewGuid():N}");
            using (var fs = new FileStream(probe, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                fs.WriteByte(0);
            }
            File.Delete(probe);

            error = null;
            return true;
        }
        catch (Exception ex)
        {
            error = $"{ex.GetType().Name}: {ex.Message}";
            return false;
        }
    }

    public async Task<string> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var safeFileName = $"{Guid.NewGuid():N}_{Path.GetFileName(fileName)}";
        var fullPath = Path.Combine(_storageRoot, safeFileName);

        try
        {
            Directory.CreateDirectory(_storageRoot);

            await using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await stream.CopyToAsync(fileStream, cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
        {
            _logger?.LogError(ex, "Failed to store upload '{FileName}' under '{StorageRoot}'.", fileName, _storageRoot);

            throw new FileStorageException(
                $"Cannot write uploads to '{_storageRoot}'. {ex.GetType().Name}: {ex.Message}",
                _storageRoot,
                ex);
        }

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
