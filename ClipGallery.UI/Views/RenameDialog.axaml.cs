using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ClipGallery.UI.Views;

public partial class RenameDialog : Window
{
    public RenameDialog()
    {
        InitializeComponent();
    }

    public RenameDialog(string currentName) : this()
    {
        NameInput.Text = currentName;
        NameInput.SelectAll();
        NameInput.Focus();
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }

    private void OnRenameClick(object? sender, RoutedEventArgs e)
    {
        Close(NameInput.Text);
    }
}
