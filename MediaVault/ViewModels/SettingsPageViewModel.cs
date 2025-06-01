using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using System.Xml.Serialization;
using MediaVault.Models;

namespace MediaVault.ViewModels
{
    public class SettingsPageViewModel : ViewModelBase
    {
        public ICommand BackCommand { get; }
        public ICommand SelectMediaFolderCommand { get; }
        public ICommand ExportConfigCommand { get; }
        public ICommand ImportConfigCommand { get; }
        public event EventHandler? BackToLibraryRequested;
        public event EventHandler<string>? MediaFolderPathChanged;

        private static readonly string DataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
        private static readonly string ConfigFilePath = Path.Combine(DataDirectory, "config.xml");

        private ConfigModel _config = new ConfigModel();

        private readonly List<string> _themes = new List<string> { "Default" };
        public IEnumerable<string> Themes => _themes;

        private readonly List<string> _languages = new List<string> { "Українська" };
        public IEnumerable<string> Languages => _languages;

        public string Theme
        {
            get => _config.Theme;
            set
            {
                if (_config.Theme != value)
                {
                    _config.Theme = value;
                    OnPropertyChanged(nameof(Theme));
                    SaveConfig();
                }
            }
        }

        public string Language
        {
            get => _config.Language;
            set
            {
                if (_config.Language != value)
                {
                    _config.Language = value;
                    OnPropertyChanged(nameof(Language));
                    SaveConfig();
                }
            }
        }

        public string MediaFolderPath
        {
            get => _config.MediaFolderPath;
            set
            {
                if (_config.MediaFolderPath != value)
                {
                    _config.MediaFolderPath = value;
                    OnPropertyChanged(nameof(MediaFolderPath));
                    SaveConfig();
                    MediaFolderPathChanged?.Invoke(this, value);
                }
            }
        }

        public void UpdateMediaFolderPath(string newPath)
        {
            if (MediaFolderPath != newPath)
                MediaFolderPath = newPath;
        }

        public SettingsPageViewModel()
        {
            BackCommand = new RelayCommand(_ => BackToLibraryRequested?.Invoke(this, EventArgs.Empty));
            SelectMediaFolderCommand = new RelayCommand(async _ => await OnSelectMediaFolder());
            ExportConfigCommand = new RelayCommand(async _ => await OnExportConfig());
            ImportConfigCommand = new RelayCommand(async _ => await OnImportConfig());
            LoadConfig();
        }

        private void LoadConfig()
        {
            try
            {
                if (!Directory.Exists(DataDirectory))
                    Directory.CreateDirectory(DataDirectory);

                if (File.Exists(ConfigFilePath))
                {
                    using var stream = File.OpenRead(ConfigFilePath);
                    var serializer = new XmlSerializer(typeof(ConfigModel));
                    var deserialized = serializer.Deserialize(stream) as ConfigModel;
                    if (deserialized != null)
                        _config = deserialized;
                    else
                        _config = new ConfigModel();
                }
                else
                {
                    _config = new ConfigModel();
                    SaveConfig();
                }
            }
            catch
            {
                _config = new ConfigModel();
            }

            if (string.IsNullOrWhiteSpace(_config.Theme))
                _config.Theme = _themes[0];
            if (string.IsNullOrWhiteSpace(_config.Language))
                _config.Language = _languages[0];

            OnPropertyChanged(nameof(Theme));
            OnPropertyChanged(nameof(Language));
            OnPropertyChanged(nameof(MediaFolderPath));
        }

        private void SaveConfig()
        {
            try
            {
                if (!Directory.Exists(DataDirectory))
                    Directory.CreateDirectory(DataDirectory);

                using var stream = File.Create(ConfigFilePath);
                var serializer = new XmlSerializer(typeof(ConfigModel));
                serializer.Serialize(stream, _config);
            }
            catch
            {
                Debug.WriteLine("Помилка при збереженні конфігурації");
            }
        }

        private async System.Threading.Tasks.Task OnSelectMediaFolder()
        {
            var lifetime = Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
            var mainWindow = lifetime?.MainWindow;
            if (mainWindow == null)
                return;

            var storageProvider = mainWindow.StorageProvider;
            if (storageProvider == null)
                return;

            var folders = await storageProvider.OpenFolderPickerAsync(new Avalonia.Platform.Storage.FolderPickerOpenOptions
            {
                Title = "Оберіть папку для медіа",
                AllowMultiple = false
            });

            if (folders != null && folders.Count > 0)
            {
                MediaFolderPath = folders[0].Path.LocalPath;
            }
        }

        private async System.Threading.Tasks.Task OnExportConfig()
        {
            var lifetime = Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
            var mainWindow = lifetime?.MainWindow;
            if (mainWindow == null)
                return;

            var storageProvider = mainWindow.StorageProvider;
            if (storageProvider == null)
                return;

            var file = await storageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = "Експортувати налаштування",
                SuggestedFileName = "config.xml",
                FileTypeChoices = new List<Avalonia.Platform.Storage.FilePickerFileType>
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("XML файли")
                    {
                        Patterns = new[] { "*.xml" }
                    }
                }
            });

            if (file != null)
            {
                try
                {
                    await using var stream = await file.OpenWriteAsync();
                    var serializer = new XmlSerializer(typeof(ConfigModel));
                    serializer.Serialize(stream, _config);
                }
                catch
                {
                    Debug.WriteLine("Помилка при експорті конфігурації");
                }
            }
        }

        private async System.Threading.Tasks.Task OnImportConfig()
        {
            var lifetime = Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
            var mainWindow = lifetime?.MainWindow;
            if (mainWindow == null)
                return;

            var storageProvider = mainWindow.StorageProvider;
            if (storageProvider == null)
                return;

            var files = await storageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                Title = "Імпортувати налаштування",
                AllowMultiple = false,
                FileTypeFilter = new List<Avalonia.Platform.Storage.FilePickerFileType>
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("XML файли")
                    {
                        Patterns = ["*.xml"]
                    }
                }
            });

            if (files != null && files.Count > 0)
            {
                try
                {
                    await using var stream = await files[0].OpenReadAsync();
                    var serializer = new XmlSerializer(typeof(ConfigModel));
                    var imported = (ConfigModel?)serializer.Deserialize(stream);
                    if (imported != null)
                    {
                        _config = imported;
                        SaveConfig();
                        OnPropertyChanged(nameof(Theme));
                        OnPropertyChanged(nameof(Language));
                        OnPropertyChanged(nameof(MediaFolderPath));
                    }
                }
                catch
                {
                    Debug.WriteLine("Помилка при імпорті конфігурації");
                }
            }
        }
    }
}
