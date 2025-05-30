using System;
using System.ComponentModel;
using System.Windows.Input;

namespace MediaVault.ViewModels
{
    public class SettingsPageViewModel : INotifyPropertyChanged
    {
        public ICommand BackCommand { get; }
        public event EventHandler? BackToLibraryRequested;

        public SettingsPageViewModel()
        {
            BackCommand = new RelayCommand(_ => BackToLibraryRequested?.Invoke(this, EventArgs.Empty));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
