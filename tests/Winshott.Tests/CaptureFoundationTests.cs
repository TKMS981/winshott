using Microsoft.Extensions.Logging.Abstractions;
using SkiaSharp;
using Winshott.Application.Interfaces;
using Winshott.Domain.Models;
using Winshott.Infrastructure.Configuration;
using Winshott.Infrastructure.Imaging;
using Winshott.Presentation.ViewModels;

namespace Winshott.Tests;

public sealed class CaptureFoundationTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "Winshott.Tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void LogicalToPixelHonorsOriginAndRetinaScale()
    {
        var display = new DisplayInfo("1", "Retina", new PixelRect(-100, 20, 100, 80), 2, 200, 160);
        Assert.Equal(new PixelRect(20, 10, 30, 24), ScreenGeometry.LogicalToPixel(new PixelRect(-90, 25, 15, 12), display));
    }

    [Fact]
    public void ClampToImageHandlesNegativeAndOverflowingEdges()
    {
        Assert.Equal(new PixelRect(0, 5, 8, 5), ScreenGeometry.ClampToImage(new PixelRect(-2, 5, 10, 10), 8, 10));
        Assert.Equal(new PixelRect(8, 0, 0, 0), ScreenGeometry.ClampToImage(new PixelRect(9, -3, 4, 2), 8, 10));
    }

    [Fact]
    public async Task CropAndPngJpegSavePreserveImageDimensions()
    {
        using var source = new SKBitmap(16, 12);
        source.Erase(SKColors.CornflowerBlue);
        using var encoded = source.Encode(SKEncodedImageFormat.Png, 100);
        var image = new CapturedImage(encoded.ToArray(), source.Width, source.Height);
        var token = TestContext.Current.CancellationToken;
        var crop = await new SkiaImageProcessingService().ProcessAsync(image, new ImageProcessingRequest(ImageOperation.Crop, new PixelRect(3, 2, 7, 5)), token);
        Assert.Equal((7, 5), (crop.Width, crop.Height));

        var files = new LocalImageFileService();
        var png = Path.Combine(_directory, LocalImageFileService.CreateTimestampedFileName(DateTimeOffset.UnixEpoch, "Png"));
        var jpeg = Path.Combine(_directory, LocalImageFileService.CreateTimestampedFileName(DateTimeOffset.UnixEpoch.AddSeconds(1), "Jpeg"));
        await files.SaveImageAsync(crop, png, "Png", cancellationToken: token);
        await files.SaveImageAsync(crop, jpeg, "Jpeg", 85, token);
        var loadedPng = await files.LoadImageAsync(png, token);
        Assert.Equal((7, 5), (loadedPng.Width, loadedPng.Height));
        var loadedJpeg = await files.LoadImageAsync(jpeg, token);
        Assert.Equal((7, 5), (loadedJpeg.Width, loadedJpeg.Height));
        Assert.Equal("image/jpeg", loadedJpeg.MediaType);
    }

    [Fact]
    public void TimestampedFileNamesHaveSafeFormatExtensions()
    {
        Assert.Equal("Winshott_2026-01-02_030405_006.png", LocalImageFileService.CreateTimestampedFileName(new DateTimeOffset(2026, 1, 2, 3, 4, 5, 6, TimeSpan.Zero), "Png"));
        Assert.EndsWith(".jpg", LocalImageFileService.CreateTimestampedFileName(DateTimeOffset.UnixEpoch, "Jpeg"));
    }

    [Fact]
    public async Task CaptureViewModelInitializesWithoutRequestingPermission()
    {
        var capture = new FakeCaptureService();
        var permission = new FakePermissionService();
        var settings = new JsonSettingsService(NullLogger<JsonSettingsService>.Instance, Path.Combine(_directory, "settings.json"));
        var vm = new CaptureViewModel(capture, permission, new NoopClipboard(), new LocalImageFileService(), settings, NullLogger<CaptureViewModel>.Instance);

        await vm.InitializeCommand.ExecuteAsync(null);

        Assert.True(vm.IsPermissionGranted);
        Assert.Equal("7", vm.SelectedDisplay?.Id);
        Assert.Equal(0, permission.RequestCalls);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
    }

    private sealed class FakePermissionService : IScreenCapturePermissionService
    {
        public int RequestCalls { get; private set; }
        public Task<ScreenCapturePermission> GetPermissionAsync(CancellationToken cancellationToken = default) => Task.FromResult(new ScreenCapturePermission(true, false, "Granted"));
        public Task<ScreenCapturePermission> RequestPermissionAsync(CancellationToken cancellationToken = default) { RequestCalls++; return Task.FromResult(new ScreenCapturePermission(true, false, "Granted")); }
        public Task OpenPermissionSettingsAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeCaptureService : IScreenCaptureService
    {
        public Task<IReadOnlyList<DisplayInfo>> GetDisplaysAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<DisplayInfo>>([new DisplayInfo("7", "Main", new PixelRect(-10, 0, 10, 10), 2, 20, 20, true)]);
        public Task<IReadOnlyList<WindowInfo>> GetWindowsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<WindowInfo>>([]);
        public Task<CapturedImage> CaptureFullScreenAsync(string? displayId = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<CapturedImage> CaptureRegionAsync(PixelRect region, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<CapturedImage> CaptureWindowAsync(string windowId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class NoopClipboard : IClipboardService
    {
        public Task CopyImageAsync(CapturedImage image, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CopyTextAsync(string text, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<CapturedImage?> GetImageAsync(CancellationToken cancellationToken = default) => Task.FromResult<CapturedImage?>(null);
        public Task<string?> GetTextAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);
    }
}
