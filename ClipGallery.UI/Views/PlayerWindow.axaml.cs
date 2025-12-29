using System;
using Avalonia.Controls;
using Avalonia.Input;
using ClipGallery.UI.ViewModels;
using System.Threading.Tasks;

namespace ClipGallery.UI.Views;

public partial class PlayerWindow : Window
{
    public PlayerWindow()
    {
        InitializeComponent();
        KeyDown += OnKeyDown;
        Opened += OnOpened;

        var trimSlider = this.FindControl<ClipGallery.UI.Controls.TrimRangeSlider>("TrimSlider");
        if (trimSlider != null)
        {
            trimSlider.StartValueDragging += (s, val) => (DataContext as PlayerViewModel)?.OnTrimDragging(val);
            trimSlider.EndValueDragging += (s, val) => (DataContext as PlayerViewModel)?.OnTrimDragging(val);
        }
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        try
        {
            if (DataContext is PlayerViewModel vm)
            {
                await vm.StartPlaybackAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to start playback: {ex}");
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
