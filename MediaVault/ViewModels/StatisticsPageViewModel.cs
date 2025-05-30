using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Globalization;

namespace MediaVault.ViewModels
{
    public class MonthlyStatistic
    {
        public string Month { get; set; }
        public double TotalHours { get; set; }
    }

    public class DailyIntervalStatistic
    {
        public string Interval { get; set; }
        public double TotalHours { get; set; }
    }

    public class GenreStatistic
    {
        public string Genre { get; set; }
        public double Percent { get; set; }
        public double TotalHours { get; set; }
        public string PathData { get; set; } // SVG path for pie sector
    }

    public class StatisticsPageViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler? BackToLibraryRequested;

        public ICommand BackCommand { get; }

        public ObservableCollection<MonthlyStatistic> MonthlyStatistics { get; }
        public ObservableCollection<DailyIntervalStatistic> DailyIntervalStatistics { get; } // нова колекція
        public ObservableCollection<GenreStatistic> GenreStatistics { get; } // нова колекція

        public ObservableCollection<int> AvailableYears { get; } = new();
        private int _selectedYear;
        public int SelectedYear
        {
            get => _selectedYear;
            set
            {
                if (_selectedYear != value)
                {
                    _selectedYear = value;
                    OnPropertyChanged(nameof(SelectedYear));
                    LoadMonthlyStatisticsFromHistory(); // Перерахувати статистику для вибраного року
                }
            }
        }

        private DateTimeOffset? _periodStart;
        public DateTimeOffset? PeriodStart
        {
            get => _periodStart;
            set
            {
                if (_periodStart != value)
                {
                    _periodStart = value;
                    OnPropertyChanged(nameof(PeriodStart));
                    LoadDailyIntervalStatistics();
                }
            }
        }

        private DateTimeOffset? _periodEnd;
        public DateTimeOffset? PeriodEnd
        {
            get => _periodEnd;
            set
            {
                if (_periodEnd != value)
                {
                    _periodEnd = value;
                    OnPropertyChanged(nameof(PeriodEnd));
                    LoadDailyIntervalStatistics();
                }
            }
        }

        private static string GetHistoryFilePath()
        {
            var exeDir = AppContext.BaseDirectory;
            var dataPath = Path.Combine(exeDir, "Data", "history.xml");
            return dataPath;
        }

        private Dictionary<string, string> fileIdToGenre = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private static string GetLibraryFilePath()
        {
            var exeDir = AppContext.BaseDirectory;
            var dataPath = Path.Combine(exeDir, "Data", "library.xml");
            return dataPath;
        }

        private List<dynamic> allRecords = new(); // Зберігаємо всі записи для фільтрації

        public StatisticsPageViewModel()
        {
            BackCommand = new RelayCommand(_ => BackToLibraryRequested?.Invoke(this, EventArgs.Empty));
            MonthlyStatistics = new ObservableCollection<MonthlyStatistic>();
            DailyIntervalStatistics = new ObservableCollection<DailyIntervalStatistic>();
            GenreStatistics = new ObservableCollection<GenreStatistic>();
            LoadLibraryGenres();
            LoadAllRecordsFromHistory();
            if (AvailableYears.Count > 0)
                SelectedYear = AvailableYears.Max();
            else
                SelectedYear = DateTime.Now.Year;

            // Встановлюємо період за замовчуванням на весь рік
            PeriodStart = new DateTimeOffset(new DateTime(SelectedYear, 1, 1));
            PeriodEnd = new DateTimeOffset(new DateTime(SelectedYear, 12, 31));

            LoadMonthlyStatisticsFromHistory();
            LoadDailyIntervalStatistics();
        }

