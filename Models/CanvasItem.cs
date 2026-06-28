using SkiaSharp;

namespace CyberBoard.Models;

public class CanvasItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LayerId { get; set; }
    public ItemType Type { get; set; } = ItemType.Image;
    public byte[]? Data { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public float Rotation { get; set; }
    public float Opacity { get; set; } = 1f;
    public bool IsSelected { get; set; }
    public string? SourcePath { get; set; }

    public SKRect Rect => new(X, Y, X + Width, Y + Height);
}

public enum ItemType
{
    Image,
    PdfPage,
    SvgImage
}
