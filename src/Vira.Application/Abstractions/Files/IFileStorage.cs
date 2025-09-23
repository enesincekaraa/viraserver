namespace Vira.Application.Abstractions.Files;

public sealed record FileSaveResult(string StoredFileName, string Url, long SizeBytes, string ContentType);

public interface IFileStorage
{
    Task<FileSaveResult> SaveAsync(
        Stream stream,           // dosya içeriği
        string fileName,         // orijinal ad (uzantı için)
        string contentType,      // "image/jpeg" vb.
        string subFolder,        // "requests/{id}" gibi alt klasör
        CancellationToken ct = default);

    Task<bool> DeleteAsync(string subFolder, string storedFileName);
}
