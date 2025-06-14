using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Threading.Tasks;
using OfficeOpenXml;
using DynamicData;
using MediaVault.Models;

namespace MediaVault.ViewModels
{
    public class MonthlyStatistic
    {
        public string? Month { get; set; }
        public double TotalHours { get; set; }
    }

    public class DailyIntervalStatistic
    {
        public string? Interval { get; set; }
        public double TotalHours { get; set; }
    }

    public class GenreStatistic
    {
        public string? Genre { get; set; }
        public double Percent { get; set; }
        public double TotalHours { get; set; }
        public string? PathData { get; set; }
    }

    public class StatisticsPageViewModel : ViewModelBase
    {
        public event EventHandler? BackToLibraryRequested;

        public ICommand BackCommand { get; }
        public ICommand ExportReportCommand { get; }

        public ObservableCollection<MonthlyStatistic> MonthlyStatistics { get; }
        public ObservableCollection<DailyIntervalStatistic> DailyIntervalStatistics { get; }
        public ObservableCollection<GenreStatistic> GenreStatistics { get; }

        public ObservableCollection<int> AvailableYears { get; } = new();
        private int _selectedYear;
        public int SelectedYear
        {
            get => _selectedYear;
            set
            {
                if (SetProperty(ref _selectedYear, value))
                {
                    LoadMonthlyStatisticsFromHistory();
                }
            }
        }

