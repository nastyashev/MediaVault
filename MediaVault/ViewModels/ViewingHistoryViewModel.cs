using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using MediaVault.Models;

namespace MediaVault.ViewModels
{
    public class ViewingHistoryViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<ViewingHistoryRecord> SortedHistory { get; }

        public ViewingHistoryViewModel()
        {
            var log = ViewingHistoryLog.Load();
            var sorted = log.Records
                .OrderByDescending(r => r.ViewDate)
                .ToList();
            SortedHistory = new ObservableCollection<ViewingHistoryRecord>(sorted);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
