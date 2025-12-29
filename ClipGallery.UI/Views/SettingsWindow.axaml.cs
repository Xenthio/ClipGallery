using Avalonia.Controls;
using Avalonia.Interactivity;
using ClipGallery.UI.ViewModels;

namespace ClipGallery.UI.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
    
    private void OnSelectTab0(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
        {
            vm.SelectedTabIndex = 0;
        }
    }
    
    private void OnSelectTab1(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
        {
            vm.SelectedTabIndex = 1;
        }
    }
}
