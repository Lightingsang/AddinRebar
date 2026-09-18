using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.Settings;
using HPRebar.McpBridge.Core.Scripting;
using Serilog;

namespace HPCivil3d.McpBridge.Service;

/// <summary>
///     The drawing unit a Civil 3D script converts to and from. Civil 3D does not read INSUNITS: every
///     coordinate, station and elevation its API hands out is in the unit of the drawing settings
///     (<c>SettingsUnitZone.DrawingUnits</c>, Meters or Feet — the enum has no other value). So `units` is
///     built from that setting, INSUNITS is only the fallback for a drawing without Civil data, and a
///     disagreement between the two is reported once so the AI knows which one the numbers follow.
/// </summary>
public static class Civil3dUnits
{
    public const string MetersLabel = "Meters";
    public const string FeetLabel = "Feet";

    /// <summary>The unit table for the two values Civil 3D knows; testable without the Civil assemblies.</summary>
    public static ScriptUnits ForDrawingUnit(string drawingUnit) => drawingUnit switch
    {
        MetersLabel => new ScriptUnits(MetersLabel, 1000),
        FeetLabel => new ScriptUnits(FeetLabel, 304.8),
        _ => new ScriptUnits(drawingUnit, 1, $"Civil drawing unit '{drawingUnit}' is not Meters or Feet; treated as millimetres."),
    };

    /// <summary>
    ///     Units for the run: the Civil drawing unit when there is a Civil document, INSUNITS otherwise.
    ///     <paramref name="insunitsMismatch"/> is true when both exist and disagree — the note explains it.
    /// </summary>
    public static ScriptUnits For(CivilDocument? civil, Database db, out string? drawingUnit, out bool insunitsMismatch)
    {
        var fromInsunits = AutocadInsunits.For((int)db.Insunits);
        drawingUnit = ReadDrawingUnit(civil);
        insunitsMismatch = false;
        if (drawingUnit is null) return fromInsunits;

        var civilUnits = ForDrawingUnit(drawingUnit);
        // relative: INSUNITS US survey feet (304.8006) and Civil "Feet" (304.8) are the same unit at plan scale
        if (Math.Abs(civilUnits.MmPerUnit - fromInsunits.MmPerUnit) / civilUnits.MmPerUnit > 1e-4)
        {
            insunitsMismatch = true;
            return new ScriptUnits(civilUnits.Label, civilUnits.MmPerUnit,
                $"INSUNITS says {fromInsunits.Label} but the Civil drawing unit is {drawingUnit}; scripts follow the Civil unit ({civilUnits.MmPerUnit} mm per unit).");
        }

        return civilUnits;
    }

    /// <summary>"Meters" / "Feet", or null when there is no Civil document or its settings cannot be read.</summary>
    public static string? ReadDrawingUnit(CivilDocument? civil)
    {
        if (civil is null) return null;

        try
        {
            return civil.Settings.DrawingSettings.UnitZoneSettings.DrawingUnits.ToString();
        }
        catch (Exception exception)
        {
            Log.Debug(exception, "Civil drawing settings could not be read");
            return null;
        }
    }

    /// <summary>
    ///     The assigned coordinate system code, or null when the drawing has no zone or the settings cannot be read.
    ///     Civil 3D reports "no zone" as the string "." (seen live on the Metric template), not as an empty string.
    /// </summary>
    public static string? ReadCoordinateSystemCode(CivilDocument? civil)
    {
        if (civil is null) return null;

        try
        {
            var code = civil.Settings.DrawingSettings.UnitZoneSettings.CoordinateSystemCode;
            return string.IsNullOrWhiteSpace(code) || code == "." ? null : code;
        }
        catch (Exception exception)
        {
            Log.Debug(exception, "Civil coordinate system code could not be read");
            return null;
        }
    }
}
