using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClipGallery.Core.Models;
using ClipGallery.Core.Services;
using LibVLCSharp.Shared;
using NAudio.Wave;
using Avalonia.Controls;
using System;
using System.Threading.Tasks; // Added
using System.Linq; // Added
using System.Collections.Generic; // Added
using System.IO; // Added

namespace ClipGallery.UI.ViewModels;

public partial class PlayerViewModel : ObservableObject, IDisposable
{
    private readonly LibVLC _libVlc;
    private readonly MediaPlayer _mediaPlayer;
    private Media? _media;
    private readonly IAudioExtractionService _audioService;
    private readonly IClipScannerService _scannerService; // Injected
    private readonly ITranscodeService _transcodeService; // Injected
    private readonly Task _initializationTask;

    private IWavePlayer? _secondaryPlayer;
    private AudioFileReader? _secondaryAudioFileReader;

    [ObservableProperty]
    private ClipViewModel _currentClip;

    [ObservableProperty] private string _tagsInput = "";
    [ObservableProperty] private int _ratingInput = 0;

    [ObservableProperty] private double _trimStart;
    [ObservableProperty] private double _trimEnd;
    [ObservableProperty] private double _videoDuration;

    // Export Presets
    public List<ExportPreset> ExportPresets { get; } = Enum.GetValues<ExportPreset>().ToList();
    [ObservableProperty] private ExportPreset _selectedPreset = ExportPreset.Lossless;

    [ObservableProperty]
    private MediaPlayer _player; // Exposed for View binding

    [ObservableProperty]
    private double _volume = 100;

    [ObservableProperty]
    private double _position; // 0-1 for Slider

    // Audio Volume Controls
    [ObservableProperty] private int _mainVolume = 100;
    [ObservableProperty] private float _secondaryVolume = 1.0f; // 0.0 to 1.0

    // Properties for timeline trim highlight
    [ObservableProperty] private GridLength _trimStartGridLength = new(0, GridUnitType.Star);
    [ObservableProperty] private GridLength _trimMiddleGridLength = new(1, GridUnitType.Star);
    [ObservableProperty] private GridLength _trimEndGridLength = new(0, GridUnitType.Star);

    partial void OnTrimStartChanged(double value) => UpdateTrimVisuals();
    partial void OnTrimEndChanged(double value) => UpdateTrimVisuals();
    partial void OnVideoDurationChanged(double value) => UpdateTrimVisuals();

    private void UpdateTrimVisuals()
    {
        if (VideoDuration <= 0)
        {
            TrimStartGridLength = new GridLength(0, GridUnitType.Star);
            TrimMiddleGridLength = new GridLength(1, GridUnitType.Star);
            TrimEndGridLength = new GridLength(0, GridUnitType.Star);
            return;
        }

        var start = Math.Clamp(TrimStart / VideoDuration, 0, 1);
        var end = Math.Clamp(TrimEnd / VideoDuration, 0, 1);
        var middle = Math.Max(0, end - start);
        var endPad = Math.Max(0, 1 - end);

        TrimStartGridLength = new GridLength(start, GridUnitType.Star);
        TrimMiddleGridLength = new GridLength(middle, GridUnitType.Star);
        TrimEndGridLength = new GridLength(endPad, GridUnitType.Star);
    }

    [RelayCommand]
    public void SeekToTrimStart() => _mediaPlayer.Time = (long)(TrimStart * 1000);

    [RelayCommand]
    public void SeekToTrimEnd() => _mediaPlayer.Time = (long)(TrimEnd * 1000);

    public void OnTrimDragging(double value)
    {
        // Seek to the position being dragged for frame preview
        if (_mediaPlayer.IsSeekable)
        {
            _mediaPlayer.Time = (long)(value * 1000);
        }
    }

