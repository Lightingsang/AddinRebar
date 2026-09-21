using SAP2000v1;
using HPRebar.McpBridge.Core.Scripting;

namespace HPSap2000.McpBridge.Service;

/// <summary>
///     Every run works in kN / m / °C: the API's present units are switched before the script and the user's
///     own units are put back in <c>finally</c>, whatever the script did.
/// </summary>
public static class SapUnitsPolicy
{
    public const eUnits ForcedUnits = eUnits.kN_m_C;

    /// <summary>The unit system every run works in: identity conversions, because the API itself is switched to metres.</summary>
    public static ScriptUnits Units { get; } = new ScriptUnits(ForcedUnits.ToString(), 1.0,
        "present units forced to kN_m_C for this run: lengths m, forces kN, moments kN·m, stresses kN/m²; the user's units are restored afterwards");

    /// <summary>
    ///     Forces the units, runs <paramref name="body"/>, restores. Returns failure text when units could not be
    ///     forced (body never ran); otherwise null. Restore failure is logged as warning.
    /// </summary>
    public static string? Run(Func<eUnits> getPresentUnits, Func<eUnits, int> setPresentUnits, List<string> logs, Action body)
    {
        var saved = getPresentUnits();
        var ret = setPresentUnits(ForcedUnits);
        if (ret != 0) return $"SAP2000 returned {ret} from SetPresentUnits({ForcedUnits}); the run was not started.";

        try
        {
            body();
        }
        finally
        {
            try
            {
                var restored = setPresentUnits(saved);
                if (restored != 0) logs.Add($"warning: SAP2000 returned {restored} restoring present units to {saved}; they may still be {ForcedUnits}.");
            }
            catch (Exception exception)
            {
                logs.Add($"warning: restoring present units to {saved} threw {exception.GetType().Name}; they may still be {ForcedUnits}.");
            }
        }

        return null;
    }
}