        private void LoadLibraryGenres()
        {
            fileIdToGenre.Clear();
            string libraryFilePath = GetLibraryFilePath();
            if (!File.Exists(libraryFilePath))
                return;

            try
            {
                var doc = XDocument.Load(libraryFilePath);
                foreach (var entry in doc.Descendants("LibraryEntry"))
                {
                    var fileId = entry.Element("file_id")?.Value ?? "";
                    var genre = entry.Element("genre")?.Value ?? "";
                    if (!string.IsNullOrWhiteSpace(fileId))
                        fileIdToGenre[fileId] = string.IsNullOrWhiteSpace(genre) ? "Інше" : genre;
                }
            }
            catch
            {
                // ignore errors
            }
        }

        private void LoadAllRecordsFromHistory()
        {
            string historyFilePath = GetHistoryFilePath();
            allRecords.Clear();
            AvailableYears.Clear();

            if (!File.Exists(historyFilePath))
                return;

            try
            {
                var doc = XDocument.Load(historyFilePath);
                var records = doc.Descendants("Record")
                    .Select(r => new
                    {
                        Date = DateTime.Parse(r.Element("ViewDate")?.Value ?? DateTime.MinValue.ToString()),
                        DurationSeconds = double.TryParse(r.Element("Duration")?.Value, out var d) ? d : 0,
                        FileId = r.Element("FileId")?.Value ?? "",
                        FileName = r.Element("FileName")?.Value ?? ""
                    })
                    .ToList();

                foreach (var y in records.Select(r => r.Date.Year).Distinct().OrderBy(y => y))
                    AvailableYears.Add(y);

                allRecords.AddRange(records);
            }
            catch
            {
                // ignore
            }
        }

        private void LoadMonthlyStatisticsFromHistory()
        {
            // Замість читання з файлу, використовуємо allRecords і SelectedYear
            var records = allRecords.Where(r => r.Date.Year == SelectedYear).ToList();

            MonthlyStatistics.Clear();
            for (int month = 1; month <= 12; month++)
            {
                var monthRecords = records.Where(r => r.Date.Month == month);
                double totalHours = Math.Round(monthRecords.Sum(x => (double)x.DurationSeconds) / 3600.0, 1);
                MonthlyStatistics.Add(new MonthlyStatistic
                {
                    Month = $"{GetMonthShortName(month)} {SelectedYear}",
                    TotalHours = totalHours
                });
            }

            // Оновлюємо період, якщо змінився рік
            PeriodStart = new DateTimeOffset(new DateTime(SelectedYear, 1, 1));
            PeriodEnd = new DateTimeOffset(new DateTime(SelectedYear, 12, 31));

            // Підрахунок жанрів через fileIdToGenre
            var genreStats = records
                .GroupBy(r =>
                {
                    if (fileIdToGenre.TryGetValue(r.FileId, out string genre) && !string.IsNullOrWhiteSpace(genre))
                        return genre;
                    return "Інше";
                })
                .Select(g => new
                {
                    Genre = g.Key,
                    TotalSeconds = g.Sum(x => (double)x.DurationSeconds)
                })
                .ToList();

            double totalSecondsAll = genreStats.Sum(g => (double)g.TotalSeconds);
            GenreStatistics.Clear();

            double startAngle = 0;
            double centerX = 80, centerY = 80, radius = 75;
            foreach (var g in genreStats.OrderByDescending(x => x.TotalSeconds))
            {
                double percent = totalSecondsAll > 0 ? Math.Round(g.TotalSeconds * 100.0 / totalSecondsAll, 1) : 0;
                double sweep = totalSecondsAll > 0 ? 360.0 * g.TotalSeconds / totalSecondsAll : 0;
                string pathData = CreatePieSlicePath(centerX, centerY, radius, startAngle, sweep);
                GenreStatistics.Add(new GenreStatistic
                {
                    Genre = g.Genre,
                    Percent = percent,
                    TotalHours = Math.Round(g.TotalSeconds / 3600.0, 2),
                    PathData = pathData
                });
                startAngle += sweep;
            }
        }

