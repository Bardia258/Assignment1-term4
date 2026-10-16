using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MunicipalElections.Web.Data;
using SkiaSharp;

namespace MunicipalElections.Web.Services;

public sealed class UploadOptions
{
    public string RootPath { get; set; } = "App_Data/uploads";

    public static string ResolveRoot(string rootPath, string contentRoot)
    {
        var root = Path.GetFullPath(rootPath, contentRoot);
        var relative = Path.GetRelativePath(Path.GetFullPath(contentRoot), root);
        var insideContent = relative != ".." && !relative.StartsWith(".." + Path.DirectorySeparatorChar) && !Path.IsPathRooted(relative);
        var parentRelative = Path.GetRelativePath(root, Path.GetFullPath(contentRoot));
        var ancestor = parentRelative != ".." && !parentRelative.StartsWith(".." + Path.DirectorySeparatorChar) && !Path.IsPathRooted(parentRelative);
        if (ancestor || (insideContent && !relative.StartsWith("App_Data" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Upload storage must use a dedicated App_Data subdirectory or an external directory, outside the application source and wwwroot.");
        return root;
    }
}

public interface IImageStorage
{
    Task<string> SaveAsync(IFormFile file, string category);
    Task DeleteIfUnreferencedAsync(string? fileName, string category);
    string? Url(string? fileName, string category);
    string RootPath { get; }
}

public sealed partial class ImageStorage(IWebHostEnvironment environment, IOptions<UploadOptions> options,
    ApplicationDbContext db, ILogger<ImageStorage> logger) : IImageStorage
{
    public const long MaxBytes = 2 * 1024 * 1024;
    public string RootPath { get; } = UploadOptions.ResolveRoot(options.Value.RootPath, environment.ContentRootPath);
    [GeneratedRegex("^[a-f0-9]{32}\\.(png|jpg|webp)$", RegexOptions.CultureInvariant)]
    private static partial Regex SafeName();

    public string? Url(string? fileName, string category) => ValidCategory(category)
        && fileName is not null && SafeName().IsMatch(fileName) ? $"/uploads/{category}/{fileName}" : null;
    private static bool ValidCategory(string category) => category is "logos" or "candidates";

    public async Task<string> SaveAsync(IFormFile file, string category)
    {
        if (!ValidCategory(category)) throw new ArgumentException("Invalid upload category.");
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var expected = extension switch { ".png" => "image/png", ".jpg" or ".jpeg" => "image/jpeg", ".webp" => "image/webp", _ => null };
        if (expected is null || !string.Equals(file.ContentType, expected, StringComparison.OrdinalIgnoreCase))
            throw new ValidationException("Choose a PNG, JPEG, or WebP image with a matching content type.");
        if (file.Length <= 0 || file.Length > MaxBytes)
            throw new ValidationException("Images must be nonempty and no larger than 2 MB.");

        using var buffer = new MemoryStream();
        await using var input = file.OpenReadStream();
        var chunk = new byte[8192];
        int read;
        while ((read = await input.ReadAsync(chunk)) > 0)
        {
            if (buffer.Length + read > MaxBytes) throw new ValidationException("Images must be no larger than 2 MB.");
            await buffer.WriteAsync(chunk.AsMemory(0, read));
        }
        using var data = SKData.CreateCopy(buffer.ToArray());
        using var codec = SKCodec.Create(data);
        if (codec is null) throw new ValidationException("The uploaded file is not a valid supported image.");
        var format = expected switch { "image/png" => SKEncodedImageFormat.Png, "image/jpeg" => SKEncodedImageFormat.Jpeg, _ => SKEncodedImageFormat.Webp };
        if (codec.EncodedFormat != format) throw new ValidationException("The image content does not match its file type.");
        var info = codec.Info;
        if ((long)info.Width * info.Height > 16_000_000 || info.Width > 8000 || info.Height > 8000)
            throw new ValidationException("Images must be at most 16 megapixels and 8000 pixels per side.");
        using var bitmap = new SKBitmap(new SKImageInfo(info.Width, info.Height, SKColorType.Rgba8888, SKAlphaType.Premul));
        if (codec.GetPixels(bitmap.Info, bitmap.GetPixels()) != SKCodecResult.Success)
            throw new ValidationException("The image could not be decoded completely.");
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(format, 90);
        if (encoded is null) throw new ValidationException("The image could not be encoded.");
        // Re-encode decoded pixels; do not retain arbitrary trailing data or metadata.
        var suffix = extension == ".jpeg" ? ".jpg" : extension;
        var name = Guid.NewGuid().ToString("N") + suffix;
        var directory = Path.Combine(RootPath, category);
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, name);
        var created = false;
        try
        {
            await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            created = true;
            await output.WriteAsync(encoded.ToArray());
        }
        catch { if (created) File.Delete(path); throw; }
        return name;
    }

    public async Task DeleteIfUnreferencedAsync(string? fileName, string category)
    {
        if (Url(fileName, category) is null) return;
        try
        {
            var used = category == "logos"
                ? await db.Municipalities.AnyAsync(m => m.LogoFileName == fileName)
                : await db.Candidates.AnyAsync(c => c.PhotoFileName == fileName);
            if (!used) File.Delete(Path.Combine(RootPath, category, fileName!));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or DbUpdateException or DbException)
        {
            logger.LogWarning("An unreferenced image could not be removed. Category: {Category}", category);
        }
    }
}
