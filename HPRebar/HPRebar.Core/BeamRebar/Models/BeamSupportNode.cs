namespace HPRebar.Core.BeamRebar.Models;

/// <summary>
/// A bearing support node supporting the continuous beam assembly.
/// For N spans, there are exactly N + 1 support nodes (Index 0 to N).
/// </summary>
public sealed record BeamSupportNode
{
    public BeamSupportNode()
    {
    }

    public BeamSupportNode(
        int index,
        string name,
        double centerX,
        double width,
        SupportType type = SupportType.Column,
        double depth = 0.0,
        string elementUniqueId = "",
        bool isExterior = false)
    {
        Index = index;
        Name = name;
        CenterX = centerX;
        Width = width;
        Type = type;
        Depth = depth > 0 ? depth : width;
        ElementUniqueId = elementUniqueId;
        IsExterior = isExterior || type == SupportType.ExteriorColumn;
    }

    /// <summary>Zero-based index of the support node along the beam chain (0, 1, ... N).</summary>
    public int Index { get; init; }

    /// <summary>User-friendly identifier (e.g., "Support 0", "C1", "W1").</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Revit Element UniqueId of the supporting column, wall, or girder.</summary>
    public string ElementUniqueId { get; init; } = string.Empty;

    /// <summary>Longitudinal coordinate X of the support centerline (mm).</summary>
    public double CenterX { get; init; }

    /// <summary>Support dimension C along the beam longitudinal axis (mm).</summary>
    public double Width { get; init; }

    /// <summary>Support dimension B transverse to the beam longitudinal axis (mm).</summary>
    public double Depth { get; init; }

    /// <summary>Support classification.</summary>
    public SupportType Type { get; init; } = SupportType.Column;

    /// <summary>Coordinate X of the support left face entering the adjacent left span (mm).</summary>
    public double LeftFaceX => CenterX - (Width / 2.0);

    /// <summary>Coordinate X of the support right face entering the adjacent right span (mm).</summary>
    public double RightFaceX => CenterX + (Width / 2.0);

    /// <summary>True if this support is an exterior terminal support.</summary>
    public bool IsExterior { get; init; }

    // --- Property Aliases ---
    public double WidthMm => Width;
    public double DepthMm => Depth;
    public double CenterXMm => CenterX;
}
