using SkiaSharp;

namespace CyberBoard.Models;

public class DrawingStroke
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DrawingToolType ToolType { get; set; } = DrawingToolType.Pen;
    public List<StrokePoint> Points { get; set; } = new();
    public SKColor Color { get; set; } = SKColors.Black;
    public float StrokeWidth { get; set; } = 2f;
    public float Opacity { get; set; } = 1f;
    public float Hardness { get; set; } = 0.5f;
    public bool IsEraser { get; set; }
    public Guid LayerId { get; set; }
    public bool IsSelected { get; set; }
    public SKRect Bounds { get; set; }
    public float Rotation { get; set; }
    public float ScaleX { get; set; } = 1f;
    public float ScaleY { get; set; } = 1f;
    public float TranslateX { get; set; }
    public float TranslateY { get; set; }

    public SKRect GetBounds()
    {
        if (Points.Count == 0) return SKRect.Empty;
        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;
        foreach (var p in Points)
        {
            if (p.X < minX) minX = p.X;
            if (p.Y < minY) minY = p.Y;
            if (p.X > maxX) maxX = p.X;
            if (p.Y > maxY) maxY = p.Y;
        }
        float margin = StrokeWidth * 2;
        return new SKRect(minX - margin, minY - margin, maxX + margin, maxY + margin);
    }

    public DrawingStroke Clone()
    {
        return new DrawingStroke
        {
            ToolType = ToolType,
            Points = new List<StrokePoint>(Points),
            Color = Color,
            StrokeWidth = StrokeWidth,
            Opacity = Opacity,
            Hardness = Hardness,
            IsEraser = IsEraser,
            LayerId = LayerId
        };
    }
}
