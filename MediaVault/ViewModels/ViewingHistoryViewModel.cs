using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using MediaVault.Models;

namespace MediaVault.ViewModels
{
    public class ViewingHistoryViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<ViewingHistoryRecord> _sortedHistory;
        public ObservableCollection<ViewingHistoryRecord> SortedHistory
        {
            get => _sortedHistory;
            set
            {
                if (_sortedHistory != value)
                {
                    _sortedHistory = value;
                    OnPropertyChanged(nameof(SortedHistory));
                }
            }
        }

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
