using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClipGallery.Core.Services;
using System.Collections.ObjectModel;
using System.Linq; // for ToList
using Avalonia.Platform.Storage; // For folder picker
using System.Threading.Tasks;

namespace ClipGallery.UI.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private ObservableCollection<string> _libraryPaths = new();

    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        // Load current paths
        LibraryPaths = new ObservableCollection<string>(_settingsService.Settings.LibraryPaths);
    }

    [RelayCommand]
    public async Task AddPath(Avalonia.Controls.Window window)
    {
        // Use Avalonia StorageProvider
        var topLevel = Avalonia.Controls.TopLevel.GetTopLevel(window);
        if (topLevel == null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Game Library Folder",
            AllowMultiple = false
        });

        if (folders.Count > 0)
        {
            var path = folders[0].Path.LocalPath;
            if (!LibraryPaths.Contains(path))
            {
                LibraryPaths.Add(path);
            }
        }
    }

    [RelayCommand]
    public void RemovePath(string path)
    {
        if (LibraryPaths.Contains(path))
        {
            LibraryPaths.Remove(path);
        }
    }

    [RelayCommand]
    public async Task SaveAndClose(Avalonia.Controls.Window window)
    {
        _settingsService.Settings.LibraryPaths = LibraryPaths.ToList();
        await _settingsService.SaveSettingsAsync();
        window.Close();
    }
}
