using Avalonia.Controls;

namespace MediaVault.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MediaVault.ViewModels.MainWindowViewModel();
        }
    }
}