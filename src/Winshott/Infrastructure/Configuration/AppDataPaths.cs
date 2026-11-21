namespace Winshott.Infrastructure.Configuration;

public static class AppDataPaths
{
    public static string GetApplicationDataDirectory()
    {
        if (OperatingSystem.IsMacOS())
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support", "Winshott");

        if (OperatingSystem.IsWindows())
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Winshott");

        var basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (string.IsNullOrWhiteSpace(basePath))
            basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
        return Path.Combine(basePath, "Winshott");
    }

    public static string GetSettingsFilePath() => Path.Combine(GetApplicationDataDirectory(), "settings.json");

    public static string GetLogFilePath() => Path.Combine(GetApplicationDataDirectory(), "logs", "winshott.log");

    public static string GetDefaultSaveLocation() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Winshott");
}
