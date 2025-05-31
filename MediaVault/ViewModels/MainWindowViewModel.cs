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
            StatisticsPageViewModel = new StatisticsPageViewModel();

            // Додаємо сторінку налаштувань
            SettingsPageViewModel = new SettingsPageViewModel();

            MediaLibraryPageViewModel.ShowViewingHistoryRequested += OnShowViewingHistoryRequested;
            ViewingHistoryViewModel.BackToLibraryRequested += OnBackToLibraryRequested;
            MediaLibraryPageViewModel.ShowStatisticsRequested += OnShowStatisticsRequested;
            StatisticsPageViewModel.BackToLibraryRequested += OnBackToLibraryRequested;

            // Додаємо обробник для переходу до налаштувань
            MediaLibraryPageViewModel.ShowSettingsRequested += OnShowSettingsRequested;
            SettingsPageViewModel.BackToLibraryRequested += OnBackToLibraryRequested;

            CurrentPage = MediaLibraryPageViewModel;
        }

        public MediaLibraryPageViewModel MediaLibraryPageViewModel { get; }
        public ViewingHistoryViewModel ViewingHistoryViewModel { get; }
        public StatisticsPageViewModel StatisticsPageViewModel { get; }
        public SettingsPageViewModel SettingsPageViewModel { get; }

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
            CurrentPage = ViewingHistoryViewModel;
        }

        private void OnShowStatisticsRequested(object? sender, EventArgs e)
        {
            CurrentPage = StatisticsPageViewModel;
        }

        private void OnShowSettingsRequested(object? sender, EventArgs e)
        {
            CurrentPage = SettingsPageViewModel;
        }

        private void OnBackToLibraryRequested(object? sender, EventArgs e)
        {
            CurrentPage = MediaLibraryPageViewModel;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
