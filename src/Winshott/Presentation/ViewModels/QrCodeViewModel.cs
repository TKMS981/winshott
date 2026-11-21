using CommunityToolkit.Mvvm.ComponentModel;

namespace Winshott.Presentation.ViewModels;

public partial class QrCodeViewModel : ObservableObject
{
    [ObservableProperty]
    private string _statusMessage = "QR and barcode reading is not implemented yet.";
}