        private void LoadDailyIntervalStatistics()
        {
            // Перевірка на null
            if (PeriodStart == null || PeriodEnd == null)
            {
                DailyIntervalStatistics.Clear();
                return;
            }
            var start = PeriodStart.Value.Date;
            var end = PeriodEnd.Value.Date.AddDays(1).AddTicks(-1); // включно до кінця дня

            var records = allRecords
                .Where(r => r.Date >= start && r.Date <= end)
                .ToList();

            // Групування по 3 години (8 інтервалів)
            var intervalCount = 8;
            var intervals = Enumerable.Range(0, intervalCount)
                .Select(i => new
                {
                    Index = i,
                    Label = $"{i * 3:00}:00-{(i + 1) * 3:00}:00"
                })
                .ToList();

            var dailyGrouped = records
                .GroupBy(r => r.Date.Hour / 3)
                .OrderBy(g => g.Key)
                .Select(g => new DailyIntervalStatistic
                {
                    Interval = intervals[g.Key].Label,
                    TotalHours = Math.Round(g.Sum(x => (double)x.DurationSeconds) / 3600.0, 2)
                })
                .ToList();

            DailyIntervalStatistics.Clear();
            foreach (var interval in intervals)
            {
                var found = dailyGrouped.FirstOrDefault(x => x.Interval == interval.Label);
                DailyIntervalStatistics.Add(new DailyIntervalStatistic
                {
                    Interval = interval.Label,
                    TotalHours = found?.TotalHours ?? 0
                });
            }
        }

        private string GetMonthShortName(int month)
        {
            string[] ukrMonths = { "Січ", "Лют", "Бер", "Кві", "Тра", "Чер", "Лип", "Сер", "Вер", "Жов", "Лис", "Гру" };
            if (month >= 1 && month <= 12)
                return ukrMonths[month - 1];
            return month.ToString();
        }

        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        // SVG path for pie sector
        private static string CreatePieSlicePath(double cx, double cy, double r, double startAngle, double sweepAngle)
        {
            if (sweepAngle <= 0)
                return "";

            var fmt = CultureInfo.InvariantCulture;

            if (Math.Abs(sweepAngle - 360.0) < 0.01)
            {
                // Малюємо повне коло як два півкола
                double startRad = Math.PI * startAngle / 180.0;
                double midAngle = startAngle + 180.0;
                double midRad = Math.PI * midAngle / 180.0;

                double x1 = cx + r * Math.Cos(startRad);
                double y1 = cy + r * Math.Sin(startRad);
                double x2 = cx + r * Math.Cos(midRad);
                double y2 = cy + r * Math.Sin(midRad);

                return
                    $"M {cx.ToString(fmt)},{cy.ToString(fmt)} " +
                    $"L {x1.ToString(fmt)},{y1.ToString(fmt)} " +
                    $"A {r.ToString(fmt)},{r.ToString(fmt)} 0 1 1 {x2.ToString(fmt)},{y2.ToString(fmt)} " +
                    $"A {r.ToString(fmt)},{r.ToString(fmt)} 0 1 1 {x1.ToString(fmt)},{y1.ToString(fmt)} Z";
            }

            double startRad2 = Math.PI * startAngle / 180.0;
            double endRad = Math.PI * (startAngle + sweepAngle) / 180.0;

            double x1b = cx + r * Math.Cos(startRad2);
            double y1b = cy + r * Math.Sin(startRad2);
            double x2b = cx + r * Math.Cos(endRad);
            double y2b = cy + r * Math.Sin(endRad);

            int largeArc = sweepAngle > 180 ? 1 : 0;

            return $"M {cx.ToString(fmt)},{cy.ToString(fmt)} L {x1b.ToString(fmt)},{y1b.ToString(fmt)} A {r.ToString(fmt)},{r.ToString(fmt)} 0 {largeArc} 1 {x2b.ToString(fmt)},{y2b.ToString(fmt)} Z";
        }
    }
}
