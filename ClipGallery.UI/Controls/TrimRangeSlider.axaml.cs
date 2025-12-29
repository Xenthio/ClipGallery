using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;

namespace ClipGallery.UI.Controls;

public partial class TrimRangeSlider : UserControl
{
    public static readonly StyledProperty<double> MinimumProperty =
        AvaloniaProperty.Register<TrimRangeSlider, double>(nameof(Minimum), 0);

    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<TrimRangeSlider, double>(nameof(Maximum), 100);

    public static readonly StyledProperty<double> StartValueProperty =
        AvaloniaProperty.Register<TrimRangeSlider, double>(nameof(StartValue), 0, coerce: CoerceStartValue);

    public static readonly StyledProperty<double> EndValueProperty =
        AvaloniaProperty.Register<TrimRangeSlider, double>(nameof(EndValue), 100, coerce: CoerceEndValue);

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

    // Events for seeking preview
    public event EventHandler<double>? StartValueDragging;
    public event EventHandler<double>? EndValueDragging;
    public event EventHandler<double>? StartValueChanged;
    public event EventHandler<double>? EndValueChanged;

    private Grid? _trackArea;
    private Grid? _startHandle;
    private Grid? _endHandle;
    private Border? _rangeHighlight;
    private Border? _startTooltip;
    private Border? _endTooltip;
    private TextBlock? _startTimeText;
    private TextBlock? _endTimeText;
    private TextBlock? _minLabel;
    private TextBlock? _maxLabel;

    private bool _isDraggingStart;
    private bool _isDraggingEnd;

    public TrimRangeSlider()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        _trackArea = this.FindControl<Grid>("TrackArea");
        _startHandle = this.FindControl<Grid>("StartHandle");
        _endHandle = this.FindControl<Grid>("EndHandle");
        _rangeHighlight = this.FindControl<Border>("RangeHighlight");
        _startTooltip = this.FindControl<Border>("StartTooltip");
        _endTooltip = this.FindControl<Border>("EndTooltip");
        _startTimeText = this.FindControl<TextBlock>("StartTimeText");
        _endTimeText = this.FindControl<TextBlock>("EndTimeText");
        _minLabel = this.FindControl<TextBlock>("MinLabel");
        _maxLabel = this.FindControl<TextBlock>("MaxLabel");

        if (_startHandle != null)
        {
            _startHandle.PointerPressed += OnStartHandlePressed;
            _startHandle.PointerMoved += OnStartHandleMoved;
            _startHandle.PointerReleased += OnStartHandleReleased;
            _startHandle.PointerCaptureLost += OnStartHandleCaptureLost;
        }

        if (_endHandle != null)
        {
            _endHandle.PointerPressed += OnEndHandlePressed;
            _endHandle.PointerMoved += OnEndHandleMoved;
            _endHandle.PointerReleased += OnEndHandleReleased;
            _endHandle.PointerCaptureLost += OnEndHandleCaptureLost;
        }

