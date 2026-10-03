using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;

namespace HPRebar.Shared.Revit;

/// <summary>Free-form bars from model curves, across the Revit versions the add-in builds for.</summary>
public static class RebarCurveFactory
{
    /// <summary>
    /// A bar following <paramref name="curves"/> with no hook at either end; the bends are part of the curves.
    /// Revit reuses a matching shape when one exists and creates a new one otherwise.
    /// </summary>
    public static Rebar CreateWithoutHooks(
        Document document,
        RebarStyle style,
        RebarBarType barType,
        Element host,
        XYZ normal,
        IList<Curve> curves)
    {
        // Multi-version: rebar terminations — Revit 2026 adds BarTerminationsData, the only overload left in
        // 2027; earlier versions take the (absent) hook types and their orientations directly.
#if REVIT2026_OR_GREATER
        var rebar = Rebar.CreateFromCurves(
            document, style, barType, host, normal, curves, new BarTerminationsData(document),
            useExistingShapeIfPossible: true, createNewShape: true);
#else
        var rebar = Rebar.CreateFromCurves(
            document, style, barType, null, null, host, normal, curves,
            RebarHookOrientation.Right, RebarHookOrientation.Right,
            useExistingShapeIfPossible: true, createNewShape: true);
#endif
        return rebar ?? throw new InvalidOperationException(
            $"Revit created no bar of type '{barType.Name}' from {curves.Count} curves.");
    }
}
