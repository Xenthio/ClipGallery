using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Collections.Generic;

namespace ClipGallery.UI.Views;

public partial class MoveToGameDialog : Window
{
    public MoveToGameDialog()
    {
        InitializeComponent();
    }

    public MoveToGameDialog(List<string> games, string currentGame) : this()
    {
        GamesList.ItemsSource = games;
        
        // Select current game
        var index = games.IndexOf(currentGame);
        if (index >= 0)
        {
            GamesList.SelectedIndex = index;
        }
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }

    private void OnMoveClick(object? sender, RoutedEventArgs e)
    {
        // Prefer new game name input if provided
        if (!string.IsNullOrWhiteSpace(NewGameInput.Text))
        {
            Close(NewGameInput.Text.Trim());
        }
        else if (GamesList.SelectedItem is string selectedGame)
        {
            Close(selectedGame);
        }
        else
        {
            Close(null);
        }
    }
}
