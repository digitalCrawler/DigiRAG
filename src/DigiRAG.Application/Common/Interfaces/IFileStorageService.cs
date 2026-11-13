namespace DigiRAG.Application.Common.Interfaces;

public interface IFileStorageService
{
    /// <summary>The fully resolved directory that uploads are written to.</summary>
    string StorageRoot { get; }

    /// <summary>
    /// Verifies the storage root exists and is writable by performing a real write.
    /// Permission problems only show up on an actual write, so an existence check is not enough.
    /// </summary>
    bool TryVerifyWritable(out string? error);

    Task<string> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task<Stream> DownloadAsync(string storagePath, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default);
}
