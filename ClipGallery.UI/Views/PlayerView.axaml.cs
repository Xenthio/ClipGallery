using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using ClipGallery.UI.ViewModels;

namespace ClipGallery.UI.Views;

public partial class PlayerView : UserControl
{
    public PlayerView()
    {
        InitializeComponent();
        
        // Handle keyboard shortcuts
        KeyDown += OnKeyDown;
        Focusable = true;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Focus();
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
                // Close player - handled by parent
                break;
        }
    }
}
