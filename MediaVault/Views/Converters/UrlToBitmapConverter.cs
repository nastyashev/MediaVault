using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace MediaVault.Views.Converters
{
    public class UrlToBitmapConverter : IValueConverter
    {
        private static readonly Bitmap Placeholder = null;
        private static readonly ConcurrentDictionary<string, Bitmap> Cache = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string path || string.IsNullOrEmpty(path))
                return Placeholder;

            if (Cache.TryGetValue(path, out var cached))
                return cached;

            try
            {
                if (File.Exists(path))
                {
                    var bitmap = new Bitmap(path);
                    Cache[path] = bitmap;
                    return bitmap;
                }
                else if (path.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    // Синхронно чекаємо на завантаження (НЕ рекомендується для великих картинок, але для preview підійде)
                    using var client = new HttpClient();
                    var bytes = client.GetByteArrayAsync(path).GetAwaiter().GetResult();
                    using var ms = new MemoryStream(bytes);
                    var bitmap = new Bitmap(ms);
                    Cache[path] = bitmap;
                    return bitmap;
                }
            }
            catch
            {
                // ignore errors
            }

            return Placeholder;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}