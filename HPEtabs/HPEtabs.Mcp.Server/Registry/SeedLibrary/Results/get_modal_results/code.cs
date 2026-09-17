string caseName = args.Str("caseName", "Modal") ?? "Modal";
int limit = Math.Clamp(args.Int("limit", 200), 1, 500);
eLoadCaseType caseType = eLoadCaseType.Modal; int subType = 0;
if (sapModel.LoadCases.GetTypeOAPI(caseName, ref caseType, ref subType) != 0) throw new ArgumentException($"caseName '{caseName}' is not a load case in this model");
if (caseType != eLoadCaseType.Modal) throw new ArgumentException($"caseName '{caseName}' is a {caseType} case, not a modal case");
int nc = 0; string[] caseNames = null; int[] caseStatus = null;
int rc = sapModel.Analyze.GetCaseStatus(ref nc, ref caseNames, ref caseStatus);
if (rc != 0) throw new InvalidOperationException($"ETABS returned {rc} from Analyze.GetCaseStatus");
int caseIndex = Array.FindIndex(caseNames ?? new string[0], c => string.Equals(c, caseName, StringComparison.OrdinalIgnoreCase));
if (caseIndex >= 0 && caseStatus[caseIndex] != 4) throw new ArgumentException($"case '{caseName}' has no results (status {(caseStatus[caseIndex] == 1 ? "not run" : caseStatus[caseIndex] == 2 ? "could not start" : "not finished")}) — run_analysis first or pick a case that ran");
int r0 = sapModel.Results.Setup.DeselectAllCasesAndCombosForOutput();
if (r0 != 0) throw new InvalidOperationException($"ETABS returned {r0} from Results.Setup.DeselectAllCasesAndCombosForOutput");
if (sapModel.Results.Setup.SetCaseSelectedForOutput(caseName) != 0) throw new InvalidOperationException($"ETABS returned non-zero from Results.Setup.SetCaseSelectedForOutput({caseName})");

int n = 0; string[] loadCase = null, stepType = null; double[] stepNum = null, period = null, frequency = null, circFreq = null, eigen = null;
int ret = sapModel.Results.ModalPeriod(ref n, ref loadCase, ref stepType, ref stepNum, ref period, ref frequency, ref circFreq, ref eigen);
if (ret != 0) throw new InvalidOperationException($"ETABS returned {ret} from Results.ModalPeriod — no modal results for '{caseName}'? run_analysis first");

int nm = 0; string[] mCase = null, mStep = null; double[] mNum = null, mPeriod = null, ux = null, uy = null, uz = null, sux = null, suy = null, suz = null, rx = null, ry = null, rz = null, srx = null, sry = null, srz = null;
int rm = sapModel.Results.ModalParticipatingMassRatios(ref nm, ref mCase, ref mStep, ref mNum, ref mPeriod, ref ux, ref uy, ref uz, ref sux, ref suy, ref suz, ref rx, ref ry, ref rz, ref srx, ref sry, ref srz);
var warnings = new List<string>();
if (rm != 0) warnings.Add($"ModalParticipatingMassRatios returned {rm}; mass ratios omitted");
else if (nm != n) warnings.Add($"ModalParticipatingMassRatios returned {nm} rows for {n} modes");

var modes = new List<object>();
for (int i = 0; i < n && i < limit; i++)
{
    ct.ThrowIfCancellationRequested();
    // Both calls list the selected case's modes in the same order; when the row counts differ, fall back to the mode number.
    int j = rm == 0 && nm == n ? i : -1;
    if (rm == 0 && j < 0) for (int k = 0; k < nm; k++) if (Math.Abs(mNum[k] - stepNum[i]) < 0.5) { j = k; break; }
    modes.Add(new
    {
        mode = (int)stepNum[i], loadCase = loadCase[i], periodS = period[i], frequencyHz = frequency[i], circularFrequencyRadS = circFreq[i], eigenvalue = eigen[i],
        ux = j >= 0 ? ux[j] : (double?)null, uy = j >= 0 ? uy[j] : (double?)null, uz = j >= 0 ? uz[j] : (double?)null,
        rx = j >= 0 ? rx[j] : (double?)null, ry = j >= 0 ? ry[j] : (double?)null, rz = j >= 0 ? rz[j] : (double?)null,
        sumUx = j >= 0 ? sux[j] : (double?)null, sumUy = j >= 0 ? suy[j] : (double?)null, sumRz = j >= 0 ? srz[j] : (double?)null,
    });
}

log($"{n} modes for {caseName}");
return new { success = true, caseName, modes, count = modes.Count, total = n, truncated = n > modes.Count, warnings, summary = $"{modes.Count} modes; T1 = {(n > 0 ? period[0].ToString("0.000") : "n/a")} s" };
