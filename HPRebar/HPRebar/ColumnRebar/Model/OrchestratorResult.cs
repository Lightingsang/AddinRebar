namespace HPRebar.ColumnRebar.Model;

/// <summary>Outcome of a full Column Rebar run.</summary>
public sealed record OrchestratorResult
{
    public bool IsOk => Validation.IsOk;

    public ValidationResult Validation { get; init; } = ValidationResult.Ok;

    public CreatedViews Views { get; init; } = new();

    public CreatedRebar Rebar { get; init; } = new();

    /// <summary>Everything put into the model, views included.</summary>
    public int Total => Views.Total + Rebar.Total;

    public static OrchestratorResult Invalid(ValidationResult validation) => new() { Validation = validation };
}
