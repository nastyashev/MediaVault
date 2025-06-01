using System.Collections.ObjectModel;
using System.Linq;
using MediaVault.Models;
using System.Windows.Input;
using Avalonia;
using System;
using Avalonia.Platform.Storage;
using System.Diagnostics;

namespace MediaVault.ViewModels
{
    public class ViewingHistoryViewModel : ViewModelBase
    {
        private ObservableCollection<ViewingHistoryRecord>? _sortedHistory;
        public ObservableCollection<ViewingHistoryRecord>? SortedHistory
        {
            get => _sortedHistory;
            set => SetProperty(ref _sortedHistory, value);
        }

        private bool _isViewingHistoryVisible;
        public bool IsViewingHistoryVisible
        {
            get => _isViewingHistoryVisible;
            set => SetProperty(ref _isViewingHistoryVisible, value);
        }

        public ICommand ExportCommand { get; }
        public ICommand HideHistoryCommand { get; }
        public ICommand RefreshCommand { get; }

        public event EventHandler? BackToLibraryRequested;

        public ViewingHistoryViewModel()
        {
            RefreshHistory();
            ExportCommand = CreateExportCommand();
            HideHistoryCommand = CreateHideHistoryCommand();
            RefreshCommand = new RelayCommand(_ => RefreshHistory());
        }

        private ICommand CreateExportCommand()
        {
            return new RelayCommand(async _ =>
            {
                var mainWindow = Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                    ? desktop.MainWindow
                    : null;

                if (mainWindow == null || mainWindow.StorageProvider == null)
                    return;

                var file = await mainWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Експортувати історію перегляду",
                    SuggestedFileName = "history.txt",
                    FileTypeChoices = new[]
                    {
                        new FilePickerFileType("Text files") { Patterns = new[] { "*.txt" } }
                    }
                });

                if (file != null)
                {
                    try
                    {
                        using var stream = await file.OpenWriteAsync();
                        using var writer = new System.IO.StreamWriter(stream, System.Text.Encoding.UTF8);
                        WriteHistoryToWriter(writer);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.WriteLine($"Помилка при експорті історії перегляду: {ex.Message}");
                    }
                }
            });
        }

        private void WriteHistoryToWriter(System.IO.StreamWriter writer)
        {
            writer.WriteLine("Історія перегляду:");
            writer.WriteLine("------------------------------------------------------");
            if (SortedHistory != null)
            {
                foreach (var record in SortedHistory)
                {
                    writer.WriteLine($"ID: {record.RecordId}");
                    writer.WriteLine($"Назва: {record.FileName}");
                    writer.WriteLine($"Дата: {record.ViewDate:g}");
                    writer.WriteLine($"Тривалість перегляду (сек): {record.Duration}");
                    writer.WriteLine($"Позиція завершення (сек): {record.EndTime}");
                    writer.WriteLine($"Статус: {record.Status}");
                    writer.WriteLine("------------------------------------------------------");
                }
            }
        }

        private ICommand CreateHideHistoryCommand()
        {
            return new RelayCommand(_ =>
            {
                IsViewingHistoryVisible = false;
                BackToLibraryRequested?.Invoke(this, EventArgs.Empty);
            });
        }

        public void RefreshHistory()
        {
            var log = ViewingHistoryLog.Load();
            var sorted = log.Records
                .OrderByDescending(r => r.ViewDate)
                .ToList();
            SortedHistory = new ObservableCollection<ViewingHistoryRecord>(sorted);
        }
    }
}
