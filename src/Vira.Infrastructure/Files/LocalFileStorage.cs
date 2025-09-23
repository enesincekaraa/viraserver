using Microsoft.AspNetCore.Hosting;
using Vira.Application.Abstractions.Files;

namespace Vira.Infrastructure.Files;

public sealed class LocalFileStorage : IFileStorage
{
    private readonly IWebHostEnvironment _env;

    public LocalFileStorage(IWebHostEnvironment env) => _env = env;

    public async Task<FileSaveResult> SaveAsync(
        Stream stream, string fileName, string contentType, string subFolder, CancellationToken ct = default)
    {
        // wwwroot/uploads/<subFolder> dizini
        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var uploadsRoot = Path.Combine(webRoot, "uploads", subFolder);
        Directory.CreateDirectory(uploadsRoot);

        var uniqueName = $"{Guid.NewGuid():N}{Path.GetExtension(fileName)}";
        var fullPath = Path.Combine(uploadsRoot, uniqueName);

        using (var fs = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            await stream.CopyToAsync(fs, ct);

        var relativeUrl = $"/uploads/{subFolder.Replace('\\', '/')}/{uniqueName}";
        var size = new FileInfo(fullPath).Length;

        return new FileSaveResult(uniqueName, relativeUrl, size, contentType);
    }

    public Task<bool> DeleteAsync(string subFolder, string storedFileName)
    {
        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var full = Path.Combine(webRoot, "uploads", subFolder, storedFileName);
        if (File.Exists(full)) { File.Delete(full); return Task.FromResult(true); }
        return Task.FromResult(false);
    }
}
