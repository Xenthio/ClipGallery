using CommunityToolkit.Mvvm.ComponentModel;
using ClipGallery.Core.Models;
using ClipGallery.Core.Services;
using Avalonia.Media.Imaging;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

namespace ClipGallery.UI.ViewModels;

public partial class ClipViewModel : ObservableObject
{
    [ObservableProperty]
    private Clip _model;

    [ObservableProperty]
    private Bitmap? _thumbnail;

    [ObservableProperty]
    private bool _isLoadingThumbnail = false;

    [ObservableProperty]
    private bool _isThumbnailFailed = false;
    
    [ObservableProperty]
    private bool _hasThumbnailFile = false;

    private readonly IClipScannerService? _scannerService;
    private CancellationTokenSource? _loadCts;

    [ObservableProperty]
    private string _durationDisplay = "--:--";

    public int Rating
    {
        get => Model.Rating ?? 0;
        set
        {
            if (Model.Rating != value)
            {
                Model.Rating = value;
                OnPropertyChanged();
                if (_scannerService != null)
                {
                    _ = _scannerService.SaveClipAsync(Model);
                }
            }
        }
    }

    public string FileName => Model.FileName;
    
    /// <summary>
    /// The actual folder name (used for filtering/moving)
    /// </summary>
    public string GameName => Model.GameName;
    
    /// <summary>
    /// Display name - can be an alias if game merging is configured
    /// </summary>
    [ObservableProperty]
    private string _displayGameName = "";

    public ClipViewModel(Clip clip, IClipScannerService? scannerService = null)
    {
        _model = clip;
        _scannerService = scannerService;
        _displayGameName = clip.GameName; // Default to actual name
        UpdateDuration();
        // Check if thumbnail file exists (doesn't load it yet)
        HasThumbnailFile = File.Exists(clip.ThumbnailPath);
    }

    public void UpdateDuration()
    {
        if (Model.DurationSeconds > 0)
        {
            var t = TimeSpan.FromSeconds(Model.DurationSeconds);
            DurationDisplay = $"{(int)t.TotalMinutes}:{t.Seconds:D2}";
        }
    }
    
    /// <summary>
    /// Mark that thumbnail file now exists (called after generation)
    /// </summary>
    public void MarkThumbnailReady()
    {
        HasThumbnailFile = File.Exists(Model.ThumbnailPath);
    }

    /// <summary>
    /// Notify that a property has changed. Used after modifying Model properties directly.
    /// </summary>
    public new void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
    }

    /// <summary>
    /// Refresh file-related properties after rename/move
    /// </summary>
    public void RefreshFileProperties()
    {
        OnPropertyChanged(nameof(FileName));
        OnPropertyChanged(nameof(GameName));
    }

    /// <summary>
    /// Load thumbnail from disk. Call when clip becomes visible.
    /// </summary>
    public async Task LoadThumbnailAsync()
    {
        // Already loaded or loading
        if (Thumbnail != null || IsLoadingThumbnail) return;
        
        // Cancel and dispose any previous load attempt
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = new CancellationTokenSource();
        var ct = _loadCts.Token;
        
        IsLoadingThumbnail = true;
        IsThumbnailFailed = false;

        if (File.Exists(Model.ThumbnailPath))
        {
            try
            {
                await Task.Run(() =>
                {
                    if (ct.IsCancellationRequested) return;
                    
                    using var stream = File.OpenRead(Model.ThumbnailPath);
                    // Decode to small size to minimize memory usage
                    // 160px width is enough for card display
                    var bitmap = Bitmap.DecodeToWidth(stream, 160);
                    
                    if (!ct.IsCancellationRequested)
                    {
                        Thumbnail = bitmap;
                        HasThumbnailFile = true;
                    }
                    else
                    {
                        bitmap.Dispose();
                    }
                }, ct);
            }
            catch (OperationCanceledException)
            {
                // Cancelled, ignore
            }
            catch
            {
                IsThumbnailFailed = true;
            }
        }
        else
        {
            // No thumbnail file yet - will be generated later
            HasThumbnailFile = false;
        }

        IsLoadingThumbnail = false;
    }
    
    /// <summary>
    /// Unload thumbnail to free memory. Call when clip is no longer visible.
    /// </summary>
    public void UnloadThumbnail()
    {
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = null;
        var oldThumbnail = Thumbnail;
        Thumbnail = null;
        IsLoadingThumbnail = false;
        oldThumbnail?.Dispose();
    }
}
