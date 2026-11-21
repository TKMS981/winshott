using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Winshott.Application.Interfaces;
using Winshott.Infrastructure.Configuration;
using Winshott.Infrastructure.Logging;
using Winshott.Infrastructure.Platform;
using Winshott.Presentation.ViewModels;
using Winshott.Presentation.Views;

namespace Winshott.Infrastructure;

public static class ServiceRegistration
{
    public static IServiceCollection AddWinshottServices(this IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddConsole();
            builder.AddProvider(new FileLoggerProvider(AppDataPaths.GetLogFilePath()));
        });
        services.AddSingleton<ISettingsService, JsonSettingsService>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<CaptureViewModel>();
        services.AddTransient<OcrViewModel>();
        services.AddTransient<QrCodeViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<HistoryViewModel>();
        services.AddTransient<MainWindow>();
        services.AddUnimplementedPlatformServices();
        return services;
    }
}
