using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClipGallery.Core.Models;
using ClipGallery.Core.Services;
using LibVLCSharp.Shared;
using NAudio.Wave;
using System;
using System.Threading.Tasks; // Added
using System.Linq; // Added
using System.Collections.Generic; // Added
using System.IO; // Added
using System.Threading;

namespace ClipGallery.UI.ViewModels;

public partial class PlayerViewModel : ObservableObject, IDisposable
{
    private readonly LibVLC _libVlc;
    private readonly MediaPlayer _mediaPlayer;
    private readonly IAudioExtractionService _audioService;
    private readonly IClipScannerService _scannerService; // Injected
    private readonly ITranscodeService _transcodeService; // Injected
    private readonly Task _initTask;
    private Media? _media;

    private IWavePlayer? _secondaryPlayer;
    private AudioFileReader? _secondaryAudioFileReader;

    [ObservableProperty]
    private ClipViewModel _currentClip;

    [ObservableProperty] private string _tagsInput = "";
    [ObservableProperty] private int _ratingInput = 0;

    [ObservableProperty] private double _trimStart;
    [ObservableProperty] private double _trimEnd;
    [ObservableProperty] private double _durationSeconds;

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

    public PlayerViewModel(ClipViewModel clipVM, IAudioExtractionService audioService, IClipScannerService scannerService, ITranscodeService transcodeService)
    {
        _currentClip = clipVM;
        _audioService = audioService;
        _scannerService = scannerService;
        _transcodeService = transcodeService;

        _tagsInput = string.Join(", ", _currentClip.Model.Tags);
        _ratingInput = _currentClip.Model.Rating ?? 0;

        _durationSeconds = _currentClip.Model.DurationSeconds > 0 ? _currentClip.Model.DurationSeconds : 10;
        _trimEnd = _durationSeconds;

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

        _initTask = InitializeAsync();
    }

    private Task InitializeAsync()
    {
        _media = new Media(_libVlc, new Uri(CurrentClip.Model.FilePath));
        _mediaPlayer.Media = _media;

        _ = LoadAudioTracks() // Fire-and-forget so playback isn't blocked while optional tracks are extracted
            .ContinueWith(t => Console.WriteLine($"Audio track extraction failed: {t.Exception?.GetBaseException().Message}"),
                TaskContinuationOptions.OnlyOnFaulted);

        // Setup Sync (rough implementation)
        _mediaPlayer.TimeChanged += OnVlcTimeChanged;
        _mediaPlayer.Paused += (s, e) => _secondaryPlayer?.Pause();
        _mediaPlayer.Playing += (s, e) => _secondaryPlayer?.Play();
        _mediaPlayer.Stopped += (s, e) => _secondaryPlayer?.Stop();

        return Task.CompletedTask;
    }

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
    private int _isSyncingTrim;
    private bool _hasAppliedDuration;

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
        }

        if (!_hasAppliedDuration && _mediaPlayer.Length > 0)
        {
            _hasAppliedDuration = true;
            var duration = _mediaPlayer.Length / 1000.0;
            DurationSeconds = duration;

            if (Interlocked.CompareExchange(ref _isSyncingTrim, 1, 0) == 0)
            {
                try
                {
                    if (TrimEnd <= 0 || TrimEnd > duration)
                    {
                        TrimEnd = duration;
                    }
                    if (TrimStart > TrimEnd)
                    {
                        TrimStart = TrimEnd;
                    }
                }
                finally
                {
                    Interlocked.Exchange(ref _isSyncingTrim, 0);
                }
            }
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

    partial void OnTrimStartChanged(double value)
    {
        if (Interlocked.CompareExchange(ref _isSyncingTrim, 1, 0) == 1)
        {
            // Re-entrant change triggered by our own clamping; skip to avoid infinite loops
            return;
        }

        double clamped;
        try
        {
            clamped = ClampTrimValue(value, isStart: true);
            if (Math.Abs(clamped - value) > double.Epsilon)
            {
                TrimStart = clamped;
            }
        }
        finally
        {
            Interlocked.Exchange(ref _isSyncingTrim, 0);
        }

        PreviewFrameAt(clamped);
    }

    partial void OnTrimEndChanged(double value)
    {
        if (Interlocked.CompareExchange(ref _isSyncingTrim, 1, 0) == 1)
        {
            // Re-entrant change triggered by our own clamping; skip to avoid infinite loops
            return;
        }

        double clamped;
        try
        {
            clamped = ClampTrimValue(value, isStart: false);
            if (Math.Abs(clamped - value) > double.Epsilon)
            {
                TrimEnd = clamped;
            }
        }
        finally
        {
            Interlocked.Exchange(ref _isSyncingTrim, 0);
        }

        PreviewFrameAt(clamped);
    }

    private double ClampTrimValue(double value, bool isStart)
    {
        var max = DurationSeconds > 0 ? DurationSeconds : double.MaxValue;
        var clamped = Math.Clamp(value, 0, max);

        if (isStart && clamped > TrimEnd)
        {
            clamped = TrimEnd;
        }
        else if (!isStart && clamped < TrimStart)
        {
            clamped = TrimStart;
        }

        return clamped;
    }

    private void PreviewFrameAt(double seconds)
    {
        if (!_mediaPlayer.IsSeekable || _mediaPlayer.Length <= 0) return;

        var targetMs = (long)(seconds * 1000);
        targetMs = Math.Clamp(targetMs, 0, _mediaPlayer.Length);
        _mediaPlayer.Time = targetMs;
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
            _ = StartPlaybackAsync();
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

    public async Task StartPlaybackAsync()
    {
        try
        {
            await _initTask;
        }
        catch (Exception ex)
        {
            // Initialization failed; surface the error but don't crash the app from fire-and-forget calls
            Console.WriteLine($"Player initialization failed: {ex.Message}");
            return;
        }
        if (_mediaPlayer.IsPlaying) return;

        _mediaPlayer.Play();
        _secondaryPlayer?.Play();
    }

    public void Dispose()
    {
        _mediaPlayer.Dispose();
        _media?.Dispose();
        _libVlc.Dispose();
        _secondaryPlayer?.Dispose();
        _secondaryAudioFileReader?.Dispose();
    }
}
