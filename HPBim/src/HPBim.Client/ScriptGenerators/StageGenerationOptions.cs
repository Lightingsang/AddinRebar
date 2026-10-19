namespace HPBim.Client.ScriptGenerators;

/// <summary>
///     Configurable options for stage script generation.
/// </summary>
public sealed record StageGenerationOptions
{
    public bool CreatePlanViews { get; init; } = true;
    public double ToleranceMm { get; init; } = 10.0;
    public bool AllowPartial { get; init; }
}
