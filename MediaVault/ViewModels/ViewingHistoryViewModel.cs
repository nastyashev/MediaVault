using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using MediaVault.Models;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia;

namespace MediaVault.ViewModels
{
    public class ViewingHistoryViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<ViewingHistoryRecord> _sortedHistory;
        public ObservableCollection<ViewingHistoryRecord> SortedHistory
        {
            get => _sortedHistory;
            set
            {
                if (_sortedHistory != value)
                {
                    _sortedHistory = value;
                    OnPropertyChanged(nameof(SortedHistory));
                }
            }
        }

        private bool _isViewingHistoryVisible;
        public bool IsViewingHistoryVisible
        {
            get => _isViewingHistoryVisible;
            set
            {
                if (_isViewingHistoryVisible != value)
                {
                    _isViewingHistoryVisible = value;
                    OnPropertyChanged(nameof(IsViewingHistoryVisible));
                }
            }
        }

        public ICommand ExportCommand { get; }
        public ICommand HideHistoryCommand { get; }

        public ViewingHistoryViewModel()
        {
            var log = ViewingHistoryLog.Load();
            var sorted = log.Records
                .OrderByDescending(r => r.ViewDate)
                .ToList();
            SortedHistory = new ObservableCollection<ViewingHistoryRecord>(sorted);

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

            HideHistoryCommand = new RelayCommand(_ => IsViewingHistoryVisible = false);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
