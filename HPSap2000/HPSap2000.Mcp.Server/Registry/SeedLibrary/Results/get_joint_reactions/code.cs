string caseOrCombo = args.Require("caseOrCombo");
int limit = Math.Clamp(args.Int("limit", 200), 1, 500);

var pointNames = args.Strings("pointNames").Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().ToList();
if (pointNames.Count > 0)
{
    int npn = 0; string[] all = null;
    int rn = sapModel.PointObj.GetNameList(ref npn, ref all);
    if (rn != 0) throw new InvalidOperationException($"SAP2000 returned {rn} from PointObj.GetNameList");
    var known = new HashSet<string>(all ?? new string[0], StringComparer.OrdinalIgnoreCase);
    var unknown = pointNames.Where(p => !known.Contains(p)).ToList();
    if (unknown.Count > 0) throw new ArgumentException($"unknown point names: {string.Join(", ", unknown)}");
}

int nc = 0; string[] caseNames = null; int[] caseStatus = null;
int rc = sapModel.Analyze.GetCaseStatus(ref nc, ref caseNames, ref caseStatus);
if (rc != 0) throw new InvalidOperationException($"SAP2000 returned {rc} from Analyze.GetCaseStatus");
int caseIndex = Array.FindIndex(caseNames ?? new string[0], c => string.Equals(c, caseOrCombo, StringComparison.OrdinalIgnoreCase));
if (caseIndex >= 0 && caseStatus[caseIndex] != 4) throw new ArgumentException($"case '{caseOrCombo}' has no results (status {(caseStatus[caseIndex] == 1 ? "not run" : caseStatus[caseIndex] == 2 ? "could not start" : "not finished")}) — run_analysis first or pick a case that ran");

int r0 = sapModel.Results.Setup.DeselectAllCasesAndCombosForOutput();
if (r0 != 0) throw new InvalidOperationException($"SAP2000 returned {r0} from Results.Setup.DeselectAllCasesAndCombosForOutput");
bool selected = sapModel.Results.Setup.SetCaseSelectedForOutput(caseOrCombo) == 0 || sapModel.Results.Setup.SetComboSelectedForOutput(caseOrCombo) == 0;
if (!selected) throw new ArgumentException($"caseOrCombo '{caseOrCombo}' is neither a load case nor a load combination in this model");

int n = 0; string[] obj = null, elm = null, loadCase = null, stepType = null; double[] stepNum = null, f1 = null, f2 = null, f3 = null, m1 = null, m2 = null, m3 = null;
int ret = pointNames.Count == 0
    ? sapModel.Results.JointReact("All", eItemTypeElm.GroupElm, ref n, ref obj, ref elm, ref loadCase, ref stepType, ref stepNum, ref f1, ref f2, ref f3, ref m1, ref m2, ref m3)
    : 0;
if (ret != 0) throw new InvalidOperationException($"SAP2000 returned {ret} from Results.JointReact — no results for '{caseOrCombo}'? run_analysis first");

var items = new List<object>();
int kept = 0;
void Add(int i)
{
    kept++;
    if (items.Count >= limit) return;
    items.Add(new { point = obj[i], loadCase = loadCase[i], stepType = stepType[i], stepNum = stepNum[i], fxKN = Math.Round(f1[i], 3), fyKN = Math.Round(f2[i], 3), fzKN = Math.Round(f3[i], 3), mxKNm = Math.Round(m1[i], 3), myKNm = Math.Round(m2[i], 3), mzKNm = Math.Round(m3[i], 3) });
}

int total = 0;
if (pointNames.Count == 0)
{
    total = n;
    for (int i = 0; i < n; i++) { ct.ThrowIfCancellationRequested(); Add(i); }
}
else
{
    foreach (var point in pointNames)
    {
        ct.ThrowIfCancellationRequested();
        int rp = sapModel.Results.JointReact(point, eItemTypeElm.ObjectElm, ref n, ref obj, ref elm, ref loadCase, ref stepType, ref stepNum, ref f1, ref f2, ref f3, ref m1, ref m2, ref m3);
        if (rp != 0) throw new InvalidOperationException($"SAP2000 returned {rp} from Results.JointReact({point}) — no results for '{caseOrCombo}'? run_analysis first");
        total += n;
        for (int i = 0; i < n; i++) Add(i);
    }
}

log($"{items.Count} reaction rows for {caseOrCombo}");
return new { success = true, caseOrCombo, items, count = items.Count, matched = kept, total, truncated = kept > items.Count, summary = $"{items.Count} of {kept} joint reaction rows for {caseOrCombo} (kN, kN·m)" };
