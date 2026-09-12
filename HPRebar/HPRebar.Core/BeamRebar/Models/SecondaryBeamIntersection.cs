namespace HPRebar.Core.BeamRebar.Models;

/// <summary>
/// Geometric representation of a secondary framing beam framing into the continuous primary beam.
/// Used to calculate hanging stirrup cages and diagonal ties.
/// </summary>
public sealed record SecondaryBeamIntersection
{
    public SecondaryBeamIntersection()
    {
    }

    public SecondaryBeamIntersection(
        int index,
        int hostSpanIndex,
        double centerX,
        double width,
        double height,
        double topElevation = 0.0,
        IntersectionSide framingSide = IntersectionSide.Both,
        string elementUniqueId = "")
    {
        Index = index;
        HostSpanIndex = hostSpanIndex;
        CenterX = centerX;
        Width = width;
        Height = height;
        TopElevation = topElevation;
        FramingSide = framingSide;
        ElementUniqueId = elementUniqueId;
    }

    /// <summary>Zero-based index of the intersection along the beam run.</summary>
    public int Index { get; init; }

    /// <summary>Index of the primary beam span containing this intersection.</summary>
    public int HostSpanIndex { get; init; }

    /// <summary>Longitudinal coordinate X of the secondary beam centerline (mm).</summary>
    public double CenterX { get; init; }

    /// <summary>Cross-section width bs of the incoming secondary beam (mm).</summary>
    public double Width { get; init; }

    /// <summary>Cross-section depth hs of the incoming secondary beam (mm).</summary>
    public double Height { get; init; }

    /// <summary>Top elevation of the secondary beam (mm).</summary>
    public double TopElevation { get; init; }

    /// <summary>Bottom soffit elevation of the secondary beam (mm).</summary>
    public double SoffitElevation => TopElevation - Height;

    /// <summary>Side(s) of the primary beam where the secondary beam frames in.</summary>
    public IntersectionSide FramingSide { get; init; } = IntersectionSide.Both;

    /// <summary>Revit Element UniqueId of the secondary beam.</summary>
    public string ElementUniqueId { get; init; } = string.Empty;

    /// <summary>Left face coordinate X of the secondary beam joint (mm).</summary>
    public double LeftFaceX => CenterX - (Width / 2.0);

    /// <summary>Right face coordinate X of the secondary beam joint (mm).</summary>
    public double RightFaceX => CenterX + (Width / 2.0);
}
