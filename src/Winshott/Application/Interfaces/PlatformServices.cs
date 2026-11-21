using Winshott.Domain.Models;

namespace Winshott.Application.Interfaces;

public interface IScreenCaptureService
{
    Task<IReadOnlyList<DisplayInfo>> GetDisplaysAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WindowInfo>> GetWindowsAsync(CancellationToken cancellationToken = default);
    Task<CapturedImage> CaptureFullScreenAsync(string? displayId = null, CancellationToken cancellationToken = default);
    Task<CapturedImage> CaptureRegionAsync(PixelRect region, CancellationToken cancellationToken = default);
    Task<CapturedImage> CaptureWindowAsync(string windowId, CancellationToken cancellationToken = default);
}

public interface IClipboardService
{
    Task CopyImageAsync(CapturedImage image, CancellationToken cancellationToken = default);
    Task CopyTextAsync(string text, CancellationToken cancellationToken = default);
    Task<CapturedImage?> GetImageAsync(CancellationToken cancellationToken = default);
    Task<string?> GetTextAsync(CancellationToken cancellationToken = default);
}

public interface IOcrService
{
    Task<OcrResult> RecognizeTextAsync(CapturedImage image, CancellationToken cancellationToken = default);
    Task<OcrResult> RecognizeTextInRegionAsync(CapturedImage image, PixelRect region, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetSupportedLanguagesAsync(CancellationToken cancellationToken = default);
}

public interface IQrCodeService
{
    Task<IReadOnlyList<CodeResult>> DetectCodesAsync(CapturedImage image, CancellationToken cancellationToken = default);
    Task<CodeResult?> DecodeImageAsync(CapturedImage image, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CodeResult>> DecodeRegionAsync(CapturedImage image, PixelRect region, CancellationToken cancellationToken = default);
}

public interface IImageProcessingService
{
    Task<CapturedImage> ProcessAsync(CapturedImage image, ImageProcessingRequest request, CancellationToken cancellationToken = default);
}

public interface IFileService
{
    Task SaveImageAsync(CapturedImage image, string path, string format, int jpegQuality = 90, CancellationToken cancellationToken = default);
    Task<CapturedImage> LoadImageAsync(string path, CancellationToken cancellationToken = default);
    Task ExportImageAsync(CapturedImage image, string path, string format, int jpegQuality = 90, CancellationToken cancellationToken = default);
    string GetSaveLocation();
}

public interface IHotkeyService
{
    Task<bool> RegisterAsync(string action, string shortcut, CancellationToken cancellationToken = default);
    Task UnregisterAsync(string action, CancellationToken cancellationToken = default);
}
