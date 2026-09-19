namespace HPGeo.Core.Imagery;

/// <summary>A BGRA32 bitmap in memory (row-major, top-left origin, 4 bytes per pixel). What WPF hands over and takes back.</summary>
public sealed class RasterBuffer
{
    public RasterBuffer(int width, int height) : this(width, height, new byte[checked(width * height * 4)]) { }

    public RasterBuffer(int width, int height, byte[] pixels)
    {
        if (pixels.Length != checked(width * height * 4)) throw new ArgumentException("Buffer size does not match width × height × 4.");
        Width = width;
        Height = height;
        Pixels = pixels;
    }

    public int Width { get; }
    public int Height { get; }
    public int Stride => Width * 4;
    public byte[] Pixels { get; }

    public (byte B, byte G, byte R, byte A) Get(int x, int y)
    {
        var i = (y * Width + x) * 4;
        return (Pixels[i], Pixels[i + 1], Pixels[i + 2], Pixels[i + 3]);
    }

    public void Set(int x, int y, byte b, byte g, byte r, byte a = 255)
    {
        var i = (y * Width + x) * 4;
        Pixels[i] = b; Pixels[i + 1] = g; Pixels[i + 2] = r; Pixels[i + 3] = a;
    }
}

/// <summary>How the mosaic is sampled at fractional positions.</summary>
public enum ResampleKernel
{
    /// <summary>Four-tap average: exact on linear ramps, softens edges by up to half a pixel.</summary>
    Bilinear,
    /// <summary>Catmull-Rom cubic (16 taps): keeps edges and texture crisp at ~1:1 scale — what a satellite tile needs.</summary>
    Bicubic,
}

/// <summary>
/// Resamples the Web Mercator mosaic onto the VN-2000 output raster: for every output pixel the control grid
/// gives the fractional source pixel, sampled bilinearly or bicubically. Source pixels outside the mosaic
/// become transparent. Pure (byte arrays in, byte array out) so the geometry is tested without a graphics stack.
/// </summary>
public static class RasterWarper
{
    public static RasterBuffer Warp(RasterBuffer source, ControlGrid grid, OutputRaster output, ResampleKernel kernel = ResampleKernel.Bilinear)
    {
        if (kernel == ResampleKernel.Bicubic) return WarpBicubic(source, grid, output);
        var dst = new RasterBuffer(output.WidthPx, output.HeightPx);
        var maxX = source.Width - 1;
        var maxY = source.Height - 1;
        for (var row = 0; row < output.HeightPx; row++)
        {
            for (var col = 0; col < output.WidthPx; col++)
            {
                var (sx, sy) = grid.Sample(col, row);
                // Source coordinates address pixel centres at +0.5; shift so integer positions are centres.
                var fx = sx - 0.5;
                var fy = sy - 0.5;
                if (fx < -0.5 || fy < -0.5 || fx > maxX + 0.5 || fy > maxY + 0.5) continue; // stays transparent
                var x0 = (int)Math.Floor(fx);
                var y0 = (int)Math.Floor(fy);
                var tx = fx - x0;
                var ty = fy - y0;
                var x1 = Math.Clamp(x0 + 1, 0, maxX);
                var y1 = Math.Clamp(y0 + 1, 0, maxY);
                x0 = Math.Clamp(x0, 0, maxX);
                y0 = Math.Clamp(y0, 0, maxY);
                var p00 = source.Get(x0, y0);
                var p10 = source.Get(x1, y0);
                var p01 = source.Get(x0, y1);
                var p11 = source.Get(x1, y1);
                dst.Set(col, row,
                    Blend(p00.B, p10.B, p01.B, p11.B, tx, ty),
                    Blend(p00.G, p10.G, p01.G, p11.G, tx, ty),
                    Blend(p00.R, p10.R, p01.R, p11.R, tx, ty),
                    Blend(p00.A, p10.A, p01.A, p11.A, tx, ty));
            }
        }
        return dst;
    }

    private static byte Blend(byte a00, byte a10, byte a01, byte a11, double tx, double ty)
    {
        var top = a00 + (a10 - a00) * tx;
        var bottom = a01 + (a11 - a01) * tx;
        return (byte)Math.Clamp(Math.Round(top + (bottom - top) * ty), 0, 255);
    }

    /// <summary>
    /// Catmull-Rom (a = −0.5) over the 4×4 neighbourhood, edge pixels clamped. A pixel whose 4×4 window would
    /// leave the mosaic on every side stays transparent, like the bilinear path; alpha is resampled the same way
    /// so a transparent edge fades rather than tears.
    /// </summary>
    private static RasterBuffer WarpBicubic(RasterBuffer source, ControlGrid grid, OutputRaster output)
    {
        var dst = new RasterBuffer(output.WidthPx, output.HeightPx);
        var maxX = source.Width - 1;
        var maxY = source.Height - 1;
        var src = source.Pixels;
        var stride = source.Stride;
        Span<double> wx = stackalloc double[4];
        Span<double> wy = stackalloc double[4];
        Span<int> xs = stackalloc int[4];
        Span<int> ys = stackalloc int[4];
        Span<double> acc = stackalloc double[4];
        for (var row = 0; row < output.HeightPx; row++)
        {
            for (var col = 0; col < output.WidthPx; col++)
            {
                var (sx, sy) = grid.Sample(col, row);
                var fx = sx - 0.5;
                var fy = sy - 0.5;
                if (fx < -0.5 || fy < -0.5 || fx > maxX + 0.5 || fy > maxY + 0.5) continue; // stays transparent
                var x0 = (int)Math.Floor(fx);
                var y0 = (int)Math.Floor(fy);
                var tx = fx - x0;
                var ty = fy - y0;
                CubicWeights(tx, wx);
                CubicWeights(ty, wy);
                for (var k = 0; k < 4; k++)
                {
                    xs[k] = Math.Clamp(x0 - 1 + k, 0, maxX);
                    ys[k] = Math.Clamp(y0 - 1 + k, 0, maxY);
                }
                acc.Clear();
                for (var j = 0; j < 4; j++)
                {
                    var rowOffset = ys[j] * stride;
                    for (var i = 0; i < 4; i++)
                    {
                        var w = wx[i] * wy[j];
                        var o = rowOffset + xs[i] * 4;
                        acc[0] += w * src[o];
                        acc[1] += w * src[o + 1];
                        acc[2] += w * src[o + 2];
                        acc[3] += w * src[o + 3];
                    }
                }
                dst.Set(col, row, Clamp(acc[0]), Clamp(acc[1]), Clamp(acc[2]), Clamp(acc[3]));
            }
        }
        return dst;
    }

    /// <summary>Catmull-Rom weights for the taps at −1, 0, +1, +2 around the fractional position t ∈ [0, 1).</summary>
    private static void CubicWeights(double t, Span<double> w)
    {
        var t2 = t * t;
        var t3 = t2 * t;
        w[0] = 0.5 * (-t3 + 2 * t2 - t);
        w[1] = 0.5 * (3 * t3 - 5 * t2 + 2);
        w[2] = 0.5 * (-3 * t3 + 4 * t2 + t);
        w[3] = 0.5 * (t3 - t2);
    }

    private static byte Clamp(double v) => (byte)Math.Clamp(Math.Round(v), 0, 255);
}
