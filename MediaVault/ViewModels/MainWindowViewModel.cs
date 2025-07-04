﻿using System;
using MediaVault.Views;

namespace MediaVault.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public MainWindowViewModel()
        {
            MediaLibraryPageViewModel = new MediaLibraryPageViewModel();
            ViewingHistoryViewModel = MediaLibraryPageViewModel.ViewingHistoryViewModel;
            StatisticsPageViewModel = new StatisticsPageViewModel();

            SettingsPageViewModel = new SettingsPageViewModel();

            MediaLibraryPageViewModel.ShowViewingHistoryRequested += OnShowViewingHistoryRequested;
            ViewingHistoryViewModel.BackToLibraryRequested += OnBackToLibraryRequested;
            MediaLibraryPageViewModel.ShowStatisticsRequested += OnShowStatisticsRequested;
            StatisticsPageViewModel.BackToLibraryRequested += OnBackToLibraryRequested;
            MediaLibraryPageViewModel.ShowSettingsRequested += OnShowSettingsRequested;
            SettingsPageViewModel.BackToLibraryRequested += OnBackToLibraryRequested;
            SettingsPageViewModel.MediaFolderPathChanged += OnMediaFolderPathChanged;

            CurrentPage = MediaLibraryPageViewModel;
        }

        public MediaLibraryPageViewModel MediaLibraryPageViewModel { get; }
        public ViewingHistoryViewModel ViewingHistoryViewModel { get; }
        public StatisticsPageViewModel StatisticsPageViewModel { get; }
        public SettingsPageViewModel SettingsPageViewModel { get; }

        private object? _currentPage;
        public object? CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

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

        private async void OnMediaFolderPathChanged(object? sender, string newPath)
        {
            await MediaLibraryPageViewModel.ScanDirectory(newPath);
        }
    }
}
