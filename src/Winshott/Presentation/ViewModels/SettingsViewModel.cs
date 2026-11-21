using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Winshott.Application.Interfaces;
using Winshott.Domain.Models;

namespace Winshott.Presentation.ViewModels;

public partial class SettingsViewModel(ISettingsService settingsService, ILogger<SettingsViewModel> logger) : ObservableObject
{
    public IReadOnlyList<string> ThemeOptions { get; } = ["System", "Light", "Dark"];
    public IReadOnlyList<string> FormatOptions { get; } = ["Png", "Jpeg"];
    public IReadOnlyList<string> StartupOptions { get; } = ["ShowWindow", "StartInTray"];

    [ObservableProperty] private string _theme = "System";
    [ObservableProperty] private string _saveLocation = string.Empty;
    [ObservableProperty] private string _defaultImageFormat = "Png";
    [ObservableProperty] private int _jpegQuality = 90;
    [ObservableProperty] private bool _autoCopy = true;
    [ObservableProperty] private bool _autoSave;
    [ObservableProperty] private string _startupAction = "ShowWindow";
    [ObservableProperty] private string _regionShortcut = "Ctrl+Shift+A";
    [ObservableProperty] private string _fullScreenShortcut = "Ctrl+Shift+1";
    [ObservableProperty] private string _windowShortcut = "Ctrl+Shift+W";
    [ObservableProperty] private string _scrollingShortcut = "Ctrl+Shift+S";
    [ObservableProperty] private string _ocrShortcut = "Ctrl+Shift+O";
    [ObservableProperty] private string _qrShortcut = "Ctrl+Shift+Q";
    [ObservableProperty] private string _statusMessage = string.Empty;

    [RelayCommand]
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var settings = await settingsService.LoadAsync(cancellationToken);
        Theme = settings.Theme;
        SaveLocation = settings.SaveLocation;
        DefaultImageFormat = settings.DefaultImageFormat;
        JpegQuality = settings.JpegQuality;
        AutoCopy = settings.AutoCopy;
        AutoSave = settings.AutoSave;
        StartupAction = settings.StartupAction;
        RegionShortcut = GetShortcut(settings, "Region", "Ctrl+Shift+A");
        FullScreenShortcut = GetShortcut(settings, "FullScreen", "Ctrl+Shift+1");
        WindowShortcut = GetShortcut(settings, "Window", "Ctrl+Shift+W");
        ScrollingShortcut = GetShortcut(settings, "Scrolling", "Ctrl+Shift+S");
        OcrShortcut = GetShortcut(settings, "OCR", "Ctrl+Shift+O");
        QrShortcut = GetShortcut(settings, "QR", "Ctrl+Shift+Q");
        ApplyTheme(Theme);
    }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        var settings = new AppSettings
        {
            Theme = Theme,
            SaveLocation = SaveLocation,
            DefaultImageFormat = DefaultImageFormat,
            JpegQuality = JpegQuality,
            AutoCopy = AutoCopy,
            AutoSave = AutoSave,
            StartupAction = StartupAction,
            GlobalShortcuts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Region"] = RegionShortcut,
                ["FullScreen"] = FullScreenShortcut,
                ["Window"] = WindowShortcut,
                ["Scrolling"] = ScrollingShortcut,
                ["OCR"] = OcrShortcut,
                ["QR"] = QrShortcut
            }
        };

        try
        {
            await settingsService.SaveAsync(settings, cancellationToken);
            ApplyTheme(Theme);
            StatusMessage = "Settings saved.";
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Settings could not be saved from the settings screen");
            StatusMessage = "Settings could not be saved. Check the application log.";
        }
    }

    private static string GetShortcut(AppSettings settings, string key, string fallback) => settings.GlobalShortcuts.TryGetValue(key, out var value) ? value : fallback;

    private static void ApplyTheme(string theme)
    {
        if (Avalonia.Application.Current is null) return;
        Avalonia.Application.Current.RequestedThemeVariant = theme switch
        {
            "Light" => ThemeVariant.Light,
            "Dark" => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };
    }
}
