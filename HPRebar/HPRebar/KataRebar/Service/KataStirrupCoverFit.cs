using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;

namespace HPRebar.KataRebar.Service;

/// <summary>
/// A shape-driven stirrup set snaps its edges to the host's concrete cover when Revit regenerates it, whatever
/// box it was scaled to. The sheet's stirrup cover wins: each edge constrained to a cover is moved outward
/// by the difference between that cover (top, bottom or side, as the constraint reports it) and the plan's.
/// </summary>
public static class KataStirrupCoverFit
{
    public static int Apply(Document doc, Rebar rebar, double stirrupCoverFt)
    {
        var manager = rebar.GetRebarConstraintsManager();
        int moved = 0;

        foreach (var handle in manager.GetAllConstrainedHandles().Where(h => h.GetHandleType() == RebarHandleType.Edge))
        {
            var constraint = manager.GetCurrentConstraintOnHandle(handle);
            if (!constraint.IsToCover() || constraint.GetTargetCoverType(0) is not { } cover) continue;

            constraint.SetDistanceToTargetCover(cover.CoverDistance - stirrupCoverFt);
            // Multi-version: preferred rebar constraint — SetPreferredConstraint replaced SetPreferredConstraintForHandle in Revit 2025.
#if REVIT2025_OR_GREATER
            manager.SetPreferredConstraint(constraint);
#else
            manager.SetPreferredConstraintForHandle(handle, constraint);
#endif
            moved++;
        }

        if (moved > 0) doc.Regenerate();
        return moved;
    }
}
