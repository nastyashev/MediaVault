using System;
using System.Timers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MediaVault.Models;
using LibVLCSharp.Shared;
using MediaVault.ViewModels;
using Avalonia.Threading;

namespace MediaVault;

public partial class MediaPlayerWindow : Window
{
    private System.Timers.Timer? _hideControlsTimer;

    public MediaPlayerWindow()
    {
        InitializeComponent();
    }

    public MediaPlayerWindow(MediaFile mediaFile)
    {
        InitializeComponent();
        var vm = new MediaPlayerViewModel(mediaFile, ToggleFullScreen);
        DataContext = vm;

        this.Opened += MediaPlayerWindow_Opened;
        this.Closing += MediaPlayerWindow_Closing;

        this.PointerMoved += MediaPlayerWindow_PointerMoved;
        this.PointerEntered += MediaPlayerWindow_PointerMoved;
        this.PointerExited += MediaPlayerWindow_PointerExited;

        // Слідкуємо за зміною WindowState для оновлення IsFullScreen
        this.GetObservable(Window.WindowStateProperty).Subscribe(state =>
        {
            if (DataContext is MediaPlayerViewModel mvm)
                mvm.IsFullScreen = (state == WindowState.FullScreen);
            UpdateControlsVisibility();
        });
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
        UpdateControlsVisibility();
    }

    private void MediaPlayerWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        VideoView.MediaPlayer = null;
        if (DataContext is MediaPlayerViewModel viewModel)
            viewModel.Dispose();
        _hideControlsTimer?.Dispose();
    }

    private void MediaPlayerWindow_PointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        if (DataContext is not MediaPlayerViewModel vm) return;
        if (vm.IsFullScreen)
        {
            vm.ControlsVisible = true;
            RestartHideControlsTimer();
        }
    }

    private void MediaPlayerWindow_PointerExited(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        if (DataContext is not MediaPlayerViewModel vm) return;
        if (vm.IsFullScreen)
        {
            StartHideControlsTimer();
        }
    }

    private void RestartHideControlsTimer()
    {
        _hideControlsTimer?.Stop();
        StartHideControlsTimer();
    }

    private void StartHideControlsTimer()
    {
        if (_hideControlsTimer == null)
        {
            _hideControlsTimer = new System.Timers.Timer(2000); // 2 секунди
            _hideControlsTimer.Elapsed += (s, e) =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (DataContext is MediaPlayerViewModel vm && vm.IsFullScreen)
                        vm.ControlsVisible = false;
                });
            };
            _hideControlsTimer.AutoReset = false;
        }
        _hideControlsTimer.Stop();
        _hideControlsTimer.Start();
    }

    private void UpdateControlsVisibility()
    {
        if (DataContext is not MediaPlayerViewModel vm) return;
        if (vm.IsFullScreen)
        {
            vm.ControlsVisible = true;
            RestartHideControlsTimer();
        }
        else
        {
            vm.ControlsVisible = true;
            _hideControlsTimer?.Stop();
        }
    }
}
