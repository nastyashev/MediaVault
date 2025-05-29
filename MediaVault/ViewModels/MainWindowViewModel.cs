using System;
using System.ComponentModel;
using MediaVault.Views;

namespace MediaVault.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public MainWindowViewModel()
        {
            MediaLibraryPageViewModel = new MediaLibraryPageViewModel();
            ViewingHistoryViewModel = MediaLibraryPageViewModel.ViewingHistoryViewModel;

            // Створюємо View і встановлюємо DataContext
            MediaLibraryPageView = new MediaLibraryPageView { DataContext = MediaLibraryPageViewModel };
            ViewingHistoryView = new ViewingHistoryView { DataContext = ViewingHistoryViewModel };

            // Підписка на події для перемикання сторінок
            MediaLibraryPageViewModel.ShowViewingHistoryRequested += OnShowViewingHistoryRequested;
            ViewingHistoryViewModel.BackToLibraryRequested += OnBackToLibraryRequested;

            CurrentPage = MediaLibraryPageView;
        }

        public MediaLibraryPageViewModel MediaLibraryPageViewModel { get; }
        public ViewingHistoryViewModel ViewingHistoryViewModel { get; }
        public MediaLibraryPageView MediaLibraryPageView { get; }
        public ViewingHistoryView ViewingHistoryView { get; }

        public object CurrentPage
        {
            get => _currentPage;
            set
            {
                if (_currentPage != value)
                {
                    _currentPage = value;
                    OnPropertyChanged(nameof(CurrentPage));
                }
            }
        }
        private object _currentPage;

        private void OnShowViewingHistoryRequested(object? sender, EventArgs e)
        {
            CurrentPage = ViewingHistoryView;
        }

        private void OnBackToLibraryRequested(object? sender, EventArgs e)
        {
            CurrentPage = MediaLibraryPageView;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
