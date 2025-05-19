using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using System;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;

namespace MediaVault.Views.Converters
{
    public class UrlToBitmapConverter : IValueConverter
    {
        private static readonly HttpClient httpClient = new HttpClient();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not string url || string.IsNullOrWhiteSpace(url))
            {
                return null;
            }

            return new BitmapLoadingTask(url);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private async Task<Bitmap?> LoadBitmapAsync(string url)
        {
            try
            {
                var response = await httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var stream = await response.Content.ReadAsStreamAsync();
                    return await Task.Run(() => Bitmap.DecodeToWidth(stream, 500)); // Задайте потрібну ширину
                }
            }
            catch
            {
                // Handle exceptions (e.g., logging)
            }

            return null;
        }

        private class BitmapLoadingTask : AvaloniaObject
        {
            public static readonly StyledProperty<Bitmap?> BitmapProperty =
                AvaloniaProperty.Register<BitmapLoadingTask, Bitmap?>(nameof(Bitmap));

            public Bitmap? Bitmap
            {
                get => GetValue(BitmapProperty);
                set => SetValue(BitmapProperty, value);
            }

            public BitmapLoadingTask(string url)
            {
                _ = LoadAsync(url);
            }

            private async Task LoadAsync(string url)
            {
                var bitmap = await new UrlToBitmapConverter().LoadBitmapAsync(url);
                Bitmap = bitmap;
            }
        }
    }
}