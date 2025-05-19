using Avalonia.Controls;
using Avalonia.Platform.Storage;
using MediaVault.ViewModels;
using MediaVault.Views;

namespace MediaVault.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
        }

        private async void OnScanDirectoryClick(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var folderResult = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select a Folder",
                AllowMultiple = false
            });

            if (folderResult != null && folderResult.Count > 0 && DataContext is MainWindowViewModel viewModel)
            {
                await viewModel.ScanDirectory(folderResult[0].Path.LocalPath);
            }
        }

        // private void OnSearchClick(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        // {
        //     if (DataContext is MainWindowViewModel viewModel && !string.IsNullOrEmpty(SearchBox?.Text))
        //     {
        //         viewModel.Search(SearchBox.Text);
        //     }
        // }

        private void OnToggleViewClick(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.ToggleViewMode();
            }
        }

        private void OnCategorySelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is MainWindowViewModel viewModel && e.AddedItems.Count > 0 && e.AddedItems[0] is string selectedCategory)
            {
                viewModel.FilterByCategory(selectedCategory);
            }
        }

        private void OnMediaItemDoubleTapped(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is MediaVault.ViewModels.MainWindowViewModel viewModel)
            {
                viewModel.PlaySelectedMedia();
            }
        }

        // private async void OnSettingsClick(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        // {
        //     var settingsWindow = new SettingsWindow();
        //     await settingsWindow.ShowDialog(this);
        // }

        private void OnViewingHistoryClick(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var historyWindow = new ViewingHistoryWindow
            {
                Title = "Історія перегляду",
                Width = 600,
                Height = 400
            };
            historyWindow.ShowDialog(this);
        }
    }
}