using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using CyberBoard.Core.Services;
using CyberBoard.Models;
using SkiaSharp;

namespace CyberBoard.Controls;

public class SkiaCanvas : Control
{
    private readonly DocumentManager _document;
    private readonly RenderingService _renderer;
    private readonly ToolService _tool;
    private bool _isPointerPressed;
    private Point _lastPointerPos;
    private Point _pointerPos;
    private Point _dragStart;
    private float _zoom = 1f;
    private float _panX;
    private float _panY;
    private bool _isPanning;

    public SkiaCanvas()
    {
        _document = App.Services?.GetService(typeof(DocumentManager)) as DocumentManager
            ?? throw new InvalidOperationException("DocumentManager not found");
        _renderer = App.Services?.GetService(typeof(RenderingService)) as RenderingService
            ?? throw new InvalidOperationException("RenderingService not found");
        _tool = App.Services?.GetService(typeof(ToolService)) as ToolService
            ?? throw new InvalidOperationException("ToolService not found");

        ClipToBounds = true;
        Focusable = true;

        _document.DocumentChanged += (_, _) => InvalidateVisual();
        _document.CurrentPageChanged += (_, _) =>
        {
            var page = _document.CurrentPage;
            _zoom = page.Zoom;
            _panX = page.PanX;
            _panY = page.PanY;
            InvalidateVisual();
        };
        _document.LayersChanged += (_, _) => InvalidateVisual();
        _document.StrokesChanged += (_, _) => InvalidateVisual();
    }

    public void InvalidateCanvas()
    {
        InvalidateVisual();
    }

    public (float worldX, float worldY) ScreenToWorld(Point screen)
    {
        var w = (float)Bounds.Width;
        var h = (float)Bounds.Height;
        var worldX = _panX + ((float)screen.X - w / 2) / _zoom;
        var worldY = _panY + ((float)screen.Y - h / 2) / _zoom;
        return (worldX, worldY);
    }

    public (float screenX, float screenY) WorldToScreen(float worldX, float worldY)
    {
        var w = (float)Bounds.Width;
        var h = (float)Bounds.Height;
        var sx = (worldX - _panX) * _zoom + w / 2;
        var sy = (worldY - _panY) * _zoom + h / 2;
        return (sx, sy);
    }

    private void RenderFrame(DrawingContext context)
    {
        var w = (int)Bounds.Width;
        var h = (int)Bounds.Height;
        if (w <= 0 || h <= 0) return;

        var page = _document.CurrentPage;
        var viewport = new SKRect(
            _panX - w / (2 * _zoom),
            _panY - h / (2 * _zoom),
            _panX + w / (2 * _zoom),
            _panY + h / (2 * _zoom));

        var info = new SKImageInfo(w, h, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        if (surface == null) return;

        var canvas = surface.Canvas;
        canvas.Clear(new SKColor(0xF0, 0xF0, 0xF0));
        _renderer.Render(canvas, page, viewport, _zoom);

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var ms = new MemoryStream(data.ToArray());
        var bitmap = new Avalonia.Media.Imaging.Bitmap(ms);

        var rect = new Rect(0, 0, w, h);
        context.DrawImage(bitmap, rect, rect);
    }

    public override void Render(DrawingContext context)
    {
        RenderFrame(context);
        base.Render(context);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        _isPointerPressed = true;
        _lastPointerPos = e.GetPosition(this);
        _pointerPos = _lastPointerPos;
        _dragStart = _lastPointerPos;

        var (wx, wy) = ScreenToWorld(_lastPointerPos);
        var tool = _tool.Settings.ActiveTool;

        if (tool == DrawingToolType.Pan || e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            _isPanning = true;
            return;
        }

        if (tool == DrawingToolType.Eraser)
        {
            var stroke = _tool.HitTest(wx, wy);
            if (stroke != null) _document.RemoveStroke(stroke);
            return;
        }

        if (IsShapeTool(tool))
        {
            return;
        }

        _tool.StartStroke(wx, wy);
        e.Pointer.Capture(this);
        InvalidateVisual();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        _pointerPos = e.GetPosition(this);
        var (wx, wy) = ScreenToWorld(_pointerPos);

        if (_isPanning && _isPointerPressed)
        {
            var dx = (float)(_pointerPos.X - _lastPointerPos.X) / _zoom;
            var dy = (float)(_pointerPos.Y - _lastPointerPos.Y) / _zoom;
            _panX -= dx;
            _panY -= dy;
            _lastPointerPos = _pointerPos;
            _document.CurrentPage.PanX = _panX;
            _document.CurrentPage.PanY = _panY;
            InvalidateVisual();
            return;
        }

        if (_isPointerPressed && _tool.IsDrawing)
        {
            _tool.ContinueStroke(wx, wy);
            InvalidateVisual();
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        var (wx, wy) = ScreenToWorld(e.GetPosition(this));
        var tool = _tool.Settings.ActiveTool;

        if (_isPanning)
        {
            _isPanning = false;
            _isPointerPressed = false;
            return;
        }

        if (IsShapeTool(tool) && _isPointerPressed)
        {
            var (sx, sy) = ScreenToWorld(_dragStart);
            var shapeStroke = _tool.CreateShapeStroke(GetShapeType(tool), sx, sy, wx, wy);
            if (shapeStroke != null) _document.AddStroke(shapeStroke);
            _isPointerPressed = false;
            InvalidateVisual();
            return;
        }

        if (_isPointerPressed && _tool.IsDrawing)
        {
            _tool.EndStroke();
            _isPointerPressed = false;
            InvalidateVisual();
        }
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        var delta = e.Delta.Y;
        var oldZoom = _zoom;
        _zoom = delta > 0 ? Math.Clamp(_zoom * 1.15f, 0.0625f, 32f)
                          : Math.Clamp(_zoom / 1.15f, 0.0625f, 32f);

        var pos = e.GetPosition(this);
        var (worldX, worldY) = ScreenToWorld(pos);
        _panX = worldX + ((float)Bounds.Width / 2 - (float)pos.X) / _zoom;
        _panY = worldY + ((float)Bounds.Height / 2 - (float)pos.Y) / _zoom;

        _document.CurrentPage.Zoom = _zoom;
        _document.CurrentPage.PanX = _panX;
        _document.CurrentPage.PanY = _panY;
        InvalidateVisual();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape && _tool.IsDrawing)
        {
            _tool.CancelStroke();
            _isPointerPressed = false;
            InvalidateVisual();
        }
        base.OnKeyDown(e);
    }

    private static bool IsShapeTool(DrawingToolType tool) => tool switch
    {
        DrawingToolType.Rectangle or DrawingToolType.Circle or
        DrawingToolType.Triangle or DrawingToolType.Arrow or
        DrawingToolType.Diamond or DrawingToolType.Line => true,
        _ => false
    };

    private static ShapeType GetShapeType(DrawingToolType tool) => tool switch
    {
        DrawingToolType.Rectangle => ShapeType.Rectangle,
        DrawingToolType.Circle => ShapeType.Circle,
        DrawingToolType.Triangle => ShapeType.Triangle,
        DrawingToolType.Arrow => ShapeType.Arrow,
        DrawingToolType.Diamond => ShapeType.Diamond,
        DrawingToolType.Line => ShapeType.Line,
        _ => ShapeType.Rectangle
    };
}
