namespace DigiRAG.Application.Common.Exceptions;

/// <summary>
/// Raised when the configured file storage location cannot be written to or read from.
/// Carries the resolved path so the failure can be reported without reading server logs -
/// an unwritable upload directory is otherwise indistinguishable from any other HTTP 500.
/// </summary>
public class FileStorageException : Exception
{
    public string StorageRoot { get; }

    public FileStorageException(string message, string storageRoot, Exception? innerException = null)
        : base(message, innerException)
    {
        StorageRoot = storageRoot;
    }
}
