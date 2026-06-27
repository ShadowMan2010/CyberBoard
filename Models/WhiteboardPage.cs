using SkiaSharp;

namespace CyberBoard.Models;

public class WhiteboardPage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Page 1";
    public PaperType PaperType { get; set; } = PaperType.Blank;
    public SKColor BackgroundColor { get; set; } = SKColors.White;
    public List<DrawingLayer> Layers { get; set; } = new();
    public float CanvasWidth { get; set; } = 10000;
    public float CanvasHeight { get; set; } = 10000;
    public float Zoom { get; set; } = 1f;
    public float PanX { get; set; }
    public float PanY { get; set; }
    public float Rotation { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    public WhiteboardPage()
    {
        var layer = new DrawingLayer { Name = "Background", Order = 0 };
        Layers.Add(layer);
    }
}
