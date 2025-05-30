using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;
using System.Xml.Serialization;
using MediaVault.Models;

namespace MediaVault.ViewModels
{
    public class SettingsPageViewModel : INotifyPropertyChanged
    {
        public ICommand BackCommand { get; }
        public ICommand SelectMediaFolderCommand { get; }
        public event EventHandler? BackToLibraryRequested;

        private static readonly string DataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
        private static readonly string ConfigFilePath = Path.Combine(DataDirectory, "config.xml");

        private ConfigModel _config = new ConfigModel();

        private List<string> _themes = new List<string> { "Default" };
        public IEnumerable<string> Themes => _themes;

        private List<string> _languages = new List<string> { "Українська" };
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
                }
            }
        }

        // Додаємо метод для зовнішнього оновлення шляху
        public void UpdateMediaFolderPath(string newPath)
        {
            if (MediaFolderPath != newPath)
                MediaFolderPath = newPath;
        }

        public SettingsPageViewModel()
        {
            BackCommand = new RelayCommand(_ => BackToLibraryRequested?.Invoke(this, EventArgs.Empty));
            SelectMediaFolderCommand = new RelayCommand(async _ => await OnSelectMediaFolder());
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
                    _config = (ConfigModel)serializer.Deserialize(stream) ?? new ConfigModel();
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

            // Ensure Theme and Language are set to available values if empty
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
                // ignore errors
            }
        }

        private async System.Threading.Tasks.Task OnSelectMediaFolder()
        {
            // Avalonia-specific dialog, works only if called from UI thread and with ApplicationLifetime
            var lifetime = Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
            var mainWindow = lifetime?.MainWindow;
            if (mainWindow == null)
                return;

            var dialog = new Avalonia.Controls.OpenFolderDialog
            {
                Title = "Оберіть папку для медіа"
            };
            var folder = await dialog.ShowAsync(mainWindow);
            if (!string.IsNullOrEmpty(folder))
            {
                MediaFolderPath = folder;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
