using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MediaVault.Models;
using MediaVault.ViewModels;
using System.Collections.ObjectModel;

namespace MediaVault.Views
{
    public partial class ViewingHistoryWindow : Window
    {
        //public ObservableCollection<ViewingHistoryRecord> HistoryRecords { get; }

        public ViewingHistoryWindow()
        {
            InitializeComponent();
            DataContext = new ViewingHistoryViewModel();
        }
        
    }
}
