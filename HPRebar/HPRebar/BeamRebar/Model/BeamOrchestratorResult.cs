namespace HPRebar.BeamRebar.Model;

/// <summary>
/// Outcome of a full Continuous Beam Rebar creation run.
/// </summary>
public sealed record BeamOrchestratorResult
{
    public bool IsOk => Validation.IsOk;

    public ValidationResult Validation { get; init; } = ValidationResult.Ok;

    public CreatedBeamViews Views { get; init; } = new();

    public CreatedBeamRebar Rebar { get; init; } = new();

    public int Total => Views.Total + Rebar.Total;

    public static BeamOrchestratorResult Invalid(ValidationResult validation) => new() { Validation = validation };

    public static BeamOrchestratorResult Success(CreatedBeamViews views, CreatedBeamRebar rebar) =>
        new() { Validation = ValidationResult.Ok, Views = views, Rebar = rebar };
}
