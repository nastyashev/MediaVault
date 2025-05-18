using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaVault.Models
{
    public class Statistics
    {
        public int TotalWatched { get; private set; }
        public int TotalDuration { get; private set; } // Total duration in minutes

        public void UpdateStatistics(IEnumerable<MediaFile> mediaFiles)
        {
            TotalWatched = mediaFiles.Count(file => file.IsWatched);
            TotalDuration = mediaFiles.Where(file => file.IsWatched && file.Duration.HasValue)
                                      .Sum(file => (int)file.Duration.Value.TotalMinutes);
        }
    }
}
