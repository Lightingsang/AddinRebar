namespace HPAutoCad.Core.HPGeoLink.Model;

/// <summary>
/// A VN-2000 grid position in metres. <b>Easting is the drawing's X, Northing its Y</b> — the invariant of this tool.
/// Cadastral paperwork writes the same pair as X = Northing, Y = Easting; that swap is a labelling
/// convention handled at the text boundary only, never on drawing geometry.
/// </summary>
public readonly record struct PlanePoint(double Easting, double Northing)
{
    public bool IsFinite => double.IsFinite(Easting) && double.IsFinite(Northing);
}
