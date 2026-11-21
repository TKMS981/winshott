using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Winshott.Application.Interfaces;
using Winshott.Domain.Models;
using Winshott.Infrastructure.Imaging;

namespace Winshott.Presentation.ViewModels;

public partial class CaptureViewModel : ObservableObject
{
    private readonly IScreenCaptureService _captureService;
    private readonly IScreenCapturePermissionService _permissionService;
    private readonly IClipboardService _clipboardService;
    private readonly IFileService _fileService;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<CaptureViewModel> _logger;

    public CaptureViewModel(IScreenCaptureService captureService, IScreenCapturePermissionService permissionService,
        IClipboardService clipboardService, IFileService fileService, ISettingsService settingsService, ILogger<CaptureViewModel> logger)
    {
        _captureService = captureService;
        _permissionService = permissionService;
        _clipboardService = clipboardService;
        _fileService = fileService;
        _settingsService = settingsService;
        _logger = logger;
    }

    [ObservableProperty] private string _statusMessage = "Screen capture is available on macOS. Select a display to begin.";
    [ObservableProperty] private string _permissionMessage = "Checking Screen Recording permission…";
    [ObservableProperty] private bool _isPermissionGranted;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private IReadOnlyList<DisplayInfo> _displays = [];
    [ObservableProperty] private DisplayInfo? _selectedDisplay;
    [ObservableProperty] private CapturedImage? _capturedImage;
    [ObservableProperty] private Bitmap? _preview;
    [ObservableProperty] private string _capturedDimensions = string.Empty;

    [RelayCommand]
    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        try
        {
            var permission = await _permissionService.GetPermissionAsync(cancellationToken);
            IsPermissionGranted = permission.IsGranted;
            PermissionMessage = permission.Message;
            if (permission.IsGranted)
            {
                Displays = await _captureService.GetDisplaysAsync(cancellationToken);
                SelectedDisplay = Displays.FirstOrDefault(display => display.IsPrimary) ?? Displays.FirstOrDefault();
                StatusMessage = Displays.Count == 0 ? "macOS did not report any displays." : $"{Displays.Count} display(s) ready. Capture is stored locally on this device.";
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Could not initialize macOS screen capture");
            StatusMessage = "Screen capture is unavailable. Check the application log for details.";
            PermissionMessage = exception.Message;
        }
    }

    [RelayCommand]
    private async Task RequestPermissionAsync(CancellationToken cancellationToken)
    {
        try
        {
            var permission = await _permissionService.RequestPermissionAsync(cancellationToken);
            IsPermissionGranted = permission.IsGranted;
            PermissionMessage = permission.Message;
            if (permission.IsGranted) await InitializeAsync(cancellationToken);
            else StatusMessage = "Grant Screen Recording to Winshott in System Settings, then restart the app if the permission state does not update.";
        }
        catch (Exception exception) { Report(exception, "requesting Screen Recording permission"); }
    }

    [RelayCommand]
    private async Task OpenPermissionSettingsAsync(CancellationToken cancellationToken)
    {
        try { await _permissionService.OpenPermissionSettingsAsync(cancellationToken); }
        catch (Exception exception) { Report(exception, "opening Screen Recording settings"); }
    }