        private DateTimeOffset? _periodStart;
        public DateTimeOffset? PeriodStart
        {
            get => _periodStart;
            set
            {
                if (SetProperty(ref _periodStart, value))
                {
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
                if (SetProperty(ref _periodEnd, value))
                {
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

        private readonly Dictionary<string, string> fileIdToGenre = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private readonly List<ViewingHistoryRecord> allRecords = new();

        public ObservableCollection<string> ExportFormats { get; } = new() { "pdf", "excel" };
        private string _selectedExportFormat = "pdf";
        public string SelectedExportFormat
        {
            get => _selectedExportFormat;
            set => SetProperty(ref _selectedExportFormat, value);
        }

        public event Func<string, string, string?, Task<string?>>? SaveFileDialogRequested;

        public StatisticsPageViewModel()
        {
            BackCommand = new RelayCommand(_ => BackToLibraryRequested?.Invoke(this, EventArgs.Empty));
            ExportReportCommand = new RelayCommand(async param => await ExportReportAsync(param));
            MonthlyStatistics = new ObservableCollection<MonthlyStatistic>();
            DailyIntervalStatistics = new ObservableCollection<DailyIntervalStatistic>();
            GenreStatistics = new ObservableCollection<GenreStatistic>();
            LoadAllRecordsFromHistory();
            if (AvailableYears.Count > 0)
                SelectedYear = AvailableYears.Max();
            else
                SelectedYear = DateTime.Now.Year;

            PeriodStart = new DateTimeOffset(new DateTime(SelectedYear, 1, 1, 0, 0, 0, DateTimeKind.Local));
            PeriodEnd = new DateTimeOffset(new DateTime(SelectedYear, 12, 31, 0, 0, 0, DateTimeKind.Local));

            LoadMonthlyStatisticsFromHistory();
            LoadDailyIntervalStatistics();

            QuestPDF.Settings.License = LicenseType.Community;
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
                    .Select(r => new ViewingHistoryRecord
                    {
                        RecordId = int.TryParse(r.Element("RecordId")?.Value, out var id) ? id : 0,
                        FileId = r.Element("FileId")?.Value ?? "",
                        FileName = r.Element("FileName")?.Value ?? "",
                        ViewDate = DateTime.TryParse(r.Element("ViewDate")?.Value, out var dt) ? dt : DateTime.MinValue,
                        Duration = int.TryParse(r.Element("Duration")?.Value, out var dur) ? dur : 0,
                        EndTime = int.TryParse(r.Element("EndTime")?.Value, out var et) ? et : 0,
                        Status = r.Element("Status")?.Value ?? "",
                        Genre = r.Element("Genre")?.Value
                            ?? (fileIdToGenre.TryGetValue(r.Element("FileId")?.Value ?? "", out var genre) ? genre : "Інше")
                    })
                    .ToList();

                foreach (var y in records.Select(r => r.ViewDate.Year).Distinct().OrderBy(y => y))
                    AvailableYears.Add(y);

                allRecords.AddRange(records);
            }
            catch
            {
                Debug.WriteLine("Error loading history records from XML.");
            }
        }

        private void LoadMonthlyStatisticsFromHistory()
        {
            var records = allRecords.Where(r => r.ViewDate.Year == SelectedYear).ToList();

            MonthlyStatistics.Clear();
            for (int month = 1; month <= 12; month++)
            {
                var monthRecords = records.Where(r => r.ViewDate.Month == month);
                double totalHours = Math.Round(monthRecords.Sum(x => x.Duration) / 3600.0, 1);
                MonthlyStatistics.Add(new MonthlyStatistic
                {
                    Month = $"{GetMonthShortName(month)} {SelectedYear}",
                    TotalHours = totalHours
                });
            }

            PeriodStart = new DateTimeOffset(new DateTime(SelectedYear, 1, 1, 0, 0, 0, DateTimeKind.Local));
            PeriodEnd = new DateTimeOffset(new DateTime(SelectedYear, 12, 31, 0, 0, 0, DateTimeKind.Local));

            var genreStats = records
                .SelectMany(r =>
                {
                    var genres = (r.Genre ?? "Інше")
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(g => g.Trim())
                        .Where(g => !string.IsNullOrWhiteSpace(g))
                        .DefaultIfEmpty("Інше")
                        .ToList();

                    double durationPerGenre = genres.Count > 0 ? (double)r.Duration / genres.Count : 0;

                    return genres.Select(g => new { Genre = g, DurationSeconds = durationPerGenre });
                })
                .GroupBy(x => string.IsNullOrWhiteSpace(x.Genre) ? "Інше" : x.Genre)
                .Select(g => new
                {
                    Genre = g.Key,
                    TotalSeconds = g.Sum(x => x.DurationSeconds)
                })
                .ToList();

            double totalSecondsAll = genreStats.Sum(g => g.TotalSeconds);
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
            if (PeriodStart == null || PeriodEnd == null)
            {
                DailyIntervalStatistics.Clear();
                return;
            }
            var start = PeriodStart.Value.Date;
            var end = PeriodEnd.Value.Date.AddDays(1).AddTicks(-1);

            var records = allRecords
                .Where(r => r.ViewDate >= start && r.ViewDate <= end)
                .ToList();

            var intervalCount = 8;
            var intervals = Enumerable.Range(0, intervalCount)
                .Select(i => new
                {
                    Index = i,
                    Label = $"{i * 3:00}:00-{(i + 1) * 3:00}:00"
                })
                .ToList();

            var dailyGrouped = records
                .GroupBy(r => r.ViewDate.Hour / 3)
                .OrderBy(g => g.Key)
                .Select(g => new DailyIntervalStatistic
                {
                    Interval = intervals[g.Key].Label,
                    TotalHours = Math.Round(g.Sum(x => (double)x.Duration) / 3600.0, 2)
                })
                .ToList();

            DailyIntervalStatistics.Clear();
            DailyIntervalStatistics.AddRange(
                intervals.Select(interval =>
                {
                    var found = dailyGrouped.FirstOrDefault(x => x.Interval == interval.Label);
                    return new DailyIntervalStatistic
                    {
                        Interval = interval.Label,
                        TotalHours = found?.TotalHours ?? 0
                    };
                })
            );
        }

        private static string GetMonthShortName(int month)
        {
            string[] ukrMonths = { "Січ", "Лют", "Бер", "Кві", "Тра", "Чер", "Лип", "Сер", "Вер", "Жов", "Лис", "Гру" };
            if (month >= 1 && month <= 12)
                return ukrMonths[month - 1];
            return month.ToString();
        }

        private static string CreatePieSlicePath(double cx, double cy, double r, double startAngle, double sweepAngle)
        {
            if (sweepAngle <= 0)
                return "";

            var fmt = CultureInfo.InvariantCulture;

            if (Math.Abs(sweepAngle - 360.0) < 0.01)
            {
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
                if (format == "excel")
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

        private sealed class ExportData
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

        private static void ExportToPdf(string filePath, ExportData data)
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

                            col.Item().Text("Щомісячний час (годин):").Bold();
                            col.Item().Text($"Діаграма: сумарний час перегляду по місяцях за {data.SelectedYear} рік.").FontSize(10).FontColor(Colors.Grey.Darken2);
                            col.Item().Element(container =>
                                container.Height(140).Svg(GenerateMonthlyBarChartSvg(data.Monthly))
                            );

                            col.Item().Text("Добовий розподіл часу (годин):").Bold();
                            col.Item().Text(
                                $"Діаграма: сумарний час перегляду по 3-годинних інтервалах доби за період " +
                                $"{data.PeriodStart:yyyy-MM-dd} — {data.PeriodEnd:yyyy-MM-dd}."
                            ).FontSize(10).FontColor(Colors.Grey.Darken2);
                            col.Item().Element(container =>
                                container.Height(140).Svg(GenerateDailyIntervalBarChartSvg(data.DailyIntervals))
                            );

                            col.Item().Text("Популярність жанрів:").Bold();
                            col.Item().Text($"Діаграма: розподіл часу перегляду за жанрами за {data.SelectedYear} рік.").FontSize(10).FontColor(Colors.Grey.Darken2);
                            col.Item().Element(container =>
                                container.Height(170).Svg(GenerateGenrePieChartSvg(data.Genres))
                            );
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
            }
        }

        private static string GenerateMonthlyBarChartSvg(List<MonthlyStatistic> monthly)
        {
            int width = 400, height = 120;
            int leftPad = 40, bottomPad = 30, topPad = 10;
            int barWidth = 20, spacing = 10;
            int chartHeight = height - bottomPad - topPad;
            int chartWidth = monthly.Count * (barWidth + spacing);
            double max = monthly.Max(x => x.TotalHours);
            if (max < 1) max = 1;

            var svg = new System.Text.StringBuilder();
            svg.AppendLine($"<svg width='{width}' height='{height}' xmlns='http://www.w3.org/2000/svg'>");
            svg.AppendLine($"<line x1='{leftPad}' y1='{height - bottomPad}' x2='{leftPad + chartWidth}' y2='{height - bottomPad}' stroke='gray' stroke-width='1'/>");
            svg.AppendLine($"<line x1='{leftPad}' y1='{height - bottomPad}' x2='{leftPad}' y2='{topPad}' stroke='gray' stroke-width='1'/>");

            for (int i = 0; i < monthly.Count; i++)
            {
                var m = monthly[i];
                double barH = m.TotalHours / max * chartHeight;
                double x = leftPad + i * (barWidth + spacing);
                double y = height - bottomPad - barH;
                svg.AppendLine($"<rect x='{x}' y='{y}' width='{barWidth}' height='{barH}' fill='#1976d2'/>");
                var monthLabel = m.Month != null && m.Month.Length >= 3 ? m.Month.Substring(0, 3) : "";
                svg.AppendLine($"<text x='{x + barWidth / 2}' y='{height - bottomPad + 12}' font-size='8' text-anchor='middle'>{monthLabel}</text>");
                svg.AppendLine($"<text x='{x + barWidth / 2}' y='{y - 2}' font-size='8' text-anchor='middle'>{m.TotalHours}</text>");
            }
            svg.AppendLine("</svg>");
            return svg.ToString();
        }

        private static string GenerateDailyIntervalBarChartSvg(List<DailyIntervalStatistic> intervals)
        {
            int width = 400, height = 120;
            int leftPad = 40, bottomPad = 30, topPad = 10;
            int barWidth = 20, spacing = 10;
            int chartHeight = height - bottomPad - topPad;
            int chartWidth = intervals.Count * (barWidth + spacing);
            double max = intervals.Max(x => x.TotalHours);
            if (max < 1) max = 1;

            var svg = new System.Text.StringBuilder();
            svg.AppendLine($"<svg width='{width}' height='{height}' xmlns='http://www.w3.org/2000/svg'>");
            svg.AppendLine($"<line x1='{leftPad}' y1='{height - bottomPad}' x2='{leftPad + chartWidth}' y2='{height - bottomPad}' stroke='gray' stroke-width='1'/>");
            svg.AppendLine($"<line x1='{leftPad}' y1='{height - bottomPad}' x2='{leftPad}' y2='{topPad}' stroke='gray' stroke-width='1'/>");
            for (int i = 0; i < intervals.Count; i++)
            {
                var d = intervals[i];
                double barH = d.TotalHours / max * chartHeight;
                double x = leftPad + i * (barWidth + spacing);
                double y = height - bottomPad - barH;
                svg.AppendLine($"<rect x='{x}' y='{y}' width='{barWidth}' height='{barH}' fill='#ff9800'/>");
                var intervalLabel = d.Interval != null && d.Interval.Length >= 5 ? d.Interval.Substring(0, 5) : "";
                svg.AppendLine($"<text x='{x + barWidth / 2}' y='{height - bottomPad + 12}' font-size='7' text-anchor='middle'>{intervalLabel}</text>");
                svg.AppendLine($"<text x='{x + barWidth / 2}' y='{y - 2}' font-size='8' text-anchor='middle'>{d.TotalHours}</text>");
            }
            svg.AppendLine("</svg>");
            return svg.ToString();
        }

        private static string GenerateGenrePieChartSvg(List<GenreStatistic> genres)
        {
            int width = 340, height = 170;
            double cx = 80, cy = 80, radius = 60;
            double total = genres.Sum(g => g.TotalHours);
            if (total <= 0) total = 1;
            string[] palette = {
                "#1976d2", "#ff9800", "#43a047", "#8e24aa", "#e53935", "#00897b", "#fbc02d", "#6d4c41",
                "#00bcd4", "#d81b60", "#cddc39", "#3949ab", "#757575"
            };

            var svg = new System.Text.StringBuilder();
            svg.AppendLine($"<svg width='{width}' height='{height}' xmlns='http://www.w3.org/2000/svg'>");

            double startAngle = 0;
            int colorIdx = 0;
            var nonZeroGenres = genres.Where(g => g.TotalHours > 0).ToList();
            if (nonZeroGenres.Count == 1)
            {
                var g = nonZeroGenres[0];
                svg.AppendLine($"<circle cx='{cx.ToString(CultureInfo.InvariantCulture)}' cy='{cy.ToString(CultureInfo.InvariantCulture)}' r='{radius.ToString(CultureInfo.InvariantCulture)}' fill='{palette[0]}' stroke='#444' stroke-width='1'/>");
            }
            else
            {
                foreach (var g in genres)
                {
                    double sweep = g.TotalHours / total * 360.0;
                    if (sweep <= 0.1) continue;
                    double endAngle = startAngle + sweep;
                    string path = DescribeArc(cx, cy, radius, startAngle, endAngle);
                    svg.AppendLine($"<path d='{path}' fill='{palette[colorIdx % palette.Length]}'/>");
                    startAngle += sweep;
                    colorIdx++;
                }
            }
            svg.AppendLine($"<circle cx='{cx.ToString(CultureInfo.InvariantCulture)}' cy='{cy.ToString(CultureInfo.InvariantCulture)}' r='{radius.ToString(CultureInfo.InvariantCulture)}' fill='none' stroke='#888' stroke-width='1'/>");

            int legendX = 170, legendY = 25, legendRectSize = 14, legendSpacing = 22;
            colorIdx = 0;
            foreach (var g in genres.Where(x => x.TotalHours > 0))
            {
                int y = legendY + colorIdx * legendSpacing;
                svg.AppendLine($"<rect x='{legendX}' y='{y}' width='{legendRectSize}' height='{legendRectSize}' fill='{palette[colorIdx % palette.Length]}'/>");
                svg.AppendLine($"<text x='{legendX + legendRectSize + 6}' y='{y + legendRectSize - 3}' font-size='11' font-family='Arial'>{g.Genre} ({g.Percent}%)</text>");
                colorIdx++;
            }

            svg.AppendLine("</svg>");
            return svg.ToString();
        }

        private static string DescribeArc(double cx, double cy, double r, double startAngle, double endAngle)
        {
            var fmt = CultureInfo.InvariantCulture;
            double startRad = Math.PI * startAngle / 180.0;
            double endRad = Math.PI * endAngle / 180.0;
            double x1 = cx + r * Math.Cos(startRad);
            double y1 = cy + r * Math.Sin(startRad);
            double x2 = cx + r * Math.Cos(endRad);
            double y2 = cy + r * Math.Sin(endRad);
            int largeArc = (endAngle - startAngle) > 180 ? 1 : 0;
            return $"M {cx.ToString(fmt)},{cy.ToString(fmt)} L {x1.ToString(fmt)},{y1.ToString(fmt)} A {r.ToString(fmt)},{r.ToString(fmt)} 0 {largeArc} 1 {x2.ToString(fmt)},{y2.ToString(fmt)} Z";
        }

        private static void ExportToExcel(string filePath, ExportData data)
        {
            ExcelPackage.License.SetNonCommercialOrganization("MediaVault");

            using (var package = new ExcelPackage())
            {
                var wsMonth = package.Workbook.Worksheets.Add("Місяці");
                wsMonth.Cells[1, 1].Value = "Щомісячний час (годин)";
                wsMonth.Cells[2, 1].Value = $"Діаграма: сумарний час перегляду по місяцях за {data.SelectedYear} рік.";
                wsMonth.Cells[3, 1].Value = "Місяць";
                wsMonth.Cells[3, 2].Value = "Годин";
                int row = 4;
                foreach (var m in data.Monthly)
                {
                    wsMonth.Cells[row, 1].Value = m.Month;
                    wsMonth.Cells[row, 2].Value = m.TotalHours;
                    row++;
                }
                wsMonth.Cells[wsMonth.Dimension.Address].AutoFitColumns();

                var chartMonth = wsMonth.Drawings.AddChart("chartMonth", OfficeOpenXml.Drawing.Chart.eChartType.ColumnClustered);
                chartMonth.Title.Text = "Щомісячний час (годин)";
                chartMonth.SetPosition(1, 0, 3, 0);
                chartMonth.SetSize(600, 300);
                var seriesMonth = chartMonth.Series.Add($"B4:B{row - 1}", $"A4:A{row - 1}");
                seriesMonth.Header = "Годин";

                var wsDay = package.Workbook.Worksheets.Add("Доба");
                wsDay.Cells[1, 1].Value = "Добовий розподіл часу (годин)";
                wsDay.Cells[2, 1].Value = $"Діаграма: сумарний час перегляду по 3-годинних інтервалах доби за період {data.PeriodStart:yyyy-MM-dd} — {data.PeriodEnd:yyyy-MM-dd}.";
                wsDay.Cells[3, 1].Value = "Інтервал";
                wsDay.Cells[3, 2].Value = "Годин";
                row = 4;
                foreach (var d in data.DailyIntervals)
                {
                    wsDay.Cells[row, 1].Value = d.Interval;
                    wsDay.Cells[row, 2].Value = d.TotalHours;
                    row++;
                }
                wsDay.Cells[wsDay.Dimension.Address].AutoFitColumns();

                var chartDay = wsDay.Drawings.AddChart("chartDay", OfficeOpenXml.Drawing.Chart.eChartType.ColumnClustered);
                chartDay.Title.Text = "Добовий розподіл часу (годин)";
                chartDay.SetPosition(1, 0, 3, 0);
                chartDay.SetSize(600, 300);
                var seriesDay = chartDay.Series.Add($"B4:B{row - 1}", $"A4:A{row - 1}");
                seriesDay.Header = "Годин";

                var wsGenre = package.Workbook.Worksheets.Add("Жанри");
                wsGenre.Cells[1, 1].Value = "Популярність жанрів";
                wsGenre.Cells[2, 1].Value = $"Діаграма: розподіл часу перегляду за жанрами за {data.SelectedYear} рік.";
                wsGenre.Cells[3, 1].Value = "Жанр";
                wsGenre.Cells[3, 2].Value = "Годин";
                wsGenre.Cells[3, 3].Value = "Відсоток";
                row = 4;
                foreach (var g in data.Genres)
                {
                    wsGenre.Cells[row, 1].Value = g.Genre;
                    wsGenre.Cells[row, 2].Value = g.TotalHours;
                    wsGenre.Cells[row, 3].Value = g.Percent;
                    row++;
                }
                wsGenre.Cells[wsGenre.Dimension.Address].AutoFitColumns();

                var chartGenre = wsGenre.Drawings.AddChart("chartGenre", OfficeOpenXml.Drawing.Chart.eChartType.Pie);
                chartGenre.Title.Text = "Популярність жанрів";
                chartGenre.SetPosition(1, 0, 4, 0);
                chartGenre.SetSize(600, 300);
                var seriesGenre = chartGenre.Series.Add($"B4:B{row - 1}", $"A4:A{row - 1}");
                seriesGenre.Header = "Годин";

                using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    package.SaveAs(fs);
                }
            }
        }
    }
}
