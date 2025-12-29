using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ClipGallery.UI.Controls;

public partial class RatingControl : UserControl
{
    private List<StarModel> _stars;
    private int _hoverValue = 0;

    public static readonly StyledProperty<int> ValueProperty =
        AvaloniaProperty.Register<RatingControl, int>(nameof(Value), defaultValue: 0);

    public int Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public static readonly StyledProperty<bool> IsReadOnlyProperty =
        AvaloniaProperty.Register<RatingControl, bool>(nameof(IsReadOnly), defaultValue: false);

    public bool IsReadOnly
    {
        get => GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    public RatingControl()
    {
        InitializeComponent();

        _stars = Enumerable.Range(1, 5).Select(i => new StarModel { Index = i }).ToList();
        StarsList.ItemsSource = _stars;

        // Pointer Events
        PointerEntered += OnPointerEntered;
        PointerExited += OnPointerExited;
        PointerMoved += OnPointerMoved;
        PointerPressed += OnPointerPressed;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ValueProperty)
        {
            UpdateVisuals();
        }
    }

    private void OnPointerEntered(object? sender, PointerEventArgs e)
    {
        if (IsReadOnly) return;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (IsReadOnly) return;
        var pos = e.GetPosition(StarsList);
        var width = StarsList.Bounds.Width;
        var starWidth = width / 5;

        _hoverValue = (int)(pos.X / starWidth) + 1;
        _hoverValue = Math.Clamp(_hoverValue, 0, 5);
        UpdateVisuals(isHover: true);
    }

    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        if (IsReadOnly) return;
        _hoverValue = 0;
        UpdateVisuals(isHover: false);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (IsReadOnly) return;
        // Commit hover value
        if (_hoverValue > 0)
        {
            Value = _hoverValue;
        }
    }

    private void UpdateVisuals(bool isHover = false)
    {
        var targetValue = isHover ? _hoverValue : Value;

        foreach (var star in _stars)
        {
            if (star.Index <= targetValue)
            {
                star.Brush = Brushes.Gold;
                star.Data = Geometry.Parse("M12 17.27L18.18 21l-1.64-7.03L22 9.24l-7.19-.61L12 2 9.19 8.63 2 9.24l5.46 4.73L5.82 21z"); // Fill
            }
            else
            {
                star.Brush = Brushes.Gray;
                star.Data = Geometry.Parse("M22 9.24l-7.19-.62L12 2 9.19 8.63 2 9.24l5.46 4.73L5.82 21 12 17.27 18.18 21l-1.65-7.03L22 9.24zM12 15.4l-3.76 2.27 1-4.28-3.32-2.88 4.38-.38L12 6.1l1.71 4.04 4.38.38-3.32 2.88 1 4.28L12 15.4z"); // Outline
            }
        }
    }
}

public class StarModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    public int Index { get; set; }

    private IBrush _brush = Brushes.Gray;
    public IBrush Brush { get => _brush; set => SetProperty(ref _brush, value); }

    private Geometry? _data;
    public Geometry? Data { get => _data; set => SetProperty(ref _data, value); }
}
