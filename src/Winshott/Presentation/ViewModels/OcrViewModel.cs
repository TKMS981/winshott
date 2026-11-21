using CommunityToolkit.Mvvm.ComponentModel;

namespace Winshott.Presentation.ViewModels;

public partial class OcrViewModel : ObservableObject
{
    [ObservableProperty]
    private string _statusMessage = "The platform OCR service is not implemented yet.";
}
