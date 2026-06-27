using System.Text.Json;
using System.Text.Json.Serialization;
using CyberBoard.Models;
using SkiaSharp;

namespace CyberBoard.Core.Services;

public class FileService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.Preserve,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new SKColorJsonConverter() }
    };

    public async Task SaveDocumentAsync(WhiteboardDocument document, string filePath)
    {
        document.ModifiedAt = DateTime.UtcNow;
        document.FilePath = filePath;

        var data = new DocumentData
        {
            Document = document,
            Version = "1.0.0"
        };

        var json = JsonSerializer.Serialize(data, JsonOptions);
        var compressed = await CompressAsync(json);
        await File.WriteAllBytesAsync(filePath, compressed);
    }

    public async Task<WhiteboardDocument?> LoadDocumentAsync(string filePath)
    {
        if (!File.Exists(filePath)) return null;

        var compressed = await File.ReadAllBytesAsync(filePath);
        var json = await DecompressAsync(compressed);
        var data = JsonSerializer.Deserialize<DocumentData>(json, JsonOptions);
        return data?.Document;
    }

    public void SaveAsImage(WhiteboardDocument document, string filePath, SKBitmap bitmap)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        var image = SKImage.FromBitmap(bitmap);
        var format = extension switch
        {
            ".png" => SKEncodedImageFormat.Png,
            ".jpg" or ".jpeg" => SKEncodedImageFormat.Jpeg,
            ".webp" => SKEncodedImageFormat.Webp,
            _ => SKEncodedImageFormat.Png
        };
        var quality = extension switch
        {
            ".png" => 100,
            ".jpg" or ".jpeg" => 95,
            ".webp" => 90,
            _ => 100
        };
        using var data = image.Encode(format, quality);
        using var stream = File.OpenWrite(filePath);
        data.SaveTo(stream);
        image.Dispose();
    }

    public async Task ExportAsSvgAsync(WhiteboardDocument document, string filePath)
    {
        var sb = new System.Text.StringBuilder();
        var page = document.CurrentPage;

        sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {page.CanvasWidth} {page.CanvasHeight}\">");

        foreach (var layer in page.Layers.Where(l => l.IsVisible))
        {
            foreach (var stroke in layer.Strokes.Where(s => s.Points.Count >= 2))
            {
                var color = stroke.Color;
                sb.AppendLine($"<polyline fill=\"none\" stroke=\"rgb({color.Red},{color.Green},{color.Blue})\" " +
                              $"stroke-width=\"{stroke.StrokeWidth}\" opacity=\"{stroke.Opacity}\" " +
                              $"stroke-linecap=\"round\" stroke-linejoin=\"round\" " +
                              $"points=\"{string.Join(" ", stroke.Points.Select(p => $"{p.X},{p.Y}"))}\" />");
            }
        }

        sb.AppendLine("</svg>");
        await File.WriteAllTextAsync(filePath, sb.ToString());
    }

    private static async Task<byte[]> CompressAsync(string text)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(text);
        await using var output = new MemoryStream();
        await using var gzip = new System.IO.Compression.GZipStream(output, System.IO.Compression.CompressionLevel.Optimal);
        await gzip.WriteAsync(bytes);
        await gzip.FlushAsync();
        return output.ToArray();
    }

    private static async Task<string> DecompressAsync(byte[] compressed)
    {
        await using var input = new MemoryStream(compressed);
        await using var gzip = new System.IO.Compression.GZipStream(input, System.IO.Compression.CompressionMode.Decompress);
        using var reader = new StreamReader(gzip);
        return await reader.ReadToEndAsync();
    }

    private class DocumentData
    {
        public WhiteboardDocument? Document { get; set; }
        public string Version { get; set; } = "";
    }

    private class SKColorJsonConverter : JsonConverter<SKColor>
    {
        public override SKColor Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var hex = reader.GetString();
            return SKColor.Parse(hex ?? "#000000");
        }

        public override void Write(Utf8JsonWriter writer, SKColor value, JsonSerializerOptions options)
        {
            writer.WriteStringValue($"#{value.Red:X2}{value.Green:X2}{value.Blue:X2}{value.Alpha:X2}");
        }
    }
}
