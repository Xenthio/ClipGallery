using Avalonia.Controls;
using Avalonia.Interactivity;
using ClipGallery.UI.ViewModels;

namespace ClipGallery.UI;

public partial class MainWindow : Window
{
    private const double ScrollThresholdPixels = 200;
    
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
    
    private void OnGalleryScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        // Infinite scroll: load more clips when scrolled near bottom
        if (sender is ScrollViewer scrollViewer && DataContext is MainViewModel mainVm)
        {
            var scrollableHeight = scrollViewer.Extent.Height - scrollViewer.Viewport.Height;
            var currentScroll = scrollViewer.Offset.Y;
            
            // Load more when within threshold of the bottom
            if (scrollableHeight > 0 && currentScroll >= scrollableHeight - ScrollThresholdPixels)
            {
                if (mainVm.Gallery.HasMoreClips)
                {
                    mainVm.Gallery.LoadMoreCommand.Execute(null);
                }
            }
        }
    }
}