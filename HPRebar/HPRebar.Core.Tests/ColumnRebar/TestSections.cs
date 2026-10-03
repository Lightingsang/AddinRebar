using HPRebar.Core.ColumnRebar.Models;

namespace HPRebar.Core.Tests.ColumnRebar;

/// <summary>Shared fixtures. A 400x600 column on a 3x4 bar grid, which gives every face a distinct bar count.</summary>
internal static class TestSections
{
    public const double Cover = 25;
    public const double StirrupDiameter = 8;
    public const double BarDiameter = 20;

    public static ColumnSection Rectangle(
        double b = 400,
        double h = 600,
        double bottom = 0,
        double top = 3000,
        double west = 0,
        double south = 0) => new()
        {
            Index = 0,
            Shape = SectionShape.Rectangle,
            B = b,
            H = h,
            Hc = top - bottom,
            Hb = 0,
            Zb = 0,
            BottomPosition = bottom,
            TopPosition = top,
            WestPosition = west,
            EastPosition = west + b,
            SouthPosition = south,
            NorthPosition = south + h
        };

    public static ColumnSection Circular(double d = 500, double bottom = 0, double top = 3000) => new()
    {
        Index = 0,
        Shape = SectionShape.Circular,
        D = d,
        Hc = top - bottom,
        BottomPosition = bottom,
        TopPosition = top,
        CenterX = 0,
        CenterY = 0
    };

    public static BarLayoutSpec Grid(int nx = 3, int ny = 4) => new()
    {
        Nx = nx,
        Ny = ny,
        Nd = 0,
        BarDiameter = BarDiameter,
        StirrupDiameter = StirrupDiameter,
        Cover = Cover
    };

    public static BarLayoutSpec Ring(int nd = 8) => new()
    {
        Nx = 0,
        Ny = 0,
        Nd = nd,
        BarDiameter = BarDiameter,
        StirrupDiameter = StirrupDiameter,
        Cover = Cover
    };
}
