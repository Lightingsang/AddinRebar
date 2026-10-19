namespace HPBim.Core.Models;

using System.Text.Json.Serialization;

public sealed record RebarDef
{
    public string Id { get; init; } = string.Empty;
    public string HostElementId { get; init; } = string.Empty;
    public int BarMark { get; init; }
    public double DiameterMm { get; init; } = 10.0;
    public string SteelGrade { get; init; } = "CB300-V";
    public RebarShapeType ShapeType { get; init; } = RebarShapeType.Straight;
    public double LengthMm { get; init; }
    public int Quantity { get; init; } = 1;
    public double SpacingMm { get; init; }
    public double CoverMm { get; init; } = 25.0;
    public string? Zone { get; init; }

    /// <summary>TCVN 5574:2018 unit weight: W = D^2 / 162.2 (kg/m).</summary>
    [JsonIgnore]
    public double UnitWeightKgPerM => (DiameterMm * DiameterMm) / 162.2;

    [JsonIgnore]
    public double TotalWeightKg => UnitWeightKgPerM * (LengthMm / 1000.0) * Quantity;
}
