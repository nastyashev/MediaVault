using System;
using System.Collections.ObjectModel;
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
using TagLib;

namespace MediaVault.ViewModels
{
    public class MediaLibraryPageViewModel : ViewModelBase
    {
        private readonly TMDbClient _tmdbClient;
        private bool _isListView;
        private bool _isGalleryView;
        private MediaFile? _selectedMediaFile;
        private static readonly string LibraryDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
        private static readonly string LibraryFilePath = Path.Combine(LibraryDirectory, "library.xml");
        private static readonly string PlaylistsFilePath = Path.Combine(LibraryDirectory, "playlists.xml");
        private const string TmdbImageBaseUrl = "https://image.tmdb.org/t/p/w500";
        private const string PlaceholderImagePath = "Assets/placeholder.png";

        public ViewingHistoryViewModel ViewingHistoryViewModel { get; } = new ViewingHistoryViewModel();

        public event EventHandler? ShowViewingHistoryRequested;
        public event EventHandler? ShowStatisticsRequested;
        public event EventHandler? ShowSettingsRequested;

        public ObservableCollection<Playlist> Playlists { get; } = new ObservableCollection<Playlist>();

        private Playlist? _selectedPlaylist;
        public Playlist? SelectedPlaylist
        {
            get => _selectedPlaylist;
            set
            {
                if (SetProperty(ref _selectedPlaylist, value))
                {
                    UpdatePlaylistMediaFiles();
                    OnPropertyChanged(nameof(PlaylistMediaFiles));
                    (AddToPlaylistCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (RemoveFromPlaylistCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        private ObservableCollection<MediaFile> _playlistMediaFiles = new ObservableCollection<MediaFile>();
        public ObservableCollection<MediaFile> PlaylistMediaFiles => _playlistMediaFiles;

        private void UpdatePlaylistMediaFiles()
        {
            _playlistMediaFiles.Clear();
            if (SelectedPlaylist != null)
            {
                var files = _allMediaFiles.Where(m => SelectedPlaylist.FileIds.Contains(m.FilePath)).ToList();
                foreach (var f in files)
                    _playlistMediaFiles.Add(f);
            }
            OnPropertyChanged(nameof(PlaylistMediaFiles));
        }

        public ICommand CreatePlaylistCommand { get; }
        public ICommand AddToPlaylistCommand { get; }
        public ICommand RemoveFromPlaylistCommand { get; }

        public MediaLibraryPageViewModel()
        {
            _tmdbClient = new TMDbClient("7354fcc62bb407139f5fcd0e5b62a435");
            MediaFiles = new ObservableCollection<MediaFile>();
            Categories = new ObservableCollection<string> { "All", "Movies", "Series", "Music" };
            _isListView = true;
            _isGalleryView = !_isListView;

            SettingsCommand = new RelayCommand(_ => OnSettings());
            ScanDirectoryCommand = new RelayCommand(_ => OnScanDirectory());
            ShowViewingHistoryCommand = new RelayCommand(_ => ShowViewingHistory());
            ShowStatisticsCommand = new RelayCommand(_ => ShowStatistics());
            EditMediaCommand = new RelayCommand(EditMedia);
            CreatePlaylistCommand = new RelayCommand(_ => CreatePlaylistDialog());
            AddToPlaylistCommand = new RelayCommand(
                _ => AddSelectedToPlaylist(),
                _ => SelectedPlaylist != null && SelectedMediaFile != null && !SelectedPlaylist.FileIds.Contains(SelectedMediaFile.FilePath)
            );
            RemoveFromPlaylistCommand = new RelayCommand(
                _ => RemoveSelectedFromPlaylist(),
                _ => SelectedPlaylist != null && SelectedMediaFile != null && SelectedPlaylist.FileIds.Contains(SelectedMediaFile.FilePath)
            );

            if (!Directory.Exists(LibraryDirectory))
                Directory.CreateDirectory(LibraryDirectory);

            if (System.IO.File.Exists(LibraryFilePath))
            {
                LoadLibraryAndDisplay();
            }
            LoadGenresFromTmdb();
            LoadPlaylists();
        }

        public ObservableCollection<MediaFile> MediaFiles { get; }
        public ObservableCollection<string> Categories { get; }

        public MediaFile? SelectedMediaFile
        {
            get => _selectedMediaFile;
            set
            {
                if (SetProperty(ref _selectedMediaFile, value))
                {
                    (AddToPlaylistCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (RemoveFromPlaylistCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public bool IsListView
        {
            get => _isListView;
            set
            {
                if (SetProperty(ref _isListView, value) && _isListView)
                {
                    IsGalleryView = false;
                }
            }
        }

        public bool IsGalleryView
        {
            get => _isGalleryView;
            set
            {
                if (SetProperty(ref _isGalleryView, value) && _isGalleryView)
                {
                    IsListView = false;
                }
            }
        }

        public ICommand SettingsCommand { get; }
        public ICommand ScanDirectoryCommand { get; }
        public ICommand ShowViewingHistoryCommand { get; }
        public ICommand ShowStatisticsCommand { get; }
        public ICommand EditMediaCommand { get; }

        private string? _selectedGenre;
        private const string AllGenresLiteral = "Всі жанри";
        public ObservableCollection<string> AvailableGenres { get; } = new ObservableCollection<string> { AllGenresLiteral };

        public string? SelectedGenre
        {
            get => _selectedGenre;
            set
            {
                if (SetProperty(ref _selectedGenre, value))
                {
                    ApplyGenreFilter();
                }
            }
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
                if (SetProperty(ref _searchText, value))
                {
                    ApplySortAndFilter();
                }
            }
        }

        private string? _selectedStatusFilter = "Всі статуси";
        public string? SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set
            {
                if (SetProperty(ref _selectedStatusFilter, value))
                {
                    ApplySortAndFilter();
                }
            }
        }

        private readonly List<MediaFile> _allMediaFiles = new List<MediaFile>();

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
                            mediaFile.AddedDate = DateTime.Now;
                            mediaFile.Duration = TimeSpan.Zero;

                            try
                            {
                                using var tagFile = TagLib.File.Create(file);
                                mediaFile.Duration = tagFile.Properties.Duration;
                                mediaFile.Duration = tagFile.Properties.Duration;
                            }
                            catch
                            {
                                mediaFile.Duration = TimeSpan.Zero;
                            }

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

        private static void SaveLibraryToXml(List<LibraryEntry> entries, string filePath)
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

        private static List<LibraryEntry> LoadLibraryFromXml(string filePath)
        {
            try
            {
                if (!System.IO.File.Exists(filePath)) return new List<LibraryEntry>();
                var serializer = new XmlSerializer(typeof(List<LibraryEntry>));
                using (var stream = new FileStream(filePath, FileMode.Open))
                {
                    var result = serializer.Deserialize(stream) as List<LibraryEntry>;
                    return result ?? new List<LibraryEntry>();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading library: {ex.Message}");
                return new List<LibraryEntry>();
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
            var filteredFiles = MediaFiles.Where(file => category == "All" || file.Genre.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
            MediaFiles.Clear();

            foreach (var file in filteredFiles)
            {
                MediaFiles.Add(file);
            }
        }

        private static MediaType GetMediaType(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            if (ext == ".mp4" || ext == ".avi" || ext == ".mkv" || ext == ".mov")
                return MediaType.Video;
            return MediaType.Audio;
        }

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
                    if (!string.IsNullOrEmpty(movie.PosterPath))
                        mediaFile.CoverImagePath = TmdbImageBaseUrl + movie.PosterPath;
                    else
                        mediaFile.CoverImagePath = PlaceholderImagePath;
                    mediaFile.ReleaseYear = movie.ReleaseDate?.Year ?? 0;
                    Debug.WriteLine($"[TMDb] Genre: {mediaFile.Genre}, Year: {mediaFile.ReleaseYear}, Cover: {mediaFile.CoverImagePath}");
                    Debug.WriteLine($"[TMDb] Genre: {mediaFile.Genre}, Year: {mediaFile.ReleaseYear}, Cover: {mediaFile.CoverImagePath}");
                }
                else
                {
                    Debug.WriteLine($"[TMDb] No movie found for '{searchTitle}'");
                    mediaFile.CoverImagePath = PlaceholderImagePath;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error fetching data from TMDb: {ex.Message}");
                mediaFile.CoverImagePath = PlaceholderImagePath;
            }
        }

        public void PlaySelectedMedia()
        {
            if (SelectedMediaFile != null && System.IO.File.Exists(SelectedMediaFile.FilePath))
            {
                var mediaPlayerWindow = new MediaPlayerWindow(SelectedMediaFile);
                mediaPlayerWindow.Show();
            }
        }

        private void OnSettings()
        {
            ShowSettingsRequested?.Invoke(this, EventArgs.Empty);
        }

        private async void OnScanDirectory()
        {
            var mainWindow = Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            if (mainWindow != null)
            {
                var folders = await mainWindow.StorageProvider.OpenFolderPickerAsync(new Avalonia.Platform.Storage.FolderPickerOpenOptions
                {
                    Title = "Оберіть директорію для сканування",
                    AllowMultiple = false
                });

                var folder = folders?.FirstOrDefault();
                if (folder != null)
                {
                    SaveMediaFolderPathToConfig(folder.Path.LocalPath);
                    await ScanDirectory(folder.Path.LocalPath);
                }
            }
        }

        private void SaveMediaFolderPathToConfig(string folderPath)
        {
            try
            {
                var dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
                var configPath = Path.Combine(dataDir, "config.xml");
                if (!Directory.Exists(dataDir))
                    Directory.CreateDirectory(dataDir);

                ConfigModel config;
                var serializer = new XmlSerializer(typeof(ConfigModel));
                if (System.IO.File.Exists(configPath))
                {
                    using var stream = System.IO.File.OpenRead(configPath);
                    config = (ConfigModel?)serializer.Deserialize(stream) ?? new ConfigModel();
                }
                else
                {
                    config = new ConfigModel();
                }
                config.MediaFolderPath = folderPath;
                using var writeStream = System.IO.File.Create(configPath);
                serializer.Serialize(writeStream, config);

                if (App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
                {
                    foreach (var window in desktop.Windows)
                    {
                        if (window.DataContext is MainWindowViewModel mainVm)
                        {
                            mainVm.SettingsPageViewModel?.UpdateMediaFolderPath(folderPath);
                        }
                    }
                }
                else
                {
                    ViewingHistoryViewModel?.GetType();
                }
            }
            catch
            {
                Debug.WriteLine("Не вдалося зберегти шлях до медіа директорії у конфігурації.");
            }
        }

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

        private async void LoadGenresFromTmdb()
        {
            try
            {
                var genreResult = await _tmdbClient.GetMovieGenresAsync();
                Dispatcher.UIThread.Post(() =>
                {
                    AvailableGenres.Clear();
                    AvailableGenres.Add(AllGenresLiteral);
                    foreach (var genre in genreResult)
                    {
                        AvailableGenres.Add(genre.Name);
                    }
                    SelectedGenre = AllGenresLiteral;
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Не вдалося завантажити жанри TMDb: {ex.Message}");
            }
        }

        public ObservableCollection<string> StatusFilters { get; } = new ObservableCollection<string>
        {
            "Всі статуси", "Переглянуто", "В процесі", "Не почато"
        };

        private void ApplySortAndFilter()
        {
            IEnumerable<MediaFile> filtered = _allMediaFiles;

            filtered = ApplyGenreFilter(filtered);
            filtered = ApplyStatusFilter(filtered);
            filtered = ApplySearchFilter(filtered);

            filtered = ApplySort(filtered);

            MediaFiles.Clear();
            foreach (var file in filtered)
                MediaFiles.Add(file);
        }

        private IEnumerable<MediaFile> ApplyGenreFilter(IEnumerable<MediaFile> files)
        {
            if (!string.IsNullOrEmpty(SelectedGenre) && SelectedGenre != AllGenresLiteral)
            {
                return files.Where(m => m.Genre != null && m.Genre.Split(',').Select(g => g.Trim()).Contains(SelectedGenre));
            }
            return files;
        }

        private IEnumerable<MediaFile> ApplyStatusFilter(IEnumerable<MediaFile> files)
        {
            if (!string.IsNullOrEmpty(SelectedStatusFilter) && SelectedStatusFilter != "Всі статуси")
            {
                var history = MediaVault.Models.ViewingHistoryLog.Load();
                var lastStatusByFile = history.Records
                    .GroupBy(r => r.FileId)
                    .Select(g => new { FileId = g.Key, Last = g.OrderByDescending(r => r.ViewDate).FirstOrDefault() })
                    .ToDictionary(x => x.FileId, x => x.Last?.Status);

                return files.Where(m =>
                {
                    if (!lastStatusByFile.TryGetValue(m.FilePath, out var status))
                        return SelectedStatusFilter == "Не почато";
                    if (status == "переглянуто")
                        return SelectedStatusFilter == "Переглянуто";
                    if (status == "в процесі")
                        return SelectedStatusFilter == "В процесі";
                    return false;
                });
            }
            return files;
        }

        private IEnumerable<MediaFile> ApplySearchFilter(IEnumerable<MediaFile> files)
        {
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                return files.Where(m => m.Title != null && m.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }
            return files;
        }

        private IEnumerable<MediaFile> ApplySort(IEnumerable<MediaFile> files)
        {
            switch (_selectedSortOption)
            {
                case SortOption.DateAddedAsc:
                    return files.OrderBy(m => m.AddedDate);
                case SortOption.DateAddedDesc:
                    return files.OrderByDescending(m => m.AddedDate);
                case SortOption.ReleaseYearAsc:
                    return files.OrderBy(m => m.ReleaseYear);
                case SortOption.ReleaseYearDesc:
                    return files.OrderByDescending(m => m.ReleaseYear);
                case SortOption.DurationAsc:
                    return files.OrderBy(m => m.Duration);
                case SortOption.DurationDesc:
                    return files.OrderByDescending(m => m.Duration);
                default:
                    return files;
            }
        }

        private void ApplyGenreFilter()
        {
            ApplySortAndFilter();
        }

        public void LoadLibraryAndDisplay()
        {
            MediaFiles.Clear();
            _allMediaFiles.Clear();
            var entries = MediaLibraryPageViewModel.LoadLibraryFromXml(LibraryFilePath);
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
            if (string.IsNullOrEmpty(metadata)) return 0;
            var parts = metadata.Split(',');
            var yearPart = parts
                .Where(part => part.Trim().StartsWith("ReleaseYear:"))
                .Select(part => part.Split(':')[1].Trim())
                .FirstOrDefault();

            if (yearPart != null && int.TryParse(yearPart, out int year))
                return year;
            return 0;
        }

        private static string ParseCoverFromMetadata(string metadata)
        {
            if (string.IsNullOrEmpty(metadata)) return string.Empty;
            var parts = metadata.Split(',');
            var coverPart = parts
                .Where(part => part.Trim().StartsWith("Cover:"))
                .Select(part => part.Split(':')[1].Trim())
                .FirstOrDefault();

            return coverPart ?? string.Empty;
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
                    if (TimeSpan.TryParse(val, System.Globalization.CultureInfo.InvariantCulture, out var ts))
                        return ts;
                }
            }
            return TimeSpan.Zero;
        }

        private async void EditMedia(object? parameter)
        {
            if (parameter is MediaFile mediaFile)
            {
                var window = Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                    ? desktop.MainWindow
                    : null;

                var genreDialog = new Avalonia.Controls.Window
                {
                    Title = "Редагувати жанр",
                    Width = 400,
                    Height = 120,
                    Content = new Avalonia.Controls.StackPanel
                    {
                        Margin = new Avalonia.Thickness(10),
                        Children =
                        {
                            new Avalonia.Controls.TextBlock { Text = "Жанр:" },
                            new Avalonia.Controls.TextBox { Name = "GenreBox", Text = mediaFile.Genre ?? "" },
                            new Avalonia.Controls.Button { Name = "OkBtn", Content = "OK", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right }
                        }
                    }
                };

                string? newGenre = mediaFile.Genre;
                if (window != null)
                {
                    var genreBox = ((Avalonia.Controls.StackPanel)genreDialog.Content!).Children[1] as Avalonia.Controls.TextBox;
                    var okBtn = ((Avalonia.Controls.StackPanel)genreDialog.Content!).Children[2] as Avalonia.Controls.Button;
                    okBtn.Click += (_, __) => genreDialog.Close(genreBox.Text);
                    newGenre = await genreDialog.ShowDialog<string>(window);
                }

                // 2. Запит нового року
                var yearDialog = new Avalonia.Controls.Window
                {
                    Title = "Редагувати рік релізу",
                    Width = 400,
                    Height = 120,
                    Content = new Avalonia.Controls.StackPanel
                    {
                        Margin = new Avalonia.Thickness(10),
                        Children =
                        {
                            new Avalonia.Controls.TextBlock { Text = "Рік релізу:" },
                            new Avalonia.Controls.TextBox { Name = "YearBox", Text = mediaFile.ReleaseYear.ToString() },
                            new Avalonia.Controls.Button { Name = "OkBtn", Content = "OK", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right }
                        }
                    }
                };

                int newYear = mediaFile.ReleaseYear;
                if (window != null)
                {
                    var yearBox = ((Avalonia.Controls.StackPanel)yearDialog.Content!).Children[1] as Avalonia.Controls.TextBox;
                    var okBtn = ((Avalonia.Controls.StackPanel)yearDialog.Content!).Children[2] as Avalonia.Controls.Button;
                    okBtn.Click += (_, __) => yearDialog.Close(yearBox.Text);
                    var yearStr = await yearDialog.ShowDialog<string>(window);
                    int.TryParse(yearStr, out newYear);
                }

                // 3. Запит нової обкладинки (URL)
                var coverDialog = new Avalonia.Controls.Window
                {
                    Title = "Редагувати обкладинку (URL)",
                    Width = 400,
                    Height = 120,
                    Content = new Avalonia.Controls.StackPanel
                    {
                        Margin = new Avalonia.Thickness(10),
                        Children =
                        {
                            new Avalonia.Controls.TextBlock { Text = "URL обкладинки:" },
                            new Avalonia.Controls.TextBox { Name = "CoverBox", Text = mediaFile.CoverImagePath ?? "" },
                            new Avalonia.Controls.Button { Name = "OkBtn", Content = "OK", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right }
                        }
                    }
                };

                string? newCover = mediaFile.CoverImagePath;
                if (window != null)
                {
                    var coverBox = ((Avalonia.Controls.StackPanel)coverDialog.Content!).Children[1] as Avalonia.Controls.TextBox;
                    var okBtn = ((Avalonia.Controls.StackPanel)coverDialog.Content!).Children[2] as Avalonia.Controls.Button;
                    okBtn.Click += (_, __) => coverDialog.Close(coverBox.Text);
                    newCover = await coverDialog.ShowDialog<string>(window);
                }

                // Оновлюємо властивості
                mediaFile.Genre = newGenre ?? mediaFile.Genre;
                mediaFile.ReleaseYear = newYear;
                mediaFile.CoverImagePath = newCover ?? mediaFile.CoverImagePath;

                // Зберігаємо зміни у бібліотеці
                var entries = LoadLibraryFromXml(LibraryFilePath);
                var entry = entries.FirstOrDefault(e => e.file_id == mediaFile.FilePath);
                if (entry != null)
                {
                    entry.genre = mediaFile.Genre ?? "";
                    entry.title = mediaFile.Title ?? "";
                    entry.metadata = $"ReleaseYear: {mediaFile.ReleaseYear}, Cover: {mediaFile.CoverImagePath}, Duration: {mediaFile.Duration}";
                }
                SaveLibraryToXml(entries, LibraryFilePath);
                ApplySortAndFilter();
            }
        }

        public void CreatePlaylist(string name)
        {
            if (!string.IsNullOrWhiteSpace(name) && !Playlists.Any(p => p.Name == name))
            {
                Playlists.Add(new Playlist { Name = name, Id = Guid.NewGuid().ToString() });
                SavePlaylists();
                OnPropertyChanged(nameof(Playlists));
            }
        }

        private async void CreatePlaylistDialog()
        {
            var window = Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            if (window != null)
            {
                var dialog = new Avalonia.Controls.Window
                {
                    Title = "Новий плейлист",
                    Width = 400,
                    Height = 120,
                    Content = new Avalonia.Controls.StackPanel
                    {
                        Margin = new Avalonia.Thickness(10),
                        Children =
                        {
                            new Avalonia.Controls.TextBlock { Text = "Назва плейлиста:" },
                            new Avalonia.Controls.TextBox { Name = "NameBox" },
                            new Avalonia.Controls.Button { Name = "OkBtn", Content = "OK", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right }
                        }
                    }
                };

                var nameBox = ((Avalonia.Controls.StackPanel)dialog.Content!).Children[1] as Avalonia.Controls.TextBox;
                var okBtn = ((Avalonia.Controls.StackPanel)dialog.Content!).Children[2] as Avalonia.Controls.Button;
                okBtn.Click += (_, __) => dialog.Close(nameBox.Text);
                var playlistName = await dialog.ShowDialog<string>(window);
                if (!string.IsNullOrWhiteSpace(playlistName))
                {
                    CreatePlaylist(playlistName);
                }
            }
        }

        public void AddSelectedToPlaylist()
        {
            if (SelectedPlaylist != null && SelectedMediaFile != null && !SelectedPlaylist.FileIds.Contains(SelectedMediaFile.FilePath))
            {
                SelectedPlaylist.FileIds.Add(SelectedMediaFile.FilePath);
                SavePlaylists();
                UpdatePlaylistMediaFiles();
                (AddToPlaylistCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (RemoveFromPlaylistCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public void RemoveSelectedFromPlaylist()
        {
            if (SelectedPlaylist != null && SelectedMediaFile != null && SelectedPlaylist.FileIds.Contains(SelectedMediaFile.FilePath))
            {
                SelectedPlaylist.FileIds.Remove(SelectedMediaFile.FilePath);
                SavePlaylists();
                UpdatePlaylistMediaFiles();
                (AddToPlaylistCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (RemoveFromPlaylistCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        private void SavePlaylists()
        {
            try
            {
                var serializer = new XmlSerializer(typeof(List<Playlist>));
                using (var stream = new FileStream(PlaylistsFilePath, FileMode.Create))
                {
                    serializer.Serialize(stream, Playlists.ToList());
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving playlists: {ex.Message}");
            }
        }

        private void LoadPlaylists()
        {
            try
            {
                if (!System.IO.File.Exists(PlaylistsFilePath)) return;
                var serializer = new XmlSerializer(typeof(List<Playlist>));
                using (var stream = new FileStream(PlaylistsFilePath, FileMode.Open))
                {
                    var loaded = (List<Playlist>)serializer.Deserialize(stream);
                    Playlists.Clear();
                    foreach (var p in loaded)
                        Playlists.Add(p);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading playlists: {ex.Message}");
            }
        }

        public class LibraryEntry
        {
            public string? file_id { get; set; }
            public string? title { get; set; }
            public string? genre { get; set; }
            public DateTime added_date { get; set; }
            public string? metadata { get; set; }
        }

        public class Playlist
        {
            public string? Id { get; set; }
            public string? Name { get; set; }
            public List<string>? FileIds { get; set; } = new List<string>();
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
    }

}