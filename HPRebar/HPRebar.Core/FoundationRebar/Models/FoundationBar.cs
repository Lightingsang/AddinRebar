namespace HPRebar.Core.FoundationRebar.Models;

/// <summary>
/// Individual reinforcement bar generated within the foundation mesh.
/// </summary>
public sealed record FoundationBar
{
    /// <summary>1-based unique sequential index of the bar.</summary>
    public int BarIndex { get; init; }

    /// <summary>Vertical layer placement of the bar.</summary>
    public FoundationBarLayer Layer { get; init; }

    /// <summary>Friendly layer name (e.g. "BottomX", "BottomY", "TopY", "TopX").</summary>
    public string LayerName { get; init; } = string.Empty;

    /// <summary>Nominal bar diameter in millimetres.</summary>
    public double Diameter { get; init; }

    /// <summary>3D polyline in world Revit coordinates (mm).</summary>
    public Polyline3 Polyline { get; init; } = new();

    /// <summary>3D polyline in local foundation coordinate frame (mm).</summary>
    public Polyline3 LocalPolyline { get; init; } = new();

    /// <summary>Total length of the centerline curve in millimetres.</summary>
    public double LengthMm => Polyline.TotalLength;

    /// <summary>Hook configuration for this bar.</summary>
    public FoundationHookType HookType { get; init; }

    /// <summary>Actual hook length used in millimetres.</summary>
    public double HookLength { get; init; }
}
