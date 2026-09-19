using HPGeo.Core.Catalog;
using HPGeo.Core.Units;

namespace HPGeo.AutoCad.UI;

/// <summary>A province in the combo box.</summary>
public sealed record ProvinceItem(Province Province)
{
    public string Name => Province.Name;

    public string Hint => Province.HasSingleMeridian
        ? CentralMeridian.Format(Province.CentralMeridians[0])
        : string.Join(" · ", Province.CentralMeridians.Select(CentralMeridian.Format)) + " (chọn KTT)";

    public override string ToString() => Name;
}

/// <summary>A central meridian of the selected province, with the former-province note when the catalogue has one.</summary>
public sealed record MeridianItem(double Degrees, string FormerProvince)
{
    public string Label => FormerProvince.Length == 0
        ? CentralMeridian.Format(Degrees)
        : $"{CentralMeridian.Format(Degrees)} — {FormerProvince}";

    public override string ToString() => Label;
}

/// <summary>A drawing unit choice; the INSUNITS one is marked so the user sees what the drawing says.</summary>
public sealed record UnitItem(DrawingUnit Unit, bool FromDrawing)
{
    public string Label => DrawingUnitFactor.Label(Unit) + (FromDrawing ? " — INSUNITS" : "");

    public override string ToString() => Label;
}

/// <summary>One row of the preview table.</summary>
public sealed record PreviewRow(string Label, string Easting, string Northing, string Lat, string Lon, string Note);

/// <summary>
/// What "Chèn ảnh vệ tinh vào CAD" hands back: the zone and unit the dialog shows, the resolution and the area
/// ratio typed beside the button (the margin in metres derived from it), and the extent (drawing units) of
/// everything the dialog holds — points and boundaries alike, so a survey of points alone gets its imagery too.
/// The command runs the pipeline after the dialog closes.
/// </summary>
public sealed record GeoImageChoice(HPGeo.Core.Projection.TmParameters Tm, DrawingUnit Unit, double MetersPerUnit, double ResolutionMPerPx, double AreaRatio, double MarginM,
    HPGeo.Core.Imagery.GridBoundingBox ExtentDrawingUnits, int PointCount, int BoundaryCount);