    public PlayerViewModel(ClipViewModel clipVM, IAudioExtractionService audioService, IClipScannerService scannerService, ITranscodeService transcodeService)
    {
        _currentClip = clipVM;
        _audioService = audioService;
        _scannerService = scannerService;
        _transcodeService = transcodeService;

        _tagsInput = string.Join(", ", _currentClip.Model.Tags);
        _ratingInput = _currentClip.Model.Rating ?? 0;

        _trimEnd = _currentClip.Model.DurationSeconds > 0 ? _currentClip.Model.DurationSeconds : 10;
        UpdateTrimVisuals();

        // Initialize LibVLC with options to embed video in the window
        _libVlc = new LibVLC(
            "--no-video-title-show",
            "--no-osd"
        );
        _mediaPlayer = new MediaPlayer(_libVlc)
        {
            EnableHardwareDecoding = true
        };
        Player = _mediaPlayer; // Bindable property
        _initializationTask = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        _media = new Media(_libVlc, new Uri(CurrentClip.Model.FilePath));
        _mediaPlayer.Media = _media;

        await LoadAudioTracks(); // Call the new method
    }

    private bool _playbackStarted;
    private readonly object _playbackStartLock = new();

    public async Task StartPlaybackAsync()
    {
        lock (_playbackStartLock)
        {
            if (_playbackStarted) return;
            _playbackStarted = true;
        }

        await _initializationTask;

        // Setup Sync (rough implementation)
        _mediaPlayer.TimeChanged += OnVlcTimeChanged;
        _mediaPlayer.Paused += OnMediaPlayerPaused;
        _mediaPlayer.Playing += OnMediaPlayerPlaying;
        _mediaPlayer.Stopped += OnMediaPlayerStopped;

        _mediaPlayer.Play();
        _secondaryPlayer?.Play();
    }

    private void OnMediaPlayerPaused(object? sender, EventArgs e) => _secondaryPlayer?.Pause();
    private void OnMediaPlayerPlaying(object? sender, EventArgs e) => _secondaryPlayer?.Play();
    private void OnMediaPlayerStopped(object? sender, EventArgs e) => _secondaryPlayer?.Stop();

    private void SetupSecondaryAudio(string path)
    {
        _secondaryPlayer = new WaveOutEvent();
        _secondaryAudioFileReader = new AudioFileReader(path);
        _secondaryPlayer.Init(_secondaryAudioFileReader);
        _secondaryAudioFileReader.Volume = SecondaryVolume;
    }

    [ObservableProperty]
    private string _currentTimeDisplay = "00:00";

    [ObservableProperty]
    private string _totalTimeDisplay = "00:00";

    private bool _isUpdatingPosition;

    private void OnVlcTimeChanged(object? sender, MediaPlayerTimeChangedEventArgs e)
    {
        // Update Position (0-1)
        if (_mediaPlayer.Length > 0)
        {
            _isUpdatingPosition = true;
            Position = (double)e.Time / _mediaPlayer.Length;
            _isUpdatingPosition = false;
        }

        // Update Time Display
        var t = TimeSpan.FromMilliseconds(e.Time);
        CurrentTimeDisplay = $"{(int)t.TotalMinutes}:{t.Seconds:D2}";

        // Ensure Total is set (sometimes Length is known only after playing starts)
        // Optimization: Don't set Total every frame if it hasn't changed.
        if (TotalTimeDisplay == "00:00" && _mediaPlayer.Length > 0)
        {
            var d = TimeSpan.FromMilliseconds(_mediaPlayer.Length);
            TotalTimeDisplay = $"{(int)d.TotalMinutes}:{d.Seconds:D2}";
            VideoDuration = _mediaPlayer.Length / 1000.0; // Set for TrimRangeSlider
        }

        // Simple Sync: Check if drift > 100ms
        if (_secondaryAudioFileReader != null)
        {
            var vlcTime = TimeSpan.FromMilliseconds(e.Time);
            var audioTime = _secondaryAudioFileReader.CurrentTime;

            if (Math.Abs((vlcTime - audioTime).TotalMilliseconds) > 100)
            {
                _secondaryAudioFileReader.CurrentTime = vlcTime;
            }
        }
    }

    // Triggered when UI Slider changes Position
    partial void OnPositionChanged(double value)
    {
        if (!_isUpdatingPosition && _mediaPlayer.IsSeekable)
        {
            _mediaPlayer.Position = (float)value;
        }
    }

    partial void OnMainVolumeChanged(int value)
    {
        _mediaPlayer.Volume = value;
    }

