using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Winshott.Application.Interfaces;
using Winshott.Domain.Models;

namespace Winshott.Infrastructure.Configuration;

public sealed class JsonSettingsService(ILogger<JsonSettingsService> logger, string? settingsPath = null) : ISettingsService
{
    private readonly string _settingsPath = settingsPath ?? AppDataPaths.GetSettingsFilePath();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                var initial = CreateDefaults();
                await SaveAsync(initial, cancellationToken);
                logger.LogInformation("Created initial settings file at {SettingsPath}", _settingsPath);
                return initial;
            }

            await using var stream = new FileStream(_settingsPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
            var settings = await JsonSerializer.DeserializeAsync<AppSettings>(stream, JsonOptions, cancellationToken);
            if (settings is null)
                throw new JsonException("Settings JSON contained no settings object.");

            Normalize(settings);
            logger.LogInformation("Settings loaded from {SettingsPath}", _settingsPath);
            return settings;
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
        {
            logger.LogError(exception, "Could not load settings from {SettingsPath}; defaults will be used", _settingsPath);
            return CreateDefaults();
        }
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        Normalize(settings);
        var directory = Path.GetDirectoryName(_settingsPath) ?? throw new InvalidOperationException("Settings path must include a directory.");
        Directory.CreateDirectory(directory);
        var temporaryPath = _settingsPath + ".tmp";
        try
        {
            await using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
            {
                await JsonSerializer.SerializeAsync(stream, settings, JsonOptions, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }
            File.Move(temporaryPath, _settingsPath, overwrite: true);
            logger.LogInformation("Settings saved to {SettingsPath}", _settingsPath);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not save settings to {SettingsPath}", _settingsPath);
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            throw;
        }
    }

    private static AppSettings CreateDefaults() => new() { SaveLocation = AppDataPaths.GetDefaultSaveLocation() };

    private static void Normalize(AppSettings settings)
    {
        if (settings.Theme is not ("System" or "Light" or "Dark")) settings.Theme = "System";
        if (settings.DefaultImageFormat is not ("Png" or "Jpeg")) settings.DefaultImageFormat = "Png";
        settings.JpegQuality = Math.Clamp(settings.JpegQuality, 1, 100);
        if (string.IsNullOrWhiteSpace(settings.SaveLocation)) settings.SaveLocation = AppDataPaths.GetDefaultSaveLocation();
        settings.GlobalShortcuts ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }
}
