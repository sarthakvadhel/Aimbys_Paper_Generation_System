namespace Aimbys.Application.Storage;

/// <summary>
/// Thrown by <see cref="IFileStorageService"/> when an upload fails a
/// validation contract (size, MIME, missing file). Controllers catch this
/// and translate to <c>400 Bad Request</c> with a user-friendly message.
/// </summary>
public class FileStorageException : Exception
{
    public FileStorageException(string message) : base(message) { }
    public FileStorageException(string message, Exception inner) : base(message, inner) { }
}
