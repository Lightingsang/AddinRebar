using System.Windows;
using HPRebar.Core.KataExport.Calculators;
using HPRebar.Core.KataExport.Models;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;

// WPF types, not the Revit ones the SDK imports globally.
using Point = System.Windows.Point;

namespace HPRebar.KataExport.View.Controls;

/// <summary>
/// The section panel beside Kata's elevation: which section it shows (the flag clicked, else the selected column's
/// cut) and where it sits (the right of the canvas, hidden when the canvas is too small for it).
/// </summary>
public sealed partial class KataElevationCanvas
{
    /// <summary>Share of the canvas width the panel takes, and the canvas size under which it is not shown.</summary>
    private const double SectionPanelShare = 0.35;

    private const double SectionPanelMinWidth = 600.0;
    private const double SectionPanelMinHeight = 240.0;

    // The section of the flag clicked last, for the plan it was clicked on; null: the selected column's.
    private int? _flagSection;
    private KataRebarPlan? _flagPlan;

    /// <summary>Where the section panel sits, null when it is not shown.</summary>
    private Rect? SectionPanel()
    {
        if (!ShowSection || !KataMode || ActualWidth < SectionPanelMinWidth || ActualHeight < SectionPanelMinHeight) return null;
        if (Drawing() is not { } drawing || drawing.Cuts.Count == 0) return null;
        double width = System.Math.Round(ActualWidth * SectionPanelShare);
        return new Rect(ActualWidth - width, 0.0, width, ActualHeight);
    }

    /// <summary>The section shown: the flag clicked, else the cut of the selected column.</summary>
    private KataSectionCut? ShownCut(KataRebarDrawing drawing, KataElevationScene scene, KataStationMap map)
    {
        if (_flagSection is { } number && ReferenceEquals(_flagPlan, drawing.Plan))
            foreach (var cut in drawing.Cuts)
                if (cut.Number == number) return cut;
        return KataElevationSectionPainter.SelectedCut(scene, drawing.Cuts, map);
    }

    /// <summary>Number of the section flag under <paramref name="at"/>, null when none is.</summary>
    private int? FlagAt(KataElevation elevation, KataElevationViewport viewport, Point at)
    {
        if (SectionPanel() is null || Drawing() is not { } drawing) return null;
        var scene = Scene(elevation, viewport);
        var map = RebarStationMap ?? KataStationMap.Identity;
        Point P(double x, double z) => new(scene.X(map.ToStation(x)), scene.Y(elevation.TopMm + z));
        foreach (var flag in drawing.Elevation.Flags)
            if (KataElevationCadPainter.FlagHit(flag, at, P, viewport.Scale)) return flag.Number;
        return null;
    }
}
