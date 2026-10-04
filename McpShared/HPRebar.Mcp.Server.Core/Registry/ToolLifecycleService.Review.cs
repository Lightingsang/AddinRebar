using System.Text;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Registry.Model;

namespace HPRebar.Mcp.Server.Registry;

/// <summary>What a human reviewer reads before approving a tool: the review page and the code-quality record.</summary>
public sealed partial class ToolLifecycleService
{
    /// <summary>Registry event holding the quality summary of the latest proposed version.</summary>
    public const string QualityEvent = "quality_checked";

    private const string NotAnalysedPrefix = "not analysed";
    private const string NoQualityRecord = "not analysed (no quality record for this tool)";
    private const int MaxQualityLines = 20;

    /// <summary>Everything a reviewer needs on one page: metadata, schema, examples, code, test runs, approve command.</summary>
    public string WriteReview(ToolRecord record)
    {
        var tests = _manager.Db.RecentRuns(record.Name, 20).Where(r => r.Kind == RunRecord.KindTest).ToList();
        var stats = _manager.Db.Stats(record.Name, _manager.Options.RunWindow);
        var sb = new StringBuilder();
        sb.AppendLine($"# Review: {record.Name} v{record.Version}");
        sb.AppendLine();
        sb.AppendLine($"- **Title:** {record.Title}");
        sb.AppendLine($"- **Host:** {record.Host ?? _bridge.Profile.HostId} {string.Join("/", record.HostVersions)} · **Category:** {record.Category} · **Tags:** {string.Join(", ", record.Tags)}");
        sb.AppendLine($"- **Status:** {ToolRegistryDb.StatusText(record.Status)} · **Author:** {record.Author} · **From run:** {record.CreatedFromRunId?.ToString() ?? "-"}");
        sb.AppendLine($"- **Transaction:** {record.Transaction} · **Timeout:** {record.TimeoutSeconds}s · **Destructive:** {record.Destructive}");
        sb.AppendLine($"- **Runs (window):** {stats.Runs}, success {stats.SuccessRate:P0}, stability {StabilityScorer.Score(stats)}");
        sb.AppendLine();
        sb.AppendLine("## Description");
        sb.AppendLine(record.Description);
        sb.AppendLine();
        sb.AppendLine("## Input schema");
        sb.AppendLine("```json");
        sb.AppendLine(RegistryJson.Serialize(record.InputSchema));
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("## Examples");
        foreach (var example in record.Examples)
            sb.AppendLine($"- **{example.Title}** `{RegistryJson.Canonical(example.Args)}`{(example.VerifiedRunId is null ? "" : $" — verified run {example.VerifiedRunId}")}");
        sb.AppendLine();
        sb.AppendLine("## Test runs");
        if (tests.Count == 0) sb.AppendLine("_none_");
        foreach (var run in tests)
            sb.AppendLine($"- #{run.Id} {run.Timestamp:u} {(run.Success ? "PASS" : "FAIL")} {(run.DryRun ? "dryRun" : "real")} {run.DurationMs} ms {run.DocTitle}{(run.Error is null ? "" : " — " + run.Error)}");
        sb.AppendLine();
        sb.AppendLine("## Code quality");
        sb.AppendLine(CurrentQuality(record));
        sb.AppendLine();
        sb.AppendLine("## Code");
        sb.AppendLine("```csharp");
        sb.AppendLine(record.Code);
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("## Decide");
        sb.AppendLine("```");
        sb.AppendLine($"{_bridge.Profile.CliExecutable} registry approve {record.Name} --by <your name>");
        sb.AppendLine($"{_bridge.Profile.CliExecutable} registry reject {record.Name} --by <your name> --reason \"why\"");
        sb.AppendLine("```");
        sb.AppendLine($"Or edit `{record.Folder}\\tool.json` and set `\"status\": \"published\"` — running servers pick it up within a second.");

        return _manager.Store.WriteAux(Path.Combine(ReviewFolder, record.Name + ".md"), sb.ToString());
    }

    /// <summary>
    ///     The event detail: the code hash it was computed for, then the summary — analysed with its findings, or why
    ///     it was not analysed. The hash lets the review tell a record of older code (hand edit, import) from a current one.
    /// </summary>
    public static string QualityRecord(ToolRecord record, AnalyzeResult? analysis) => CodeStamp(record) + "\n" + QualitySummary(analysis);

    /// <summary>The summary part of a quality record.</summary>
    public static string QualitySummary(AnalyzeResult? analysis)
    {
        if (analysis is null) return NotAnalysedPrefix + " (bridge offline)";
        if (!analysis.QualityAnalysed) return NotAnalysedPrefix + " (the bridge predates the quality check — redeploy it)";

        var findings = analysis.QualityFindings;
        var errors = findings.Count(f => f.Severity == QualityFinding.Error);
        var sb = new StringBuilder($"analysed: {errors} error(s), {findings.Count - errors} warning(s)");
        foreach (var finding in findings.Take(MaxQualityLines))
            sb.Append($"\n- {finding.RuleId} {finding.Line}:{finding.Column} {finding.Message}");
        if (findings.Count > MaxQualityLines) sb.Append($"\n- … {findings.Count - MaxQualityLines} more");
        return sb.ToString();
    }

    /// <summary>The quality summary that belongs to the tool's current code, or why there is none.</summary>
    private string CurrentQuality(ToolRecord record)
    {
        var detail = _manager.Db.LatestEventDetail(record.Name, QualityEvent);
        if (detail is null) return NoQualityRecord;

        var stamp = CodeStamp(record) + "\n";
        return detail.StartsWith(stamp, StringComparison.Ordinal)
            ? detail[stamp.Length..]
            : NotAnalysedPrefix + " (stale: the code changed after the last check — propose a new version to check it again)";
    }

    /// <summary>The auto policy writes no review file, so the publish message carries the "not analysed" line.</summary>
    private string QualityNoteForPublish(ToolRecord record)
    {
        var quality = CurrentQuality(record);
        return quality.StartsWith(NotAnalysedPrefix, StringComparison.Ordinal) ? $" Code quality {quality}." : string.Empty;
    }

    private static string CodeStamp(ToolRecord record) => "code " + RegistryJson.Sha256(record.Code)[..12];
}
