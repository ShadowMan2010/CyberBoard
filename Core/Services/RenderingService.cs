using CyberBoard.Models;
using SkiaSharp;

namespace CyberBoard.Core.Services;

public class RenderingService
{
    private readonly Dictionary<Guid, SKPicture> _layerCache = new();
    private readonly Dictionary<Guid, SKPicture> _strokeCache = new();
    private SKSurface? _surface;
    private int _surfaceWidth;
    private int _surfaceHeight;

    public int MaxCacheSize { get; set; } = 50;

    public void ResizeSurface(int width, int height)
    {
        if (width == _surfaceWidth && height == _surfaceHeight) return;
        _surface?.Dispose();
        _surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
        _surfaceWidth = width;
        _surfaceHeight = height;
    }

    public void Render(SKCanvas canvas, WhiteboardPage page, SKRect viewport, float zoom)
    {
        canvas.Clear(page.BackgroundColor);
        canvas.Save();
        canvas.Scale(zoom);
        canvas.Translate(-viewport.Left, -viewport.Top);

        DrawPaper(canvas, page, viewport);

        var visibleLayers = page.Layers
            .Where(l => l.IsVisible)
            .OrderBy(l => l.Order);

        foreach (var layer in visibleLayers)
        {
            if (layer.Strokes.Count == 0) continue;
            DrawLayer(canvas, layer, viewport);
        }

        canvas.Restore();
    }

    private void DrawLayer(SKCanvas canvas, DrawingLayer layer, SKRect viewport)
    {
        if (Math.Abs(layer.Opacity) < 0.001f) return;

        canvas.Save();
        var paint = new SKPaint { ColorF = new SKColorF(1, 1, 1, layer.Opacity), BlendMode = GetBlendMode(layer.BlendMode) };
        canvas.SaveLayer(paint);
        paint.Dispose();

        foreach (var stroke in layer.Strokes)
        {
            if (stroke.Points.Count < 2) continue;
            if (!stroke.GetBounds().IntersectsWith(viewport)) continue;
            DrawStroke(canvas, stroke);
        }

        canvas.Restore();
        canvas.Restore();
    }

    private void DrawStroke(SKCanvas canvas, DrawingStroke stroke)
    {
        using var paint = new SKPaint
        {
            Color = stroke.Color.WithAlpha((byte)(stroke.Color.Alpha * stroke.Opacity)),
            StrokeWidth = stroke.StrokeWidth,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
            FilterQuality = SKFilterQuality.High,
            IsLinearText = true
        };

        if (stroke.IsEraser)
        {
            paint.BlendMode = SKBlendMode.Clear;
            paint.StrokeWidth = stroke.StrokeWidth * 5;
        }

        using var path = new SKPath();
        path.MoveTo(stroke.Points[0].X, stroke.Points[0].Y);

        if (stroke.Points.Count == 2)
        {
            path.LineTo(stroke.Points[1].X, stroke.Points[1].Y);
        }
        else if (stroke.Points.Count >= 3)
        {
            for (int i = 1; i < stroke.Points.Count - 1; i++)
            {
                var p0 = stroke.Points[i - 1];
                var p1 = stroke.Points[i];
                var p2 = stroke.Points[i + 1];
                var midX = (p1.X + p2.X) / 2;
                var midY = (p1.Y + p2.Y) / 2;
                path.QuadTo(p1.X, p1.Y, midX, midY);
            }
            var last = stroke.Points[^1];
            var prev = stroke.Points[^2];
            path.LineTo(last.X, last.Y);
        }

        canvas.DrawPath(path, paint);
    }

