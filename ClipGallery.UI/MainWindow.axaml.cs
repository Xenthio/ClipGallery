using Avalonia.Controls;
using Avalonia.Interactivity;
using ClipGallery.UI.ViewModels;

namespace ClipGallery.UI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnClipContextMenuOpened(object? sender, RoutedEventArgs e)
    {
        // When context menu opens, set ContextClip so SetRating can work
        if (sender is ContextMenu menu && 
            menu.DataContext is ClipViewModel clipVm &&
            DataContext is MainViewModel mainVm)
        {
            mainVm.ContextClip = clipVm;
        }
    }
}