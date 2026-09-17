using ETABSv1;
using HPRebar.McpBridge.Core.Scripting;

namespace HPEtabs.McpBridge.Service;

/// <summary>
///     Every run works in kN / mm / °C: the API's present units are switched before the script and the user's
///     own units are put back in <c>finally</c>, whatever the script did. The two OAPI calls are injected so
///     the guarantee is testable without ETABS.
/// </summary>
public static class EtabsUnitsPolicy
{
    public const eUnits ForcedUnits = eUnits.kN_mm_C;

    /// <summary>The unit system every run works in: identity conversions, because the API itself is switched to millimetres.</summary>
    public static ScriptUnits Units { get; } = new ScriptUnits(ForcedUnits.ToString(), 1.0,
        "present units forced to kN_mm_C for this run: lengths mm, forces kN, moments kN·mm, stresses kN/mm²; the user's units are restored afterwards");

    /// <summary>
    ///     Forces the units, runs <paramref name="body"/>, restores. Returns the failure text when the units could not be
    ///     forced (the body never ran); otherwise null. A restore failure is a warning line, not an error — the run happened.
    /// </summary>
    public static string? Run(Func<eUnits> getPresentUnits, Func<eUnits, int> setPresentUnits, List<string> logs, Action body)
    {
        var saved = getPresentUnits();
        var ret = setPresentUnits(ForcedUnits);
        if (ret != 0) return $"ETABS returned {ret} from SetPresentUnits({ForcedUnits}); the run was not started.";

        try
        {
            body();
        }
        finally
        {
            try
            {
                var restored = setPresentUnits(saved);
                if (restored != 0) logs.Add($"warning: ETABS returned {restored} restoring present units to {saved}; they may still be {ForcedUnits}.");
            }
            catch (Exception exception)
            {
                logs.Add($"warning: restoring present units to {saved} threw {exception.GetType().Name}; they may still be {ForcedUnits}.");
            }
        }

        return null;
    }
}
