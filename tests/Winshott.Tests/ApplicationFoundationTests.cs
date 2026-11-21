using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Winshott.Application.Interfaces;
using Winshott.Infrastructure;
using Winshott.Infrastructure.Platform;
using Winshott.Presentation.ViewModels;

namespace Winshott.Tests;

public sealed class ApplicationFoundationTests
{
    [Fact]
    public void MainWindowViewModelStartsOnCapturePage()
    {
        var services = new ServiceCollection();
        services.AddTransient<CaptureViewModel>();
        using var provider = services.BuildServiceProvider();

        var viewModel = new MainWindowViewModel(provider, NullLogger<MainWindowViewModel>.Instance);

        Assert.IsType<CaptureViewModel>(viewModel.CurrentPage);
    }

    [Fact]
    public void ContainerRegistersSettingsViewModelsAndPlatformContracts()
    {
        var services = new ServiceCollection();
        services.AddWinshottServices();
        using var provider = services.BuildServiceProvider();

        Assert.IsAssignableFrom<ISettingsService>(provider.GetRequiredService<ISettingsService>());
        Assert.IsAssignableFrom<IScreenCaptureService>(provider.GetRequiredService<IScreenCaptureService>());
        Assert.IsAssignableFrom<IClipboardService>(provider.GetRequiredService<IClipboardService>());
        Assert.IsAssignableFrom<IOcrService>(provider.GetRequiredService<IOcrService>());
        Assert.IsAssignableFrom<IQrCodeService>(provider.GetRequiredService<IQrCodeService>());
        Assert.IsAssignableFrom<IImageProcessingService>(provider.GetRequiredService<IImageProcessingService>());
        Assert.IsAssignableFrom<IFileService>(provider.GetRequiredService<IFileService>());
        Assert.IsAssignableFrom<IHotkeyService>(provider.GetRequiredService<IHotkeyService>());
        Assert.NotNull(provider.GetRequiredService<CaptureViewModel>());
        Assert.NotNull(provider.GetRequiredService<OcrViewModel>());
        Assert.NotNull(provider.GetRequiredService<QrCodeViewModel>());
        Assert.NotNull(provider.GetRequiredService<SettingsViewModel>());
    }

    [Fact]
    public async Task UnimplementedPlatformServicesFailExplicitly()
    {
        var services = new ServiceCollection();
        services.AddUnimplementedPlatformServices();
        using var provider = services.BuildServiceProvider();

        var capture = provider.GetRequiredService<IScreenCaptureService>();
        var exception = await Assert.ThrowsAsync<PlatformNotSupportedException>(() => capture.GetDisplaysAsync(TestContext.Current.CancellationToken));
        Assert.Contains("not implemented", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
