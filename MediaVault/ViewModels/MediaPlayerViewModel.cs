using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using LibVLCSharp.Shared;
using MediaVault.Models;

namespace MediaVault.ViewModels
{
    public class MediaPlayerViewModel : ViewModelBase, IDisposable
    {
        private readonly LibVLC _libVLC;
        public MediaPlayer MediaPlayer { get; }

        public ICommand PlayCommand { get; }
        public ICommand PauseCommand { get; }
        public ICommand StopCommand { get; }

        private double _position;
        private bool _isSeeking = false;
        private double _lastSetPosition = 0;

        public double Position
        {
            get => _position;
            set
            {
                if (Math.Abs(_position - value) > 0.01)
                {
                    _position = value;
                    OnPropertyChanged(nameof(Position));
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

        // Оновлюйте позицію під час відтворення
        public MediaPlayerViewModel(MediaFile mediaFile)
        {
            Core.Initialize();
            _libVLC = new LibVLC(new[] { "--no-video-title-show", "--avcodec-hw=none" });
            MediaPlayer = new MediaPlayer(_libVLC);

            var uri = new Uri(mediaFile.FilePath);
            var media = new Media(_libVLC, uri);
            MediaPlayer.Media = media;

            PlayCommand = new RelayCommand(_ => Play());
            PauseCommand = new RelayCommand(_ => Pause());

            Task.Run(async () =>
            {
                await media.Parse(MediaParseOptions.ParseLocal);
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    OnPropertyChanged(nameof(Duration));
                    UpdateIsSeekable();
                });
            });

            UpdateIsSeekable();

            MediaPlayer.PositionChanged += (s, e) =>
            {
                if (!_isSeeking && Duration > 0)
                {
                    _position = MediaPlayer.Position * Duration;
                    OnPropertyChanged(nameof(Position));
                }
            };
            MediaPlayer.LengthChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(Duration));
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
            };

            MediaPlayer.Volume = _volume;
        }

        // Додаємо допоміжний метод:
        private void UpdateIsSeekable()
        {
            IsSeekable = MediaPlayer?.IsSeekable ?? false;
        }

        private void Play()
        {
            MediaPlayer.Play();
            OnPropertyChanged(nameof(IsSeekable));
        }
        private void Pause() => MediaPlayer.Pause();

        public void Dispose()
        {
            MediaPlayer.Dispose();
            _libVLC.Dispose();
        }
    }


}
