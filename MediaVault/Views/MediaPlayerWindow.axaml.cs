using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MediaVault.Models;
using LibVLCSharp.Shared;
using MediaVault.ViewModels;

namespace MediaVault;

public partial class MediaPlayerWindow : Window
{
    public MediaPlayerWindow()
    {
        InitializeComponent();
    }

    public MediaPlayerWindow(MediaFile mediaFile)
    {
        InitializeComponent();
        DataContext = new MediaPlayerViewModel(mediaFile, ToggleFullScreen);

        this.Opened += MediaPlayerWindow_Opened;
        this.Closing += MediaPlayerWindow_Closing;
    }

    private void ToggleFullScreen()
    {
        if (WindowState == WindowState.FullScreen)
            WindowState = WindowState.Normal;
        else
            WindowState = WindowState.FullScreen;
    }

    private void MediaPlayerWindow_Opened(object? sender, EventArgs e)
    {
        var viewModel = (MediaPlayerViewModel)DataContext!;
        VideoView.MediaPlayer = viewModel.MediaPlayer;
        viewModel.PlayCommand.Execute(null); // Додаємо автозапуск
    }

    private void MediaPlayerWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        // Від'єднайте MediaPlayer від VideoView перед Dispose
        VideoView.MediaPlayer = null;

        if (DataContext is MediaPlayerViewModel viewModel)
            viewModel.Dispose();
    }

}
