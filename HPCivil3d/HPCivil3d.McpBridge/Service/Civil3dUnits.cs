using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil.ApplicationServices;
using HPRebar.McpBridge.Core.Scripting;
using Serilog;

namespace HPCivil3d.McpBridge.Service;

/// <summary>
///     Reads the Civil drawing settings the unit rule needs (<see cref="Civil3dUnitTable"/> holds the rule itself,
///     free of Civil types). Every getter is guarded: a drawing whose settings cannot be read falls back to INSUNITS
///     and the reason goes to the debug log, never to the AI.
/// </summary>
public static class Civil3dUnits
{
    /// <summary>
    ///     Units for the run: the Civil drawing unit when there is a Civil document, INSUNITS otherwise.
    ///     <paramref name="insunitsMismatch"/> is true when both exist and disagree — the note explains it.
    /// </summary>
    public static ScriptUnits For(CivilDocument? civil, Database db, out string? drawingUnit, out bool insunitsMismatch)
    {
        drawingUnit = ReadDrawingUnit(civil);
        return Civil3dUnitTable.Resolve(drawingUnit, (int)db.Insunits, out insunitsMismatch);
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

    /// <summary>The assigned coordinate system code, or null when the drawing has no zone or the settings cannot be read.</summary>
    public static string? ReadCoordinateSystemCode(CivilDocument? civil)
    {
        if (civil is null) return null;

        try
        {
            return Civil3dUnitTable.NormalizeCoordinateSystemCode(civil.Settings.DrawingSettings.UnitZoneSettings.CoordinateSystemCode);
        }
        catch (Exception exception)
        {
            Log.Debug(exception, "Civil coordinate system code could not be read");
            return null;
        }
    }
}