    [RelayCommand]
    private async Task CaptureFullScreenAsync(CancellationToken cancellationToken)
    {
        if (!IsPermissionGranted)
        {
            StatusMessage = "Screen Recording permission is required. Use the permission request or settings action above.";
            return;
        }
        if (SelectedDisplay is null) { StatusMessage = "Select an available display first."; return; }
        IsBusy = true;
        try
        {
            var image = await _captureService.CaptureFullScreenAsync(SelectedDisplay.Id, cancellationToken);
            await SetCapturedImageAsync(image, cancellationToken);
        }
        catch (Exception exception) { Report(exception, "capturing display"); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task CopyImageAsync(CancellationToken cancellationToken)
    {
        if (CapturedImage is null) return;
        try { await _clipboardService.CopyImageAsync(CapturedImage, cancellationToken); StatusMessage = "Captured image copied to the clipboard."; }
        catch (Exception exception) { Report(exception, "copying capture to clipboard"); }
    }

    [RelayCommand]
    private async Task SaveImageAsync(CancellationToken cancellationToken)
    {
        if (CapturedImage is null) return;
        try
        {
            var settings = await _settingsService.LoadAsync(cancellationToken);
            var format = settings.DefaultImageFormat;
            var directory = string.IsNullOrWhiteSpace(settings.SaveLocation) ? _fileService.GetSaveLocation() : settings.SaveLocation;
            var path = Path.Combine(directory, LocalImageFileService.CreateTimestampedFileName(DateTimeOffset.Now, format));
            await SaveImageToPathAsync(path, format, settings.JpegQuality, cancellationToken);
        }
        catch (Exception exception) { Report(exception, "saving capture"); }
    }

    public async Task CaptureRegionAsync(PixelRect logicalRegion, CancellationToken cancellationToken = default)
    {
        if (!IsPermissionGranted) { StatusMessage = "Screen Recording permission is required."; return; }
        IsBusy = true;
        try
        {
            var image = await _captureService.CaptureRegionAsync(logicalRegion, cancellationToken);
            await SetCapturedImageAsync(image, cancellationToken);
        }
        catch (Exception exception) { Report(exception, "capturing selected region"); }
        finally { IsBusy = false; }
    }

    public async Task<CapturedImage?> CaptureFrameForRegionAsync(CancellationToken cancellationToken = default)
    {
        if (!IsPermissionGranted || SelectedDisplay is null) { StatusMessage = "Screen Recording permission and a selected display are required."; return null; }
        try { return await _captureService.CaptureFullScreenAsync(SelectedDisplay.Id, cancellationToken); }
        catch (Exception exception) { Report(exception, "preparing region selection"); return null; }
    }

    public async Task SaveImageToPathAsync(string path, string? format = null, int? jpegQuality = null, CancellationToken cancellationToken = default)
    {
        if (CapturedImage is null) return;
        try
        {
            var settings = await _settingsService.LoadAsync(cancellationToken);
            var selectedFormat = format ?? settings.DefaultImageFormat;
            var quality = jpegQuality ?? settings.JpegQuality;
            await _fileService.SaveImageAsync(CapturedImage, path, selectedFormat, quality, cancellationToken);
            StatusMessage = $"Saved capture to {path}";
        }
        catch (Exception exception) { Report(exception, "saving capture"); }
    }

    public Task<AppSettings> LoadExportSettingsAsync(CancellationToken cancellationToken = default) => _settingsService.LoadAsync(cancellationToken);

    private async Task SetCapturedImageAsync(CapturedImage image, CancellationToken cancellationToken)
    {
        CapturedImage = image;
        using var stream = new MemoryStream(image.Data, writable: false);
        Preview = new Bitmap(stream);
        CapturedDimensions = $"{image.Width:N0} × {image.Height:N0} px";
        StatusMessage = "Full-display capture complete. Review the preview, copy it, or save it locally.";
        var settings = await _settingsService.LoadAsync(cancellationToken);
        if (settings.AutoCopy)
        {
            try { await _clipboardService.CopyImageAsync(image, cancellationToken); StatusMessage = "Capture complete and copied to the clipboard."; }
            catch (Exception exception) { _logger.LogWarning(exception, "Auto-copy failed after capture"); StatusMessage = "Capture complete. Clipboard copy failed; use Copy to retry."; }
        }
        if (settings.AutoSave) await SaveImageAsync(cancellationToken);
    }

    private void Report(Exception exception, string operation)
    {
        _logger.LogError(exception, "Error while {Operation}", operation);
        StatusMessage = $"Could not complete capture action: {exception.Message}";
    }
}
