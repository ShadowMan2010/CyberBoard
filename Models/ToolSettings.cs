using CommunityToolkit.Mvvm.ComponentModel;

namespace CyberBoard.Models;

public partial class ToolSettings : ObservableObject
{
    [ObservableProperty] private DrawingToolType _activeTool = DrawingToolType.Pen;
    [ObservableProperty] private float _strokeWidth = 2f;
    [ObservableProperty] private float _opacity = 1f;
    [ObservableProperty] private float _hardness = 0.5f;
    [ObservableProperty] private byte _red = 0;
    [ObservableProperty] private byte _green = 0;
    [ObservableProperty] private byte _blue = 0;
    [ObservableProperty] private bool _enablePressure = true;
    [ObservableProperty] private bool _enableStabilizer;
    [ObservableProperty] private float _stabilizerStrength = 10f;
    [ObservableProperty] private ShapeType _selectedShape = ShapeType.Rectangle;
    [ObservableProperty] private bool _snapToGrid;
    [ObservableProperty] private float _gridSize = 20f;
}
