using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaVault.Models
{
    public class MediaLibrary
    {
        private readonly ObservableCollection<MediaFile> _mediaFiles;

        public MediaLibrary()
        {
            _mediaFiles = new ObservableCollection<MediaFile>();
        }

        public ReadOnlyObservableCollection<MediaFile> MediaFiles => new ReadOnlyObservableCollection<MediaFile>(_mediaFiles);

        public void AddMediaFile(MediaFile mediaFile)
        {
            if (!_mediaFiles.Contains(mediaFile))
            {
                _mediaFiles.Add(mediaFile);
            }
        }

        public void RemoveMediaFile(MediaFile mediaFile)
        {
            if (_mediaFiles.Contains(mediaFile))
            {
                _mediaFiles.Remove(mediaFile);
            }
        }

        public MediaFile FindMediaFileByTitle(string title)
        {
            return _mediaFiles.FirstOrDefault(file => file.Title.Equals(title, System.StringComparison.OrdinalIgnoreCase));
        }

        public void MarkAsWatched(MediaFile mediaFile)
        {
            var file = _mediaFiles.FirstOrDefault(f => f.Equals(mediaFile));
            if (file != null)
            {
                file.IsWatched = true;
                file.LastWatched = DateTime.Now;
            }
        }
    }
}
