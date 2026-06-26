using BusSeatBookingSystem.Service.DTOs;
using Microsoft.Extensions.Options;

namespace BusSeatBookingSystem.Api.Services;

public interface IImageStorageService
{
    Task<string> SaveAsync(IFormFile file, CancellationToken ct);
    void Delete(string? relativePath);
}

public class ImageStorageService(IOptions<ImageStorageOptions> options) : IImageStorageService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };
    private readonly ImageStorageOptions _options = options.Value;

    public async Task<string> SaveAsync(IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0 || file.Length > _options.MaxFileSizeBytes)
            throw new InvalidOperationException($"Image must be between 1 byte and {_options.MaxFileSizeBytes / 1_048_576} MB.");
        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension) || !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only JPG, PNG, and WEBP images are allowed.");
        Directory.CreateDirectory(_options.RootPath);
        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var fullPath = Path.Combine(_options.RootPath, fileName);
        await using var stream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
        await file.CopyToAsync(stream, ct);
        return fileName;
    }

    public void Delete(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return;
        var root = Path.GetFullPath(_options.RootPath);
        var fullPath = Path.GetFullPath(Path.Combine(root, Path.GetFileName(relativePath)));
        if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase)) return;
        if (File.Exists(fullPath)) File.Delete(fullPath);
    }
}
