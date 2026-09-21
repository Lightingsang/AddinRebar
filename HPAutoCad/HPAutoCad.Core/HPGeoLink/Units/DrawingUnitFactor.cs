namespace HPAutoCad.Core.HPGeoLink.Units;

/// <summary>
/// Metres per drawing unit. VN-2000 grid values are metres; a survey DWG is usually in metres but a
/// site plan handed to structural engineers is often in millimetres. Unknown/unitless drawings are
/// <see cref="Unknown"/> and the UI must ask — the tool never assumes.
/// </summary>
public enum DrawingUnit
{
    Unknown = 0,
    Meters,
    Millimeters,
    Centimeters,
    Decimeters,
    Kilometers,
    Feet,
    Inches,
}

public static class DrawingUnitFactor
{
    /// <summary>Metres per drawing unit, or null when the unit is unknown.</summary>
    public static double? MetersPerUnit(DrawingUnit unit) => unit switch
    {
        DrawingUnit.Meters => 1.0,
        DrawingUnit.Millimeters => 0.001,
        DrawingUnit.Centimeters => 0.01,
        DrawingUnit.Decimeters => 0.1,
        DrawingUnit.Kilometers => 1000.0,
        DrawingUnit.Feet => 0.3048,
        DrawingUnit.Inches => 0.0254,
        _ => null,
    };

    /// <summary>AutoCAD INSUNITS code → unit (0 unitless, 1 in, 2 ft, 4 mm, 5 cm, 6 m, 7 km, 14 dm; the rest unknown).</summary>
    public static DrawingUnit FromInsUnits(int insUnits) => insUnits switch
    {
        1 => DrawingUnit.Inches,
        2 => DrawingUnit.Feet,
        4 => DrawingUnit.Millimeters,
        5 => DrawingUnit.Centimeters,
        6 => DrawingUnit.Meters,
        7 => DrawingUnit.Kilometers,
        14 => DrawingUnit.Decimeters,
        _ => DrawingUnit.Unknown,
    };

    public static string Label(DrawingUnit unit) => unit switch
    {
        DrawingUnit.Meters => "Meters (m)",
        DrawingUnit.Millimeters => "Millimeters (mm)",
        DrawingUnit.Centimeters => "Centimeters (cm)",
        DrawingUnit.Decimeters => "Decimeters (dm)",
        DrawingUnit.Kilometers => "Kilometers (km)",
        DrawingUnit.Feet => "Feet (ft)",
        DrawingUnit.Inches => "Inches (in)",
        _ => "Unknown",
    };
}
