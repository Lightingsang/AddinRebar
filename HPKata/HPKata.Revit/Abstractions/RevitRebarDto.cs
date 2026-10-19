namespace HPKata.Revit.Abstractions;

/// <summary>
/// Headless DTO representing a Revit Rebar element for host-neutral processing and testing.
/// </summary>
public sealed class RevitRebarDto
{
    public long ElementId { get; init; }
    public string UniqueId { get; init; } = string.Empty;
    public string Partition { get; init; } = string.Empty;
    public string Mark { get; init; } = string.Empty;
    public int Quantity { get; init; } = 1;
    public double DiameterMm { get; init; }
    public double SingleBarLengthMm { get; init; }
    public string ShapeName { get; init; } = "M_00";
    public double[] ParametersMm { get; init; } = [];
    public string Comments { get; init; } = string.Empty;
    public bool IsFreeform { get; init; }
    public double SpacingMm { get; init; }
    public double ArrayLengthMm { get; init; }
}
