using CyberBoard.Models;

namespace CyberBoard.Core.Services;

public class DocumentManager
{
    private readonly Stack<(WhiteboardDocument document, string action)> _undoStack = new();
    private readonly Stack<(WhiteboardDocument document, string action)> _redoStack = new();
    private const int MaxUndoDepth = 100;

    public WhiteboardDocument CurrentDocument { get; private set; } = new();

    public event EventHandler? DocumentChanged;
    public event EventHandler? CurrentPageChanged;
    public event EventHandler? LayersChanged;
    public event EventHandler? StrokesChanged;

    public WhiteboardPage CurrentPage => CurrentDocument.CurrentPage;

    public void NewDocument()
    {
        PushUndo();
        CurrentDocument = new WhiteboardDocument();
        OnDocumentChanged();
    }

    public void AddPage()
    {
        PushUndo();
        var page = new WhiteboardPage { Name = $"Page {CurrentDocument.Pages.Count + 1}" };
        CurrentDocument.Pages.Add(page);
        OnDocumentChanged();
    }

    public void DeletePage(int index)
    {
        if (CurrentDocument.Pages.Count <= 1) return;
        PushUndo();
        if (index >= 0 && index < CurrentDocument.Pages.Count)
        {
            CurrentDocument.Pages.RemoveAt(index);
            if (CurrentDocument.CurrentPageIndex >= CurrentDocument.Pages.Count)
                CurrentDocument.CurrentPageIndex = CurrentDocument.Pages.Count - 1;
            OnCurrentPageChanged();
        }
    }

    public void SwitchToPage(int index)
    {
        if (index >= 0 && index < CurrentDocument.Pages.Count)
        {
            CurrentDocument.CurrentPageIndex = index;
            OnCurrentPageChanged();
        }
    }

    public DrawingLayer AddLayer()
    {
        PushUndo();
        var layer = new DrawingLayer
        {
            Name = $"Layer {CurrentPage.Layers.Count}",
            Order = CurrentPage.Layers.Count
        };
        CurrentPage.Layers.Add(layer);
        OnLayersChanged();
        return layer;
    }

    public void DeleteLayer(Guid layerId)
    {
        PushUndo();
        var layer = CurrentPage.Layers.FirstOrDefault(l => l.Id == layerId);
        if (layer != null && CurrentPage.Layers.Count > 1)
        {
            CurrentPage.Layers.Remove(layer);
            OnLayersChanged();
        }
    }

    public void ReorderLayer(Guid layerId, int newOrder)
    {
        PushUndo();
        var layer = CurrentPage.Layers.FirstOrDefault(l => l.Id == layerId);
        if (layer != null)
        {
            newOrder = Math.Clamp(newOrder, 0, CurrentPage.Layers.Count - 1);
            CurrentPage.Layers.Remove(layer);
            CurrentPage.Layers.Insert(newOrder, layer);
            for (int i = 0; i < CurrentPage.Layers.Count; i++)
                CurrentPage.Layers[i].Order = i;
            OnLayersChanged();
        }
    }

    public void DuplicateLayer(Guid layerId)
    {
        PushUndo();
        var source = CurrentPage.Layers.FirstOrDefault(l => l.Id == layerId);
        if (source != null)
        {
            var clone = source.Clone();
            clone.Order = CurrentPage.Layers.Count;
            CurrentPage.Layers.Add(clone);
            OnLayersChanged();
        }
    }

    public void MergeLayerDown(Guid layerId)
    {
        PushUndo();
        var layer = CurrentPage.Layers.FirstOrDefault(l => l.Id == layerId);
        if (layer == null) return;
        var idx = CurrentPage.Layers.IndexOf(layer);
        if (idx <= 0) return;
        var below = CurrentPage.Layers[idx - 1];
        below.Strokes.AddRange(layer.Strokes);
        CurrentPage.Layers.Remove(layer);
        OnLayersChanged();
    }

    public void AddStroke(DrawingStroke stroke)
    {
        stroke.LayerId = CurrentPage.Layers[^1].Id;
        CurrentPage.Layers[^1].Strokes.Add(stroke);
        OnStrokesChanged();
    }

    public void AddStrokeToLayer(DrawingStroke stroke, Guid layerId)
    {
        var layer = CurrentPage.Layers.FirstOrDefault(l => l.Id == layerId);
        if (layer != null)
        {
            stroke.LayerId = layerId;
            layer.Strokes.Add(stroke);
            OnStrokesChanged();
        }
    }

    public void RemoveStroke(DrawingStroke stroke)
    {
        foreach (var layer in CurrentPage.Layers)
        {
            if (layer.Strokes.Remove(stroke))
            {
                OnStrokesChanged();
                return;
            }
        }
    }

    public void ClearStrokes()
    {
        PushUndo();
        foreach (var layer in CurrentPage.Layers)
            layer.Strokes.Clear();
        OnStrokesChanged();
    }

    public void PushUndo()
    {
        _undoStack.Push((CloneDocument(CurrentDocument), "change"));
        _redoStack.Clear();
        if (_undoStack.Count > MaxUndoDepth)
        {
            var items = _undoStack.ToArray();
            _undoStack.Clear();
            for (int i = items.Length - 1; i > 0; i--)
                _undoStack.Push(items[i]);
        }
    }

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public void Undo()
    {
        if (_undoStack.Count == 0) return;
        var (doc, _) = _undoStack.Pop();
        _redoStack.Push((CurrentDocument, "undo"));
        CurrentDocument = doc;
        OnDocumentChanged();
    }

    public void Redo()
    {
        if (_redoStack.Count == 0) return;
        var (doc, _) = _redoStack.Pop();
        _undoStack.Push((CurrentDocument, "redo"));
        CurrentDocument = doc;
        OnDocumentChanged();
    }

    private static WhiteboardDocument CloneDocument(WhiteboardDocument source)
    {
        var doc = new WhiteboardDocument
        {
            Id = source.Id,
            Title = source.Title,
            FilePath = source.FilePath,
            CurrentPageIndex = source.CurrentPageIndex,
            Theme = source.Theme,
            CreatedAt = source.CreatedAt,
            ModifiedAt = source.ModifiedAt,
            Version = source.Version,
            Pages = source.Pages.Select(p => new WhiteboardPage
            {
                Id = p.Id,
                Name = p.Name,
                PaperType = p.PaperType,
                BackgroundColor = p.BackgroundColor,
                Zoom = p.Zoom,
                PanX = p.PanX,
                PanY = p.PanY,
                Rotation = p.Rotation,
                CreatedAt = p.CreatedAt,
                ModifiedAt = p.ModifiedAt,
                Layers = p.Layers.Select(l => new DrawingLayer
                {
                    Id = l.Id,
                    Name = l.Name,
                    Opacity = l.Opacity,
                    IsVisible = l.IsVisible,
                    IsLocked = l.IsLocked,
                    BlendMode = l.BlendMode,
                    Order = l.Order,
                    Strokes = l.Strokes.Select(s => s.Clone()).ToList()
                }).ToList()
            }).ToList()
        };
        return doc;
    }

    private void OnDocumentChanged() => DocumentChanged?.Invoke(this, EventArgs.Empty);
    private void OnCurrentPageChanged() => CurrentPageChanged?.Invoke(this, EventArgs.Empty);
    private void OnLayersChanged() => LayersChanged?.Invoke(this, EventArgs.Empty);
    private void OnStrokesChanged() => StrokesChanged?.Invoke(this, EventArgs.Empty);
}
