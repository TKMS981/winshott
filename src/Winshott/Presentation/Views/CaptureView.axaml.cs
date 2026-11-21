using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Winshott.Domain.Models;
using Winshott.Infrastructure.Imaging;
using Winshott.Presentation.ViewModels;

namespace Winshott.Presentation.Views;

public partial class CaptureView : UserControl
{
    public CaptureView() => InitializeComponent();

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is CaptureViewModel viewModel)
            await viewModel.InitializeCommand.ExecuteAsync(null);
    }

    private async void OnSelectRegion(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not CaptureViewModel viewModel || viewModel.SelectedDisplay is not { } display) return;
        var frame = await viewModel.CaptureFrameForRegionAsync();
        if (frame is null) return;
        var dialog = new RegionSelectionWindow(frame, display.ScaleFactor);
        if (TopLevel.GetTopLevel(this) is not Window owner) return;
        await dialog.ShowDialog(owner);
        if (dialog.SelectedRegion is not { } pixels) return;
        var logical = new PixelRect(display.Bounds.X + (int)Math.Round(pixels.X / display.ScaleFactor),
            display.Bounds.Y + (int)Math.Round(pixels.Y / display.ScaleFactor),
            Math.Max(1, (int)Math.Round(pixels.Width / display.ScaleFactor)),
            Math.Max(1, (int)Math.Round(pixels.Height / display.ScaleFactor)));
        await viewModel.CaptureRegionAsync(logical);
    }

    private async void OnSaveImage(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not CaptureViewModel viewModel) return;
        var storage = TopLevel.GetTopLevel(this)?.StorageProvider;
        if (storage is null || !storage.CanSave) return;
        var settings = await viewModel.LoadExportSettingsAsync();
        var jpeg = settings.DefaultImageFormat.Equals("Jpeg", StringComparison.OrdinalIgnoreCase);
        var pngType = new FilePickerFileType("PNG image") { Patterns = ["*.png"] };
        var jpegType = new FilePickerFileType("JPEG image") { Patterns = ["*.jpg", "*.jpeg"] };
        IStorageFolder? suggestedFolder = null;
        if (Directory.Exists(settings.SaveLocation))
            suggestedFolder = await storage.TryGetFolderFromPathAsync(new Uri(Path.GetFullPath(settings.SaveLocation)));
        var options = new FilePickerSaveOptions
        {
            Title = "Save capture",
            SuggestedFileName = LocalImageFileService.CreateTimestampedFileName(DateTimeOffset.Now, settings.DefaultImageFormat),
            SuggestedStartLocation = suggestedFolder,
            DefaultExtension = jpeg ? ".jpg" : ".png",
            SuggestedFileType = jpeg ? jpegType : pngType,
            FileTypeChoices =
            [
                pngType,
                jpegType
            ]
        };
        var file = await storage.SaveFilePickerAsync(options);
        var path = file?.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path)) return;
        var format = Path.GetExtension(path).Equals(".jpg", StringComparison.OrdinalIgnoreCase) || Path.GetExtension(path).Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ? "Jpeg" : "Png";
        await viewModel.SaveImageToPathAsync(path, format);
    }
}
