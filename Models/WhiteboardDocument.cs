namespace CyberBoard.Models;

public class WhiteboardDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "Untitled";
    public string FilePath { get; set; } = string.Empty;
    public List<WhiteboardPage> Pages { get; set; } = new() { new WhiteboardPage() };
    public int CurrentPageIndex { get; set; }
    public ThemeMode Theme { get; set; } = ThemeMode.Auto;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    public string Version { get; set; } = "1.0.0";

    public WhiteboardPage CurrentPage => Pages[CurrentPageIndex];
}
