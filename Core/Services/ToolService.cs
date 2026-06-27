using CyberBoard.Models;
using SkiaSharp;

namespace CyberBoard.Core.Services;

public class ToolService
{
    private readonly DocumentManager _document;
    private readonly ToolSettings _settings = new();
    private readonly List<StrokePoint> _currentStrokePoints = new();
    private readonly List<StrokePoint> _stabilizedPoints = new();
    private DrawingStroke? _currentStroke;
    private bool _isDrawing;

    public ToolSettings Settings => _settings;
    public bool IsDrawing => _isDrawing;
    public IReadOnlyList<StrokePoint> CurrentStrokePoints => _currentStrokePoints;

    public event EventHandler<DrawingStroke>? StrokeCompleted;
    public event EventHandler? StrokeStarted;
    public event EventHandler? CurrentPointChanged;

    public ToolService(DocumentManager document)
    {
        _document = document;
    }

    public void StartStroke(float x, float y, float pressure = 0.5f)
    {
        _isDrawing = true;
        _currentStrokePoints.Clear();
        _stabilizedPoints.Clear();

        var point = new StrokePoint(x, y, pressure);
        _currentStrokePoints.Add(point);
        _stabilizedPoints.Add(point);

        _currentStroke = new DrawingStroke
        {
            ToolType = _settings.ActiveTool,
            Color = new SKColor(_settings.Red, _settings.Green, _settings.Blue, (byte)(_settings.Opacity * 255)),
            StrokeWidth = _settings.StrokeWidth,
            Opacity = _settings.Opacity,
            Hardness = _settings.Hardness,
            IsEraser = _settings.ActiveTool == DrawingToolType.Eraser ||
                       _settings.ActiveTool == DrawingToolType.ObjectEraser ||
                       _settings.ActiveTool == DrawingToolType.StrokeEraser
        };

        StrokeStarted?.Invoke(this, EventArgs.Empty);
    }

    public void ContinueStroke(float x, float y, float pressure = 0.5f)
    {
        if (!_isDrawing || _currentStroke == null) return;

        var point = new StrokePoint(x, y, pressure);
        _currentStrokePoints.Add(point);

        if (_settings.EnableStabilizer)
            AddStabilizedPoint(point);
        else
            _stabilizedPoints.Add(point);

        _currentStroke.Points = new List<StrokePoint>(_stabilizedPoints);
        CurrentPointChanged?.Invoke(this, EventArgs.Empty);
    }

    public void EndStroke()
    {
        if (!_isDrawing || _currentStroke == null) return;

        _currentStroke.Points = new List<StrokePoint>(_stabilizedPoints);

        if (_currentStroke.Points.Count >= 2)
        {
            _currentStroke.Bounds = _currentStroke.GetBounds();
            _document.AddStroke(_currentStroke);
            StrokeCompleted?.Invoke(this, _currentStroke);
        }

        _currentStroke = null;
        _isDrawing = false;
        _currentStrokePoints.Clear();
        _stabilizedPoints.Clear();
    }

    public void CancelStroke()
    {
        _isDrawing = false;
        _currentStroke = null;
        _currentStrokePoints.Clear();
        _stabilizedPoints.Clear();
    }

