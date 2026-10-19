namespace HPBim.Core.Models;

using HPBim.Core.Geometry;

public sealed record ColumnDef
{
    public string Id { get; init; } = string.Empty;
    public Point2D CenterLocation { get; init; }
    public double WidthMm { get; init; } = 220.0;
    public double DepthMm { get; init; } = 220.0;
    public string BaseLevel { get; init; } = string.Empty;
    public string TopLevel { get; init; } = string.Empty;
    public double BaseOffsetMm { get; init; }
    public double TopOffsetMm { get; init; }
    public double RotationDeg { get; init; }
    public string Material { get; init; } = "Concrete - B20";

    public BoundingBox2D GetBoundingBox()
    {
        double halfW = WidthMm / 2.0;
        double halfD = DepthMm / 2.0;
        return new BoundingBox2D(
            new Point2D(CenterLocation.X - halfW, CenterLocation.Y - halfD),
            new Point2D(CenterLocation.X + halfW, CenterLocation.Y + halfD));
    }
}
