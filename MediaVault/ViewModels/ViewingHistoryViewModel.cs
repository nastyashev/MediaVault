using System.Collections.ObjectModel;
using System.Linq;
using MediaVault.Models;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia;
using System;

namespace MediaVault.ViewModels
{
    public class ViewingHistoryViewModel : ViewModelBase
    {
        private ObservableCollection<ViewingHistoryRecord> _sortedHistory;
        public ObservableCollection<ViewingHistoryRecord> SortedHistory
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
        public ICommand RefreshCommand { get; } // Додаємо команду оновлення

        public event EventHandler? BackToLibraryRequested;

        public ViewingHistoryViewModel()
        {
            RefreshHistory();

            ExportCommand = new RelayCommand(async _ =>
            {
                var dialog = new SaveFileDialog
                {
                    Title = "Експортувати історію перегляду",
                    InitialFileName = "history.txt",
                    Filters = { new FileDialogFilter { Name = "Text files", Extensions = { "txt" } } }
                };

                var mainWindow = Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                    ? desktop.MainWindow
                    : null;

                var path = await dialog.ShowAsync(mainWindow);
                if (!string.IsNullOrEmpty(path))
                {
                    try
                    {
                        using var writer = new System.IO.StreamWriter(path, false, System.Text.Encoding.UTF8);
                        writer.WriteLine("Історія перегляду:");
                        writer.WriteLine("------------------------------------------------------");
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
                    catch (System.Exception ex)
                    {
                        // Можна додати повідомлення про помилку
                    }
                }
            });

            HideHistoryCommand = new RelayCommand(_ =>
            {
                IsViewingHistoryVisible = false;
                BackToLibraryRequested?.Invoke(this, EventArgs.Empty);
            });

            RefreshCommand = new RelayCommand(_ => RefreshHistory()); // Ініціалізуємо команду
        }

        // Додаємо метод для оновлення історії
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
