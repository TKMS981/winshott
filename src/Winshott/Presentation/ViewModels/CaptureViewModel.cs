using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Winshott.Presentation.ViewModels;

public partial class CaptureViewModel : ObservableObject
{
    [ObservableProperty]
    private string _statusMessage = "Capture modes are planned for Phase 2.";

    [RelayCommand]
    private void SelectMode(string mode) => StatusMessage = $"{mode} capture is not implemented yet. It is planned for a later phase.";
}
