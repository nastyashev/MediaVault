using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaVault.Models
{
    public class MediaFile
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
        public string CoverImagePath { get; set; }
        public DateTime AddedDate { get; set; }

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
            CoverImagePath = string.Empty;
            AddedDate = DateTime.Now;
        }
    }

    public enum MediaType
    {
        Video,
        Audio
    }
}
