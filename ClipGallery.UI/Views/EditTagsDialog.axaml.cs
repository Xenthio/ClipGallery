using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ClipGallery.UI.Views;

public partial class EditTagsDialog : Window
{
    public EditTagsDialog()
    {
        InitializeComponent();
    }

    public EditTagsDialog(List<string> currentTags) : this()
    {
        TagsInput.Text = string.Join(", ", currentTags);
        TagsInput.Focus();
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }

    private void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        var tags = (TagsInput.Text ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
        Close(tags);
    }
}
