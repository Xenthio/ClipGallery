using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;

namespace ClipGallery.UI.Controls;

public partial class TrimSlider : UserControl
{
    private bool _isDraggingStart = false;
    private bool _isDraggingEnd = false;
    private Border? _startThumb;
    private Border? _endThumb;
    private Border? _trimmedRegion;
    private Canvas? _timelineCanvas;

    public static readonly StyledProperty<double> MinimumProperty =
        AvaloniaProperty.Register<TrimSlider, double>(nameof(Minimum), defaultValue: 0);

    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<TrimSlider, double>(nameof(Maximum), defaultValue: 100);

    public static readonly StyledProperty<double> StartValueProperty =
        AvaloniaProperty.Register<TrimSlider, double>(nameof(StartValue), defaultValue: 0);

    public static readonly StyledProperty<double> EndValueProperty =
        AvaloniaProperty.Register<TrimSlider, double>(nameof(EndValue), defaultValue: 100);

    public double Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public double StartValue
    {
        get => GetValue(StartValueProperty);
        set => SetValue(StartValueProperty, value);
    }

    public double EndValue
    {
        get => GetValue(EndValueProperty);
        set => SetValue(EndValueProperty, value);
    }

    // Events for value changes
    public event EventHandler<double>? StartValueChanged;
    public event EventHandler<double>? EndValueChanged;
    public event EventHandler<double>? StartValueChanging;
    public event EventHandler<double>? EndValueChanging;

    public TrimSlider()
    {
        InitializeComponent();
        
        // Add loaded event to get references to controls
        Loaded += TrimSlider_Loaded;
    }

    private void TrimSlider_Loaded(object? sender, RoutedEventArgs e)
    {
        _startThumb = this.FindControl<Border>("StartThumb");
        _endThumb = this.FindControl<Border>("EndThumb");
        _trimmedRegion = this.FindControl<Border>("TrimmedRegion");
        _timelineCanvas = this.FindControl<Canvas>("TimelineCanvas");

        UpdateVisuals();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        
        if (change.Property == StartValueProperty || 
            change.Property == EndValueProperty ||
            change.Property == MinimumProperty ||
            change.Property == MaximumProperty)
        {
            UpdateVisuals();
        }
    }

    private void UpdateVisuals()
    {
        if (_timelineCanvas == null || _startThumb == null || _endThumb == null || _trimmedRegion == null)
            return;

        var width = _timelineCanvas.Bounds.Width;
        if (width <= 0) return;

        var range = Maximum - Minimum;
        if (range <= 0) return;

        // Calculate positions
        var startPos = ((StartValue - Minimum) / range) * width;
        var endPos = ((EndValue - Minimum) / range) * width;

        // Position thumbs
        Canvas.SetLeft(_startThumb, Math.Max(0, startPos - 8));
        Canvas.SetLeft(_endThumb, Math.Min(width - 16, endPos - 8));

        // Update trimmed region
        var regionLeft = startPos;
        var regionWidth = Math.Max(0, endPos - startPos);
        Canvas.SetLeft(_trimmedRegion, regionLeft);
        _trimmedRegion.Width = regionWidth;

        // Update time labels
        var startTime = this.FindControl<TextBlock>("StartTime");
        var endTime = this.FindControl<TextBlock>("EndTime");
        var durationText = this.FindControl<TextBlock>("DurationText");

        if (startTime != null)
            startTime.Text = FormatTime(StartValue);
        if (endTime != null)
            endTime.Text = FormatTime(EndValue);
        if (durationText != null)
        {
            var duration = EndValue - StartValue;
            durationText.Text = $"Duration: {FormatTime(duration)}";
        }
    }

    private string FormatTime(double seconds)
    {
        var ts = TimeSpan.FromSeconds(seconds);
        return $"{(int)ts.TotalMinutes}:{ts.Seconds:D2}.{ts.Milliseconds / 100}";
    }

    private void StartThumb_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _isDraggingStart = true;
        if (_startThumb != null)
        {
            _startThumb.PointerMoved += Thumb_PointerMoved;
            _startThumb.PointerReleased += Thumb_PointerReleased;
            _startThumb.PointerCaptureLost += Thumb_PointerCaptureLost;
        }
        e.Handled = true;
    }

    private void EndThumb_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _isDraggingEnd = true;
        if (_endThumb != null)
        {
            _endThumb.PointerMoved += Thumb_PointerMoved;
            _endThumb.PointerReleased += Thumb_PointerReleased;
            _endThumb.PointerCaptureLost += Thumb_PointerCaptureLost;
        }
        e.Handled = true;
    }

    private void Thumb_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (_timelineCanvas == null) return;
        if (!_isDraggingStart && !_isDraggingEnd) return;

        var pos = e.GetPosition(_timelineCanvas);
        var width = _timelineCanvas.Bounds.Width;
        
        if (width <= 0) return;

        var range = Maximum - Minimum;
        var value = Minimum + (pos.X / width) * range;
        value = Math.Clamp(value, Minimum, Maximum);

        if (_isDraggingStart)
        {
            // Don't allow start to go past end
            value = Math.Min(value, EndValue - 0.1);
            StartValue = value;
            StartValueChanging?.Invoke(this, value);
        }
        else if (_isDraggingEnd)
        {
            // Don't allow end to go before start
            value = Math.Max(value, StartValue + 0.1);
            EndValue = value;
            EndValueChanging?.Invoke(this, value);
        }

        e.Handled = true;
    }

    private void Thumb_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        CleanupDragging();
        e.Handled = true;
    }

    private void Thumb_PointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        CleanupDragging();
    }

    private void CleanupDragging()
    {
        if (_isDraggingStart)
        {
            StartValueChanged?.Invoke(this, StartValue);
            if (_startThumb != null)
            {
                _startThumb.PointerMoved -= Thumb_PointerMoved;
                _startThumb.PointerReleased -= Thumb_PointerReleased;
                _startThumb.PointerCaptureLost -= Thumb_PointerCaptureLost;
            }
        }
        else if (_isDraggingEnd)
        {
            EndValueChanged?.Invoke(this, EndValue);
            if (_endThumb != null)
            {
                _endThumb.PointerMoved -= Thumb_PointerMoved;
                _endThumb.PointerReleased -= Thumb_PointerReleased;
                _endThumb.PointerCaptureLost -= Thumb_PointerCaptureLost;
            }
        }

        _isDraggingStart = false;
        _isDraggingEnd = false;
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        UpdateVisuals();
    }
}
