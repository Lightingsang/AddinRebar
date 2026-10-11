using Autodesk.Navisworks.Api;
using Autodesk.Navisworks.Api.Clash;
using Autodesk.Navisworks.Api.Interop;

namespace HPNavis.BIMCoordinator.Reports;

/// <summary>
///     Writes the Navisworks clash report (Clash Detective > Report tab) for chosen results, so its pictures are the ones
///     Navisworks renders. The public API has no report writer; this drives the same static <c>LcClClashReport</c> calls
///     the Report tab's "Write Report" button makes, with a GUI proxy that answers the file prompt with our path instead
///     of a dialog. The tab's settings (format, driver, fields, statuses, filter mode) are the user's and are restored
///     afterwards. Internal: reached only through the heavy <see cref="CoordinatorTools.ExportClashSnapshot" />.
///     Navisworks main thread only.
/// </summary>
internal static class NavisClashReportWriter
{
    /// <param name="Pictured">GUIDs of the results the report carries (one picture each).</param>
    /// <param name="Candidates">Reported results that asked for a picture, pictured or not (over the budget).</param>
    public sealed record Outcome(string HtmlPath, IReadOnlyCollection<Guid> Pictured, int Candidates, double Seconds,
        IReadOnlyList<string> Fields, IReadOnlyList<string> Errors);

    /// <summary>The report, or null when no result qualifies (nothing to picture: no report is written).</summary>
    public static Outcome? Write(Document doc, IEnumerable<string> testNames, string folder, int maxPictures, CancellationToken ct)
    {
        if (!LcClClashReport.CanWriteReport(doc.State)) throw new InvalidOperationException("Navisworks cannot write a clash report now (Clash Detective not ready)");
        using var selector = new LcClashReportResultSelector();
        var budget = new PictureBudget(maxPictures);
        SelectReportedResults(doc, testNames, selector, budget, ct);
        if (budget.Taken.Count == 0) return null;

        Directory.CreateDirectory(folder);
        using var proxy = new FixedPathProxy(Path.Combine(folder, ClashReportRules.HtmlFileName), folder);
        var saved = ReportTabSettings.Capture();
        var started = DateTime.UtcNow;
        IReadOnlyList<string> fields;
        Exception? failure = null;
        try
        {
            fields = ReportTabSettings.ForPictures();
            // the last argument is the filter text the report prints in its header; there is none, every result is chosen by GUID
            var written = LcClClashReport.WriteReport(doc.State, proxy, selector, string.Empty);
            if (!written || !File.Exists(proxy.LastPath)) throw new InvalidOperationException("Navisworks did not write the clash report: " + string.Join("; ", proxy.Errors.DefaultIfEmpty("no reason given")));
        }
        catch (Exception exception)
        {
            failure = exception;
            throw;
        }
        finally
        {
            RestoreKeepingTheFirstError(saved, failure);
        }

        return new Outcome(proxy.LastPath, budget.Taken, budget.Seen, Math.Round((DateTime.UtcNow - started).TotalSeconds, 1), fields, proxy.Errors);
    }

    /// <summary>A failed restore must not hide why the report failed; when the report itself succeeded it surfaces.</summary>
    private static void RestoreKeepingTheFirstError(ReportTabSettings saved, Exception? failure)
    {
        try
        {
            saved.Restore();
        }
        catch (Exception) when (failure is not null)
        {
            // the report's own failure is the one the caller needs; the tab keeps whatever was not restored
        }
    }

    /// <summary>How many pictures the report may render (each one costs Navisworks a render) and which results got one.</summary>
    private sealed class PictureBudget
    {
        private readonly int _max;

        public PictureBudget(int max) => _max = max;

        public HashSet<Guid> Taken { get; } = new();
        public int Seen { get; private set; }

        public bool Take(Guid guid)
        {
            Seen++;
            if (Taken.Count >= _max) return false;
            Taken.Add(guid);
            return true;
        }
    }

    /// <summary>
    ///     The reported results of the named tests up to the picture budget: open results first (they are what a meeting
    ///     acts on), then resolved ones, each pass in test-name order (priority group 1 first). A result group that holds a
    ///     chosen result is added too, the way the Report tab's own selection walks groups by GUID.
    /// </summary>
    private static void SelectReportedResults(Document doc, IEnumerable<string> testNames, LcClashReportResultSelector selector, PictureBudget budget, CancellationToken ct)
    {
        var wanted = new HashSet<string>(testNames, StringComparer.Ordinal);
        var tests = ClashTests.NavisClashTestCompiler.AllTests(doc).Where(t => wanted.Contains(t.DisplayName)).OrderBy(t => t.DisplayName, StringComparer.Ordinal).ToList();
        foreach (var pass in new Func<string, bool>[] { ClashReportRules.IsOpen, status => ClashReportRules.IsReported(status) && !ClashReportRules.IsOpen(status) })
        {
            foreach (var test in tests)
            {
                ct.ThrowIfCancellationRequested();
                Select(test, pass, selector, budget);
            }
        }
    }

