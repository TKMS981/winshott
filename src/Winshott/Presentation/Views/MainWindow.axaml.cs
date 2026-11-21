using Avalonia.Controls;
using Winshott.Presentation.ViewModels;

namespace Winshott.Presentation.Views;

public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    public MainWindow(MainWindowViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }
}
