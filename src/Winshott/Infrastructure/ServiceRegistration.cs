using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Winshott.Application.Interfaces;
using Winshott.Infrastructure.Configuration;
using Winshott.Infrastructure.Logging;
using Winshott.Infrastructure.Platform;
using Winshott.Infrastructure.Imaging;
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
        services.AddSingleton<IOcrService, UnimplementedPlatformServices>();
        services.AddSingleton<IQrCodeService, UnimplementedPlatformServices>();
        services.AddSingleton<IHotkeyService, UnimplementedPlatformServices>();
        services.AddSingleton<IImageProcessingService, SkiaImageProcessingService>();
        services.AddSingleton<IFileService, LocalImageFileService>();
        if (OperatingSystem.IsMacOS())
        {
            services.AddSingleton<MacScreenCaptureService>();
            services.AddSingleton<IScreenCaptureService>(sp => sp.GetRequiredService<MacScreenCaptureService>());
            services.AddSingleton<IScreenCapturePermissionService>(sp => sp.GetRequiredService<MacScreenCaptureService>());
            services.AddSingleton<IClipboardService, MacClipboardService>();
        }
        else
        {
            services.AddSingleton<UnimplementedPlatformServices>();
            services.AddSingleton<IScreenCaptureService>(sp => sp.GetRequiredService<UnimplementedPlatformServices>());
            services.AddSingleton<IScreenCapturePermissionService>(sp => sp.GetRequiredService<UnimplementedPlatformServices>());
            services.AddSingleton<IClipboardService>(sp => sp.GetRequiredService<UnimplementedPlatformServices>());
        }
        return services;
    }
}
