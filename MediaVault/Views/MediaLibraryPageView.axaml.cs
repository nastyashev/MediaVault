using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Interactivity;
using MediaVault.ViewModels;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using System.Linq;
using Avalonia;

namespace MediaVault.Views
{
    public partial class MediaLibraryPageView : UserControl
    {
        public MediaLibraryPageView()
        {
            InitializeComponent();
        }

        private async void OnScanDirectoryClick(object sender, RoutedEventArgs e)
        {
            var window = TopLevel.GetTopLevel(this) as Window;
            if (window == null) return;

            var folderResult = await window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select a Folder",
                AllowMultiple = false
            });

            if (folderResult != null && folderResult.Count > 0 && DataContext is MediaLibraryPageViewModel viewModel)
            {
                await viewModel.ScanDirectory(folderResult[0].Path.LocalPath);
            }
        }

        private void OnToggleViewClick(object sender, RoutedEventArgs e)
        {
            if (DataContext is MediaLibraryPageViewModel viewModel)
            {
                viewModel.ToggleViewMode();
            }
        }

        private void OnCategorySelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is MediaLibraryPageViewModel viewModel && e.AddedItems.Count > 0 && e.AddedItems[0] is string selectedCategory)
            {
                viewModel.FilterByCategory(selectedCategory);
            }
        }

        private void OnMediaItemDoubleTapped(object? sender, RoutedEventArgs e)
        {
            if (DataContext is MediaLibraryPageViewModel viewModel)
            {
                viewModel.PlaySelectedMedia();
            }
        }

        private void OnViewingHistoryClick(object sender, RoutedEventArgs e)
        {
            if (DataContext is MediaLibraryPageViewModel viewModel)
            {
                viewModel.ShowViewingHistoryCommand.Execute(null);
            }
        }
    }
}
