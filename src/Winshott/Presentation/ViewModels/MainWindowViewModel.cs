using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Winshott.Presentation.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IServiceProvider _services;
    private readonly ILogger<MainWindowViewModel> _logger;

    [ObservableProperty]
    private object _currentPage;

    public MainWindowViewModel(IServiceProvider services, ILogger<MainWindowViewModel> logger)
    {
        _services = services;
        _logger = logger;
        _currentPage = services.GetRequiredService<CaptureViewModel>();
    }

    [RelayCommand]
    private void NavigateCapture() => SetPage<CaptureViewModel>("Capture");

    [RelayCommand]
    private void NavigateOcr() => SetPage<OcrViewModel>("OCR");

    [RelayCommand]
    private void NavigateQrCode() => SetPage<QrCodeViewModel>("QR reader");

    [RelayCommand]
    private void NavigateHistory() => SetPage<HistoryViewModel>("History");

    [RelayCommand]
    private async Task NavigateSettingsAsync()
    {
        var viewModel = _services.GetRequiredService<SettingsViewModel>();
        await viewModel.LoadCommand.ExecuteAsync(null);
        SetPage(viewModel, "Settings");
    }

    private void SetPage<TViewModel>(string name) where TViewModel : notnull => SetPage(_services.GetRequiredService<TViewModel>(), name);

    private void SetPage(object page, string name)
    {
        CurrentPage = page;
        _logger.LogInformation("Navigated to {Page}", name);
    }
}
