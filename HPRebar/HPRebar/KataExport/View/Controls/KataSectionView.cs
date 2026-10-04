using System;
using System.Windows;

// WPF types, not the Revit ones the SDK imports globally.
using Point = System.Windows.Point;

namespace HPRebar.KataExport.View.Controls;

/// <summary>
/// Zoom and pan of the floating section panel, on top of the section fitted to it: <see cref="Zoom"/> 1 and no
/// <see cref="Pan"/> is "zoom extents" (a middle double click goes back there).
/// </summary>
internal readonly record struct KataSectionView(double Zoom, Vector Pan)
{
    public const double MinZoom = 0.25;
    public const double MaxZoom = 40.0;

    public static KataSectionView Fitted => new(1.0, default);

    /// <summary>Zoomed by <paramref name="factor"/> keeping the drawing point under <paramref name="at"/> where it is.</summary>
    public KataSectionView ZoomAt(double factor, Point at, Rect panel)
    {
        double zoom = Math.Max(MinZoom, Math.Min(MaxZoom, Zoom * factor));
        double f = zoom / Zoom;
        var fromCentre = at - new Point(panel.Left + panel.Width / 2.0, panel.Top + panel.Height / 2.0);
        return new KataSectionView(zoom, fromCentre - (fromCentre - Pan) * f);
    }

    public KataSectionView PanBy(Vector by) => this with { Pan = Pan + by };
}
