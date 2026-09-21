var cases = args.Strings("cases").Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().ToList();
bool deleteResultsFirst = args.Bool("deleteResultsFirst", false);

string StatusName(int status) => status == 1 ? "not-run" : status == 2 ? "could-not-start" : status == 3 ? "not-finished" : status == 4 ? "finished" : status.ToString();

if (cases.Count > 0)
{
    int nk = 0; string[] known = null;
    int rk = sapModel.LoadCases.GetNameList(ref nk, ref known);
    if (rk != 0) throw new InvalidOperationException($"SAP2000 returned {rk} from LoadCases.GetNameList");
    var unknown = cases.Where(c => !(known ?? new string[0]).Contains(c, StringComparer.OrdinalIgnoreCase)).ToList();
    if (unknown.Count > 0) throw new ArgumentException($"not load cases in this model: {string.Join(", ", unknown)} (see get_load_definitions)");

    int rAll = sapModel.Analyze.SetRunCaseFlag("", false, true);
    if (rAll != 0) throw new InvalidOperationException($"SAP2000 returned {rAll} from Analyze.SetRunCaseFlag(all=false)");
    foreach (var c in cases)
    {
        int rc = sapModel.Analyze.SetRunCaseFlag(c, true);
        if (rc != 0) throw new InvalidOperationException($"SAP2000 returned {rc} from Analyze.SetRunCaseFlag({c})");
    }
}
else
{
    int rAll = sapModel.Analyze.SetRunCaseFlag("", true, true);
    if (rAll != 0) throw new InvalidOperationException($"SAP2000 returned {rAll} from Analyze.SetRunCaseFlag(all=true)");
}

if (deleteResultsFirst)
{
    int rd = sapModel.Analyze.DeleteResults("", true);
    if (rd != 0) throw new InvalidOperationException($"SAP2000 returned {rd} from Analyze.DeleteResults(all)");
}

ct.ThrowIfCancellationRequested();
var started = DateTime.UtcNow;
log($"RunAnalysis starting ({(cases.Count > 0 ? string.Join(", ", cases) : "all cases")})…");
int ret = sapModel.Analyze.RunAnalysis();
double seconds = (DateTime.UtcNow - started).TotalSeconds;
if (ret != 0) throw new InvalidOperationException($"SAP2000 returned {ret} from Analyze.RunAnalysis after {seconds:0.0} s");

int nc = 0; string[] caseNames = null; int[] caseStatus = null;
int rs = sapModel.Analyze.GetCaseStatus(ref nc, ref caseNames, ref caseStatus);
if (rs != 0) throw new InvalidOperationException($"SAP2000 returned {rs} from Analyze.GetCaseStatus");
var ranCases = Enumerable.Range(0, nc).Select(i => new { name = caseNames[i], status = StatusName(caseStatus[i]) }).ToList();
int finished = ranCases.Count(c => c.status == "finished");
var errors = ranCases.Where(c => c.status == "could-not-start" || c.status == "not-finished").Select(c => new { code = "CASE_FAILED", message = $"case {c.name}: {c.status}", name = c.name }).ToList();
log($"analysis finished in {seconds:0.0} s; {finished} of {ranCases.Count} cases finished, {errors.Count} failed");
return new { success = errors.Count == 0, ranCases, finishedCount = finished, errors, durationSeconds = Math.Round(seconds, 1), isLocked = sapModel.GetModelIsLocked(), summary = $"analysis ran in {seconds:0.0} s; {finished}/{ranCases.Count} cases finished{(errors.Count > 0 ? $", {errors.Count} failed" : "")}; snapshot in the result" };
