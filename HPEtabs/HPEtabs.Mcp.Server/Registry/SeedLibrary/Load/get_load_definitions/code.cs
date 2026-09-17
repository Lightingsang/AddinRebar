bool includeComboCases = args.Bool("includeComboCases", false);
int limit = Math.Clamp(args.Int("limit", 200), 1, 500);
var warnings = new List<string>();
string ComboTypeName(int t) => t == 0 ? "linear-additive" : t == 1 ? "envelope" : t == 2 ? "absolute-additive" : t == 3 ? "srss" : t == 4 ? "range-additive" : t.ToString();

int np = 0; string[] patNames = null;
int ret = sapModel.LoadPatterns.GetNameList(ref np, ref patNames);
if (ret != 0) throw new InvalidOperationException($"ETABS returned {ret} from LoadPatterns.GetNameList");
var patterns = new List<object>();
foreach (var name in (patNames ?? new string[0]).Take(limit))
{
    ct.ThrowIfCancellationRequested();
    eLoadPatternType type = eLoadPatternType.Dead; double swm = 0;
    int rt = sapModel.LoadPatterns.GetLoadType(name, ref type);
    int rs = sapModel.LoadPatterns.GetSelfWTMultiplier(name, ref swm);
    patterns.Add(new { name, type = rt == 0 ? type.ToString() : null, selfWeightMultiplier = rs == 0 ? swm : (double?)null });
}

int nc = 0; string[] caseNames = null;
int retC = sapModel.LoadCases.GetNameList(ref nc, ref caseNames);
if (retC != 0) throw new InvalidOperationException($"ETABS returned {retC} from LoadCases.GetNameList");
var cases = new List<object>();
foreach (var name in (caseNames ?? new string[0]).Take(limit))
{
    ct.ThrowIfCancellationRequested();
    eLoadCaseType type = eLoadCaseType.LinearStatic; int subType = 0;
    int rt = sapModel.LoadCases.GetTypeOAPI(name, ref type, ref subType);
    cases.Add(new { name, type = rt == 0 ? type.ToString() : null, subType = rt == 0 ? subType : (int?)null });
}

int nk = 0; string[] comboNames = null;
int retK = sapModel.RespCombo.GetNameList(ref nk, ref comboNames);
if (retK != 0) throw new InvalidOperationException($"ETABS returned {retK} from RespCombo.GetNameList");
var combos = new List<object>();
foreach (var name in (comboNames ?? new string[0]).Take(limit))
{
    ct.ThrowIfCancellationRequested();
    int comboType = 0;
    int rt = sapModel.RespCombo.GetTypeOAPI(name, ref comboType);
    object members = null;
    if (includeComboCases)
    {
        int ni = 0; eCNameType[] kinds = null; string[] cnames = null; double[] factors = null;
        int rl = sapModel.RespCombo.GetCaseList(name, ref ni, ref kinds, ref cnames, ref factors);
        if (rl != 0) warnings.Add($"combo {name}: GetCaseList returned {rl}");
        else members = Enumerable.Range(0, ni).Select(i => new { name = cnames[i], kind = kinds[i].ToString(), scaleFactor = factors[i] }).ToList();
    }
    combos.Add(new { name, comboType = rt == 0 ? ComboTypeName(comboType) : null, cases = members });
}

log($"{np} patterns, {nc} cases, {nk} combos");
return new { success = true, patterns, cases, combos, patternCount = np, caseCount = nc, comboCount = nk, truncated = np > limit || nc > limit || nk > limit, warnings, summary = $"{np} load patterns, {nc} load cases, {nk} combinations" };