    public void RenderSelection(SKCanvas canvas, SKRect selectionRect)
    {
        using var paint = new SKPaint
        {
            Color = new SKColor(0x33, 0x99, 0xFF),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2,
            IsAntialias = true,
            PathEffect = SKPathEffect.CreateDash(new[] { 5f, 5f }, 0)
        };
        canvas.DrawRect(selectionRect, paint);

        for (float x = selectionRect.Left; x <= selectionRect.Right; x += 10)
        {
            for (float y = selectionRect.Top; y <= selectionRect.Bottom; y += 10)
            {
                var handle = new SKRect(x - 3, y - 3, x + 3, y + 3);
                if (x == selectionRect.Left || x == selectionRect.Right ||
                    y == selectionRect.Top || y == selectionRect.Bottom)
                {
                    canvas.DrawRect(handle, new SKPaint
                    {
                        Color = SKColors.White,
                        Style = SKPaintStyle.Fill,
                        StrokeWidth = 1
                    });
                    canvas.DrawRect(handle, new SKPaint
                    {
                        Color = new SKColor(0x33, 0x99, 0xFF),
                        Style = SKPaintStyle.Stroke,
                        StrokeWidth = 1
                    });
                }
            }
        }
    }

    public void RenderGrid(SKCanvas canvas, WhiteboardPage page, SKRect viewport, float zoom)
    {
        if (page.PaperType == PaperType.Blank) return;

        using var paint = new SKPaint
        {
            Color = new SKColor(0, 0, 0, 30),
            StrokeWidth = 0.5f,
            IsAntialias = true
        };

        float gridSize = page.PaperType switch
        {
            PaperType.Grid => 20f,
            PaperType.DotGrid => 20f,
            PaperType.Ruled => 24f,
            PaperType.Graph => 10f,
            _ => 20f
        };

        float startX = (float)(Math.Floor(viewport.Left / gridSize) * gridSize);
        float startY = (float)(Math.Floor(viewport.Top / gridSize) * gridSize);
        float endX = viewport.Right;
        float endY = viewport.Bottom;

        if (page.PaperType == PaperType.DotGrid)
        {
            for (float x = startX; x <= endX; x += gridSize)
                for (float y = startY; y <= endY; y += gridSize)
                    canvas.DrawCircle(x, y, 1.5f, paint);
        }
        else
        {
            for (float x = startX; x <= endX; x += gridSize)
                canvas.DrawLine(x, startY, x, endY, paint);
            for (float y = startY; y <= endY; y += gridSize)
                canvas.DrawLine(startX, y, endX, y, paint);
        }
    }

    private void DrawPaper(SKCanvas canvas, WhiteboardPage page, SKRect viewport)
    {
        switch (page.PaperType)
        {
            case PaperType.Grid:
            case PaperType.DotGrid:
            case PaperType.Ruled:
            case PaperType.Graph:
                RenderGrid(canvas, page, viewport, 1f);
                break;
        }
    }

    private static SKBlendMode GetBlendMode(LayerBlendMode mode) => mode switch
    {
        LayerBlendMode.Multiply => SKBlendMode.Multiply,
        LayerBlendMode.Screen => SKBlendMode.Screen,
        LayerBlendMode.Overlay => SKBlendMode.Overlay,
        LayerBlendMode.Darken => SKBlendMode.Darken,
        LayerBlendMode.Lighten => SKBlendMode.Lighten,
        LayerBlendMode.ColorDodge => SKBlendMode.ColorDodge,
        LayerBlendMode.ColorBurn => SKBlendMode.ColorBurn,
        LayerBlendMode.SoftLight => SKBlendMode.SoftLight,
        LayerBlendMode.HardLight => SKBlendMode.HardLight,
        LayerBlendMode.Difference => SKBlendMode.Difference,
        LayerBlendMode.Exclusion => SKBlendMode.Exclusion,
        _ => SKBlendMode.SrcOver
    };

    public void InvalidateStroke(Guid strokeId)
    {
        _strokeCache.Remove(strokeId);
    }

    public void InvalidateLayer(Guid layerId)
    {
        _layerCache.Remove(layerId);
    }

    public void ClearCache()
    {
        foreach (var pic in _layerCache.Values) pic.Dispose();
        foreach (var pic in _strokeCache.Values) pic.Dispose();
        _layerCache.Clear();
        _strokeCache.Clear();
    }

    public void Dispose()
    {
        ClearCache();
        _surface?.Dispose();
    }
}
