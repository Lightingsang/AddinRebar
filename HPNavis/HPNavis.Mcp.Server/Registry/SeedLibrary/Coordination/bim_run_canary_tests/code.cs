// heavy: run the canary tests chosen by the engine; the run stays here so the bridge's heavy gate sees it
string lod = args.Str("lod", "350");
int count = args.Int("count", 3);
var ruleIds = args.Strings("ruleIds");
if (!app.HasClashModule) throw new InvalidOperationException("Clash Detective is not available in this Navisworks (Simulate); tests need Navisworks Manage.");

var names = HPNavis.BIMCoordinator.CoordinatorTools.CanaryTestNames(doc, lod, units.ToMm(1), count, ruleIds, ct);
var clash = doc.GetClash().TestsData;
var runs = new List<object>();
foreach (var name in names)
{
    ct.ThrowIfCancellationRequested();
    progress(runs.Count, names.Count, name);
    var started = DateTime.UtcNow;
    clash.TestsRunTest(HPNavis.BIMCoordinator.CoordinatorTools.FindTest(doc, name));
    var elapsedMs = (long)(DateTime.UtcNow - started).TotalMilliseconds;
    log($"canary '{name}' ran in {elapsedMs} ms");
    runs.Add(HPNavis.BIMCoordinator.CoordinatorTools.Summarize(doc, name, elapsedMs));
}

return new { success = true, lod, summary = $"{runs.Count} canary test(s) run at LOD{lod}", runs };
