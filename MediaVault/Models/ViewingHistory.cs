using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaVault.Models
{
    public class ViewingHistory
    {
        private readonly List<MediaFile> _viewedFiles;

        public ViewingHistory()
        {
            _viewedFiles = new List<MediaFile>();
        }

        public IReadOnlyList<MediaFile> ViewedFiles => _viewedFiles.AsReadOnly();

        public void AddToHistory(MediaFile mediaFile)
        {
            if (!_viewedFiles.Contains(mediaFile))
            {
                _viewedFiles.Add(mediaFile);
                mediaFile.IsWatched = true;
                mediaFile.LastWatched = DateTime.Now;
            }
        }
    }
}
