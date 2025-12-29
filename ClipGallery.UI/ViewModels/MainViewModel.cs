using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClipGallery.Core.Services;
using ClipGallery.Core.Models;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;
using Avalonia.Threading;
using Avalonia;
using Avalonia.Controls;

namespace ClipGallery.UI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IClipScannerService _scannerService;
    private readonly IAudioExtractionService _audioService;
    private readonly ITranscodeService _transcodeService;
    private readonly ISettingsService _settingsService;
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private GalleryViewModel _gallery;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isLibraryExpanded = true;

    [ObservableProperty]
    private bool _isTagsExpanded = true;

    // For context menu actions - track currently right-clicked clip
    [ObservableProperty]
    private ClipViewModel? _contextClip;

    [ObservableProperty]
    private string _searchQuery = "";

    partial void OnSearchQueryChanged(string value)
    {
        Gallery.SetSearchQuery(value);
    }

    public MainViewModel(IClipScannerService scannerService, IAudioExtractionService audioService,
        ITranscodeService transcodeService, ISettingsService settingsService, IServiceProvider serviceProvider)
    {
        _scannerService = scannerService;
        _audioService = audioService;
        _transcodeService = transcodeService;
        _settingsService = settingsService;
        _serviceProvider = serviceProvider;
        _gallery = new GalleryViewModel();

        _settingsService.SettingsUpdated += async (s, e) =>
        {
            // Clear and reload
            // We need to stop watchers too? The scanner keeps watchers in valid list.
            // For now just calling InitializeLibraryAsync again will create new watchers and scan.
            // Ideally we should clear old watchers but ScannerService logic is additive.
            // We'll trust ScannerService or just restart. But let's try reloading.
            Gallery.Clips.Clear();
            await InitializeLibraryAsync();
        };

        // Start scanning in background
        _ = InitializeLibraryAsync();
    }

    private async Task InitializeLibraryAsync()
    {
        IsLoading = true;

        await _settingsService.LoadSettingsAsync();
        var paths = _settingsService.Settings.LibraryPaths;

        // 1. Fast Scan (Files only)
        var clips = await _scannerService.BuildLibraryAsync(paths);

        // Start Watching
        _scannerService.ClipAdded += OnClipAdded;
        _scannerService.ClipRemoved += OnClipRemoved;
        foreach (var path in paths)
        {
            _scannerService.StartWatching(path);
        }

        // 2. Wrap via ViewModels and apply aliases (from both legacy aliases and registered games)
        var aliases = _settingsService.Settings.GameAliases;
        var registeredGames = _settingsService.Settings.RegisteredGames;
        var vms = clips.Select(c => 
        {
            var vm = new ClipViewModel(c, _scannerService);
            // Apply display name from registered games first
            var registeredGame = registeredGames.FirstOrDefault(g => g.FolderNames.Contains(c.GameName));
            if (registeredGame != null)
            {
                vm.DisplayGameName = registeredGame.DisplayName;
            }
            // Fall back to legacy game alias
            else if (aliases.TryGetValue(c.GameName, out var displayName))
            {
                vm.DisplayGameName = displayName;
            }
            return vm;
        }).ToList();
        
        Gallery.LoadClips(vms);
        IsLoading = false;

        // 3. Background Enrichment (Metadata + Thumbnails)
        _ = Task.Run(async () =>
        {
            foreach (var vm in vms)
            {
                await _scannerService.EnrichClipAsync(vm.Model);
                vm.UpdateDuration();
                await vm.LoadThumbnailAsync();
            }
        });
    }

    private string GetDisplayGameName(string folderName)
    {
        var registeredGame = _settingsService.Settings.RegisteredGames.FirstOrDefault(g => g.FolderNames.Contains(folderName));
        if (registeredGame != null)
        {
            return registeredGame.DisplayName;
        }
        if (_settingsService.Settings.GameAliases.TryGetValue(folderName, out var displayName))
        {
            return displayName;
        }
        return folderName;
    }

    private void OnClipAdded(object? sender, Clip clip)
    {
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var vm = new ClipViewModel(clip, _scannerService);
            
            // Apply display name
            vm.DisplayGameName = GetDisplayGameName(clip.GameName);
            
            Gallery.Clips.Insert(0, vm); // Add to top

            // Enrich
            await Task.Run(async () =>
            {
                await _scannerService.EnrichClipAsync(clip);
                vm.UpdateDuration();
                await vm.LoadThumbnailAsync();
            });
        });
    }

    private void OnClipRemoved(object? sender, string path)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            var toRemove = Gallery.Clips.FirstOrDefault(c => c.Model.FilePath == path);
            if (toRemove != null)
            {
                Gallery.Clips.Remove(toRemove);
            }
        });
    }

    [RelayCommand]
    public void OpenPlayer(ClipViewModel vm)
    {
        // Open video in a separate window
        var playerVm = new PlayerViewModel(vm, _audioService, _scannerService, _transcodeService);
        var playerWindow = new Views.PlayerWindow
        {
            DataContext = playerVm
        };
        playerWindow.Show();
    }

    [RelayCommand]
    public void FilterByGame(string? gameName)
    {
        Gallery.FilterByGame(gameName);
    }

    [RelayCommand]
    public void FilterByTag(string? tagName)
    {
        Gallery.FilterByTag(tagName);
    }

    [RelayCommand]
    public void OpenSettings()
    {
        OpenSettingsWithGame(null);
    }
    
    [RelayCommand]
    public void EditGame(string? gameName)
    {
        OpenSettingsWithGame(gameName);
    }
    
    private void OpenSettingsWithGame(string? gameName)
    {
        var vm = _serviceProvider.GetService(typeof(SettingsViewModel)) as SettingsViewModel;
        if (vm == null) return;

        // If game name provided, switch to Games tab and create/select the game
        if (!string.IsNullOrEmpty(gameName))
        {
            vm.SelectedTabIndex = 1; // Games tab
            var game = vm.CreateGameFromFolder(gameName);
            vm.SelectedGame = game;
        }

        var win = new Views.SettingsWindow
        {
            DataContext = vm
        };

        if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            win.ShowDialog(desktop.MainWindow);
        }
    }

    [RelayCommand]
    public void ToggleLibraryExpanded()
    {
        IsLibraryExpanded = !IsLibraryExpanded;
    }

    [RelayCommand]
    public void ToggleTagsExpanded()
    {
        IsTagsExpanded = !IsTagsExpanded;
    }

    // Context Menu Commands

    [RelayCommand]
    public async Task RenameClip(ClipViewModel? vm)
    {
        if (vm == null) return;
        ContextClip = vm;
        
        // Show rename dialog
        if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            var dialog = new Views.RenameDialog(vm.FileName);
            var result = await dialog.ShowDialog<string?>(desktop.MainWindow);
            
            if (!string.IsNullOrWhiteSpace(result) && result != vm.FileName)
            {
                try
                {
                    var dir = Path.GetDirectoryName(vm.Model.FilePath);
                    var newPath = Path.Combine(dir!, result);
                    
                    // Rename the file
                    File.Move(vm.Model.FilePath, newPath);
                    
                    // Update model
                    vm.Model.FilePath = newPath;
                    vm.Model.FileName = result;
                    vm.RefreshFileProperties();
                }
                catch (Exception ex)
                {
                    // Show error - in production would use proper dialog
                    Console.WriteLine($"Rename failed: {ex.Message}");
                }
            }
        }
    }

    [RelayCommand]
    public async Task EditTags(ClipViewModel? vm)
    {
        if (vm == null) return;
        ContextClip = vm;
        
        if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            var dialog = new Views.EditTagsDialog(vm.Model.Tags);
            var result = await dialog.ShowDialog<List<string>?>(desktop.MainWindow);
            
            if (result != null)
            {
                vm.Model.Tags = result;
                await _scannerService.SaveClipAsync(vm.Model);
                
                // Update gallery tags
                Gallery.RefreshTags();
            }
        }
    }

    [RelayCommand]
    public async Task SetRating(string ratingStr)
    {
        if (ContextClip == null) return;
        
        if (int.TryParse(ratingStr, out var rating))
        {
            ContextClip.Rating = rating;
        }
    }

    [RelayCommand]
    public async Task MoveToGame(ClipViewModel? vm)
    {
        if (vm == null) return;
        ContextClip = vm;
        
        if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            var dialog = new Views.MoveToGameDialog(Gallery.Games.ToList(), vm.GameName);
            var result = await dialog.ShowDialog<string?>(desktop.MainWindow);
            
            if (!string.IsNullOrWhiteSpace(result) && result != vm.GameName)
            {
                try
                {
                    // Find the library path that contains this clip
                    var currentDir = Path.GetDirectoryName(vm.Model.FilePath)!;
                    var libraryPath = _settingsService.Settings.LibraryPaths
                        .FirstOrDefault(p => vm.Model.FilePath.StartsWith(p, StringComparison.OrdinalIgnoreCase));
                    
                    if (libraryPath != null)
                    {
                        var newGameDir = Path.Combine(libraryPath, result);
                        Directory.CreateDirectory(newGameDir);
                        
                        var newPath = Path.Combine(newGameDir, vm.Model.FileName);
                        File.Move(vm.Model.FilePath, newPath);
                        
                        // Update model
                        vm.Model.FilePath = newPath;
                        vm.Model.GameName = result;
                        vm.RefreshFileProperties();
                        
                        // Refresh games list
                        Gallery.RefreshGames();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Move failed: {ex.Message}");
                }
            }
        }
    }

    [RelayCommand]
    public void ShowInExplorer(ClipViewModel? vm)
    {
        if (vm == null) return;
        
        try
        {
            var dir = Path.GetDirectoryName(vm.Model.FilePath);
            if (dir != null && Directory.Exists(dir))
            {
                // Cross-platform open folder
                if (OperatingSystem.IsWindows())
                {
                    Process.Start("explorer.exe", $"/select,\"{vm.Model.FilePath}\"");
                }
                else if (OperatingSystem.IsMacOS())
                {
                    Process.Start("open", $"-R \"{vm.Model.FilePath}\"");
                }
                else if (OperatingSystem.IsLinux())
                {
                    Process.Start("xdg-open", dir);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Show in explorer failed: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task DeleteClip(ClipViewModel? vm)
    {
        if (vm == null) return;
        
        // In production, show confirmation dialog first
        try
        {
            if (File.Exists(vm.Model.FilePath))
            {
                File.Delete(vm.Model.FilePath);
            }
            
            // Also delete sidecar and thumbnail if they exist
            if (File.Exists(vm.Model.SidecarPath))
            {
                File.Delete(vm.Model.SidecarPath);
            }
            if (File.Exists(vm.Model.ThumbnailPath))
            {
                File.Delete(vm.Model.ThumbnailPath);
            }
            
            // Remove from gallery
            Gallery.Clips.Remove(vm);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Delete failed: {ex.Message}");
        }
    }
}
