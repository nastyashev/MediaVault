﻿using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;

namespace MediaVault.Models
{
    public class MediaFile : INotifyPropertyChanged
    {
        public string Title { get; set; }
        public string FilePath { get; set; }
        public MediaType Type { get; set; }
        public bool IsWatched { get; set; }
        public DateTime? LastWatched { get; set; }
        public TimeSpan? Duration { get; set; }
        public string Genre { get; set; }
        public double? Rating { get; set; }
        public string Cast { get; set; }
        public int ReleaseYear { get; set; }
        public string UserNotes { get; set; }
        private string _coverImagePath;
        public string CoverImagePath
        {
            get => _coverImagePath;
            set
            {
                if (_coverImagePath != value)
                {
                    _coverImagePath = value;
                    OnPropertyChanged(nameof(CoverImagePath));
                }
            }
        }
        public DateTime AddedDate { get; set; }

        private Bitmap? _cover;
        public Bitmap? Cover
        {
            get => _cover;
            set
            {
                if (_cover != value)
                {
                    _cover = value;
                    OnPropertyChanged(nameof(Cover));
                }
            }
        }

        public MediaFile(string title, string filePath, MediaType type)
        {
            Title = title;
            FilePath = filePath;
            Type = type;
            IsWatched = false;
            LastWatched = null;
            Duration = null;
            Genre = string.Empty;
            Rating = null;
            Cast = string.Empty;
            ReleaseYear = 0;
            UserNotes = string.Empty;
            _coverImagePath = string.Empty;
            AddedDate = DateTime.Now;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public enum MediaType
    {
        Video,
        Audio
    }
}
