using System.Globalization;
using HPAutoCad.Core.HPGeoLink.Model;
using HPAutoCad.Core.HPGeoLink.Projection;

namespace HPAutoCad.Core.HPGeoLink.Imagery;

/// <summary>
/// The raster the drawing receives: north-up on the VN-2000 grid, square pixels of <see cref="PixelSizeM"/>,
/// <see cref="OriginE"/>/<see cref="OriginN"/> = the top-left corner (not pixel centre). Being axis-aligned in
/// the drawing's own coordinates is what lets the RasterImage sit with no rotation and the world file be six
/// trivial lines.
/// </summary>
public sealed record OutputRaster(double OriginE, double OriginN, double PixelSizeM, int WidthPx, int HeightPx)
{
    public double WidthM => WidthPx * PixelSizeM;
    public double HeightM => HeightPx * PixelSizeM;

    /// <summary>Lower-left corner in VN-2000 metres — where AutoCAD anchors a RasterImage.</summary>
    public PlanePoint LowerLeft => new(OriginE, OriginN - HeightM);

    /// <summary>VN-2000 position of a pixel centre (col, row from the top-left).</summary>
    public PlanePoint PixelCenter(double col, double row) => new(OriginE + (col + 0.5) * PixelSizeM, OriginN - (row + 0.5) * PixelSizeM);

    /// <summary>
    /// ESRI world file for the PNG: pixel size, two zero rotation terms, negative pixel size (rows go south),
    /// then the centre of the top-left pixel. Written with the invariant culture, 6 lines, LF.
    /// </summary>
    public string WorldFile()
    {
        var ci = CultureInfo.InvariantCulture;
        var cx = OriginE + PixelSizeM / 2;
        var cy = OriginN - PixelSizeM / 2;
        return string.Join("\n", PixelSizeM.ToString("0.############", ci), "0.0", "0.0", (-PixelSizeM).ToString("0.############", ci),
            cx.ToString("0.####", ci), cy.ToString("0.####", ci)) + "\n";
    }

    /// <summary>
    /// The mapping the warp needs: for a lattice of output pixels (every <paramref name="stepPx"/> plus the last
    /// column/row), the fractional source pixel in the mosaic. Computed through the exact chain
    /// (VN-2000 → WGS84 → Web Mercator pixel) so the imagery lands where the survey says, not where an affine
    /// approximation puts it.
    /// </summary>
    public ControlGrid BuildControlGrid(TilePlan plan, TmParameters tm, Vn2000Wgs84Transform transform, int stepPx = ControlGrid.DefaultStepPx)
    {
        var cols = ControlGrid.Lattice(WidthPx, stepPx);
        var rows = ControlGrid.Lattice(HeightPx, stepPx);
        var origin = plan.MosaicOrigin;
        var sx = new double[rows.Length, cols.Length];
        var sy = new double[rows.Length, cols.Length];
        for (var r = 0; r < rows.Length; r++)
        {
            for (var c = 0; c < cols.Length; c++)
            {
                var geo = transform.ToWgs84(PixelCenter(cols[c], rows[r]), tm);
                var px = WebMercator.ToPixel(geo, plan.Zoom);
                sx[r, c] = px.X - origin.X;
                sy[r, c] = px.Y - origin.Y;
            }
        }
        return new ControlGrid(cols, rows, sx, sy);
    }
}

/// <summary>
/// Source-pixel positions on a lattice of output pixels, interpolated bilinearly in between. A 64 px step over a
/// 2048 px raster is 33 × 33 exact transforms instead of four million, and the projection is smooth enough for
/// the interpolation error to stay far below a pixel.
/// </summary>
public sealed class ControlGrid
{
    public const int DefaultStepPx = 64;

    public ControlGrid(int[] cols, int[] rows, double[,] sourceX, double[,] sourceY)
    {
        Cols = cols;
        Rows = rows;
        SourceX = sourceX;
        SourceY = sourceY;
    }

    /// <summary>Output pixel columns / rows (centres) the lattice samples; the last entry is the last pixel.</summary>
    public int[] Cols { get; }
    public int[] Rows { get; }
    public double[,] SourceX { get; }
    public double[,] SourceY { get; }

    public static int[] Lattice(int sizePx, int stepPx)
    {
        var list = new List<int>();
        for (var v = 0; v < sizePx; v += stepPx) list.Add(v);
        if (list[^1] != sizePx - 1) list.Add(sizePx - 1);
        return list.ToArray();
    }

    /// <summary>Source pixel for an output pixel (col, row) by bilinear interpolation of the lattice.</summary>
    public (double X, double Y) Sample(double col, double row)
    {
        var (c0, tc) = Locate(Cols, col);
        var (r0, tr) = Locate(Rows, row);
        var c1 = Math.Min(c0 + 1, Cols.Length - 1);
        var r1 = Math.Min(r0 + 1, Rows.Length - 1);
        double Lerp(double[,] a) =>
            (1 - tr) * ((1 - tc) * a[r0, c0] + tc * a[r0, c1]) + tr * ((1 - tc) * a[r1, c0] + tc * a[r1, c1]);
        return (Lerp(SourceX), Lerp(SourceY));
    }

    private static (int Index, double T) Locate(int[] lattice, double v)
    {
        if (v <= lattice[0]) return (0, 0);
        for (var i = 1; i < lattice.Length; i++)
        {
            if (v <= lattice[i])
            {
                var span = lattice[i] - lattice[i - 1];
                return (i - 1, span == 0 ? 0 : (v - lattice[i - 1]) / span);
            }
        }
        return (lattice.Length - 1, 0);
    }

    /// <summary>All lattice points as (output col,row) → (source x,y), for the affine fit.</summary>
    public IEnumerable<(double OutCol, double OutRow, double SrcX, double SrcY)> Points()
    {
        for (var r = 0; r < Rows.Length; r++)
            for (var c = 0; c < Cols.Length; c++)
                yield return (Cols[c], Rows[r], SourceX[r, c], SourceY[r, c]);
    }
}