    private static int Select(GroupItem parent, Func<string, bool> inPass, LcClashReportResultSelector selector, PictureBudget budget)
    {
        var count = 0;
        foreach (var child in parent.Children)
        {
            if (child is ClashResult result && inPass(result.Status.ToString()))
            {
                if (!budget.Take(result.Guid)) continue;
                selector.AddGuid(result.Guid);
                count++;
            }
            else if (child is GroupItem group)
            {
                var inside = Select(group, inPass, selector, budget);
                if (inside > 0 && !selector.ContainsGuid(group.Guid)) selector.AddGuid(group.Guid);
                count += inside;
            }
        }

        return count;
    }

    /// <summary>Answers the report's save prompt with a fixed path; errors are collected instead of shown.</summary>
    private sealed class FixedPathProxy : LcClClashGUIProxy
    {
        private readonly string _file;
        private readonly string _folder;

        public FixedPathProxy(string file, string folder)
        {
            _file = file;
            _folder = folder;
            LastPath = file;
        }

        public string LastPath { get; private set; }
        public List<string> Errors { get; } = new();

        /// <summary>Our file name with the extension the chosen format asks for (the HTML formats ask for "html").</summary>
        public override bool PromptForFilename(string extension, string suggestedName, string filter, out string filename)
        {
            filename = string.IsNullOrWhiteSpace(extension) ? _file : Path.ChangeExtension(_file, extension);
            LastPath = filename;
            return true;
        }

        public override bool PromptForFolder(string title, string startFolder, out string folder)
        {
            folder = _folder;
            LastPath = _folder;
            return true;
        }

        public override void ReportError(string message) => Errors.Add(message);
    }

    /// <summary>The Report tab's settings, captured so writing our report leaves the user's choices as they were.</summary>
    private sealed class ReportTabSettings
    {
        private int _formatter;
        private int _driver;
        private bool _filterMode;
        private bool[] _fields = Array.Empty<bool>();
        private bool[] _statusFlags = Array.Empty<bool>();

        public static ReportTabSettings Capture() => new()
        {
            _formatter = LcClClashReport.GetCurrentReportFormatter(),
            _driver = LcClClashReport.GetCurrentReportDriver(),
            _filterMode = LcClClashReport.GetCurrentFilterMode(),
            _fields = Enumerable.Range(0, LcClClashReport.NumReportFields()).Select(i => LcClClashReport.GetReportField(i).GetEnabled()).ToArray(),
            _statusFlags = Enumerable.Range(0, LcClClashReport.NumStatusFlags()).Select(i => LcClClashReport.GetStatusFlag(i).GetEnabled()).ToArray(),
        };

        /// <summary>
        ///     HTML table, every test in one file, every status, no UI filter, and every field except the items' full
        ///     property lists (live: they add one column per property of the model — 826 on THCSLT, 12 MB for 20 clashes).
        ///     Field names are only exposed localised; without an English "Image" field the report would not be the one the
        ///     workbook reads, so it is refused before anything is written. Returns the names of the fields written.
        /// </summary>
        public static IReadOnlyList<string> ForPictures()
        {
            var names = Enumerable.Range(0, LcClClashReport.NumReportFields()).Select(FieldName).ToList();
            if (!names.Contains(ClashReportRules.PictureField, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException($"the clash report fields are not the English ones (no '{ClashReportRules.PictureField}' among: {string.Join(", ", names)}) — write the report by hand (skill hp-bim-clash-report)");

            LcClClashReport.SetCurrentReportFormatter(ClashReportRules.IndexOf(Names(LcClClashReport.NumReportFormatters(), FormatterName), ClashReportRules.HtmlTabularFormatter, "format"));
            LcClClashReport.SetCurrentReportDriver(ClashReportRules.IndexOf(Names(LcClClashReport.NumReportDrivers(), DriverName), ClashReportRules.CombinedDriver, "driver"));
            LcClClashReport.SetCurrentFilterMode(false);
            var written = new List<string>();
            for (var i = 0; i < names.Count; i++)
            {
                var wanted = !ClashReportRules.IsPropertyListField(names[i]);
                LcClClashReport.GetReportField(i).SetEnabled(wanted);
                if (wanted) written.Add(names[i]);
            }

            for (var i = 0; i < LcClClashReport.NumStatusFlags(); i++) LcClClashReport.GetStatusFlag(i).SetEnabled(true);
            return written;
        }

        public void Restore()
        {
            LcClClashReport.SetCurrentReportFormatter(_formatter);
            LcClClashReport.SetCurrentReportDriver(_driver);
            LcClClashReport.SetCurrentFilterMode(_filterMode);
            for (var i = 0; i < _fields.Length; i++) LcClClashReport.GetReportField(i).SetEnabled(_fields[i]);
            for (var i = 0; i < _statusFlags.Length; i++) LcClClashReport.GetStatusFlag(i).SetEnabled(_statusFlags[i]);
        }

        private static IReadOnlyList<string> Names(int count, Func<int, string> name) => Enumerable.Range(0, count).Select(name).ToList();

        private static string FieldName(int index)
        {
            LcClClashReport.GetReportField(index).GetUserNameW(out var name);
            return name;
        }

        private static string FormatterName(int index)
        {
            LcClClashReport.GetReportFormatterName(index, out var name);
            return name;
        }

        private static string DriverName(int index)
        {
            LcClClashReport.GetReportDriverInternalName(index, out var name);
            return name;
        }
    }
}
