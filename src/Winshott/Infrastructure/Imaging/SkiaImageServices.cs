using SkiaSharp;
using Winshott.Application.Interfaces;
using Winshott.Domain.Models;
using Winshott.Infrastructure.Configuration;

namespace Winshott.Infrastructure.Imaging;

public sealed class SkiaImageProcessingService : IImageProcessingService
{
    public Task<CapturedImage> ProcessAsync(CapturedImage image, ImageProcessingRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request.Operation != ImageOperation.Crop || request.Region is not { } requested)
            throw new PlatformNotSupportedException($"Image operation {request.Operation} is not implemented yet.");
        using var source = SKBitmap.Decode(image.Data) ?? throw new InvalidDataException("The image data could not be decoded.");
        var rect = ScreenGeometry.ClampToImage(requested, source.Width, source.Height);
        if (rect.Width == 0 || rect.Height == 0) throw new ArgumentException("Crop region does not intersect the image.", nameof(request));
        using var cropped = new SKBitmap(rect.Width, rect.Height, source.ColorType, source.AlphaType);
        using (var canvas = new SKCanvas(cropped))
            canvas.DrawBitmap(source, new SKRectI(rect.X, rect.Y, rect.X + rect.Width, rect.Y + rect.Height), new SKRect(0, 0, rect.Width, rect.Height));
        using var encoded = cropped.Encode(SKEncodedImageFormat.Png, 100) ?? throw new InvalidOperationException("PNG encoding failed.");
        return Task.FromResult(new CapturedImage(encoded.ToArray(), rect.Width, rect.Height));
    }
}

public sealed class LocalImageFileService : IFileService
{
    public async Task SaveImageAsync(CapturedImage image, string path, string format, int jpegQuality = 90, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var encodedBytes = Encode(image, format, jpegQuality);
        var fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllBytesAsync(fullPath, encodedBytes, cancellationToken);
    }
    public async Task<CapturedImage> LoadImageAsync(string path, CancellationToken cancellationToken = default)
    {
        var data = await File.ReadAllBytesAsync(path, cancellationToken);
        using var bitmap = SKBitmap.Decode(data) ?? throw new InvalidDataException("The image file could not be decoded.");
        return new CapturedImage(data, bitmap.Width, bitmap.Height, MediaType(Path.GetExtension(path)));
    }
    public Task ExportImageAsync(CapturedImage image, string path, string format, int jpegQuality = 90, CancellationToken cancellationToken = default) => SaveImageAsync(image, path, format, jpegQuality, cancellationToken);
    public string GetSaveLocation() => AppDataPaths.GetDefaultSaveLocation();

    public static string CreateTimestampedFileName(DateTimeOffset timestamp, string format)
    {
        var extension = format.Equals("Jpeg", StringComparison.OrdinalIgnoreCase) || format.Equals("Jpg", StringComparison.OrdinalIgnoreCase) ? "jpg" : "png";
        return $"Winshott_{timestamp:yyyy-MM-dd_HHmmss_fff}.{extension}";
    }

    private static byte[] Encode(CapturedImage image, string format, int jpegQuality)
    {
        using var bitmap = SKBitmap.Decode(image.Data) ?? throw new InvalidDataException("The image data could not be decoded.");
        var isJpeg = format.Equals("Jpeg", StringComparison.OrdinalIgnoreCase) || format.Equals("Jpg", StringComparison.OrdinalIgnoreCase);
        using var encoded = bitmap.Encode(isJpeg ? SKEncodedImageFormat.Jpeg : SKEncodedImageFormat.Png, isJpeg ? Math.Clamp(jpegQuality, 1, 100) : 100)
            ?? throw new InvalidOperationException($"{format} encoding failed.");
        return encoded.ToArray();
    }
    private static string MediaType(string extension) => extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ? "image/jpeg" : "image/png";
}