        UpdateHandlePositions();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == MinimumProperty || change.Property == MaximumProperty ||
            change.Property == StartValueProperty || change.Property == EndValueProperty)
        {
            UpdateHandlePositions();
        }
    }

    private static double CoerceStartValue(AvaloniaObject obj, double value)
    {
        var slider = (TrimRangeSlider)obj;
        return Math.Clamp(value, slider.Minimum, slider.EndValue);
    }

    private static double CoerceEndValue(AvaloniaObject obj, double value)
    {
        var slider = (TrimRangeSlider)obj;
        return Math.Clamp(value, slider.StartValue, slider.Maximum);
    }

    private void UpdateHandlePositions()
    {
        if (_trackArea == null || _startHandle == null || _endHandle == null || _rangeHighlight == null)
            return;

        // Calculate handle width offset (handle is 24px, centered on position)
        const double handleHalfWidth = 12;
        var trackWidth = _trackArea.Bounds.Width - 24; // Account for handle widths
        if (trackWidth <= 0) trackWidth = 1;

        var range = Maximum - Minimum;
        if (range <= 0) range = 1;

        var startRatio = (StartValue - Minimum) / range;
        var endRatio = (EndValue - Minimum) / range;

        var startX = startRatio * trackWidth;
        var endX = endRatio * trackWidth;

        Canvas.SetLeft(_startHandle, startX);
        Canvas.SetLeft(_endHandle, endX);

        // Update range highlight
        _rangeHighlight.Margin = new Thickness(startX + handleHalfWidth, 0, 0, 0);
        _rangeHighlight.Width = Math.Max(0, endX - startX);

        // Update time labels
        if (_minLabel != null) _minLabel.Text = FormatTime(Minimum);
        if (_maxLabel != null) _maxLabel.Text = FormatTime(Maximum);
        if (_startTimeText != null) _startTimeText.Text = FormatTime(StartValue);
        if (_endTimeText != null) _endTimeText.Text = FormatTime(EndValue);
    }

    private static string FormatTime(double seconds)
    {
        var ts = TimeSpan.FromSeconds(seconds);
        return ts.TotalHours >= 1
            ? $"{(int)ts.TotalHours}:{ts.Minutes:D2}:{ts.Seconds:D2}"
            : $"{(int)ts.TotalMinutes}:{ts.Seconds:D2}";
    }

    private void OnStartHandlePressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _isDraggingStart = true;
            e.Pointer.Capture(_startHandle);
            if (_startTooltip != null) _startTooltip.IsVisible = true;
            e.Handled = true;
        }
    }

    private void OnStartHandleMoved(object? sender, PointerEventArgs e)
    {
        if (!_isDraggingStart || _trackArea == null) return;

        var pos = e.GetPosition(_trackArea);
        var trackWidth = _trackArea.Bounds.Width - 24;
        if (trackWidth <= 0) return;

        var ratio = Math.Clamp(pos.X / trackWidth, 0, 1);
        var newValue = Minimum + ratio * (Maximum - Minimum);
        newValue = Math.Min(newValue, EndValue - 0.1); // Keep at least 0.1s gap

        StartValue = newValue;
        StartValueDragging?.Invoke(this, newValue);
    }

    private void OnStartHandleReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isDraggingStart)
        {
            _isDraggingStart = false;
            e.Pointer.Capture(null);
            if (_startTooltip != null) _startTooltip.IsVisible = false;
            StartValueChanged?.Invoke(this, StartValue);
        }
    }

    private void OnStartHandleCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        _isDraggingStart = false;
        if (_startTooltip != null) _startTooltip.IsVisible = false;
    }

    private void OnEndHandlePressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _isDraggingEnd = true;
            e.Pointer.Capture(_endHandle);
            if (_endTooltip != null) _endTooltip.IsVisible = true;
            e.Handled = true;
        }
    }

    private void OnEndHandleMoved(object? sender, PointerEventArgs e)
    {
        if (!_isDraggingEnd || _trackArea == null) return;

        var pos = e.GetPosition(_trackArea);
        var trackWidth = _trackArea.Bounds.Width - 24;
        if (trackWidth <= 0) return;

        var ratio = Math.Clamp(pos.X / trackWidth, 0, 1);
        var newValue = Minimum + ratio * (Maximum - Minimum);
        newValue = Math.Max(newValue, StartValue + 0.1); // Keep at least 0.1s gap

        EndValue = newValue;
        EndValueDragging?.Invoke(this, newValue);
    }

    private void OnEndHandleReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isDraggingEnd)
        {
            _isDraggingEnd = false;
            e.Pointer.Capture(null);
            if (_endTooltip != null) _endTooltip.IsVisible = false;
            EndValueChanged?.Invoke(this, EndValue);
        }
    }

    private void OnEndHandleCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        _isDraggingEnd = false;
        if (_endTooltip != null) _endTooltip.IsVisible = false;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var result = base.MeasureOverride(availableSize);
        // Schedule layout update after measure
        Avalonia.Threading.Dispatcher.UIThread.Post(UpdateHandlePositions);
        return result;
    }
}
