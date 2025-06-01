using Avalonia.Controls;
using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Media;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using System.Threading.Tasks;

namespace MediaVault.Views
{
    public partial class StatisticsPageView : UserControl
    {
        public StatisticsPageView()
        {
            InitializeComponent();

            if (!this.Resources.ContainsKey("HoursToHeightConverter"))
                this.Resources.Add("HoursToHeightConverter", new HoursToHeightConverter());
            if (!this.Resources.ContainsKey("GenreToColorConverter"))
                this.Resources.Add("GenreToColorConverter", new GenreToColorConverter());

            if (DataContext is ViewModels.StatisticsPageViewModel vm)
            {
                vm.SaveFileDialogRequested += ShowSaveFileDialogAsync;
            }
            this.DataContextChanged += (_, _) =>
            {
                if (this.DataContext is ViewModels.StatisticsPageViewModel vm2)
                    vm2.SaveFileDialogRequested += ShowSaveFileDialogAsync;
            };
        }

        private async Task<string?> ShowSaveFileDialogAsync(string defaultFileName, string? format, string? _)
        {
            var window = this.VisualRoot as Window;
            if (window == null || window.StorageProvider is null)
                return null;

            var fileTypes = new List<FilePickerFileType>();

            if (string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase))
            {
                fileTypes.Add(new FilePickerFileType("PDF")
                {
                    Patterns = new[] { "*.pdf" },
                    MimeTypes = new[] { "application/pdf" }
                });
            }
            else if (string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase))
            {
                fileTypes.Add(new FilePickerFileType("Excel")
                {
                    Patterns = new[] { "*.xlsx" },
                    MimeTypes = new[] { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" }
                });
            }

            var options = new FilePickerSaveOptions
            {
                SuggestedFileName = defaultFileName,
                FileTypeChoices = fileTypes
            };

            var result = await window.StorageProvider.SaveFilePickerAsync(options);
            return result?.Path.LocalPath;
        }


        public class HoursToHeightConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is double hours)
                    return Math.Max(10, hours * 8);
                if (value is int intHours)
                    return Math.Max(10, intHours * 8);
                return 10;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        public class GenreToColorConverter : IValueConverter
        {
            private static readonly List<Color> Palette = new()
        {
            Colors.SteelBlue,
            Colors.Orange,
            Colors.MediumSeaGreen,
            Colors.MediumPurple,
            Colors.Crimson,
            Colors.Teal,
            Colors.Goldenrod,
            Colors.CadetBlue,
            Colors.SlateBlue,
            Colors.DarkCyan,
            Colors.OliveDrab,
            Colors.Tomato,
            Colors.Gray
        };

            private static readonly Dictionary<string, Color> GenreColors = new(StringComparer.OrdinalIgnoreCase);

            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                var genre = value as string ?? "Інше";
                if (!GenreColors.TryGetValue(genre, out var color))
                {
                    color = Palette[GenreColors.Count % Palette.Count];
                    GenreColors[genre] = color;
                }
                return new SolidColorBrush(color);
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
    }
}
