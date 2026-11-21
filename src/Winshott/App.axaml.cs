using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Winshott.Infrastructure;
using Winshott.Presentation.Views;

namespace Winshott;

public partial class App : Avalonia.Application
{
    private ServiceProvider? _services;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        services.AddWinshottServices();
        _services = services.BuildServiceProvider();
        var logger = _services.GetRequiredService<ILogger<App>>();
        logger.LogInformation("Winshott starting on {OperatingSystem} ({Architecture})", Environment.OSVersion, System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = _services.GetRequiredService<MainWindow>();
            desktop.Exit += (_, _) =>
            {
                logger.LogInformation("Winshott stopping");
                _services.Dispose();
                _services = null;
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
