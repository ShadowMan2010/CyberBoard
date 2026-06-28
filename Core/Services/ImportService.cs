using CyberBoard.Models;
using SkiaSharp;

namespace CyberBoard.Core.Services;

public class ImportService
{
    private static readonly HashSet<string> SupportedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp", ".tiff", ".tif"
    };

    private static readonly HashSet<string> SupportedVectorExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".svg"
    };

    public bool CanImportFile(string path)
    {
        var ext = Path.GetExtension(path);
        return SupportedImageExtensions.Contains(ext) || SupportedVectorExtensions.Contains(ext) ||
               ext.Equals(".pdf", StringComparison.OrdinalIgnoreCase);
    }

    public string GetFilter()
    {
        return "All Supported|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.webp;*.svg;*.pdf|" +
               "Images|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.webp|" +
               "SVG|*.svg|" +
               "PDF|*.pdf";
    }

    public async Task<List<CanvasItem>> ImportFileAsync(string filePath, float insertX, float insertY, float maxDim = 800f)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        var items = new List<CanvasItem>();

        if (ext == ".pdf")
        {
            items = await ImportPdfAsync(filePath, insertX, insertY, maxDim);
        }
        else
        {
            var item = await ImportImageAsync(filePath, insertX, insertY, maxDim);
            if (item != null)
                items.Add(item);
        }

        return items;
    }

    public async Task<CanvasItem?> ImportImageAsync(string filePath, float insertX, float insertY, float maxDim = 800f)
    {
        if (!File.Exists(filePath)) return null;

        try
        {
            var data = await File.ReadAllBytesAsync(filePath);

            using var ms = new MemoryStream(data);
            using var original = SKImage.FromEncodedData(ms);
            if (original == null) return null;

            var w = original.Width;
            var h = original.Height;

            if (w > maxDim || h > maxDim)
            {
                var scale = Math.Min(maxDim / w, maxDim / h);
                w = (int)(w * scale);
                h = (int)(h * scale);
            }

            return new CanvasItem
            {
                Type = ItemType.Image,
                Data = data,
                X = insertX,
                Y = insertY,
                Width = w,
                Height = h,
            };
        }
        catch
        {
            return null;
        }
    }

    public Task<List<CanvasItem>> ImportImageFromClipboardAsync(byte[] imageData, float insertX, float insertY)
    {
        var items = new List<CanvasItem>();
        try
        {
            using var ms = new MemoryStream(imageData);
            using var original = SKImage.FromEncodedData(ms);
            if (original == null) return Task.FromResult(items);

            var maxDim = 800f;
            var w = original.Width;
            var h = original.Height;
            if (w > maxDim || h > maxDim)
            {
                var scale = Math.Min(maxDim / w, maxDim / h);
                w = (int)(w * scale);
                h = (int)(h * scale);
            }

            items.Add(new CanvasItem
            {
                Type = ItemType.Image,
                Data = imageData,
                X = insertX,
                Y = insertY,
                Width = w,
                Height = h,
            });
        }
        catch { }

        return Task.FromResult(items);
    }

    private async Task<List<CanvasItem>> ImportPdfAsync(string filePath, float insertX, float insertY, float maxDim)
    {
        var items = new List<CanvasItem>();
        if (!File.Exists(filePath)) return items;

        try
        {
            var data = await File.ReadAllBytesAsync(filePath);
            items.Add(new CanvasItem
            {
                Type = ItemType.PdfPage,
                Data = data,
                X = insertX,
                Y = insertY,
                Width = maxDim,
                Height = maxDim * 1.414f,
                SourcePath = filePath
            });
        }
        catch { }

        return items;
    }
}
