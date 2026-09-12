using System;
using System.Collections.Generic;

namespace HPRebar.Core.BeamRebar.Models;

/// <summary>
/// Parameters for a single RebarShape-driven stirrup array.
/// Compatible with Revit's SetLayoutAsNumberWithSpacing API method.
/// </summary>
public sealed record StirrupRun
{
    /// <summary>Total number of stirrups in the array.</summary>
    public int Count { get; init; }

    /// <summary>Center-to-center spacing in millimetres.</summary>
    public double Spacing { get; init; }

    /// <summary>Start offset distance from host span/support start face (mm).</summary>
    public double StartOffset { get; init; }

    /// <summary>Total longitudinal length covered by the run (mm).</summary>
    public double Length { get; init; }

    /// <summary>Origin point (lower-left corner of the first stirrup) in local coordinates (mm).</summary>
    public Point3 Origin { get; init; }

    /// <summary>Out-to-out width for ScaleToBox (mm).</summary>
    public double Width { get; init; }

    /// <summary>Out-to-out height for ScaleToBox (mm).</summary>
    public double Height { get; init; }

    /// <summary>Absolute start coordinate X along continuous beam axis (mm).</summary>
    public double StartX { get; init; }

    /// <summary>Absolute end coordinate X along continuous beam axis (mm).</summary>
    public double EndX { get; init; }

    /// <summary>Individual longitudinal coordinates X for each stirrup (mm).</summary>
    public IReadOnlyList<double> Positions { get; init; } = Array.Empty<double>();
}
