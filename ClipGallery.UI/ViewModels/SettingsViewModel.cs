using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClipGallery.Core.Services;
using ClipGallery.Core.Models;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Platform.Storage;
using System.Threading.Tasks;

namespace ClipGallery.UI.ViewModels;

/// <summary>
/// View model for a game alias entry (folder name -> display name mapping)
/// </summary>
public partial class GameAliasViewModel : ObservableObject
{
    [ObservableProperty]
    private string _folderName = "";
    
    [ObservableProperty]
    private string _displayName = "";
}

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private ObservableCollection<string> _libraryPaths = new();
    
    [ObservableProperty]
    private ObservableCollection<GameAliasViewModel> _gameAliases = new();
    
    [ObservableProperty]
    private ObservableCollection<RegisteredGameViewModel> _registeredGames = new();
    
    [ObservableProperty]
    private RegisteredGameViewModel? _selectedGame;
    
    [ObservableProperty]
    private int _selectedTabIndex = 0;

    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        // Load current paths
        LibraryPaths = new ObservableCollection<string>(_settingsService.Settings.LibraryPaths);
        
        // Load game aliases (legacy)
        GameAliases = new ObservableCollection<GameAliasViewModel>(
            _settingsService.Settings.GameAliases.Select(kv => new GameAliasViewModel 
            { 
                FolderName = kv.Key, 
                DisplayName = kv.Value 
            })
        );
        
        // Load registered games
        RegisteredGames = new ObservableCollection<RegisteredGameViewModel>(
            _settingsService.Settings.RegisteredGames.Select(g => new RegisteredGameViewModel(g))
        );
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
    public void AddGameAlias()
    {
        GameAliases.Add(new GameAliasViewModel());
    }
    
    [RelayCommand]
    public void RemoveGameAlias(GameAliasViewModel alias)
    {
        GameAliases.Remove(alias);
    }
    
    [RelayCommand]
    public void AddRegisteredGame()
    {
        var newGame = new RegisteredGameViewModel { DisplayName = "New Game" };
        RegisteredGames.Add(newGame);
        SelectedGame = newGame;
    }
    
    [RelayCommand]
    public void RemoveRegisteredGame(RegisteredGameViewModel game)
    {
        if (SelectedGame == game)
        {
            SelectedGame = null;
        }
        RegisteredGames.Remove(game);
    }
    
    [RelayCommand]
    public void SelectGame(RegisteredGameViewModel game)
    {
        SelectedGame = game;
    }
    
    /// <summary>
    /// Creates a new registered game from a folder name (called from main window context menu)
    /// </summary>
    public RegisteredGameViewModel CreateGameFromFolder(string folderName)
    {
        // Check if game already exists with this folder
        var existing = RegisteredGames.FirstOrDefault(g => g.FolderNames.Contains(folderName));
        if (existing != null)
        {
            return existing;
        }
        
        var newGame = new RegisteredGameViewModel
        {
            DisplayName = folderName,
            FolderNames = new ObservableCollection<string> { folderName }
        };
        RegisteredGames.Add(newGame);
        return newGame;
    }

    [RelayCommand]
    public async Task SaveAndClose(Avalonia.Controls.Window window)
    {
        _settingsService.Settings.LibraryPaths = LibraryPaths.ToList();
        
        // Save game aliases (only non-empty entries)
        _settingsService.Settings.GameAliases = GameAliases
            .Where(a => !string.IsNullOrWhiteSpace(a.FolderName) && !string.IsNullOrWhiteSpace(a.DisplayName))
            .ToDictionary(a => a.FolderName, a => a.DisplayName);
        
        // Save registered games
        _settingsService.Settings.RegisteredGames = RegisteredGames
            .Where(g => !string.IsNullOrWhiteSpace(g.DisplayName))
            .Select(g => g.ToModel())
            .ToList();
        
        await _settingsService.SaveSettingsAsync();
        window.Close();
    }
}
