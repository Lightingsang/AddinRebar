using System.Text.Json;
using System.Text.Json.Serialization;
using HPNavis.BIMCoordinator.Rules;

namespace HPNavis.BIMCoordinator.Reports;

/// <summary>One clashing element as a coordinator reads it: the Revit element (not the solid inside it), its file and level.</summary>
public sealed record ClashItemInfo(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("category")] string Category,
    [property: JsonPropertyName("elementId")] string ElementId,
    [property: JsonPropertyName("sourceFile")] string SourceFile,
    [property: JsonPropertyName("level")] string Level);

/// <summary>
///     One clash result. <see cref="Guid" /> is the result's identity across runs of the same test. <see cref="FoundAt" />
///     is when Navisworks first found it (local time, the report's own format); <see cref="Pictured" /> says the export's
///     HTML report carries its picture, so the workbook can tell "over the picture budget" from "picture lost".
/// </summary>
public sealed record ClashSnapshotRow(
    [property: JsonPropertyName("test")] string Test,
    [property: JsonPropertyName("ruleId")] string RuleId,
    [property: JsonPropertyName("priority")] int Priority,
    [property: JsonPropertyName("pair")] string Pair,
    [property: JsonPropertyName("guid")] string Guid,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("group")] string? Group,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("distanceMm")] double DistanceMm,
    [property: JsonPropertyName("centerMm")] double[] CenterMm,
    [property: JsonPropertyName("level")] string Level,
    [property: JsonPropertyName("item1")] ClashItemInfo Item1,
    [property: JsonPropertyName("item2")] ClashItemInfo Item2,
    [property: JsonPropertyName("foundAt")] string? FoundAt = null,
    [property: JsonPropertyName("pictured")] bool Pictured = false);

/// <summary>One HP clash test: when it last ran and its results by status (complete, whatever the row cap).</summary>
public sealed record ClashSnapshotTest(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("ruleId")] string RuleId,
    [property: JsonPropertyName("priority")] int Priority,
    [property: JsonPropertyName("pair")] string Pair,
    [property: JsonPropertyName("left")] string Left,
    [property: JsonPropertyName("right")] string Right,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("lastRun")] string? LastRun,
    [property: JsonPropertyName("resultCount")] int ResultCount,
    [property: JsonPropertyName("byStatus")] IReadOnlyDictionary<string, int> ByStatus);

/// <summary>
///     Everything one export saw: the HP tests of one design phase and every result row. Written as JSON beside the model
///     (<c>ClashReports\</c>) and turned into the workbook — and compared with the previous export — by
///     <c>tools/bim-coordinator/build-clash-report.py</c>.
/// </summary>
public sealed record ClashSnapshot(
    [property: JsonPropertyName("schemaVersion")] int SchemaVersion,
    [property: JsonPropertyName("exportedAt")] string ExportedAt,
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("phase")] string Phase,
    [property: JsonPropertyName("toleranceMm")] double ToleranceMm,
    [property: JsonPropertyName("tests")] IReadOnlyList<ClashSnapshotTest> Tests,
    [property: JsonPropertyName("rows")] IReadOnlyList<ClashSnapshotRow> Rows,
    [property: JsonPropertyName("truncated")] bool Truncated,
    [property: JsonPropertyName("htmlReport")] string? HtmlReport = null);

/// <summary>The pure rules of a clash export: which tests belong to it, where the file goes, how it is named and summarised.</summary>
public static class ClashSnapshotRules
{
    public const int SchemaVersion = 1;
    public const string FolderName = "ClashReports";
    public const string UnknownLevel = "(không rõ)";

    /// <summary>Rows beyond this are not written (the test counts stay complete); a phase run on a sound model stays far below it.</summary>
    public const int MaxRows = 50_000;

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    /// <summary>
    ///     The phase to export: the one asked for, or — when none is given — the single phase the model's HP tests belong to.
    ///     Several phases with none named is the caller's error (the skill keeps one phase per model).
    /// </summary>
    public static string ResolvePhase(string? requested, IEnumerable<string> testNames)
    {
        if (!string.IsNullOrWhiteSpace(requested) && requested != "auto") return ClashMatrix.PhaseOf(requested!);
        var phases = testNames.Select(ClashTestNaming.Parse).Where(p => p is not null).Select(p => ClashMatrix.PhaseOf(p!.Value.Lod)).Distinct().ToList();
        return phases.Count switch
        {
            0 => throw new ArgumentException("the model holds no HP clash test — create them with bim_sync_clash_tests (skill /hp-bim-clash-phase) first"),
            1 => phases[0],
            _ => throw new ArgumentException($"the model holds HP tests of several phases ({string.Join(", ", phases.OrderBy(p => p, StringComparer.Ordinal))}): name the one to export with lod"),
        };
    }

    /// <summary>
    ///     <c>ClashReports</c> beside the model. The folder comes from the open model only (never from the caller), so an
    ///     export cannot be pointed anywhere else; an unsaved model or one on a network share is refused.
    /// </summary>
    public static string FolderFor(string? modelPath)
    {
        if (string.IsNullOrWhiteSpace(modelPath) || !Path.IsPathRooted(modelPath))
            throw new ArgumentException("the model has not been saved to a file — save it first, the report goes beside it");
        if (modelPath!.StartsWith(@"\\", StringComparison.Ordinal))
            throw new ArgumentException("the model sits on a network share (UNC path): open it from a drive letter to export beside it");
        return Path.Combine(Path.GetDirectoryName(modelPath)!, FolderName);
    }

    /// <summary><c>clash-&lt;phase&gt;-&lt;yyyyMMdd-HHmmss&gt;.json</c> — sorts by time and names its phase.</summary>
    public static string FileNameFor(string phase, DateTime exportedAtUtc) =>
        $"clash-LOD{phase}-{exportedAtUtc.ToLocalTime():yyyyMMdd-HHmmss}.json";

    /// <summary>The report's own date format (local time), so pictured and unpictured rows read alike.</summary>
    public static string? FoundAtText(DateTime? createdTime) =>
        createdTime?.ToLocalTime().ToString("yyyy/MM/dd HH:mm", System.Globalization.CultureInfo.InvariantCulture);

    public static string Serialize(ClashSnapshot snapshot) => JsonSerializer.Serialize(snapshot, Json);

    /// <summary>Results by status over the whole export.</summary>
    public static IReadOnlyDictionary<string, int> ByStatus(ClashSnapshot snapshot) =>
        snapshot.Tests.SelectMany(t => t.ByStatus).GroupBy(p => p.Key).OrderBy(g => g.Key, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.Sum(p => p.Value));

    /// <summary>The level of a result: the first element's, else the second's, else <see cref="UnknownLevel" />.</summary>
    public static string LevelOf(ClashItemInfo item1, ClashItemInfo item2) =>
        !string.IsNullOrWhiteSpace(item1.Level) ? item1.Level : !string.IsNullOrWhiteSpace(item2.Level) ? item2.Level : UnknownLevel;
}
