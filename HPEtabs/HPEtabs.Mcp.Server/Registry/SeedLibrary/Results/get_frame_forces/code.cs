string caseOrCombo = args.Require("caseOrCombo");
int limit = Math.Clamp(args.Int("limit", 200), 1, 500);

var frameNames = args.Strings("frameNames").Where(f => !string.IsNullOrWhiteSpace(f)).Distinct().ToList();
if (frameNames.Count > 0)
{
    int nfn = 0; string[] all = null;
    int rn = sapModel.FrameObj.GetNameList(ref nfn, ref all);
    if (rn != 0) throw new InvalidOperationException($"ETABS returned {rn} from FrameObj.GetNameList");
    var known = new HashSet<string>(all ?? new string[0], StringComparer.OrdinalIgnoreCase);
    var unknown = frameNames.Where(f => !known.Contains(f)).ToList();
    if (unknown.Count > 0) throw new ArgumentException($"not unique frame names (labels are not names — use get_structural_objects.name): {string.Join(", ", unknown)}");
}

int nc = 0; string[] caseNames = null; int[] caseStatus = null;
int rc = sapModel.Analyze.GetCaseStatus(ref nc, ref caseNames, ref caseStatus);
if (rc != 0) throw new InvalidOperationException($"ETABS returned {rc} from Analyze.GetCaseStatus");
int caseIndex = Array.FindIndex(caseNames ?? new string[0], c => string.Equals(c, caseOrCombo, StringComparison.OrdinalIgnoreCase));
// A load case that exists but was never run has no results: say so instead of reporting an empty table (status 4 = finished).
if (caseIndex >= 0 && caseStatus[caseIndex] != 4) throw new ArgumentException($"case '{caseOrCombo}' has no results (status {(caseStatus[caseIndex] == 1 ? "not run" : caseStatus[caseIndex] == 2 ? "could not start" : "not finished")}) — run_analysis first or pick a case that ran");
int r0 = sapModel.Results.Setup.DeselectAllCasesAndCombosForOutput();
if (r0 != 0) throw new InvalidOperationException($"ETABS returned {r0} from Results.Setup.DeselectAllCasesAndCombosForOutput");
bool selected = sapModel.Results.Setup.SetCaseSelectedForOutput(caseOrCombo) == 0 || sapModel.Results.Setup.SetComboSelectedForOutput(caseOrCombo) == 0;
if (!selected) throw new ArgumentException($"caseOrCombo '{caseOrCombo}' is neither a load case nor a load combination in this model");


int n = 0; string[] obj = null, elm = null, loadCase = null, stepType = null; double[] objSta = null, elmSta = null, stepNum = null, p = null, v2 = null, v3 = null, t = null, m2 = null, m3 = null;
var items = new List<object>();
int total = 0;
void Take(int rows)
{
    total += rows;
    for (int i = 0; i < rows && items.Count < limit; i++)
        items.Add(new { frame = obj[i], stationMm = objSta[i], loadCase = loadCase[i], stepType = stepType[i], stepNum = stepNum[i], pKN = p[i], v2KN = v2[i], v3KN = v3[i], tKNm = t[i] / 1000, m2KNm = m2[i] / 1000, m3KNm = m3[i] / 1000 });
}

if (frameNames.Count == 0)
{
    int ret = sapModel.Results.FrameForce("All", eItemTypeElm.GroupElm, ref n, ref obj, ref objSta, ref elm, ref elmSta, ref loadCase, ref stepType, ref stepNum, ref p, ref v2, ref v3, ref t, ref m2, ref m3);
    if (ret != 0) throw new InvalidOperationException($"ETABS returned {ret} from Results.FrameForce — no results for '{caseOrCombo}'? run_analysis first");
    Take(n);
}
else
{
    foreach (var frame in frameNames)
    {
        ct.ThrowIfCancellationRequested();
        int ret = sapModel.Results.FrameForce(frame, eItemTypeElm.ObjectElm, ref n, ref obj, ref objSta, ref elm, ref elmSta, ref loadCase, ref stepType, ref stepNum, ref p, ref v2, ref v3, ref t, ref m2, ref m3);
        if (ret != 0) throw new InvalidOperationException($"ETABS returned {ret} from Results.FrameForce({frame}) — no results for '{caseOrCombo}'? run_analysis first");
        Take(n);
    }
}

log($"{items.Count} force rows for {caseOrCombo}");
return new { success = true, caseOrCombo, items, count = items.Count, total, truncated = total > items.Count, summary = $"{items.Count} frame force rows for {caseOrCombo} (kN, kN·m, stations in mm)" };
