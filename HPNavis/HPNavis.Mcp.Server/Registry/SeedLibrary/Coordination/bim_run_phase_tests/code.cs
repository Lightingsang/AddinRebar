// heavy: runs the HP tests of one phase in batches; the run stays here so the bridge's heavy gate sees it.
// Once a test ran nothing may throw: a failed run makes the bridge roll back its undo entry, which undoes the clash runs.
string lod = args.Str("lod", "auto");
string after = args.Str("after", "");
bool onlyNotRun = args.Bool("onlyNotRun", false);
int budgetSeconds = args.Int("budgetSeconds", 300);
if (!app.HasClashModule) throw new InvalidOperationException("Clash Detective is not available in this Navisworks (Simulate); tests need Navisworks Manage.");
if (budgetSeconds < 30 || budgetSeconds > 420) throw new ArgumentException("budgetSeconds must be 30..420: the slowest test has to fit in what is left of the 600 s call");

var list = HPNavis.BIMCoordinator.CoordinatorTools.PhaseTestNames(doc, lod, onlyNotRun);
var todo = HPNavis.BIMCoordinator.Reports.ClashReportRules.RemainingAfter(list.Names, after);
var clash = doc.GetClash().TestsData;
var started = DateTime.UtcNow;
var runs = new List<object>();
var failed = new List<object>();
double slowestSeconds = 0;
string lastAttempted = null;
foreach (string name in todo)
{
    double elapsed = (DateTime.UtcNow - started).TotalSeconds;
    if (ct.IsCancellationRequested || !HPNavis.BIMCoordinator.Reports.ClashReportRules.MayStartAnother(elapsed, slowestSeconds, budgetSeconds)) break;
    progress(runs.Count + failed.Count, todo.Count, name);
    var testStarted = DateTime.UtcNow;
    lastAttempted = name;
    try
    {
        clash.TestsRunTest(HPNavis.BIMCoordinator.CoordinatorTools.FindTest(doc, name));
        long elapsedMs = (long)(DateTime.UtcNow - testStarted).TotalMilliseconds;
        runs.Add(HPNavis.BIMCoordinator.CoordinatorTools.Summarize(doc, name, elapsedMs));
    }
    catch (Exception ex)
    {
        // a failing test (or its summary) must not end the batch, and a throw here would undo the runs already done
        failed.Add(new { name, error = ex.GetType().Name + ": " + ex.Message });
    }

    slowestSeconds = Math.Max(slowestSeconds, (DateTime.UtcNow - testStarted).TotalSeconds);
    log($"'{name}' took {(DateTime.UtcNow - testStarted).TotalMilliseconds:0} ms");
}

int attempted = runs.Count + failed.Count;
bool finished = attempted == todo.Count;
return new
{
    success = failed.Count == 0,
    phase = list.Phase,
    total = list.Names.Count,
    skippedAlreadyRun = list.SkippedAlreadyRun,
    ran = runs.Count,
    failedCount = failed.Count,
    nextAfter = finished ? null : lastAttempted ?? after,
    remaining = todo.Count - attempted,
    seconds = Math.Round((DateTime.UtcNow - started).TotalSeconds, 1),
    summary = finished ? $"phase {list.Phase}: all {todo.Count} test(s) of this call attempted" : $"phase {list.Phase}: stopped on the time budget, call again with after = nextAfter",
    failed,
    runs,
};
