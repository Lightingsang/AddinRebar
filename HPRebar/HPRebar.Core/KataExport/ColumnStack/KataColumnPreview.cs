using System.Collections.Generic;
using System.Linq;

namespace HPRebar.Core.KataExport.ColumnStack;

/// <summary>A storey as the Kata Column Export page lists and sketches it.</summary>
/// <param name="Shape">"rectangle" or "circle".</param>
/// <param name="Placement">
/// "first", "centred" or "leftFace": how Kata will place the storey on the one below; "stub" for the row above the
/// top storey, which Kata draws only as the bars' lap.
/// </param>
public sealed record KataColumnPreviewRow(
    string Level,
    double HeightM,
    double SizeB,
    double SizeH,
    double BeamDepthMm,
    string Shape,
    string Placement,
    double BaseMm,
    double TopMm,
    double MinXMm,
    double MaxXMm,
    double MinYMm,
    double MaxYMm);

/// <summary>What the page shows before anything is written: the cells and the measured stack behind them.</summary>
/// <param name="PileCapBottomM">C5 in metres.</param>
/// <param name="PileCapHeightM">B9 in metres (0 when no cap was found).</param>
public sealed record KataColumnPreview(
    string Name,
    int Count,
    double? CrossingBeamWidthMm,
    double? SlabThicknessMm,
    double PileCapBottomM,
    double PileCapHeightM,
    IReadOnlyList<KataColumnPreviewRow> Rows,
    IReadOnlyList<string> Warnings);

public static class KataColumnPreviewMapper
{
    /// <summary>Height the page sketches the stub row with: Kata draws only the lap above the top storey.</summary>
    private const double StubSketchMm = 800;

    public static KataColumnPreview Map(KataColumnStackInput input, KataColumnSheet sheet)
    {
        var rows = input.Storeys.Select((storey, i) => Row(storey, sheet.Storeys[i], i)).ToList();
        var top = input.Storeys[input.Storeys.Count - 1];
        rows.Add(Row(top with { BaseElevationMm = top.TopElevationMm, TopElevationMm = top.TopElevationMm + StubSketchMm }, sheet.Storeys[sheet.Storeys.Count - 1], -1));
        return new KataColumnPreview(
            input.Name?.ToString() ?? string.Empty,
            input.Count is int count ? count : 1,
            sheet.Header["C1"] as double?,
            sheet.Header["C2"] as double?,
            (double)sheet.Header["C5"]!,
            (double)sheet.Header["B9"]!,
            rows,
            sheet.Warnings);
    }

    private static KataColumnPreviewRow Row(KataColumnStorey storey, KataColumnRow row, int index)
    {
        var section = storey.Section;
        string placement = row.IsCapRow ? "stub" : index == 0 ? "first" : row.SizeB < 0 || row.SizeH < 0 ? "leftFace" : "centred";
        return new KataColumnPreviewRow(
            row.Name.Value,
            row.HeightM,
            row.SizeB,
            row.SizeH,
            row.BeamDepthMm,
            section.Shape == KataColumnShape.Circle ? "circle" : "rectangle",
            placement,
            storey.BaseElevationMm,
            storey.TopElevationMm,
            section.MinXMm,
            section.MaxXMm,
            section.MinYMm,
            section.MaxYMm);
    }
}
