namespace HPKata.Revit.Abstractions;

/// <summary>
/// Headless DTO representing a generated rebar schedule table for display, insertion, or export.
/// </summary>
public sealed class RevitScheduleTableDto
{
    public string Title { get; init; } = "BẢNG THỐNG KÊ CỐT THÉP";
    public IReadOnlyList<string> Headers { get; init; } = [];
    public IReadOnlyList<IReadOnlyList<string>> Rows { get; init; } = [];
    public double TotalWeightKg { get; init; }
    public int TotalBarCount { get; init; }
    public IReadOnlyDictionary<string, double> WeightByDiameter { get; init; } =
        new Dictionary<string, double>();
}
