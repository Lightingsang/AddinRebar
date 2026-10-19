namespace HPGrasshopper.McpBridge.Tools;

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Grasshopper.GUI.Canvas;
using Serilog;

/// <summary>
///     Renders and exports snapshots of the active Grasshopper canvas to PNG / base64 data URLs.
/// </summary>
public sealed class ImageExporter
{
    public const double MinZoom = 0.1;
    public const double MaxZoom = 5.0;

    public object Export(
        GH_Canvas? canvas,
        string mode = "FullCanvas",
        double zoom = 1.0,
        string? outputPath = null,
        bool returnBase64 = true)
    {
        if (canvas is null)
            return new { success = false, error = "Grasshopper canvas is not open or accessible." };

        var savedViewport = TryDuplicateViewport(canvas);
        try
        {
            ApplyViewModeAndZoom(canvas, mode, zoom);

            using var bmp = RenderCanvasToBitmap(canvas, zoom, out var width, out var height);
            var base64 = returnBase64 ? ConvertToBase64(bmp) : null;

            if (!string.IsNullOrWhiteSpace(outputPath))
                SaveBitmapToFile(bmp, outputPath);

            return new
            {
                success = true,
                width,
                height,
                mode,
                outputPath = outputPath ?? string.Empty,
                base64 = base64 ?? string.Empty
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "ImageExporter: Failed to export canvas image");
            return new { success = false, error = $"{ex.GetType().Name}: {ex.Message}" };
        }
        finally
        {
            TryRestoreViewport(canvas, savedViewport);
        }
    }

    private static GH_Viewport? TryDuplicateViewport(GH_Canvas canvas)
    {
        try
        {
            return canvas.Viewport?.Duplicate();
        }
        catch (Exception ex)
        {
            // Defensive: viewport duplication can fail on headless or uninitialized controls
            Log.Debug(ex, "ImageExporter: Could not duplicate canvas viewport");
            return null;
        }
    }

    private static void TryRestoreViewport(GH_Canvas canvas, GH_Viewport? savedViewport)
    {
        if (savedViewport is null || canvas.Viewport is null) return;
        try
        {
            canvas.Viewport.Set(savedViewport);
        }
        catch (Exception ex)
        {
            // Defensive: viewport restore should not interrupt export result
            Log.Debug(ex, "ImageExporter: Could not restore canvas viewport");
        }
    }

    public static (int width, int height) CalculateDimensions(int canvasWidth, int canvasHeight, double zoom)
    {
        double clampedZoom = Math.Clamp(zoom, MinZoom, MaxZoom);
        int width = Math.Max(200, (int)(canvasWidth * clampedZoom));
        int height = Math.Max(200, (int)(canvasHeight * clampedZoom));
        return (width, height);
    }

    private static void ApplyViewModeAndZoom(GH_Canvas canvas, string mode, double zoom)
    {
        if (canvas.Viewport is null) return;

        if (string.Equals(mode, "Selection", StringComparison.OrdinalIgnoreCase))
        {
            ApplySelectionMode(canvas);
        }
        else if (!string.Equals(mode, "Viewport", StringComparison.OrdinalIgnoreCase))
        {
            ApplyFullCanvasMode(canvas);
        }

        float clampedZoom = (float)Math.Clamp(zoom, MinZoom, MaxZoom);
        canvas.Viewport.Zoom = (float)(canvas.Viewport.Zoom * clampedZoom);
    }

    private static void ApplySelectionMode(GH_Canvas canvas)
    {
        var selectedAttrs = canvas.Document?.Objects
            .Where(o => o.Attributes is { Selected: true })
            .Select(o => o.Attributes)
            .ToList();

        if (selectedAttrs != null && selectedAttrs.Count > 0)
        {
            canvas.Viewport?.Focus(selectedAttrs);
        }
        else
        {
            Log.Information("ImageExporter: Selection mode requested but no objects selected, falling back to FullCanvas");
            ApplyFullCanvasMode(canvas);
        }
    }

    private static void ApplyFullCanvasMode(GH_Canvas canvas)
    {
        var allAttrs = canvas.Document?.Objects
            .Select(o => o.Attributes)
            .Where(a => a != null)
            .ToList();

        if (allAttrs != null && allAttrs.Count > 0)
        {
            canvas.Viewport?.Focus(allAttrs);
        }
    }

    private static Bitmap RenderCanvasToBitmap(GH_Canvas canvas, double zoom, out int width, out int height)
    {
        (width, height) = CalculateDimensions(canvas.Width, canvas.Height, zoom);

        var bmp = new Bitmap(width, height);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.FromArgb(240, 240, 240));
        canvas.DrawToBitmap(bmp, new Rectangle(0, 0, width, height));
        return bmp;
    }

    private static string ConvertToBase64(Bitmap bmp)
    {
        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        return "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());
    }

    private static void SaveBitmapToFile(Bitmap bmp, string outputPath)
    {
        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        bmp.Save(outputPath, ImageFormat.Png);
    }
}
