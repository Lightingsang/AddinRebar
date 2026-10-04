using System;
using System.Globalization;
using System.Linq;
using HPRebar.Core.KataExport.Models;

namespace HPRebar.Core.KataExport.Calculators;

/// <summary>
/// Row 20 of sheet "Dam". At a support it is the beam crossing the run there: "b x h", or "b" alone when the crossing
/// beam is as deep as the beam itself (B5). At a span it carries the span's own width ahead of the user's side bars
/// ("300;2f12"): the width comes from Revit, the side bars stay as typed. Where Revit shows no crossing beam the
/// support cell is left as the user typed it: the run's probes miss a beam framing in from one side only.
/// </summary>
public static class KataRow20
{
    /// <summary>Sizes closer than this are the same (a crossing beam as deep as B5, a span as wide as B6).</summary>
    public const double SameSizeToleranceMm = 1.0;

    private static readonly char[] Separators = { ';', '+', ',' };

    /// <summary>The support cell: the crossing beam merged into a column or footing; null keeps the cell as it is.</summary>
    public static object? SupportCell(KataSupport support, double beamHeightMm)
    {
        if (support.Kind == KataSupportKind.Beam || support.CrossingBeamSection is not { } section)
            return null;

        if (!TryParseSection(section, out double width, out double height))
            return section;

        return Math.Abs(height - beamHeightMm) <= SameSizeToleranceMm ? KataFormat.Round(width) : section;
    }

    /// <summary>
    /// The span cell. Its width is written when it differs from B6, and for every span of a run in which any span
    /// differs: whether Kata carries an empty cell's width on to the next span or reads it as B6, the sheet then says
    /// the same thing.
    /// </summary>
    public static KataSpanWidthCell SpanCell(KataBeamPiece piece, double beamWidthMm, bool runChangesWidth) =>
        new(runChangesWidth || DiffersFrom(piece, beamWidthMm) ? KataFormat.Round(piece.WidthMm) : null);

    public static bool DiffersFrom(KataBeamPiece piece, double beamWidthMm) =>
        Math.Abs(piece.WidthMm - beamWidthMm) > SameSizeToleranceMm;

    /// <summary>
    /// A span cell as it will be written over <paramref name="existing"/>: the width (if any) first, then the side
    /// bars the user typed. Only a positive number in the first place is a width; "0" turns the side bars off and stays.
    /// </summary>
    public static object ComposeSpanCell(string? existing, double? widthMm)
    {
        var kept = (existing ?? string.Empty)
            .Split(Separators)
            .Select(t => t.Trim())
            .Where((t, i) => t.Length > 0 && !(i == 0 && IsPositiveNumber(t)))
            .ToList();

        if (widthMm is { } width)
            return kept.Count == 0 ? width : string.Join(";", new[] { KataFormat.Whole(width) }.Concat(kept));
        return kept.Count == 0 ? string.Empty : string.Join(";", kept);
    }

    /// <summary>Reads "b x h" as written by <see cref="KataFormat.Section"/>.</summary>
    public static bool TryParseSection(string text, out double width, out double height)
    {
        width = height = 0.0;
        var parts = text.Split(new[] { 'x', 'X' }, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 2
            && double.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out width)
            && double.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out height);
    }

    private static bool IsPositiveNumber(string token) =>
        double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) && v > 0.0;
}

/// <summary>
/// A span cell of row 20 before it meets the sheet: <see cref="WidthMm"/> is the span's width when it has to be
/// written; the writer merges it with the side bars already in the cell (<see cref="KataRow20.ComposeSpanCell"/>).
/// </summary>
public sealed record KataSpanWidthCell(double? WidthMm)
{
    public override string ToString() => WidthMm is { } w ? KataFormat.Whole(w) : string.Empty;
}
