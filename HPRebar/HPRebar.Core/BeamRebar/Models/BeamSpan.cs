namespace HPRebar.Core.BeamRebar.Models;

/// <summary>
/// An individual span segment in a continuous beam assembly.
/// All spatial coordinates and dimensions are in millimetres.
/// </summary>
public sealed record BeamSpan
{
    public BeamSpan()
    {
    }

    public BeamSpan(
        int index = 0,
        string name = "",
        double lengthCenter = 0.0,
        double width = 0.0,
        double height = 0.0,
        double topElevation = 0.0,
        double cover = 25.0,
        double? clearLength = null,
        double startX = 0.0,
        CantileverPosition cantilever = CantileverPosition.None,
        double? TopOffsetMm = null,
        double? CoverMm = null,
        double? ClearLengthMm = null,
        string elementUniqueId = "")
    {
        Index = index;
        Name = name;
        LengthCenter = lengthCenter;
        Width = width;
        Height = height;
        TopElevation = TopOffsetMm ?? topElevation;
        Cover = CoverMm ?? cover;
        LengthClear = ClearLengthMm ?? clearLength ?? (lengthCenter > 0 ? lengthCenter : 0.0);
        StartX = startX;
        Cantilever = cantilever;
        ElementUniqueId = elementUniqueId;
        CenterStartX = startX;
    }

    /// <summary>Zero-based index of the span in the continuous chain (0, 1, ... N-1).</summary>
    public int Index { get; init; }

    /// <summary>User-friendly identifier (e.g., "Span 1", "D1").</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Revit Element UniqueId of the underlying Structural Framing instance.</summary>
    public string ElementUniqueId { get; init; } = string.Empty;

    /// <summary>Center-to-center span length Lc between adjacent support centerlines (mm).</summary>
    public double LengthCenter { get; init; }

    /// <summary>Clear span length Ln between adjacent support inner faces (mm).</summary>
    public double LengthClear { get; init; }

    /// <summary>Cross-section width b (mm).</summary>
    public double Width { get; init; }

    /// <summary>Cross-section total height h (mm).</summary>
    public double Height { get; init; }

    /// <summary>Top surface elevation Z_top (mm).</summary>
    public double TopElevation { get; init; }

    /// <summary>Bottom soffit elevation Z_bot = Z_top - Height (mm).</summary>
    public double BottomElevation => TopElevation - Height;

    /// <summary>Longitudinal coordinate X of the clear span start face (mm).</summary>
    public double StartX { get; init; }

    /// <summary>Longitudinal coordinate X of the clear span end face (mm).</summary>
    public double EndX => StartX + LengthClear;

    /// <summary>Longitudinal coordinate X of the left support centerline (mm).</summary>
    public double CenterStartX { get; init; }

    /// <summary>Longitudinal coordinate X of the right support centerline (mm).</summary>
    public double CenterEndX => CenterStartX + LengthCenter;

    /// <summary>Specified concrete cover thickness c (mm).</summary>
    public double Cover { get; init; } = 25.0;

    /// <summary>Cantilever classification if this span overhangs an exterior support.</summary>
    public CantileverPosition Cantilever { get; init; } = CantileverPosition.None;

    /// <summary>True if this span is an overhang / cantilever.</summary>
    public bool IsCantilever => Cantilever != CantileverPosition.None;

    // --- Property Aliases for Test & Calculator Ergonomics ---
    public double ClearLengthMm => LengthClear;
    public double CoverMm => Cover;
    public double TopOffsetMm => TopElevation;
    public double LengthMm => LengthCenter;
    public double WidthMm => Width;
    public double HeightMm => Height;

    /// <summary>Effective depth d = h - Cover - stirrupDiameter - barDiameter / 2.</summary>
    public double EffectiveDepth(double barDiameter, double stirrupDiameter) =>
        Height - Cover - stirrupDiameter - (barDiameter / 2.0);
}
