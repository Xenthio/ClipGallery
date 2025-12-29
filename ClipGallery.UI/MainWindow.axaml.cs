using Avalonia.Controls;
using Avalonia.Input;
using ClipGallery.UI.ViewModels;

namespace ClipGallery.UI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        KeyDown += OnKeyDown;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && DataContext is MainViewModel vm && vm.CurrentPlayer != null)
        {
            vm.ClosePlayerCommand.Execute(null);
            e.Handled = true;
        }
    }
}