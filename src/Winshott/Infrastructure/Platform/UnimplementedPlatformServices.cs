using Microsoft.Extensions.DependencyInjection;
using Winshott.Application.Interfaces;
using Winshott.Domain.Models;

namespace Winshott.Infrastructure.Platform;

public static class PlatformServiceCollectionExtensions
{
    public static IServiceCollection AddUnimplementedPlatformServices(this IServiceCollection services)
    {
        services.AddSingleton<UnimplementedPlatformServices>();
        services.AddSingleton<IScreenCaptureService>(provider => provider.GetRequiredService<UnimplementedPlatformServices>());
        services.AddSingleton<IScreenCapturePermissionService>(provider => provider.GetRequiredService<UnimplementedPlatformServices>());
        services.AddSingleton<IClipboardService>(provider => provider.GetRequiredService<UnimplementedPlatformServices>());
        services.AddSingleton<IOcrService>(provider => provider.GetRequiredService<UnimplementedPlatformServices>());
        services.AddSingleton<IQrCodeService>(provider => provider.GetRequiredService<UnimplementedPlatformServices>());
        services.AddSingleton<IImageProcessingService>(provider => provider.GetRequiredService<UnimplementedPlatformServices>());
        services.AddSingleton<IFileService>(provider => provider.GetRequiredService<UnimplementedPlatformServices>());
        services.AddSingleton<IHotkeyService>(provider => provider.GetRequiredService<UnimplementedPlatformServices>());
        return services;
    }
}

/// <summary>Explicit unavailable services for the foundation build; no operation reports false success.</summary>
public sealed class UnimplementedPlatformServices : IScreenCaptureService, IScreenCapturePermissionService, IClipboardService, IOcrService, IQrCodeService, IImageProcessingService, IFileService, IHotkeyService
{
    private static PlatformNotSupportedException Missing(string feature) => new($"{feature} is not implemented in this foundation build.");

    public Task<IReadOnlyList<DisplayInfo>> GetDisplaysAsync(CancellationToken cancellationToken = default) => throw Missing("Screen capture");
    public Task<IReadOnlyList<WindowInfo>> GetWindowsAsync(CancellationToken cancellationToken = default) => throw Missing("Window listing");
    public Task<CapturedImage> CaptureFullScreenAsync(string? displayId = null, CancellationToken cancellationToken = default) => throw Missing("Full-screen capture");
    public Task<CapturedImage> CaptureRegionAsync(PixelRect region, CancellationToken cancellationToken = default) => throw Missing("Region capture");
    public Task<CapturedImage> CaptureWindowAsync(string windowId, CancellationToken cancellationToken = default) => throw Missing("Window capture");
    public Task<ScreenCapturePermission> GetPermissionAsync(CancellationToken cancellationToken = default) => throw Missing("Screen Recording permission");
    public Task<ScreenCapturePermission> RequestPermissionAsync(CancellationToken cancellationToken = default) => throw Missing("Screen Recording permission");
    public Task OpenPermissionSettingsAsync(CancellationToken cancellationToken = default) => throw Missing("Screen Recording settings");
    public Task CopyImageAsync(CapturedImage image, CancellationToken cancellationToken = default) => throw Missing("Image clipboard");
    public Task CopyTextAsync(string text, CancellationToken cancellationToken = default) => throw Missing("Text clipboard");
    public Task<CapturedImage?> GetImageAsync(CancellationToken cancellationToken = default) => throw Missing("Image clipboard");
    public Task<string?> GetTextAsync(CancellationToken cancellationToken = default) => throw Missing("Text clipboard");
    public Task<OcrResult> RecognizeTextAsync(CapturedImage image, CancellationToken cancellationToken = default) => throw Missing("OCR");
    public Task<OcrResult> RecognizeTextInRegionAsync(CapturedImage image, PixelRect region, CancellationToken cancellationToken = default) => throw Missing("Region OCR");
    public Task<IReadOnlyList<string>> GetSupportedLanguagesAsync(CancellationToken cancellationToken = default) => throw Missing("OCR language listing");
    public Task<IReadOnlyList<CodeResult>> DetectCodesAsync(CapturedImage image, CancellationToken cancellationToken = default) => throw Missing("QR/barcode detection");
    public Task<CodeResult?> DecodeImageAsync(CapturedImage image, CancellationToken cancellationToken = default) => throw Missing("QR/barcode decoding");
    public Task<IReadOnlyList<CodeResult>> DecodeRegionAsync(CapturedImage image, PixelRect region, CancellationToken cancellationToken = default) => throw Missing("Region QR/barcode decoding");
    public Task<CapturedImage> ProcessAsync(CapturedImage image, ImageProcessingRequest request, CancellationToken cancellationToken = default) => throw Missing("Image processing");
    public Task SaveImageAsync(CapturedImage image, string path, string format, int jpegQuality = 90, CancellationToken cancellationToken = default) => throw Missing("Image saving");
    public Task<CapturedImage> LoadImageAsync(string path, CancellationToken cancellationToken = default) => throw Missing("Image loading");
    public Task ExportImageAsync(CapturedImage image, string path, string format, int jpegQuality = 90, CancellationToken cancellationToken = default) => throw Missing("Image exporting");
    public string GetSaveLocation() => throw Missing("Image save location");
    public Task<bool> RegisterAsync(string action, string shortcut, CancellationToken cancellationToken = default) => throw Missing("Global hotkeys");
    public Task UnregisterAsync(string action, CancellationToken cancellationToken = default) => throw Missing("Global hotkeys");
}
