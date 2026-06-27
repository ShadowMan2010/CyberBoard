namespace CyberBoard.Models;

public record struct StrokePoint(float X, float Y, float Pressure = 0.5f, float TiltX = 0, float TiltY = 0, float Velocity = 0);
