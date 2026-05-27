namespace Aimbys.Application.Storage;

/// <summary>
/// Marker interface for the local-disk binding of
/// <see cref="IFileStorageService"/>. Most code should depend on the parent
/// interface so the cloud adapter can drop in transparently; this interface
/// exists for the rare site that has to know it is talking to local disk
/// (e.g. orphan-cleanup utilities that walk the filesystem directly).
/// </summary>
public interface ILocalFileStorageService : IFileStorageService
{
}
