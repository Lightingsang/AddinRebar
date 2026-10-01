using System;
using System.Collections.Generic;
using HPRebar.Core.BeamRebar.Models;

namespace HPRebar.Core.KataRebar.Models;

/// <summary>
/// One flat bar repeated along the beam at <see cref="Stations"/>: a C tie holding the side bars, an inner U or
/// C stirrup. <see cref="Shape"/> is the bar at the first station, drawn in the section plane (constant X);
/// every copy is the same bar moved along X. The ends carry Revit hooks of <see cref="HookAngle"/>.
/// </summary>
public sealed record KataBarSet
{
    public string BarMark { get; init; } = "";

    public string Description { get; init; } = "";

    public KataBarRole Role { get; init; } = KataBarRole.CrossTie;

    public double Diameter { get; init; }

    public int SpanIndex { get; init; } = -1;

    public string ZoneName { get; init; } = "";

    /// <summary>Centreline at the first station, local frame (mm).</summary>
    public Polyline3 Shape { get; init; } = new();

    /// <summary>Stations of every copy (mm, local X), evenly spaced by <see cref="Spacing"/>.</summary>
    public IReadOnlyList<double> Stations { get; init; } = Array.Empty<double>();

    public double Spacing { get; init; }

    /// <summary>Hook at both ends (°), 0 for none.</summary>
    public int HookAngle { get; init; }

    /// <summary>Straight part after the hook, in bar diameters.</summary>
    public double HookFactor { get; init; }

    /// <summary>
    /// A point of the section (local Y, Z; X is ignored) each hook turns towards, seen from its own end: the
    /// section centre for a stirrup, the wrapped bar for a C tie.
    /// </summary>
    public Point3 HookToward { get; init; } = new(0.0, 0.0, 0.0);

    /// <summary>
    /// The two points of <see cref="Shape"/> are the centres of the bars the hooks wrap, not the tie's ends: the
    /// tie runs <see cref="WrapOffset"/> × the hook's bend radius beside them and each hook turns round its bar.
    /// The bend radius belongs to the bar type, so the model side lays the tie out.
    /// </summary>
    public bool WrapEnds { get; init; }

    /// <summary>Unit direction (local Y, Z) from the wrapped bars to the straight part of a wrapping tie.</summary>
    public Point3 WrapOffset { get; init; } = new(0.0, 0.0, -1.0);

    public int Count => Stations.Count;

    /// <summary>Length of one bar (mm), hooks included (a 180° hook as a half circle of radius 2d plus its tail).</summary>
    public double BarLength => Shape.TotalLength + 2.0 * HookAllowance;

    private double HookAllowance => HookAngle switch
    {
        <= 0 => 0.0,
        _ => HookFactor * Diameter + Math.PI * 2.0 * Diameter * HookAngle / 180.0
    };
}
