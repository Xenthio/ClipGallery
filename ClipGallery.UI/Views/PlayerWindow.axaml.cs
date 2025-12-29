using System;
using Avalonia.Controls;
using Avalonia.Input;
using ClipGallery.UI.ViewModels;
using LibVLCSharp.Avalonia;

namespace ClipGallery.UI.Views;

public partial class PlayerWindow : Window
{
    public PlayerWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        KeyDown += OnKeyDown;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (this.FindControl<VideoView>("VideoSurface") is { } videoView)
        {
            if (DataContext is PlayerViewModel vm)
            {
                videoView.MediaPlayer = vm.Player;
                _ = vm.StartPlaybackAsync();
            }
            else
            {
                videoView.MediaPlayer = null;
            }
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not PlayerViewModel vm) return;

        switch (e.Key)
        {
            case Key.Space:
                vm.TogglePlayPauseCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Left:
                vm.SeekRelative(-5);
                e.Handled = true;
                break;
            case Key.Right:
                vm.SeekRelative(5);
                e.Handled = true;
                break;
            case Key.Up:
                vm.MainVolume = Math.Min(100, vm.MainVolume + 5);
                e.Handled = true;
                break;
            case Key.Down:
                vm.MainVolume = Math.Max(0, vm.MainVolume - 5);
                e.Handled = true;
                break;
            case Key.Escape:
                Close();
                e.Handled = true;
                break;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        
        // Dispose the player when window closes
        if (DataContext is PlayerViewModel vm)
        {
            vm.Dispose();
        }
    }
}
