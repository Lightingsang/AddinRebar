using System.Text;
using Autodesk.Navisworks.Api;
using HPNavis.BIMCoordinator.Reports;
using HPNavis.BIMCoordinator.Rules;

namespace HPNavis.BIMCoordinator;

/// <summary>The report half of the facade: the clash snapshot a coordinator turns into the weekly workbook.</summary>
public static partial class CoordinatorTools
{
    private const int MaxLevelsListed = 40;
    private const int WriteAttempts = 3;

    /// <summary>
    ///     HEAVY (writes a file): reads the HP tests of one phase and every result, and writes the snapshot JSON to
    ///     <c>ClashReports\</c> beside the open model — the folder comes from the model, never from the caller. The heavy
    ///     gate refuses this member until the user ticks "Allow heavy operations". The model itself is not touched.
    ///     With <paramref name="writeHtml" /> it first writes the Navisworks HTML (Tabular) report of the reported results
    ///     (<see cref="ClashReportRules.ReportedStatuses" />) into a folder named after the snapshot, so the workbook gets
    ///     the pictures Navisworks renders; the snapshot records that folder in <c>htmlReport</c>.
    ///     Returns the file name and the totals; the rows stay in the file (<c>tools/bim-coordinator/build-clash-report.py</c>).
    /// </summary>
    public static object ExportClashSnapshot(Document doc, string? lod, double mmPerUnit, bool writeHtml, int maxPictures, CancellationToken ct)
    {
        RequirePositive(mmPerUnit);
        if (maxPictures is < 1 or > ClashReportRules.MaxPictures) throw new ArgumentException($"maxPictures must be 1..{ClashReportRules.MaxPictures}: Navisworks renders each picture");
        var folder = ClashSnapshotRules.FolderFor(doc.FileName);
        var exportedAt = DateTime.UtcNow;
        var snapshot = NavisClashSnapshotReader.Read(doc, ClashMatrix.Default, lod, mmPerUnit, exportedAt, ct);
        var path = Path.Combine(folder, ClashSnapshotRules.FileNameFor(snapshot.Phase, exportedAt));
        var warnings = new List<string>();
        object? html = null;
        if (writeHtml)
        {
            var htmlFolder = ClashReportRules.HtmlFolderFor(path);
            var outcome = NavisClashReportWriter.Write(doc, snapshot.Tests.Select(t => t.Name), htmlFolder, maxPictures, ct);
            if (outcome is null)
            {
                warnings.Add("no New/Active/Reviewed/Resolved clash to picture: no HTML report written");
            }
            else
            {
                var pictured = new HashSet<string>(outcome.Pictured.Select(g => g.ToString()), StringComparer.OrdinalIgnoreCase);
                snapshot = snapshot with
                {
                    HtmlReport = MakeRelative(folder, outcome.HtmlPath),
                    Rows = snapshot.Rows.Select(r => pictured.Contains(r.Guid) ? r with { Pictured = true } : r).ToList(),
                };
                warnings.AddRange(outcome.Errors.Select(e => "Navisworks report: " + e));
                if (outcome.Candidates > outcome.Pictured.Count)
                    warnings.Add($"pictures for {outcome.Pictured.Count} of {outcome.Candidates} reported clash(es) only (maxPictures; open clashes first, priority group 1 first)");
                html = new { path = outcome.HtmlPath, pictures = outcome.Pictured.Count, candidates = outcome.Candidates, outcome.Seconds, outcome.Fields };
            }
        }

        WriteAtomically(path, ClashSnapshotRules.Serialize(snapshot));

        var notRun = snapshot.Tests.Where(t => t.LastRun is null).Select(t => t.Name).ToList();
        if (notRun.Count > 0) warnings.Add($"{notRun.Count} of {snapshot.Tests.Count} test(s) never ran — run them (Clash Detective > Run All) before reporting");
        if (snapshot.Truncated) warnings.Add($"rows stop at {ClashSnapshotRules.MaxRows}; the test counts are complete");
        return new
        {
            success = true,
            path,
            file = Path.GetFileName(path),
            phase = snapshot.Phase,
            snapshot.ToleranceMm,
            tests = snapshot.Tests.Count,
            notRun = notRun.Count,
            results = snapshot.Tests.Sum(t => t.ResultCount),
            rows = snapshot.Rows.Count,
            html,
            byStatus = ClashSnapshotRules.ByStatus(snapshot),
            byLevel = snapshot.Rows.GroupBy(r => r.Level).OrderByDescending(g => g.Count()).Take(MaxLevelsListed).ToDictionary(g => g.Key, g => g.Count()),
            warnings,
        };
    }

    /// <summary>
    ///     The HP tests of one phase in run order, for the seed that runs them in batches (it calls the heavy run itself
    ///     so the bridge's gate sees it). <paramref name="onlyNotRun" /> resumes after the tests that already ran.
    /// </summary>
    public static PhaseTestList PhaseTestNames(Document doc, string? lod, bool onlyNotRun) =>
        ClashReportRules.PhaseTests(
            ClashTests.NavisClashTestCompiler.AllTests(doc).Select(t => new PhaseTestState(t.DisplayName, t.LastRun is not null)).ToList(),
            lod, onlyNotRun);

    private static string MakeRelative(string folder, string path) =>
        path.StartsWith(folder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ? path.Substring(folder.Length + 1) : path;

    /// <summary>
    ///     Writes through a temporary file and a rename, so a reader never sees half a snapshot; a synced drive (Google
    ///     Drive) can hold the folder for a moment, hence the short retries.
    /// </summary>
    private static void WriteAtomically(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + ".tmp";
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                File.WriteAllText(temporary, content, new UTF8Encoding(false));
                if (File.Exists(path)) File.Delete(path);
                File.Move(temporary, path);
                return;
            }
            catch (IOException) when (attempt < WriteAttempts)
            {
                // a sync client can lock the new file for a moment; the next attempt rewrites the temporary file
                Thread.Sleep(500 * attempt);
            }
        }
    }
}
