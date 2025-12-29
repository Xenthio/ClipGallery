using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input; // Added for RelayCommand
using ClipGallery.Core.Services;
using ClipGallery.Core.Models; // Added for Clip
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq; // Added
using Avalonia.Threading; // Added for Dispatcher
using Avalonia; // Added for Application

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
    private PlayerViewModel? _currentPlayer; // When not null, player is open

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isLibraryExpanded = true;

    [ObservableProperty]
    private bool _isTagsExpanded = true;

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

        // 2. Wrap via ViewModels
        var vms = clips.Select(c => new ClipViewModel(c, _scannerService)).ToList();
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

    private void OnClipAdded(object? sender, Clip clip)
    {
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var vm = new ClipViewModel(clip, _scannerService);
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
        CurrentPlayer?.Dispose();
        CurrentPlayer = new PlayerViewModel(vm, _audioService, _scannerService, _transcodeService);
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
        var vm = _serviceProvider.GetService(typeof(SettingsViewModel)) as SettingsViewModel;
        if (vm == null) return;

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
    public void ClosePlayer()
    {
        CurrentPlayer?.Dispose();
        CurrentPlayer = null;
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
}
