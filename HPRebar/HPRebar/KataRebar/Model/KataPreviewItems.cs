namespace HPRebar.KataRebar.Model;

/// <summary>One span of the plan as the window's span table shows it.</summary>
public sealed record KataSpanPreviewItem
{
    public int SpanNumber { get; init; }
    public double LeftColumnWidthMm { get; init; }
    public double ClearSpanLengthMm { get; init; }
    public double RightColumnWidthMm { get; init; }
    public string StirrupSpacing { get; init; } = "";
}

/// <summary>One group of longitudinal bars as the window's bar table shows it.</summary>
public sealed record KataBarLayerPreviewItem
{
    public string Category { get; init; } = "";
    public string Location { get; init; } = "";
    public string Notation { get; init; } = "";
    public int Count { get; init; }
    public double DiameterMm { get; init; }
    public string Details { get; init; } = "";
}
