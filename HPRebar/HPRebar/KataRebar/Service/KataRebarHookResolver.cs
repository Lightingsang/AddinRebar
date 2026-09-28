using System;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;

namespace HPRebar.KataRebar.Service;

/// <summary>A hook type of the project for a bent end, and the bar style it may be used with.</summary>
public sealed record KataHookChoice(RebarHookType Hook, RebarStyle Style);

/// <summary>
/// Picks the project's hook type for an angle: a stirrup/tie hook when there is one (so the bar stays a
/// stirrup), else a standard hook with a standard bar; among those, the tail closest to the wanted length.
/// </summary>
public static class KataRebarHookResolver
{
    private const double AngleToleranceRad = 1e-3;

    public static KataHookChoice? Find(Document doc, int angleDegrees, double tailFactor)
    {
        if (angleDegrees <= 0) return null;

        double angle = angleDegrees * Math.PI / 180.0;
        var candidates = new FilteredElementCollector(doc)
            .OfClass(typeof(RebarHookType))
            .Cast<RebarHookType>()
            .Where(h => Math.Abs(h.HookAngle - angle) < AngleToleranceRad)
            .ToList();

        var best = candidates
            .OrderBy(h => h.Style == RebarStyle.StirrupTie ? 0 : 1)
            .ThenBy(h => Math.Abs(h.StraightLineMultiplier - tailFactor))
            .ThenBy(h => h.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        return best is null ? null : new KataHookChoice(best, best.Style == RebarStyle.StirrupTie ? RebarStyle.StirrupTie : RebarStyle.Standard);
    }
}
