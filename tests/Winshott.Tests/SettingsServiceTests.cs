using Microsoft.Extensions.Logging.Abstractions;
using Winshott.Domain.Models;
using Winshott.Infrastructure.Configuration;

namespace Winshott.Tests;

public sealed class SettingsServiceTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "Winshott.Tests", Guid.NewGuid().ToString("N"));
    private string SettingsPath => Path.Combine(_directory, "settings.json");

    [Fact]
    public async Task LoadCreatesAndReturnsInitialSettings()
    {
        var service = new JsonSettingsService(NullLogger<JsonSettingsService>.Instance, SettingsPath);

        var settings = await service.LoadAsync(TestContext.Current.CancellationToken);

        Assert.True(File.Exists(SettingsPath));
        Assert.Equal("System", settings.Theme);
        Assert.Equal("Png", settings.DefaultImageFormat);
        Assert.Equal(90, settings.JpegQuality);
        Assert.True(settings.AutoCopy);
        Assert.False(settings.AutoSave);
        Assert.False(string.IsNullOrWhiteSpace(settings.SaveLocation));
    }

    [Fact]
    public async Task SaveAndLoadPreservesSettings()
    {
        var service = new JsonSettingsService(NullLogger<JsonSettingsService>.Instance, SettingsPath);
        var expected = new AppSettings
        {
            Theme = "Dark",
            SaveLocation = Path.Combine(_directory, "captures"),
            DefaultImageFormat = "Jpeg",
            JpegQuality = 76,
            AutoCopy = false,
            AutoSave = true,
            StartupAction = "StartInTray"
        };

        await service.SaveAsync(expected, TestContext.Current.CancellationToken);
        var actual = await service.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(expected.Theme, actual.Theme);
        Assert.Equal(expected.SaveLocation, actual.SaveLocation);
        Assert.Equal(expected.DefaultImageFormat, actual.DefaultImageFormat);
        Assert.Equal(expected.JpegQuality, actual.JpegQuality);
        Assert.Equal(expected.AutoCopy, actual.AutoCopy);
        Assert.Equal(expected.AutoSave, actual.AutoSave);
        Assert.Equal(expected.StartupAction, actual.StartupAction);
    }

    [Fact]
    public async Task MalformedJsonReturnsDefaultsAndPreservesOriginalFile()
    {
        Directory.CreateDirectory(_directory);
        const string malformed = "{ this is not valid JSON";
        await File.WriteAllTextAsync(SettingsPath, malformed, TestContext.Current.CancellationToken);
        var service = new JsonSettingsService(NullLogger<JsonSettingsService>.Instance, SettingsPath);

        var settings = await service.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal("System", settings.Theme);
        Assert.Equal("Png", settings.DefaultImageFormat);
        Assert.Equal(malformed, await File.ReadAllTextAsync(SettingsPath, TestContext.Current.CancellationToken));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
    }
}
