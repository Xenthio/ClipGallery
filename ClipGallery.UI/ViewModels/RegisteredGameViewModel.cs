using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClipGallery.Core.Models;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;

namespace ClipGallery.UI.ViewModels;

/// <summary>
/// View model for a registered game entry in settings
/// </summary>
public partial class RegisteredGameViewModel : ObservableObject
{
    [ObservableProperty]
    private string _id = "";
    
    [ObservableProperty]
    private string _displayName = "";
    
    [ObservableProperty]
    private string? _iconPath;
    
    [ObservableProperty]
    private string? _boxArtPath;
    
    [ObservableProperty]
    private ObservableCollection<string> _folderNames = new();
    
    [ObservableProperty]
    private Bitmap? _iconImage;
    
    [ObservableProperty]
    private Bitmap? _boxArtImage;
    
    [ObservableProperty]
    private string _newFolderName = "";
    
    public RegisteredGameViewModel()
    {
        Id = Guid.NewGuid().ToString();
    }
    
    public RegisteredGameViewModel(RegisteredGame game)
    {
        Id = game.Id;
        DisplayName = game.DisplayName;
        IconPath = game.IconPath;
        BoxArtPath = game.BoxArtPath;
        FolderNames = new ObservableCollection<string>(game.FolderNames);
        
        LoadImages();
    }
    
    private void LoadImages()
    {
        if (!string.IsNullOrEmpty(IconPath) && File.Exists(IconPath))
        {
            try
            {
                // Load bitmap from file path directly - Avalonia handles stream management
                IconImage = new Bitmap(IconPath);
            }
            catch { /* Ignore */ }
        }
        
        if (!string.IsNullOrEmpty(BoxArtPath) && File.Exists(BoxArtPath))
        {
            try
            {
                // Load bitmap from file path directly - Avalonia handles stream management
                BoxArtImage = new Bitmap(BoxArtPath);
            }
            catch { /* Ignore */ }
        }
    }
    
    public RegisteredGame ToModel()
    {
        return new RegisteredGame
        {
            Id = Id,
            DisplayName = DisplayName,
            IconPath = IconPath,
            BoxArtPath = BoxArtPath,
            FolderNames = new System.Collections.Generic.List<string>(FolderNames)
        };
    }
    
    [RelayCommand]
    public void AddFolderName()
    {
        if (!string.IsNullOrWhiteSpace(NewFolderName) && !FolderNames.Contains(NewFolderName))
        {
            FolderNames.Add(NewFolderName.Trim());
            NewFolderName = "";
        }
    }
    
    [RelayCommand]
    public void RemoveFolderName(string folderName)
    {
        FolderNames.Remove(folderName);
    }
    
    [RelayCommand]
    public async Task BrowseIcon(Avalonia.Controls.Window window)
    {
        var topLevel = Avalonia.Controls.TopLevel.GetTopLevel(window);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Game Icon",
            AllowMultiple = false,
            FileTypeFilter = new[] 
            { 
                new FilePickerFileType("Images") { Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.ico" } }
            }
        });

        if (files.Count > 0)
        {
            IconPath = files[0].Path.LocalPath;
            LoadImages();
        }
    }
    
    [RelayCommand]
    public async Task BrowseBoxArt(Avalonia.Controls.Window window)
    {
        var topLevel = Avalonia.Controls.TopLevel.GetTopLevel(window);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Box Art",
            AllowMultiple = false,
            FileTypeFilter = new[] 
            { 
                new FilePickerFileType("Images") { Patterns = new[] { "*.png", "*.jpg", "*.jpeg" } }
            }
        });

        if (files.Count > 0)
        {
            BoxArtPath = files[0].Path.LocalPath;
            LoadImages();
        }
    }
}
