using System;
using System.ComponentModel;
using System.Windows.Input;

namespace MediaVault.ViewModels
{
    public class StatisticsPageViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler? BackToLibraryRequested;

        public ICommand BackCommand { get; }

        public StatisticsPageViewModel()
        {
            BackCommand = new RelayCommand(_ => BackToLibraryRequested?.Invoke(this, EventArgs.Empty));
        }

        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
