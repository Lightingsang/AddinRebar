using System;
using System.Collections.Generic;

namespace HPRebar.Core.BeamRebar.Models;

/// <summary>
/// A fully calculated physical reinforcing bar centerline curve and associated detailing metadata.
/// Coordinates are in millimetres in the continuous beam datum.
/// </summary>
public sealed record BarPolyline
{
    /// <summary>Sequential bar identifier in the generation run.</summary>
    public int BarIndex { get; init; }

    /// <summary>Alias for BarIndex.</summary>
    public int BarId => BarIndex;

    /// <summary>Structural role of this bar.</summary>
    public BarType Type { get; init; }

    /// <summary>Bar nominal diameter in millimetres.</summary>
    public double Diameter { get; init; }

    /// <summary>Alias for Diameter.</summary>
    public double DiameterMm => Diameter;

    /// <summary>Vertical layer index (1 = outer layer, 2 = inner secondary layer).</summary>
    public int Layer { get; init; } = 1;

    /// <summary>3D polyline geometry defining the bar centerline.</summary>
    public Polyline3 Polyline { get; init; } = new();

    /// <summary>Points of the polyline.</summary>
    public IReadOnlyList<Point3> Points => Polyline.Points;

    /// <summary>Start hook bend angle.</summary>
    public HookAngle StartHookAngle { get; init; } = HookAngle.None;

    /// <summary>End hook bend angle.</summary>
    public HookAngle EndHookAngle { get; init; } = HookAngle.None;

    /// <summary>Start hook length in millimetres (if modeled parametrically).</summary>
    public double StartHookLength { get; init; }

    /// <summary>End hook length in millimetres (if modeled parametrically).</summary>
    public double EndHookLength { get; init; }

    /// <summary>Left extension length into left adjacent span (for additional top bars).</summary>
    public double LeftExtension { get; init; }

    /// <summary>Right extension length into right adjacent span (for additional top bars).</summary>
    public double RightExtension { get; init; }

    /// <summary>Start coordinate X along beam axis.</summary>
    public double StartX => Points.Count > 0 ? Points[0].X : 0.0;

    /// <summary>End coordinate X along beam axis.</summary>
    public double EndX => Points.Count > 0 ? Points[Points.Count - 1].X : 0.0;

    /// <summary>Transverse position Y across the beam width (mm, centered at 0).</summary>
    public double TransverseY { get; init; }

    /// <summary>Span index hosting this bar (-1 if continuous across multiple spans).</summary>
    public int HostSpanIndex { get; init; } = -1;

    /// <summary>Support index hosting this bar (-1 if not associated with a support node).</summary>
    public int HostSupportIndex { get; init; } = -1;

    /// <summary>Revit RebarBarType name matched in project document.</summary>
    public string BarTypeName { get; init; } = string.Empty;

    /// <summary>Total cut length of this bar including polyline and hooks (mm).</summary>
    public double TotalLength => Polyline.TotalLength + StartHookLength + EndHookLength;

    /// <summary>Alias for TotalLength.</summary>
    public double Length => TotalLength;
}
