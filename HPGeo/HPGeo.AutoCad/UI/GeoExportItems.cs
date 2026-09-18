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
