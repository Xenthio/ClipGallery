using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ClipGallery.UI.ViewModels;

namespace ClipGallery.UI.Views;

public partial class PlayerWindow : Window
{
    public PlayerWindow()
    {
        InitializeComponent();
        KeyDown += OnKeyDown;
        Loaded += PlayerWindow_Loaded;
    }

    private void PlayerWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        // Wire up TrimSlider events
        var trimSlider = this.FindControl<Controls.TrimSlider>("TrimSlider");
        if (trimSlider != null && DataContext is PlayerViewModel vm)
        {
            trimSlider.StartValueChanging += (s, value) => vm.HandleTrimStartChanging(value);
            trimSlider.EndValueChanging += (s, value) => vm.HandleTrimEndChanging(value);
            trimSlider.StartValueChanged += (s, value) => vm.HandleTrimStartChanged(value);
            trimSlider.EndValueChanged += (s, value) => vm.HandleTrimEndChanged(value);
            
            // Start playback after window is loaded and VideoView is ready
            vm.StartPlayback();
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
