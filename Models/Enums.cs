namespace CyberBoard.Models;

public enum DrawingToolType
{
    Pen, Pencil, Marker, Brush, Highlighter, CalligraphyPen,
    Airbrush, Chalk, Watercolor, PixelBrush, LaserPointer,
    Eraser, ObjectEraser, StrokeEraser,
    SelectionTool, Lasso, RectangleSelection, MagicSelection,
    Eyedropper, BucketFill, GradientFill,
    Rectangle, Circle, Triangle, Arrow, Diamond, Star, Polygon,
    Line, Curve, BezierCurve,
    Text, StickyNote,
    Pan, Zoom
}

public enum ShapeType
{
    Rectangle, Circle, Triangle, Arrow, Diamond, Star, Polygon,
    Line, Curve, BezierCurve
}

public enum LayerBlendMode
{
    Normal, Multiply, Screen, Overlay, Darken, Lighten,
    ColorDodge, ColorBurn, SoftLight, HardLight,
    Difference, Exclusion
}

public enum ThemeMode
{
    Auto, Light, Dark
}

public enum PaperType
{
    Blank, Grid, DotGrid, Ruled, Graph
}

public enum CanvasOrientation
{
    Portrait, Landscape
}
