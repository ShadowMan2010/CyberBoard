using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CyberBoard.Core.Services;
using CyberBoard.Models;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;

namespace CyberBoard.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly DocumentManager _document;
    private readonly ThemeService _theme;
    private readonly ToolService _toolService;
    private readonly RenderingService _renderer;
    private readonly FileService _fileService;
    private readonly ImportService _importService;
    private readonly IServiceProvider _services;

    [ObservableProperty] private string _title = "CyberBoard - Untitled";
    [ObservableProperty] private bool _isDarkMode;
    [ObservableProperty] private double _zoomLevel = 1.0;
    [ObservableProperty] private double _canvasOffsetX;
    [ObservableProperty] private double _canvasOffsetY;
    [ObservableProperty] private double _brushSize = 2.0;
    [ObservableProperty] private Color _selectedColor = Colors.Black;
    [ObservableProperty] private bool _showLayerPanel = true;
    [ObservableProperty] private bool _showToolPanel = true;
    [ObservableProperty] private bool _showStatusBar = true;
    [ObservableProperty] private bool _isFullScreen;
    [ObservableProperty] private string _statusText = "Ready";
    [ObservableProperty] private int _currentPageIndex;
    [ObservableProperty] private string _pageCount = "1 / 1";

    public ObservableCollection<DrawingLayer> Layers { get; } = new();
    public ObservableCollection<WhiteboardPage> Pages { get; } = new();
    public DrawingLayer? SelectedLayer { get; set; }

    public DocumentManager Document => _document;
    public ToolService ToolService => _toolService;
    public ThemeService Theme => _theme;
    public RenderingService Renderer => _renderer;

    public MainViewModel(DocumentManager document, ThemeService theme,
        ToolService toolService, RenderingService renderer,
        FileService fileService, ImportService importService,
        IServiceProvider services)
    {
        _document = document;
        _theme = theme;
        _toolService = toolService;
        _renderer = renderer;
        _fileService = fileService;
        _importService = importService;
        _services = services;

        _document.DocumentChanged += OnDocumentChanged;
        _document.CurrentPageChanged += OnCurrentPageChanged;
        _document.LayersChanged += OnLayersChanged;
        _document.StrokesChanged += (_, _) => OnPropertyChanged(nameof(StrokeCount));

        SyncLayers();
        SyncPages();
        UpdateTitle();
    }

    private void OnDocumentChanged(object? s, EventArgs e)
    {
        SyncLayers();
        SyncPages();
        UpdateTitle();
        OnPropertyChanged(nameof(StrokeCount));
    }

    private void OnCurrentPageChanged(object? s, EventArgs e)
    {
        SyncLayers();
        UpdatePageNav();
        ZoomLevel = _document.CurrentPage.Zoom;
        OnPropertyChanged(nameof(StrokeCount));
    }

    private void OnLayersChanged(object? s, EventArgs e)
    {
        SyncLayers();
    }

    private void SyncLayers()
    {
        Layers.Clear();
        foreach (var layer in _document.CurrentPage.Layers.OrderBy(l => l.Order))
            Layers.Add(layer);
    }

    private void SyncPages()
    {
        Pages.Clear();
        foreach (var page in _document.CurrentDocument.Pages)
            Pages.Add(page);
        UpdatePageNav();
    }

    private void UpdatePageNav() =>
        PageCount = $"{_document.CurrentDocument.CurrentPageIndex + 1} / {_document.CurrentDocument.Pages.Count}";

    private void UpdateTitle() =>
        Title = $"CyberBoard - {_document.CurrentDocument.Title}";

    public int StrokeCount => _document.CurrentPage.Layers.Sum(l => l.Strokes.Count);

    [RelayCommand]
    private void NewDocument()
    {
        _document.NewDocument();
        SyncLayers();
        SyncPages();
        UpdateTitle();
        StatusText = "New document created";
    }

    [RelayCommand]
    private void SaveDocument()
    {
        StatusText = "Saving...";
        Task.Run(async () =>
        {
            var path = _document.CurrentDocument.FilePath;
            if (string.IsNullOrEmpty(path))
            {
                path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    $"{_document.CurrentDocument.Title}.cboard");
            }
            await _fileService.SaveDocumentAsync(_document.CurrentDocument, path);
        }).ContinueWith(_ =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                StatusText = "Saved successfully");
        });
    }

    [RelayCommand]
    private async Task LoadDocument()
    {
        var window = TopLevel.GetTopLevel(
            Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow : null) as Window;
        if (window == null) return;

        var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            FileTypeFilter = new[] { new FilePickerFileType("CyberBoard Files")
            {
                Patterns = new[] { "*.cboard" }
            }}
        });

        if (files.Count > 0)
        {
            var path = files[0].Path.LocalPath;
            var doc = await _fileService.LoadDocumentAsync(path);
            if (doc != null)
            {
                var prop = typeof(DocumentManager).GetProperty("CurrentDocument",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                prop?.SetValue(_document, doc);
                _document.CurrentDocument.FilePath = path;
                OnDocumentChanged(null, EventArgs.Empty);
                StatusText = "Document loaded";
            }
        }
    }

    [RelayCommand]
    private void Undo() { _document.Undo(); StatusText = "Undo"; }

    [RelayCommand]
    private void Redo() { _document.Redo(); StatusText = "Redo"; }

    [RelayCommand]
    private void ToggleTheme()
    {
        _theme.CurrentMode = _theme.IsDark ? ThemeMode.Light : ThemeMode.Dark;
        IsDarkMode = _theme.IsDark;
    }

    [RelayCommand]
    private void ZoomIn()
    {
        ZoomLevel = Math.Min(ZoomLevel * 1.25, 32.0);
        _document.CurrentPage.Zoom = (float)ZoomLevel;
    }

    [RelayCommand]
    private void ZoomOut()
    {
        ZoomLevel = Math.Max(ZoomLevel / 1.25, 0.0625);
        _document.CurrentPage.Zoom = (float)ZoomLevel;
    }

    [RelayCommand]
    private void ZoomToFit()
    {
        ZoomLevel = 1.0;
        CanvasOffsetX = 0;
        CanvasOffsetY = 0;
        _document.CurrentPage.Zoom = 1f;
    }

    [RelayCommand]
    private void AddNewPage()
    {
        _document.AddPage();
        SyncPages();
    }

    [RelayCommand]
    private void AddNewLayer()
    {
        _document.AddLayer();
    }

    [RelayCommand]
    private void DeleteLayer(Guid layerId)
    {
        _document.DeleteLayer(layerId);
    }

    [RelayCommand]
    private void DuplicateLayer(Guid layerId)
    {
        _document.DuplicateLayer(layerId);
    }

    [RelayCommand]
    private void SelectTool(string toolName)
    {
        if (Enum.TryParse<DrawingToolType>(toolName, out var tool))
        {
            _toolService.Settings.ActiveTool = tool;
            StatusText = $"Tool: {tool}";
        }
    }

    public void SetColor(byte r, byte g, byte b)
    {
        _toolService.Settings.Red = r;
        _toolService.Settings.Green = g;
        _toolService.Settings.Blue = b;
        SelectedColor = Color.FromRgb(r, g, b);
    }

    partial void OnBrushSizeChanged(double value)
    {
        _toolService.Settings.StrokeWidth = (float)value;
    }

    partial void OnZoomLevelChanged(double value)
    {
        _document.CurrentPage.Zoom = (float)value;
    }

    public void SetStatus(string status) => StatusText = status;

    [RelayCommand]
    private async Task ImportImage()
    {
        var window = GetWindow();
        if (window == null) return;

        var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = true,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Images")
                {
                    Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.gif", "*.bmp", "*.webp" }
                },
                new FilePickerFileType("PDF Documents")
                {
                    Patterns = new[] { "*.pdf" }
                },
                new FilePickerFileType("SVG Graphics")
                {
                    Patterns = new[] { "*.svg" }
                }
            }
        });

        foreach (var file in files)
        {
            var path = file.Path.LocalPath;
            var items = await _importService.ImportFileAsync(path, 100, 100, 800);
            foreach (var item in items)
            {
                _document.PushUndo();
                _document.CurrentPage.Layers[^1].Items.Add(item);
            }
            if (items.Count > 0)
                StatusText = $"Imported {Path.GetFileName(path)}";
        }

        _renderer.InvalidateLayer(_document.CurrentPage.Layers[^1].Id);
    }

    [RelayCommand]
    private async Task PasteFromClipboard()
    {
        try
        {
            var clipboard = TopLevel.GetTopLevel(GetWindow())?.Clipboard;
            if (clipboard == null) return;

            var hasImage = await clipboard.GetFormatsAsync().ContinueWith(t =>
                t.Result?.Contains("image/png") == true || t.Result?.Contains("image/bmp") == true);

            if (hasImage)
            {
                var bitmap = await clipboard.GetDataAsync("image/png") as byte[];
                if (bitmap == null) return;

                var items = await _importService.ImportImageFromClipboardAsync(bitmap, 100, 100);
                foreach (var item in items)
                {
                    _document.PushUndo();
                    _document.CurrentPage.Layers[^1].Items.Add(item);
                }
                StatusText = items.Count > 0 ? "Pasted image from clipboard" : "Nothing to paste";
                _renderer.InvalidateLayer(_document.CurrentPage.Layers[^1].Id);
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Paste failed: {ex.Message}";
        }
    }

    private static Window? GetWindow()
    {
        return TopLevel.GetTopLevel(
            Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow : null) as Window;
    }
}
