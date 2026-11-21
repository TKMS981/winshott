namespace Winshott.Domain.Models;

public sealed class AppSettings
{
    public string Theme { get; set; } = "System";
    public string SaveLocation { get; set; } = string.Empty;
    public string DefaultImageFormat { get; set; } = "Png";
    public int JpegQuality { get; set; } = 90;
    public bool AutoCopy { get; set; } = true;
    public bool AutoSave { get; set; }
    public Dictionary<string, string> GlobalShortcuts { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Region"] = "Ctrl+Shift+A",
        ["FullScreen"] = "Ctrl+Shift+1",
        ["Window"] = "Ctrl+Shift+W",
        ["Scrolling"] = "Ctrl+Shift+S",
        ["OCR"] = "Ctrl+Shift+O",
        ["QR"] = "Ctrl+Shift+Q"
    };
    public string StartupAction { get; set; } = "ShowWindow";

    public AppSettings Clone() => new()
    {
        Theme = Theme,
        SaveLocation = SaveLocation,
        DefaultImageFormat = DefaultImageFormat,
        JpegQuality = JpegQuality,
        AutoCopy = AutoCopy,
        AutoSave = AutoSave,
        GlobalShortcuts = new Dictionary<string, string>(GlobalShortcuts, StringComparer.OrdinalIgnoreCase),
        StartupAction = StartupAction
    };
}
