using CommunityToolkit.Mvvm.ComponentModel;
using ClipGallery.Core.Models;
using ClipGallery.Core.Services;
using Avalonia.Media.Imaging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ClipGallery.UI.ViewModels;

public partial class ClipViewModel : ObservableObject
{
    [ObservableProperty]
    private Clip _model;

    [ObservableProperty]
    private Bitmap? _thumbnail;

    [ObservableProperty]
    private bool _isLoadingThumbnail = true;

    [ObservableProperty]
    private bool _isThumbnailFailed = false;

    private readonly IClipScannerService? _scannerService;

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

    public async Task LoadThumbnailAsync()
    {
        IsLoadingThumbnail = true;

        if (File.Exists(Model.ThumbnailPath))
        {
            try
            {
                // Decode to 320px width to save MASSIVE amount of memory
                // Full 4k images -> ~30MB raw. 320px -> ~100KB.
                await Task.Run(() =>
                {
                    using var stream = File.OpenRead(Model.ThumbnailPath);
                    var bitmap = Bitmap.DecodeToWidth(stream, 320);

                    // Update on UI Thread? ObservableProperty usually handles this if called from mapped task context
                    // But Bitmap creation might need UI thread or be immutable. 
                    // Avalonia Bitmaps are generally thread-safe for creation but binding update checks.
                    Thumbnail = bitmap;
                });
            }
            catch
            {
                IsThumbnailFailed = true;
            }
        }
        else
        {
            IsThumbnailFailed = true;
        }

        IsLoadingThumbnail = false;
    }
}
