using Avalonia.Controls;
using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using System.Threading.Tasks; // Ensure Task is imported

namespace MediaVault.Views
{
    public partial class StatisticsPageView : UserControl
    {
        public StatisticsPageView()
        {
            InitializeComponent();

            // Додаємо конвертер у ресурси, якщо потрібно
            if (!this.Resources.ContainsKey("HoursToHeightConverter"))
                this.Resources.Add("HoursToHeightConverter", new HoursToHeightConverter());
            if (!this.Resources.ContainsKey("GenreToColorConverter"))
                this.Resources.Add("GenreToColorConverter", new GenreToColorConverter());

            // Підписка на подію для діалогу збереження (асинхронна версія)
            if (DataContext is MediaVault.ViewModels.StatisticsPageViewModel vm)
            {
                vm.SaveFileDialogRequested += ShowSaveFileDialogAsync;
            }
            this.DataContextChanged += (_, _) =>
            {
                if (this.DataContext is MediaVault.ViewModels.StatisticsPageViewModel vm2)
                    vm2.SaveFileDialogRequested += ShowSaveFileDialogAsync;
            };
        }

        private void ExportButton_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && sender is Button btn && btn.ContextMenu != null)
            {
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.Open();
                e.Handled = true;
            }
        }

        private async Task<string?> ShowSaveFileDialogAsync(string defaultFileName, string? format, string? _)
        {
            var window = this.VisualRoot as Window;
            if (window == null || window.StorageProvider is null)
                return null;

            var fileTypes = new List<FilePickerFileType>
            {
                new FilePickerFileType("PDF")
                {
                    Patterns = ["*.pdf"],
                    MimeTypes = ["application/pdf"]
                }
            };


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
                // Масштабуємо: 1 година = 8 пікселів, мінімум 10 пікселів
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
                // Повертаємо SolidColorBrush замість Color
                return new SolidColorBrush(color);
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
    }
}
