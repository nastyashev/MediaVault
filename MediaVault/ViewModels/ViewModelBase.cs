using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MediaVault.ViewModels
{
    public class ViewModelBase : ObservableObject
    {
        public new event PropertyChangedEventHandler? PropertyChanged;

        // protected new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        // {
        //     PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        // }

        // protected new bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        // {
        //     if (Equals(field, value)) return false;
        //     field = value;
        //     OnPropertyChanged(propertyName);
        //     return true;
        // }
    }
}
