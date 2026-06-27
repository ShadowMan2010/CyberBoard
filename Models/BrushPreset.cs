using SkiaSharp;

namespace CyberBoard.Models;

public class BrushPreset
{
    public string Name { get; set; } = "Default";
    public DrawingToolType ToolType { get; set; } = DrawingToolType.Pen;
    public SKColor Color { get; set; } = SKColors.Black;
    public float StrokeWidth { get; set; } = 2f;
    public float Opacity { get; set; } = 1f;
    public float Hardness { get; set; } = 0.5f;
    public float Spacing { get; set; } = 0.1f;
    public bool UseTexture { get; set; }
    public byte[]? TextureData { get; set; }
    public bool IsFavorite { get; set; }
}
