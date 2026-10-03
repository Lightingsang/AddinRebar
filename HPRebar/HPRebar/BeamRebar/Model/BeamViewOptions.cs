namespace HPRebar.BeamRebar.Model;

/// <summary>What the Views tab asks the run to draw besides the reinforcement.</summary>
public sealed record BeamViewOptions
{
    public static BeamViewOptions Default { get; } = new();

    public bool CreateElevationView { get; init; } = true;

    public string DetailViewName { get; init; } = "Beam Detail";

    /// <summary>Elevation scale denominator (50 = 1:50). Ignored when the view template controls the scale.</summary>
    public int ElevationScale { get; init; } = 50;

    public bool CreateSectionViews { get; init; } = true;

    /// <summary>Cross-sections cut per span: 2 (support, midspan) or 3 (both supports and midspan).</summary>
    public int SectionsPerSpan { get; init; } = 3;

    public string SectionPrefix { get; init; } = "Sec";

    public bool CreateDimensions { get; init; } = true;

    /// <summary>The bar table drawn beside each cross-section.</summary>
    public bool CreateTables { get; init; } = true;
}
