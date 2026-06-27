using CyberBoard.Models;
using SkiaSharp;

namespace CyberBoard.Core.Services;

public class ThemeService
{
    private ThemeMode _currentMode = ThemeMode.Auto;

    public event EventHandler<ThemeMode>? ThemeChanged;

    public ThemeMode CurrentMode
    {
        get => _currentMode;
        set
        {
            _currentMode = value;
            ThemeChanged?.Invoke(this, value);
        }
    }

    public bool IsDark
    {
        get
        {
            return _currentMode switch
            {
                ThemeMode.Dark => true,
                ThemeMode.Light => false,
                _ => DetectSystemTheme()
            };
        }
    }

    public SKColor BackgroundColor => IsDark ? new SKColor(0x1E, 0x1E, 0x1E) : new SKColor(0xFF, 0xFF, 0xFF);
    public SKColor SurfaceColor => IsDark ? new SKColor(0x2D, 0x2D, 0x2D) : new SKColor(0xF5, 0xF5, 0xF5);
    public SKColor TextColor => IsDark ? new SKColor(0xFF, 0xFF, 0xFF) : new SKColor(0x00, 0x00, 0x00);
    public SKColor AccentColor => new(0x33, 0x99, 0xFF);
    public SKColor BorderColor => IsDark ? new SKColor(0x3D, 0x3D, 0x3D) : new SKColor(0xE0, 0xE0, 0xE0);

    public string AvaloniaTheme => IsDark ? "Dark" : "Light";

    private static bool DetectSystemTheme()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                var key = "HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize";
                var val = Microsoft.Win32.Registry.GetValue(key, "AppsUseLightTheme", "1");
                if (val is int i) return i == 0;
            }
        }
        catch { }
        return false;
    }
}
