using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using MediaVault.Models;
using TMDbLib.Client;

namespace MediaVault.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private readonly TMDbClient _tmdbClient;
        private bool _isListView;
        private bool _isGalleryView;
        private MediaFile? _selectedMediaFile;

        public MainWindowViewModel()
        {
            _tmdbClient = new TMDbClient("7354fcc62bb407139f5fcd0e5b62a435"); // Вставте TMDB API Key
            MediaFiles = new ObservableCollection<MediaFile>();
            Categories = new ObservableCollection<string> { "All", "Movies", "Series", "Music" };
            _isListView = true;
            _isGalleryView = !_isListView;

            SettingsCommand = new RelayCommand(_ => OnSettings());
            ScanDirectoryCommand = new RelayCommand(_ => OnScanDirectory());
            SearchCommand = new RelayCommand(_ => OnSearch());
        }

        public string Greeting { get; } = "Welcome to Avalonia!";
        public ObservableCollection<MediaFile> MediaFiles { get; }
        public ObservableCollection<string> Categories { get; }

        public MediaFile? SelectedMediaFile
        {
            get => _selectedMediaFile;
            set
            {
                if (_selectedMediaFile != value)
                {
                    _selectedMediaFile = value;
                    OnPropertyChanged(nameof(SelectedMediaFile));
                }
            }
        }

        public bool IsListView
        {
            get => _isListView;
            set
            {
                if (_isListView != value)
                {
                    _isListView = value;
                    OnPropertyChanged(nameof(IsListView));
                }
            }
        }

        public bool IsGalleryView
        {
            get => _isGalleryView;
            set
            {
                if (_isGalleryView != value)
                {
                    _isGalleryView = value;
                    OnPropertyChanged(nameof(IsGalleryView));
                }
            }
        }

        public ICommand SettingsCommand { get; }
        public ICommand ScanDirectoryCommand { get; }
        public ICommand SearchCommand { get; }

        public void ScanDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                // Список підтримуваних розширень
                var supportedExtensions = new[] { ".mp4", ".avi", ".mkv", ".mov", ".mp3", ".wav", ".flac" };

                var files = Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories)
                                     .Where(file => supportedExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase));
                foreach (var file in files)
                {
                    var mediaFile = new MediaFile(Path.GetFileName(file), file, GetMediaType(file));
                    MediaFiles.Add(mediaFile);
                }
            }
        }

        public void Search(string searchTerm)
        {
            for (int i = MediaFiles.Count - 1; i >= 0; i--)
            {
                if (!MediaFiles[i].Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                {
                    MediaFiles.RemoveAt(i);
                }
            }
        }

        public void ToggleViewMode()
        {
            IsListView = !IsListView;
            IsGalleryView = !IsGalleryView;
        }

        public void FilterByCategory(string category)
        {
            // Логіка фільтрації категорій (приклад з простим порівнянням)
            var filteredFiles = MediaFiles.Where(file => category == "All" || file.Genre.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
            MediaFiles.Clear();

            foreach (var file in filteredFiles)
            {
                MediaFiles.Add(file);
            }
        }

        private MediaType GetMediaType(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            if (ext == ".mp4" || ext == ".avi" || ext == ".mkv" || ext == ".mov")
                return MediaType.Video;
            return MediaType.Audio;
        }

        private async Task UpdateMediaDetailsFromTMDb(MediaFile mediaFile)
        {
            try
            {
                var movie = await _tmdbClient.GetMovieAsync(mediaFile.Title);
                if (movie != null)
                {
                    mediaFile.Genre = string.Join(", ", movie.Genres.Select(g => g.Name));
                    mediaFile.CoverImagePath = "https://image.tmdb.org/t/p/w500" + movie.PosterPath; // Базовий URL для зображень
                    mediaFile.ReleaseYear = movie.ReleaseDate?.Year ?? 0;
                }
            }
            catch (Exception ex)
            {
                // Логіка обробки помилок: поки що просто виводимо в консоль
                Console.WriteLine($"Error fetching data from TMDb: {ex.Message}");
            }
        }

        public void PlaySelectedMedia()
        {
            if (SelectedMediaFile != null && File.Exists(SelectedMediaFile.FilePath))
            {
                var mediaPlayerWindow = new MediaPlayerWindow(SelectedMediaFile);
                mediaPlayerWindow.Show();
            }
        }

        private void OnSettings() { /* ... */ }

        private async void OnScanDirectory()
        {
            var dialog = new Avalonia.Controls.OpenFolderDialog
            {
                Title = "Оберіть директорію для сканування"
            };

            // Потрібно отримати reference на головне вікно (MainWindow)
            var mainWindow = Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            if (mainWindow != null)
            {
                var folder = await dialog.ShowAsync(mainWindow);
                if (!string.IsNullOrEmpty(folder))
                {
                    MediaFiles.Clear();
                    ScanDirectory(folder);
                }
            }
        }

        private void OnSearch() { /* ... */ }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
