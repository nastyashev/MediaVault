using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml.Serialization;
using MediaVault.Models;
using TMDbLib.Client;
using System.Collections.Generic;
using Avalonia.Threading;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Movies;
using MediaVault.ViewModels; // додайте цей using, якщо потрібно

namespace MediaVault.ViewModels
{
    public class MediaLibraryPageViewModel : INotifyPropertyChanged
    {
        private readonly TMDbClient _tmdbClient;
        private bool _isListView;
        private bool _isGalleryView;
        private MediaFile? _selectedMediaFile;
        private static readonly string LibraryDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
        private static readonly string LibraryFilePath = Path.Combine(LibraryDirectory, "library.xml");

        // Додаємо ViewModel для історії перегляду
        public ViewingHistoryViewModel ViewingHistoryViewModel { get; } = new ViewingHistoryViewModel();

        // Додаємо подію для перемикання на історію перегляду
        public event EventHandler? ShowViewingHistoryRequested;
        public event EventHandler? ShowStatisticsRequested;

        public MediaLibraryPageViewModel()
        {
            _tmdbClient = new TMDbClient("7354fcc62bb407139f5fcd0e5b62a435"); // Вставте TMDB API Key
            MediaFiles = new ObservableCollection<MediaFile>();
            Categories = new ObservableCollection<string> { "All", "Movies", "Series", "Music" };
            _isListView = true;
            _isGalleryView = !_isListView;

            SettingsCommand = new RelayCommand(_ => OnSettings());
            ScanDirectoryCommand = new RelayCommand(_ => OnScanDirectory());
            SearchCommand = new RelayCommand(_ => OnSearch());
            ShowViewingHistoryCommand = new RelayCommand(_ => ShowViewingHistory());
            ShowStatisticsCommand = new RelayCommand(_ => ShowStatistics());

            // Ensure Data directory exists
            if (!Directory.Exists(LibraryDirectory))
                Directory.CreateDirectory(LibraryDirectory);

            // Load library from file if exists
            if (File.Exists(LibraryFilePath))
            {
                LoadLibraryAndDisplay();
            }

            // Завантажити жанри з TMDb
            LoadGenresFromTmdb();

            Debug.WriteLine("[MainWindowViewModel] Initialized");
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
                    if (_isListView) IsGalleryView = false;
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
                    if (_isGalleryView) IsListView = false;
                    OnPropertyChanged(nameof(IsGalleryView));
                }
            }
        }

        public ICommand SettingsCommand { get; }
        public ICommand ScanDirectoryCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand ShowViewingHistoryCommand { get; }
        public ICommand ShowStatisticsCommand { get; }

        private string? _selectedGenre;
        public ObservableCollection<string> AvailableGenres { get; } = new ObservableCollection<string> { "Всі жанри" };

        public string? SelectedGenre
        {
            get => _selectedGenre;
            set
            {
                if (_selectedGenre != value)
                {
                    _selectedGenre = value;
                    OnPropertyChanged(nameof(SelectedGenre));
                    ApplyGenreFilter();
                }
            }
        }

        public enum SortOption
        {
            None,
            DateAddedAsc,
            DateAddedDesc,
            ReleaseYearAsc,
            ReleaseYearDesc,
            DurationAsc,
            DurationDesc
        }

        private SortOption _selectedSortOption = SortOption.None;
        public ObservableCollection<string> SortOptions { get; } = new ObservableCollection<string>
        {
            "Без сортування",
            "Дата додавання ↑",
            "Дата додавання ↓",
            "Рік релізу ↑",
            "Рік релізу ↓",
            "Тривалість ↑",
            "Тривалість ↓"
        };

        public string SelectedSortOption
        {
            get => SortOptions[(int)_selectedSortOption];
            set
            {
                var idx = SortOptions.IndexOf(value);
                if (idx >= 0 && _selectedSortOption != (SortOption)idx)
                {
                    _selectedSortOption = (SortOption)idx;
                    OnPropertyChanged(nameof(SelectedSortOption));
                    ApplySortAndFilter();
                }
            }
        }

        private string? _searchText;
        public string? SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged(nameof(SearchText));
                    ApplySortAndFilter();
                }
            }
        }

        private List<MediaFile> _allMediaFiles = new List<MediaFile>();

        public async Task ScanDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                var supportedExtensions = new[] { ".mp4", ".avi", ".mkv", ".mov", ".mp3", ".wav", ".flac" };
                var files = Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories)
                                     .Where(file => supportedExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase));

                var libraryEntries = new List<LibraryEntry>();

                MediaFiles.Clear();
                _allMediaFiles.Clear();

                var updateTasks = new List<Task>();

                foreach (var file in files)
                {
                    var mediaFile = new MediaFile(Path.GetFileName(file), file, GetMediaType(file));
                    string titleWithoutExtension = Path.GetFileNameWithoutExtension(file);
                    var updateTask = UpdateMediaDetailsFromTMDb(mediaFile, titleWithoutExtension).ContinueWith(_ =>
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            // Додаємо дату додавання та тривалість (якщо можливо)
                            mediaFile.AddedDate = DateTime.Now;
                            // mediaFile.Duration = ... // тут можна додати визначення тривалості, якщо реалізовано

                            MediaFiles.Add(mediaFile);
                            _allMediaFiles.Add(mediaFile);

                            var entry = new LibraryEntry
                            {
                                file_id = file,
                                title = mediaFile.Title,
                                genre = mediaFile.Genre,
                                added_date = mediaFile.AddedDate,
                                metadata = $"ReleaseYear: {mediaFile.ReleaseYear}, Cover: {mediaFile.CoverImagePath}, Duration: {mediaFile.Duration}"
                            };
                            libraryEntries.Add(entry);
                        });
                    });
                    updateTasks.Add(updateTask);
                }

                await Task.WhenAll(updateTasks);

                SaveLibraryToXml(libraryEntries, LibraryFilePath);
                ApplySortAndFilter();
            }
        }

        private void SaveLibraryToXml(List<LibraryEntry> entries, string filePath)
        {
            try
            {
                // Ensure Data directory exists before saving
                var dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var serializer = new XmlSerializer(typeof(List<LibraryEntry>));
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    serializer.Serialize(stream, entries);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving library: {ex.Message}");
            }
        }

        private List<LibraryEntry> LoadLibraryFromXml(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return new List<LibraryEntry>();
                var serializer = new XmlSerializer(typeof(List<LibraryEntry>));
                using (var stream = new FileStream(filePath, FileMode.Open))
                {
                    return (List<LibraryEntry>)serializer.Deserialize(stream);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading library: {ex.Message}");
                return new List<LibraryEntry>();
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
            if (IsListView)
            {
                IsListView = false;
                IsGalleryView = true;
            }
            else
            {
                IsListView = true;
                IsGalleryView = false;
            }
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

        private MediaVault.Models.MediaType GetMediaType(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            if (ext == ".mp4" || ext == ".avi" || ext == ".mkv" || ext == ".mov")
                return MediaVault.Models.MediaType.Video;
            return MediaVault.Models.MediaType.Audio;
        }

        // Змініть сигнатуру і додайте логування
        private async Task UpdateMediaDetailsFromTMDb(MediaFile mediaFile, string searchTitle)
        {
            try
            {
                Debug.WriteLine($"[TMDb] Searching for: {searchTitle}");
                var searchResult = await _tmdbClient.SearchMovieAsync(searchTitle);
                Debug.WriteLine($"[TMDb] Results for '{searchTitle}': {searchResult.Results.Count}");
                var movie = searchResult.Results.FirstOrDefault();
                if (movie != null)
                {
                    Debug.WriteLine($"[TMDb] Found: {movie.Title} (ID: {movie.Id})");
                    var movieDetails = await _tmdbClient.GetMovieAsync(movie.Id);
                    mediaFile.Genre = string.Join(", ", movieDetails.Genres.Select(g => g.Name));
                    mediaFile.CoverImagePath = "https://image.tmdb.org/t/p/w500" + movieDetails.PosterPath;
                    mediaFile.ReleaseYear = movieDetails.ReleaseDate?.Year ?? 0;
                    Debug.WriteLine($"[TMDb] Genre: {mediaFile.Genre}, Year: {mediaFile.ReleaseYear}, Cover: {mediaFile.CoverImagePath}");
                }
                else
                {
                    Debug.WriteLine($"[TMDb] No movie found for '{searchTitle}'");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error fetching data from TMDb: {ex.Message}");
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

            var mainWindow = Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            if (mainWindow != null)
            {
                var folder = await dialog.ShowAsync(mainWindow);
                if (!string.IsNullOrEmpty(folder))
                {
                    await ScanDirectory(folder);
                }
            }
        }

        private void OnSearch() { /* ... */ }

        private void ShowViewingHistory()
        {
            ShowViewingHistoryRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ShowStatistics()
        {
            ShowStatisticsRequested?.Invoke(this, EventArgs.Empty);
        }

        public void HideViewingHistory()
        {
            ViewingHistoryViewModel.IsViewingHistoryVisible = false;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        // Допоміжний клас для XML
        public class LibraryEntry
        {
            public string file_id { get; set; }
            public string title { get; set; }
            public string genre { get; set; }
            public DateTime added_date { get; set; }
            public string metadata { get; set; }
        }

        private async void LoadGenresFromTmdb()
        {
            try
            {
                var genres = await _tmdbClient.GetMovieGenresAsync();
                Dispatcher.UIThread.Post(() =>
                {
                    AvailableGenres.Clear();
                    AvailableGenres.Add("Всі жанри");
                    foreach (var genre in genres)
                    {
                        AvailableGenres.Add(genre.Name);
                    }
                    SelectedGenre = "Всі жанри";
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Не вдалося завантажити жанри TMDb: {ex.Message}");
            }
        }

        private void ApplySortAndFilter()
        {
            IEnumerable<MediaFile> filtered = _allMediaFiles;

            // Фільтрація за жанром
            if (!string.IsNullOrEmpty(SelectedGenre) && SelectedGenre != "Всі жанри")
            {
                filtered = filtered.Where(m => m.Genre != null && m.Genre.Split(',').Select(g => g.Trim()).Contains(SelectedGenre));
            }

            // Фільтрація за пошуком
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                filtered = filtered.Where(m => m.Title != null && m.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }

            // Сортування
            switch (_selectedSortOption)
            {
                case SortOption.DateAddedAsc:
                    filtered = filtered.OrderBy(m => m.AddedDate);
                    break;
                case SortOption.DateAddedDesc:
                    filtered = filtered.OrderByDescending(m => m.AddedDate);
                    break;
                case SortOption.ReleaseYearAsc:
                    filtered = filtered.OrderBy(m => m.ReleaseYear);
                    break;
                case SortOption.ReleaseYearDesc:
                    filtered = filtered.OrderByDescending(m => m.ReleaseYear);
                    break;
                case SortOption.DurationAsc:
                    filtered = filtered.OrderBy(m => m.Duration);
                    break;
                case SortOption.DurationDesc:
                    filtered = filtered.OrderByDescending(m => m.Duration);
                    break;
                default:
                    break;
            }

            MediaFiles.Clear();
            foreach (var file in filtered)
                MediaFiles.Add(file);
        }

        private void ApplyGenreFilter()
        {
            ApplySortAndFilter();
        }

        public void LoadLibraryAndDisplay()
        {
            MediaFiles.Clear();
            _allMediaFiles.Clear();
            var entries = LoadLibraryFromXml(LibraryFilePath);
            foreach (var entry in entries)
            {
                var mf = LibraryEntryToMediaFile(entry);
                MediaFiles.Add(mf);
                _allMediaFiles.Add(mf);
            }
            ApplySortAndFilter();
        }

        private MediaFile LibraryEntryToMediaFile(LibraryEntry entry)
        {
            var mediaType = GetMediaType(entry.file_id);
            var mediaFile = new MediaFile(entry.title, entry.file_id, mediaType)
            {
                Genre = entry.genre,
                ReleaseYear = ParseReleaseYearFromMetadata(entry.metadata),
                CoverImagePath = ParseCoverFromMetadata(entry.metadata),
                AddedDate = entry.added_date,
                Duration = ParseDurationFromMetadata(entry.metadata)
            };
            return mediaFile;
        }

        private static int ParseReleaseYearFromMetadata(string metadata)
        {
            // metadata format: "ReleaseYear: {year}, Cover: {cover}"
            if (string.IsNullOrEmpty(metadata)) return 0;
            var parts = metadata.Split(',');
            foreach (var part in parts)
            {
                if (part.Trim().StartsWith("ReleaseYear:"))
                {
                    var val = part.Split(':')[1].Trim();
                    if (int.TryParse(val, out int year))
                        return year;
                }
            }
            return 0;
        }

        private string ParseCoverFromMetadata(string metadata)
        {
            if (string.IsNullOrEmpty(metadata)) return string.Empty;
            var parts = metadata.Split(',');
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (trimmed.StartsWith("Cover:"))
                {
                    var val = trimmed.Substring("Cover:".Length).Trim();
                    return val;
                }
            }
            return string.Empty;
        }

        private static TimeSpan ParseDurationFromMetadata(string metadata)
        {
            if (string.IsNullOrEmpty(metadata)) return TimeSpan.Zero;
            var parts = metadata.Split(',');
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (trimmed.StartsWith("Duration:"))
                {
                    var val = trimmed.Substring("Duration:".Length).Trim();
                    if (TimeSpan.TryParse(val, out var ts))
                        return ts;
                }
            }
            return TimeSpan.Zero;
        }
    }
}
