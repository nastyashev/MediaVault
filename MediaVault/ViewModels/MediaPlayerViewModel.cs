using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using LibVLCSharp.Shared;
using MediaVault.Models;

namespace MediaVault.ViewModels
{
    public class MediaPlayerViewModel : ViewModelBase, IDisposable
    {
        private bool _disposed = false;
        private readonly LibVLC _libVLC;
        public MediaPlayer MediaPlayer { get; }

        public ICommand PlayCommand { get; }
        public ICommand PauseCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand ToggleFullScreenCommand { get; }

        private bool _isFullScreen;
        public bool IsFullScreen
        {
            get => _isFullScreen;
            set => SetProperty(ref _isFullScreen, value);
        }

        private bool _controlsVisible = true;
        public bool ControlsVisible
        {
            get => _controlsVisible;
            set => SetProperty(ref _controlsVisible, value);
        }

        private double _position;
        private bool _isSeeking = false;
        private double _lastSetPosition;

        public double Position
        {
            get => _position;
            set
            {
                if (Math.Abs(_position - value) > 0.01)
                {
                    _position = value;
                    OnPropertyChanged(nameof(Position));
                    OnPropertyChanged(nameof(PositionString));
                    if (MediaPlayer != null && Duration > 0 && Math.Abs(MediaPlayer.Position * Duration - value) > 1)
                    {
                        _isSeeking = true;
                        MediaPlayer.Position = (float)(value / Duration);
                        _lastSetPosition = value;
                        _isSeeking = false;
                    }
                }
            }
        }

        public string PositionString => FormatTime(Position);
        public string DurationString => FormatTime(Duration);

        private static string FormatTime(double seconds)
        {
            if (double.IsNaN(seconds) || seconds < 0.5)
                return "00:00";
            var ts = TimeSpan.FromSeconds(seconds);
            if (ts.TotalHours >= 1)
                return ts.ToString(@"hh\:mm\:ss");
            else
                return ts.ToString(@"mm\:ss");
        }

        public double Duration => MediaPlayer?.Media?.Duration / 1000.0 ?? 0;
        private bool _isSeekable;
        public bool IsSeekable
        {
            get => _isSeekable;
            private set => SetProperty(ref _isSeekable, value);
        }

        private int _volume = 100;
        public int Volume
        {
            get => _volume;
            set
            {
                if (_volume != value)
                {
                    _volume = value;
                    MediaPlayer.Volume = _volume;
                    OnPropertyChanged(nameof(Volume));
                }
            }
        }

        private readonly MediaFile _mediaFile;
        private DateTime? _viewStartTime;
        private int _currentRecordId;

        public MediaPlayerViewModel(MediaFile mediaFile, Action? toggleFullScreenAction = null)
        {
            Core.Initialize();
            _libVLC = new LibVLC("--no-video-title-show", "--avcodec-hw=none");
            MediaPlayer = new MediaPlayer(_libVLC);

            var uri = new Uri(mediaFile.FilePath);
            var media = new Media(_libVLC, uri);
            MediaPlayer.Media = media;

            int resumePosition = 0;
            var log = ViewingHistoryLog.Load();
            var lastRecord = log.Records
                .Where(r => r.FileId == mediaFile.FilePath && r.EndTime > 0)
                .OrderByDescending(r => r.ViewDate)
                .FirstOrDefault();
            if (lastRecord != null)
                resumePosition = lastRecord.EndTime;

            PlayCommand = new RelayCommand(_ => Play());
            PauseCommand = new RelayCommand(_ => Pause());
            ToggleFullScreenCommand = new RelayCommand(_ =>
            {
                toggleFullScreenAction?.Invoke();
                IsFullScreen = !IsFullScreen;
            });

            Task.Run(async () =>
            {
                await media.Parse(MediaParseOptions.ParseLocal);
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    OnPropertyChanged(nameof(Duration));
                    UpdateIsSeekable();
                    if (resumePosition > 0 && Duration > 0)
                    {
                        Position = resumePosition;
                        MediaPlayer.Position = (float)(resumePosition / Duration);
                    }
                });
            });

            UpdateIsSeekable();

            MediaPlayer.PositionChanged += (s, e) =>
            {
                if (!_isSeeking && Duration > 0)
                {
                    _position = MediaPlayer.Position * Duration;
                    OnPropertyChanged(nameof(Position));
                    OnPropertyChanged(nameof(PositionString));
                }
            };
            MediaPlayer.LengthChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(Duration));
                OnPropertyChanged(nameof(DurationString));
                UpdateIsSeekable();
            };
            MediaPlayer.MediaChanged += (s, e) =>
            {
                UpdateIsSeekable();
            };
            MediaPlayer.Playing += (s, e) =>
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(UpdateIsSeekable);
                OnPropertyChanged(nameof(Duration));
                OnPropertyChanged(nameof(DurationString));
            };

            MediaPlayer.Volume = _volume;
            _mediaFile = mediaFile;
            Title = mediaFile?.Title ?? System.IO.Path.GetFileNameWithoutExtension(mediaFile?.FilePath);
        }

        private string _title;
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private void UpdateIsSeekable()
        {
            IsSeekable = MediaPlayer?.IsSeekable ?? false;
        }

        private void Play()
        {
            MediaPlayer.Play();
            OnPropertyChanged(nameof(IsSeekable));
            if (_viewStartTime == null)
            {
                _viewStartTime = DateTime.Now;
                LogViewStart();
            }
        }
        private void Pause()
        {
            MediaPlayer.Pause();
            LogViewPauseOrStop("в процесі");
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                LogViewPauseOrStop("в процесі");
                MediaPlayer?.Dispose();
                _libVLC?.Dispose();
            }

            _disposed = true;
        }

        ~MediaPlayerViewModel()
        {
            Dispose(false);
        }

        private void LogViewStart()
        {
            var log = ViewingHistoryLog.Load();
            var record = new ViewingHistoryRecord
            {
                RecordId = log.GetNextRecordId(),
                FileId = _mediaFile.FilePath,
                FileName = _mediaFile.Title,
                ViewDate = _viewStartTime ?? DateTime.Now,
                Duration = 0,
                EndTime = 0,
                Status = "в процесі",
                Genre = _mediaFile.Genre ?? ""
            };
            _currentRecordId = record.RecordId;
            log.AddRecord(record);
            log.Save();
        }

        private void LogViewPauseOrStop(string status)
        {
            if (_viewStartTime == null) return;
            var log = ViewingHistoryLog.Load();
            var record = log.Records.FirstOrDefault(r => r.RecordId == _currentRecordId);
            if (record != null)
            {
                var now = DateTime.Now;
                var duration = (int)(now - _viewStartTime.Value).TotalSeconds;
                int endTime = (int)Position;
                record.Duration = duration;
                record.EndTime = endTime;

                if (MediaPlayer != null && Duration > 0 && (MediaPlayer.Position >= 0.95f || endTime >= 0.95 * Duration))
                {
                    record.Status = "переглянуто";
                    _mediaFile.IsWatched = true;
                }
                else
                {
                    record.Status = status;
                }

                if (!string.IsNullOrWhiteSpace(_mediaFile.Genre))
                    record.Genre = _mediaFile.Genre;
                log.Save();
            }
        }
    }
}
