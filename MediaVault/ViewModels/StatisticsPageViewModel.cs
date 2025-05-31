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
using System.Diagnostics;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Drawing;
using QuestPDF.Previewer;
using System.Threading.Tasks;

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
        public ICommand ExportReportCommand { get; } // Додаємо команду експорту

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

        public ObservableCollection<string> ExportFormats { get; } = new() { "pdf", "excel" };
        private string _selectedExportFormat = "pdf";
        public string SelectedExportFormat
        {
            get => _selectedExportFormat;
            set
            {
                if (_selectedExportFormat != value)
                {
                    _selectedExportFormat = value;
                    OnPropertyChanged(nameof(SelectedExportFormat));
                }
            }
        }

        // Додаємо подію для запиту шляху збереження
        public event Func<string, string, string?, Task<string?>>? SaveFileDialogRequested;

        public StatisticsPageViewModel()
        {
            BackCommand = new RelayCommand(_ => BackToLibraryRequested?.Invoke(this, EventArgs.Empty));
            ExportReportCommand = new RelayCommand(async param => await ExportReportAsync(param));
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

            QuestPDF.Settings.License = LicenseType.Community;
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

        private async Task ExportReportAsync(object? parameter)
        {
            string? format = parameter as string;
            if (string.IsNullOrWhiteSpace(format))
                format = SelectedExportFormat;

            var exportData = GetExportData();

            string defaultFileName = $"MediaVault_Statistics_{PeriodStart:yyyyMMdd}_{PeriodEnd:yyyyMMdd}.{(format == "pdf" ? "pdf" : "xlsx")}";

            string filter = format == "pdf" ? "PDF files (*.pdf)|*.pdf" : "Excel files (*.xlsx;*.csv)|*.xlsx;*.csv";
            string? filePath = SaveFileDialogRequested != null
                ? await SaveFileDialogRequested.Invoke(defaultFileName, filter, format)
                : null;

            if (string.IsNullOrWhiteSpace(filePath))
                return;

            try
            {
                if (format == "pdf")
                {
                    ExportToPdf(filePath, exportData);
                }
                else if (format == "excel")
                {
                    ExportToExcel(filePath, exportData);
                }
                if (File.Exists(filePath))
                    Process.Start(new ProcessStartInfo { FileName = filePath, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Export error: " + ex.Message);
            }
        }

        private class ExportData
        {
            public int SelectedYear { get; set; }
            public List<MonthlyStatistic> Monthly { get; set; } = new();
            public DateTimeOffset? PeriodStart { get; set; }
            public DateTimeOffset? PeriodEnd { get; set; }
            public List<DailyIntervalStatistic> DailyIntervals { get; set; } = new();
            public List<GenreStatistic> Genres { get; set; } = new();
        }

        private ExportData GetExportData()
        {
            return new ExportData
            {
                SelectedYear = SelectedYear,
                Monthly = MonthlyStatistics.ToList(),
                PeriodStart = PeriodStart,
                PeriodEnd = PeriodEnd,
                DailyIntervals = DailyIntervalStatistics.ToList(),
                Genres = GenreStatistics.ToList()
            };
        }

        private void ExportToPdf(string filePath, ExportData data)
        {
            try
            {
                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Margin(30);
                        page.Size(PageSizes.A4);
                        page.DefaultTextStyle(x => x.FontSize(12));
                        page.Header()
                            .Text($"Звіт MediaVault")
                            .SemiBold().FontSize(18).FontColor(Colors.Blue.Medium);
                        page.Content().Column(col =>
                        {
                            col.Spacing(10);

                            col.Item().Text($"Рік: {data.SelectedYear}");

                            // Щомісячний час
                            col.Item().Text("Щомісячний час (годин):").Bold();
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(120);
                                    columns.RelativeColumn();
                                });
                                table.Header(header =>
                                {
                                    header.Cell().Text("Місяць").Bold();
                                    header.Cell().Text("Годин").Bold();
                                });
                                foreach (var m in data.Monthly)
                                {
                                    table.Cell().Text(m.Month);
                                    table.Cell().Text(m.TotalHours.ToString());
                                }
                            });

                            // Добовий розподіл
                            col.Item().Text($"Добовий розподіл (годин) за період {data.PeriodStart:yyyy-MM-dd} — {data.PeriodEnd:yyyy-MM-dd}:").Bold();
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(120);
                                    columns.RelativeColumn();
                                });
                                table.Header(header =>
                                {
                                    header.Cell().Text("Інтервал").Bold();
                                    header.Cell().Text("Годин").Bold();
                                });
                                foreach (var d in data.DailyIntervals)
                                {
                                    table.Cell().Text(d.Interval);
                                    table.Cell().Text(d.TotalHours.ToString());
                                }
                            });

                            // Популярність жанрів
                            col.Item().Text("Популярність жанрів:").Bold();
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(120);
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                });
                                table.Header(header =>
                                {
                                    header.Cell().Text("Жанр").Bold();
                                    header.Cell().Text("Годин").Bold();
                                    header.Cell().Text("Відсоток").Bold();
                                });
                                foreach (var g in data.Genres)
                                {
                                    table.Cell().Text(g.Genre);
                                    table.Cell().Text(g.TotalHours.ToString());
                                    table.Cell().Text($"{g.Percent}%");
                                }
                            });
                        });
                        page.Footer()
                            .AlignCenter()
                            .Text($"MediaVault • {DateTime.Now:yyyy-MM-dd HH:mm}")
                            .FontSize(10).FontColor(Colors.Grey.Medium);
                    });
                });
                document.GeneratePdf(filePath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("QuestPDF export error: " + ex.Message);
                // Можна показати повідомлення користувачу через MessageBox або інший механізм
            }
        }

        private void ExportToExcel(string filePath, ExportData data)
        {
            // TODO: Замінити на реальну генерацію Excel.
            using (var sw = new StreamWriter(filePath, false, System.Text.Encoding.UTF8))
            {
                sw.WriteLine($"Звіт MediaVault");
                sw.WriteLine($"Рік;{data.SelectedYear}");
                sw.WriteLine();
                sw.WriteLine("Щомісячний час (годин):");
                sw.WriteLine("Місяць;Годин");
                foreach (var m in data.Monthly)
                    sw.WriteLine($"{m.Month};{m.TotalHours}");
                sw.WriteLine();
                sw.WriteLine($"Добовий розподіл (годин) за період;{data.PeriodStart:yyyy-MM-dd};{data.PeriodEnd:yyyy-MM-dd}");
                sw.WriteLine("Інтервал;Годин");
                foreach (var d in data.DailyIntervals)
                    sw.WriteLine($"{d.Interval};{d.TotalHours}");
                sw.WriteLine();
                sw.WriteLine("Популярність жанрів:");
                sw.WriteLine("Жанр;Годин;Відсоток");
                foreach (var g in data.Genres)
                    sw.WriteLine($"{g.Genre};{g.TotalHours};{g.Percent}");
            }
        }
    }
}
