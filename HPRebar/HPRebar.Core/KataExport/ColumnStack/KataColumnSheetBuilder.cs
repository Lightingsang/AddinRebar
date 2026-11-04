using System;
using System.Collections.Generic;
using HPRebar.Core.KataExport.Calculators;
using HPRebar.Core.KataExport.Models;

namespace HPRebar.Core.KataExport.ColumnStack;

/// <summary>
/// Builds the geometry cells of sheet "Cot" for one column stack. Kata stacks the storeys from the pile-cap
/// bottom (C5) upward: cap height (B9), then each storey height (B10..), so the levels it draws are only right
/// when the cap top meets the first storey base.
/// </summary>
public static class KataColumnSheetBuilder
{
    public static KataColumnSheet Build(KataColumnStackInput input)
    {
        KataColumnInputValidator.Validate(input);
        var warnings = new List<string>();
        var header = BuildHeader(input, warnings);
        var rows = BuildRows(input.Storeys, warnings);
        return new KataColumnSheet(header, rows, warnings);
    }

    private static Dictionary<string, object?> BuildHeader(KataColumnStackInput input, List<string> warnings)
    {
        double firstBaseMm = input.Storeys[0].BaseElevationMm;
        var (capBottomMm, capHeightMm) = PileCapCells(input.PileCap, firstBaseMm, warnings);

        return new Dictionary<string, object?>
        {
            ["C1"] = Millimetres(input.CrossingBeamWidthMm),
            ["C2"] = Millimetres(input.SlabThicknessMm),
            ["C3"] = input.Name,
            ["C4"] = input.Count,
            ["C5"] = Metres(capBottomMm),
            ["B9"] = Metres(capHeightMm),
        };
    }

    private static (double BottomMm, double HeightMm) PileCapCells(KataPileCap? cap, double firstBaseMm, List<string> warnings)
    {
        if (cap is null)
        {
            warnings.Add("No pile cap or footing under the column: C5 is the base of the first storey and B9 (cap height) is 0.");
            return (firstBaseMm, 0);
        }

        double capTopMm = cap.BottomElevationMm + cap.HeightMm;
        if (Math.Abs(capTopMm - firstBaseMm) > KataTolerance.StationMm)
        {
            warnings.Add(
                $"The pile cap top ({KataFormat.Elevation(capTopMm)}) is not the base of the first storey " +
                $"({KataFormat.Elevation(firstBaseMm)}); B9 is written as the distance from the cap bottom to that base.");
        }

        return (cap.BottomElevationMm, firstBaseMm - cap.BottomElevationMm);
    }

    private static List<KataColumnRow> BuildRows(IReadOnlyList<KataColumnStorey> storeys, List<string> warnings)
    {
        var rows = new List<KataColumnRow>(storeys.Count);
        var names = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        KataColumnSection? below = null;
        foreach (var storey in storeys)
        {
            var step = KataColumnStepRule.Place(storey.Section, below, storey.LevelName);
            string name = UniqueName(storey.LevelName, names, warnings);
            if (step.Warning is not null)
            {
                warnings.Add(step.Warning);
            }

            rows.Add(new KataColumnRow(
                new KataText(name),
                (KataFormat.Round(storey.TopElevationMm) - KataFormat.Round(storey.BaseElevationMm)) / 1000.0,
                step.SignedBMm,
                step.SignedHMm,
                KataFormat.Round(storey.BeamDepthMm)));
            below = storey.Section;
        }

        rows.Add(CapRow(storeys[storeys.Count - 1], rows[rows.Count - 1], names, warnings));
        return rows;
    }

    /// <summary>
    /// Kata draws every row but the last as a storey and the last one only as the stub the bars lap into (drawings
    /// of its VC command with 6 and 7 rows), so the top storey of the stack needs one more row above it. It repeats
    /// the top storey's section, centred, under the name of the level at the column head.
    /// </summary>
    private static KataColumnRow CapRow(KataColumnStorey top, KataColumnRow topRow, Dictionary<string, int> names, List<string> warnings)
    {
        string level = string.IsNullOrWhiteSpace(top.TopLevelName) ? $"{top.LevelName} (đỉnh)" : top.TopLevelName!;
        return new KataColumnRow(
            new KataText(UniqueName(level, names, warnings)),
            topRow.HeightM,
            Math.Abs(topRow.SizeB),
            topRow.SizeH,
            topRow.BeamDepthMm,
            IsCapRow: true);
    }

    /// <summary>
    /// Kata finds a storey's row by its name (the section picker looks column A up), so two storeys on one level
    /// (a column split at mid-height) get "(2)", "(3)"… after the second.
    /// </summary>
    private static string UniqueName(string level, Dictionary<string, int> seen, List<string> warnings)
    {
        int count = seen.TryGetValue(level, out var n) ? n + 1 : 1;
        seen[level] = count;
        if (count == 1)
        {
            return level;
        }

        string name = $"{level} ({count})";
        warnings.Add($"Two storeys start on level {level}; the upper one is written as \"{name}\".");
        return name;
    }

    private static double? Millimetres(double? valueMm) => valueMm is { } value ? KataFormat.Round(value) : null;

    /// <summary>Whole millimetres expressed in metres, as Kata's B and C5 cells hold them (e.g. 3.6, -5.5).</summary>
    private static double Metres(double valueMm) => KataFormat.Round(valueMm) / 1000.0;
}
