using HPRebar.Core.BeamRebar.Models;

namespace HPRebar.Core.KataRebar.Models;

/// <summary>
/// Explicit 3D rebar centerline curve and fabrication attributes in beam local coordinate system.
/// </summary>
public sealed record KataRebarCurve
{
    /// <summary>Unique index of the bar in the generated assembly.</summary>
    public int BarId { get; init; }

    /// <summary>Structural role of this reinforcing bar.</summary>
    public KataBarRole Role { get; init; } = KataBarRole.MainTop;

    /// <summary>Bar nominal diameter in mm.</summary>
    public double Diameter { get; init; }

    /// <summary>Layer index (1 = outermost, 2 = second layer inwards, etc.).</summary>
    public int Layer { get; init; } = 1;

    /// <summary>3D polyline defining the bar centerline in local millimetres.</summary>
    public Polyline3 Polyline { get; init; } = new();

    /// <summary>Bend angle at the start of the bar.</summary>
    public HookAngle StartHookAngle { get; init; } = HookAngle.None;

    /// <summary>Bend angle at the end of the bar.</summary>
    public HookAngle EndHookAngle { get; init; } = HookAngle.None;

    /// <summary>Length of start hook tail in mm.</summary>
    public double StartHookLength { get; init; }

    /// <summary>Length of end hook tail in mm.</summary>
    public double EndHookLength { get; init; }

    /// <summary>Transverse Y coordinate of the bar centerline in mm (centered at 0).</summary>
    public double TransverseY { get; init; }

    /// <summary>Index of host span (0-based) if localized to a span, or -1 if continuous.</summary>
    public int HostSpanIndex { get; init; } = -1;

    /// <summary>Index of host support (0-based) if localized to a support, or -1 if continuous.</summary>
    public int HostSupportIndex { get; init; } = -1;

    /// <summary>Kata standard bar shape code (e.g. "00", "05a", "15a", "41", "45", "24a", "51").</summary>
    public string ShapeCode { get; init; } = "00";

    /// <summary>Bar schedule mark / item designation (e.g. "1", "2", "3", "d1").</summary>
    public string BarMark { get; init; } = "";

    /// <summary>
    /// Kata's bar number ("số hiệu", the drawing's circled 1, 2, 3...): identical bars share it
    /// (<see cref="Calculators.KataBarNumbering"/>); 0 until numbered. The schedule mark in Revit.
    /// </summary>
    public int BarNumber { get; init; }

    /// <summary>Human-readable engineering description (e.g. "Thép chủ trên", "Gia cường gối T1").</summary>
    public string BarDescription { get; init; } = "";

    /// <summary>Fabrication segment dimension A in mm.</summary>
    public double DimA { get; init; }

    /// <summary>Fabrication segment dimension B in mm.</summary>
    public double DimB { get; init; }

    /// <summary>Fabrication segment dimension C in mm.</summary>
    public double DimC { get; init; }

    /// <summary>Fabrication segment dimension D in mm.</summary>
    public double DimD { get; init; }

    /// <summary>Fabrication segment dimension E in mm.</summary>
    public double DimE { get; init; }

    /// <summary>Internal bend radius R in mm.</summary>
    public double DimR { get; init; }

    /// <summary>CAD sequential item number matching Kata drawing schedule.</summary>
    public int SttCad { get; init; }

    /// <summary>Total cut length of this reinforcing bar in mm.</summary>
    public double TotalLength => Polyline.Points.Count > 2
        ? Polyline.TotalLength
        : Polyline.TotalLength + StartHookLength + EndHookLength;
}
