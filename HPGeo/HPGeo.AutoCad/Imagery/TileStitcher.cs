using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HPGeo.Core.Imagery;

namespace HPGeo.AutoCad.Imagery;

/// <summary>
/// WPF imaging glue: decodes the fetched tiles (JPEG or PNG, sniffed) into one BGRA mosaic laid out by the
/// plan, and writes a <see cref="RasterBuffer"/> out as PNG. Decoding needs no dispatcher, so this runs on
/// a worker thread; only the attribution stamp (a DrawingVisual) wants the command's main thread.
/// </summary>
public static class TileStitcher
{
    public static RasterBuffer Stitch(TilePlan plan, IReadOnlyDictionary<TileAddress, byte[]> tiles)
    {
        var mosaic = new RasterBuffer(plan.MosaicWidthPx, plan.MosaicHeightPx);
        var stride = mosaic.Stride;
        foreach (var tile in plan.Tiles())
        {
            if (!tiles.TryGetValue(tile, out var bytes)) continue; // a missing tile stays transparent; the caller refused the run already
            var bitmap = Decode(bytes);
            var w = Math.Min(bitmap.PixelWidth, WebMercator.TileSizePx);
            var h = Math.Min(bitmap.PixelHeight, WebMercator.TileSizePx);
            var offsetX = (tile.X - plan.XMin) * WebMercator.TileSizePx;
            var offsetY = (tile.Y - plan.YMin) * WebMercator.TileSizePx;
            bitmap.CopyPixels(new Int32Rect(0, 0, w, h), mosaic.Pixels, stride, offsetY * stride + offsetX * 4);
        }
        return mosaic;
    }

    /// <summary>Any format WIC knows, converted to Bgra32 so the buffer layout is fixed.</summary>
    public static BitmapSource Decode(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var frame = decoder.Frames[0];
        if (frame.Format == PixelFormats.Bgra32) return frame;
        var converted = new FormatConvertedBitmap(frame, PixelFormats.Bgra32, null, 0);
        converted.Freeze();
        return converted;
    }

    public static BitmapSource ToBitmap(RasterBuffer buffer) =>
        BitmapSource.Create(buffer.Width, buffer.Height, 96, 96, PixelFormats.Bgra32, null, buffer.Pixels, buffer.Stride);

    public static void SavePng(BitmapSource bitmap, string path)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        encoder.Save(file);
    }

    public static void SavePng(RasterBuffer buffer, string path) => SavePng(ToBitmap(buffer), path);

    public static byte[] EncodePng(RasterBuffer buffer)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(ToBitmap(buffer)));
        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Burns the attribution into the bottom-left corner. Needs a WPF-capable (STA) thread — the command's;
    /// on any failure the plain bitmap is returned and the caller logs the attribution instead.
    /// </summary>
    public static BitmapSource StampAttribution(BitmapSource bitmap, string text)
    {
        try
        {
            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                dc.DrawImage(bitmap, new Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
                var formatted = new FormattedText(text, System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                    new Typeface("Segoe UI"), 11, Brushes.White, 1.0);
                var pad = 4.0;
                var box = new Rect(0, bitmap.PixelHeight - formatted.Height - 2 * pad, Math.Min(bitmap.PixelWidth, formatted.Width + 2 * pad), formatted.Height + 2 * pad);
                dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(150, 0, 0, 0)), null, box);
                dc.DrawText(formatted, new Point(box.X + pad, box.Y + pad));
            }
            var target = new RenderTargetBitmap(bitmap.PixelWidth, bitmap.PixelHeight, 96, 96, PixelFormats.Pbgra32);
            target.Render(visual);
            target.Freeze();
            return target;
        }
        catch (Exception exception)
        {
            HPGeoLog.Warning("attribution stamp skipped: " + exception.Message);
            return bitmap;
        }
    }
}
