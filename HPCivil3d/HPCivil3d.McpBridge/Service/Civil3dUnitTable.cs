using HPRebar.McpBridge.Core.Scripting;

namespace HPCivil3d.McpBridge.Service;

/// <summary>
///     The unit rule of the Civil bridge without any Civil type, so it can be unit-tested on a machine without
///     Civil 3D (the test project links this file). Civil 3D does not read INSUNITS: every coordinate, station
///     and elevation its API hands out is in the unit of the drawing settings — Meters or Feet, the enum has
///     no other value — so `units` is built from that setting and INSUNITS is only the fallback for a drawing
///     whose Civil settings cannot be read. A disagreement between the two is reported once, because a drawing
///     without Civil settings (a plain acad.dwt drawing opened in Civil 3D) reports Feet whatever INSUNITS says.
/// </summary>
public static class Civil3dUnitTable
{
    public const string MetersLabel = "Meters";
    public const string FeetLabel = "Feet";

    /// <summary>
    ///     Relative tolerance below which the Civil unit and INSUNITS count as the same unit: US survey feet
    ///     (304.8006 mm) and international feet (304.8 mm) differ by 2 ppm, irrelevant at plan scale.
    /// </summary>
    public const double SameUnitTolerance = 1e-4;

    /// <summary>The unit table for the two values Civil 3D knows; anything else is treated as millimetres and says so.</summary>
    public static ScriptUnits ForDrawingUnit(string drawingUnit) => drawingUnit switch
    {
        MetersLabel => new ScriptUnits(MetersLabel, 1000),
        FeetLabel => new ScriptUnits(FeetLabel, 304.8),
        _ => new ScriptUnits(drawingUnit, 1, $"Civil drawing unit '{drawingUnit}' is not Meters or Feet; treated as millimetres."),
    };

    /// <summary>
    ///     Units for the run: the Civil drawing unit when there is one, INSUNITS otherwise.
    ///     <paramref name="insunitsMismatch"/> is true when both exist and disagree — the note explains it.
    /// </summary>
    public static ScriptUnits Resolve(string? drawingUnit, int insunits, out bool insunitsMismatch)
    {
        var fromInsunits = AutocadInsunits.For(insunits);
        insunitsMismatch = false;
        if (drawingUnit is null) return fromInsunits;

        var civilUnits = ForDrawingUnit(drawingUnit);
        if (Math.Abs(civilUnits.MmPerUnit - fromInsunits.MmPerUnit) / civilUnits.MmPerUnit > SameUnitTolerance)
        {
            insunitsMismatch = true;
            return new ScriptUnits(civilUnits.Label, civilUnits.MmPerUnit,
                $"INSUNITS says {fromInsunits.Label} but the Civil drawing unit is {drawingUnit}; scripts follow the Civil unit ({civilUnits.MmPerUnit} mm per unit).");
        }

        return civilUnits;
    }

    /// <summary>Civil 3D reports "no zone" as the string "." (seen live on the Metric template), not as an empty string.</summary>
    public static string? NormalizeCoordinateSystemCode(string? code) =>
        string.IsNullOrWhiteSpace(code) || code == "." ? null : code;
}
