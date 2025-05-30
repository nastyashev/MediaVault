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

            MediaLibraryPageView = new MediaLibraryPageView { DataContext = MediaLibraryPageViewModel };
            ViewingHistoryView = new ViewingHistoryView { DataContext = ViewingHistoryViewModel };
            StatisticsPageView = new StatisticsPageView { DataContext = StatisticsPageViewModel };

            // Додаємо сторінку налаштувань
            SettingsPageViewModel = new SettingsPageViewModel();
            SettingsPageView = new SettingsPageView { DataContext = SettingsPageViewModel };

            MediaLibraryPageViewModel.ShowViewingHistoryRequested += OnShowViewingHistoryRequested;
            ViewingHistoryViewModel.BackToLibraryRequested += OnBackToLibraryRequested;
            MediaLibraryPageViewModel.ShowStatisticsRequested += OnShowStatisticsRequested;
            StatisticsPageViewModel.BackToLibraryRequested += OnBackToLibraryRequested;

            // Додаємо обробник для переходу до налаштувань
            MediaLibraryPageViewModel.ShowSettingsRequested += OnShowSettingsRequested;
            SettingsPageViewModel.BackToLibraryRequested += OnBackToLibraryRequested;

            CurrentPage = MediaLibraryPageView;
        }

        public MediaLibraryPageViewModel MediaLibraryPageViewModel { get; }
        public ViewingHistoryViewModel ViewingHistoryViewModel { get; }
        public StatisticsPageViewModel StatisticsPageViewModel { get; }
        public MediaLibraryPageView MediaLibraryPageView { get; }
        public ViewingHistoryView ViewingHistoryView { get; }
        public StatisticsPageView StatisticsPageView { get; }
        public SettingsPageViewModel SettingsPageViewModel { get; }
        public SettingsPageView SettingsPageView { get; }

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

        private void OnShowStatisticsRequested(object? sender, EventArgs e)
        {
            CurrentPage = StatisticsPageView;
        }

        private void OnShowSettingsRequested(object? sender, EventArgs e)
        {
            CurrentPage = SettingsPageView;
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
