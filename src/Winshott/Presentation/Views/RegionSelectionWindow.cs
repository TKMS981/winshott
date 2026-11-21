using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Winshott.Domain.Models;
using CaptureRect = Winshott.Domain.Models.PixelRect;

namespace Winshott.Presentation.Views;

/// <summary>Lets the user mark a region on a frozen display frame, so the selection UI cannot enter the captured pixels.</summary>
public sealed class RegionSelectionWindow : Window
{
    private readonly Canvas _canvas = new();
    private readonly Rectangle _selection = new() { Stroke = Brushes.DodgerBlue, StrokeThickness = 2, Fill = new SolidColorBrush(Color.FromArgb(55, 30, 110, 255)), IsVisible = false };
    private readonly double _pixelScale;
    private Point? _start;
    private CaptureRect? _result;

    public RegionSelectionWindow(CapturedImage image, double pixelScale)
    {
        _pixelScale = pixelScale;
        Title = "Select capture region — Enter to capture · Esc to cancel";
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        CanResize = false;
        SizeToContent = SizeToContent.WidthAndHeight;
        Background = Brushes.Black;
        var source = new Bitmap(new MemoryStream(image.Data, writable: false));
        var imageWidth = image.Width / Math.Max(pixelScale, 1);
        var imageHeight = image.Height / Math.Max(pixelScale, 1);
        var fit = Math.Min(1, Math.Min(1080 / imageWidth, 700 / imageHeight));
        var width = imageWidth * fit;
        var height = imageHeight * fit;
        _canvas.Width = width;
        _canvas.Height = height;
        _canvas.Children.Add(new Image { Source = source, Width = width, Height = height, Stretch = Stretch.Fill, IsHitTestVisible = false });
        _canvas.Children.Add(_selection);
        Content = new StackPanel
        {
            Spacing = 8,
            Margin = new Thickness(12),
            Children =
            {
                new TextBlock { Text = "Drag over the frozen display image. Press Enter to capture or Esc to cancel.", Foreground = Brushes.White },
                _canvas
            }
        };
        _canvas.PointerPressed += OnPointerPressed;
        _canvas.PointerMoved += OnPointerMoved;
        _canvas.PointerReleased += OnPointerReleased;
        KeyDown += OnKeyDown;
        Closed += (_, _) => source.Dispose();
        Tag = fit;
    }

    public CaptureRect? SelectedRegion => _result;

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(_canvas).Properties.IsLeftButtonPressed) return;
        _start = Clamp(e.GetPosition(_canvas));
        _selection.IsVisible = true;
        UpdateSelection(_start.Value, _start.Value);
        e.Pointer.Capture(_canvas);
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_start is { } start) UpdateSelection(start, Clamp(e.GetPosition(_canvas)));
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_start is not { } start) return;
        var end = Clamp(e.GetPosition(_canvas));
        UpdateSelection(start, end);
        var fit = (double)Tag!;
        var scale = fit * _pixelScale;
        var x = (int)Math.Floor(Math.Min(start.X, end.X) * scale);
        var y = (int)Math.Floor(Math.Min(start.Y, end.Y) * scale);
        var right = (int)Math.Ceiling(Math.Max(start.X, end.X) * scale);
        var bottom = (int)Math.Ceiling(Math.Max(start.Y, end.Y) * scale);
        _result = right > x && bottom > y ? new CaptureRect(x, y, right - x, bottom - y) : null;
        _start = null;
        e.Pointer.Capture(null);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) { _result = null; Close(); e.Handled = true; }
        else if (e.Key == Key.Enter && _result is { Width: > 0, Height: > 0 }) { Close(); e.Handled = true; }
    }

    private void UpdateSelection(Point start, Point end)
    {
        var left = Math.Min(start.X, end.X);
        var top = Math.Min(start.Y, end.Y);
        _selection.Width = Math.Abs(end.X - start.X);
        _selection.Height = Math.Abs(end.Y - start.Y);
        Canvas.SetLeft(_selection, left);
        Canvas.SetTop(_selection, top);
    }

    private Point Clamp(Point point) => new(Math.Clamp(point.X, 0, _canvas.Width), Math.Clamp(point.Y, 0, _canvas.Height));
}
