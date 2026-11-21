using CommunityToolkit.Mvvm.ComponentModel;

namespace Winshott.Presentation.ViewModels;

public partial class HistoryViewModel : ObservableObject
{
    [ObservableProperty]
    private string _statusMessage = "Capture history is not implemented yet.";
}