    partial void OnSecondaryVolumeChanged(float value)
    {
        if (_secondaryAudioFileReader != null)
        {
            _secondaryAudioFileReader.Volume = value;
        }
    }

    private async Task LoadAudioTracks()
    {
        if (CurrentClip.Model.HasMultiTrackAudio)
        {
            try
            {
                var tracks = await _audioService.ExtractAudioTracksAsync(CurrentClip.Model.FilePath);
                // Assign to ObservableCollection if bound
            }
            catch { /* Ignore */ }
        }
    }

    [RelayCommand]
    public void TogglePlayPause()
    {
        if (_mediaPlayer.IsPlaying)
        {
            _mediaPlayer.Pause();
        }
        else
        {
            _mediaPlayer.Play();
        }
    }

    /// <summary>
    /// Seek relative to current position by the specified seconds (can be negative)
    /// </summary>
    public void SeekRelative(double seconds)
    {
        if (_mediaPlayer.Length <= 0) return;

        var currentMs = _mediaPlayer.Time;
        var newMs = currentMs + (long)(seconds * 1000);
        newMs = Math.Clamp(newMs, 0, _mediaPlayer.Length);
        _mediaPlayer.Time = newMs;
    }

    // Fix UpdateDuration logic (TimeChanged)
    private void UpdateDurationDisplay()
    {
        if (_mediaPlayer.Length > 0)
        {
            var total = TimeSpan.FromMilliseconds(_mediaPlayer.Length);
            // ...
        }
    }

    [RelayCommand]
    public async Task SaveMetadata()
    {
        CurrentClip.Rating = RatingInput == 0 ? 0 : RatingInput;
        // Tags
        CurrentClip.Model.Tags = TagsInput.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        // Save via ScannerService is NOT needed if we update ClipViewModel properties, BUT ClipViewModel doesn't expose Tags yet.
        // So we update Model directly and Save.
        // CurrentClip is ClipViewModel.
        // CurrentClip.Rating setter handles auto-save for Rating.
        // For Tags, we need to save manually or update ClipViewModel to handle Tags.
        // Let's manually save for now.
        await _scannerService.SaveClipAsync(CurrentClip.Model);
    }

    [RelayCommand]
    public void SetTrimStart()
    {
        TrimStart = _mediaPlayer.Time / 1000.0;
    }

    [RelayCommand]
    public void SetTrimEnd()
    {
        TrimEnd = _mediaPlayer.Time / 1000.0;
    }

    [RelayCommand]
    public async Task ExportClip()
    {
        var ext = SelectedPreset == ExportPreset.Lossless ? Path.GetExtension(CurrentClip.Model.FilePath) : ".mp4";
        var exportPath = Path.Combine(Path.GetDirectoryName(CurrentClip.Model.FilePath)!,
            $"{Path.GetFileNameWithoutExtension(CurrentClip.FileName)}_trim_{SelectedPreset}_{DateTime.Now:HHmmss}{ext}");

        await _transcodeService.TrimClipAsync(CurrentClip.Model, TrimStart, TrimEnd, exportPath, SelectedPreset);
    }

    public void Dispose()
    {
        // Unsubscribe all events first to avoid callbacks during disposal
        if (_playbackStarted)
        {
            _mediaPlayer.TimeChanged -= OnVlcTimeChanged;
            _mediaPlayer.Paused -= OnMediaPlayerPaused;
            _mediaPlayer.Playing -= OnMediaPlayerPlaying;
            _mediaPlayer.Stopped -= OnMediaPlayerStopped;
        }

        // Stop playback before disposing to prevent ExecutionEngineException
        try
        {
            if (_mediaPlayer.IsPlaying)
            {
                _mediaPlayer.Stop();
            }
        }
        catch { /* Ignore errors during shutdown */ }

        // Dispose secondary audio first
        _secondaryPlayer?.Stop();
        _secondaryPlayer?.Dispose();
        _secondaryAudioFileReader?.Dispose();

        _mediaPlayer.Media = null;
        _media?.Dispose();
        _media = null;

        // Dispose VLC resources last
        _mediaPlayer.Dispose();
        _libVlc.Dispose();
    }
}
