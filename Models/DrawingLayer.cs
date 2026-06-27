namespace CyberBoard.Models;

public class DrawingLayer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Layer";
    public float Opacity { get; set; } = 1f;
    public bool IsVisible { get; set; } = true;
    public bool IsLocked { get; set; }
    public LayerBlendMode BlendMode { get; set; } = LayerBlendMode.Normal;
    public int Order { get; set; }
    public List<DrawingStroke> Strokes { get; set; } = new();

    public DrawingLayer Clone()
    {
        return new DrawingLayer
        {
            Name = Name + " (Copy)",
            Opacity = Opacity,
            IsVisible = IsVisible,
            IsLocked = IsLocked,
            BlendMode = BlendMode,
            Order = Order,
            Strokes = Strokes.Select(s => s.Clone()).ToList()
        };
    }
}