    public DrawingStroke? CreateShapeStroke(ShapeType shapeType, float x1, float y1, float x2, float y2)
    {
        var stroke = new DrawingStroke
        {
            ToolType = DrawingToolType.Pen,
            Color = new SKColor(_settings.Red, _settings.Green, _settings.Blue, (byte)(_settings.Opacity * 255)),
            StrokeWidth = _settings.StrokeWidth,
            Opacity = _settings.Opacity
        };

        var points = new List<StrokePoint>();
        switch (shapeType)
        {
            case ShapeType.Line:
                points.Add(new StrokePoint(x1, y1));
                points.Add(new StrokePoint(x2, y2));
                break;

            case ShapeType.Rectangle:
                points.Add(new StrokePoint(x1, y1));
                points.Add(new StrokePoint(x2, y1));
                points.Add(new StrokePoint(x2, y2));
                points.Add(new StrokePoint(x1, y2));
                points.Add(new StrokePoint(x1, y1));
                break;

            case ShapeType.Circle:
                var cx = (x1 + x2) / 2;
                var cy = (y1 + y2) / 2;
                var rx = Math.Abs(x2 - x1) / 2;
                var ry = Math.Abs(y2 - y1) / 2;
                for (int i = 0; i <= 64; i++)
                {
                    var angle = 2 * Math.PI * i / 64;
                    points.Add(new StrokePoint(
                        cx + rx * (float)Math.Cos(angle),
                        cy + ry * (float)Math.Sin(angle)));
                }
                break;

            case ShapeType.Triangle:
                var mx = (x1 + x2) / 2;
                points.Add(new StrokePoint(mx, y1));
                points.Add(new StrokePoint(x2, y2));
                points.Add(new StrokePoint(x1, y2));
                points.Add(new StrokePoint(mx, y1));
                break;

            case ShapeType.Diamond:
                var dx = (x1 + x2) / 2;
                var dy = (y1 + y2) / 2;
                points.Add(new StrokePoint(dx, y1));
                points.Add(new StrokePoint(x2, dy));
                points.Add(new StrokePoint(dx, y2));
                points.Add(new StrokePoint(x1, dy));
                points.Add(new StrokePoint(dx, y1));
                break;

            case ShapeType.Arrow:
                points.Add(new StrokePoint(x1, y1));
                points.Add(new StrokePoint(x2, y2));
                var angle2 = Math.Atan2(y2 - y1, x2 - x1);
                var arrowLen = 15f;
                points.Add(new StrokePoint(
                    x2 - arrowLen * (float)Math.Cos(angle2 - 0.5),
                    y2 - arrowLen * (float)Math.Sin(angle2 - 0.5)));
                points.Add(new StrokePoint(x2, y2));
                points.Add(new StrokePoint(
                    x2 - arrowLen * (float)Math.Cos(angle2 + 0.5),
                    y2 - arrowLen * (float)Math.Sin(angle2 + 0.5)));
                break;
        }

        stroke.Points = points;
        stroke.Bounds = stroke.GetBounds();
        return stroke;
    }

    private void AddStabilizedPoint(StrokePoint point)
    {
        if (_stabilizedPoints.Count == 0)
        {
            _stabilizedPoints.Add(point);
            return;
        }

        var last = _stabilizedPoints[^1];
        var strength = _settings.StabilizerStrength;

        var stabilized = new StrokePoint(
            last.X + (point.X - last.X) / strength,
            last.Y + (point.Y - last.Y) / strength,
            point.Pressure);

        _stabilizedPoints.Add(stabilized);
    }

    public DrawingStroke? HitTest(float x, float y, float radius = 5f)
    {
        foreach (var layer in _document.CurrentPage.Layers.Reverse<DrawingLayer>())
        {
            if (!layer.IsVisible || layer.IsLocked) continue;
            foreach (var stroke in layer.Strokes.AsEnumerable().Reverse())
            {
                var bounds = stroke.GetBounds();
                bounds.Inflate(radius, radius);
                if (bounds.Contains(x, y))
                {
                    for (int i = 0; i < stroke.Points.Count - 1; i++)
                    {
                        var p1 = stroke.Points[i];
                        var p2 = stroke.Points[i + 1];
                        var dist = DistanceToSegment(x, y, p1.X, p1.Y, p2.X, p2.Y);
                        if (dist < radius) return stroke;
                    }
                }
            }
        }
        return null;
    }

    public List<DrawingStroke> HitTestRect(SKRect rect)
    {
        var result = new List<DrawingStroke>();
        foreach (var layer in _document.CurrentPage.Layers.Reverse<DrawingLayer>())
        {
            if (!layer.IsVisible || layer.IsLocked) continue;
            foreach (var stroke in layer.Strokes)
            {
                if (rect.IntersectsWith(stroke.GetBounds()))
                    result.Add(stroke);
            }
        }
        return result;
    }

    private static float DistanceToSegment(float px, float py, float x1, float y1, float x2, float y2)
    {
        var dx = x2 - x1;
        var dy = y2 - y1;
        var lengthSq = dx * dx + dy * dy;
        if (lengthSq == 0) return MathF.Sqrt((px - x1) * (px - x1) + (py - y1) * (py - y1));
        var t = Math.Clamp(((px - x1) * dx + (py - y1) * dy) / lengthSq, 0f, 1f);
        var projX = x1 + t * dx;
        var projY = y1 + t * dy;
        return MathF.Sqrt((px - projX) * (px - projX) + (py - projY) * (py - projY));
    }
}
