# Dispatch: worker_m5_remediation_beam

- **Assigned Agent**: worker_m5_remediation_beam
- **Role**: teamwork_preview_worker
- **Objective**: Remediate Defects in Milestone M5 (ClosedXmlBeamForceReader & BeamCalculationReportExporter)
- **Primary References**:
  - Challenger Rejection Report: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m5_beam_1\handoff.md`
  - Reviewer Report: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m5_beam_1\handoff.md`
  - Test Harness: `tests/m5_challenger_harness/`

- **Remediation Tasks**:
  1. In `HPRebar/HPRebar/BeamRebar/Service/ClosedXmlBeamForceReader.cs`:
     - Fix `ReadDouble` method (around line 145-162):
       Change `NumberStyles.Any` to `NumberStyles.Float` so `CultureInfo.InvariantCulture` does not treat commas as thousands grouping separators (which was turning `"180,25"` into `18025.0`!). This allows comma-separated decimals to fall through cleanly to `CultureInfo.GetCultureInfo("vi-VN")`.
       Guard against `NaN` and `Infinity`:
       ```csharp
       private static double ReadDouble(IXLRow row, int col, double defaultValue)
       {
           if (col <= 0) return defaultValue;
           var cell = row.Cell(col);
           if (cell.IsEmpty()) return defaultValue;

           if (cell.TryGetValue<double>(out double val))
               return double.IsNaN(val) || double.IsInfinity(val) ? defaultValue : val;

           string str = cell.GetString().Trim();
           if (double.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedInv) &&
               !double.IsNaN(parsedInv) && !double.IsInfinity(parsedInv))
               return parsedInv;

           if (double.TryParse(str, NumberStyles.Float, CultureInfo.GetCultureInfo("vi-VN"), out double parsedVi) &&
               !double.IsNaN(parsedVi) && !double.IsInfinity(parsedVi))
               return parsedVi;

           return defaultValue;
       }
       ```
     - Fix `MatchHeader` method (around line 114-136):
       Prevent substring shadowing where generic `"nhịp"` matched before `"m_nhịp"` and `"q_nhịp"`.
       Specifically, check compound force keywords (`m_nhịp`, `m_nhip`, `q_nhịp`, `q_nhip`, etc.) BEFORE checking generic `"nhịp"`, or check `text.Equals("nhịp") || text.Equals("span") || (text.Contains("nhịp") && !text.Contains("m_") && !text.Contains("q_") && !text.Contains("v_"))`.

  2. In `HPRebar/HPRebar/BeamRebar/Service/BeamCalculationReportExporter.cs`:
     - In `Export` and `CreateWorkbook`: Add defensive argument validation:
       `if (reports == null) throw new ArgumentNullException(nameof(reports));`

  3. Verification:
     - Run the empirical challenger test harness:
       `dotnet run --project tests/m5_challenger_harness/M5ChallengerHarness.csproj`
       Ensure all tests pass!
     - Run `dotnet build HPRebar/HPRebar.csproj -c Debug.R26 -p:DeployAddin=false` and confirm 0 errors.
     - Run `dotnet test HPRebar.Core.Tests` and confirm all 1,682 tests pass.
     - Check Clean Code: All files <= 300 lines, all methods <= 50 lines, 0 empty catches.


## 2026-10-08T00:57:00Z
Received dispatch from orchestrator_3:
Remediate defects in Milestone M5 (ClosedXmlBeamForceReader & BeamCalculationReportExporter).
1. Fix ReadDouble (NumberStyles.Float + NaN/Infinity guards) and MatchHeader (avoid substring shadowing of nhịp vs m_nhịp/q_nhịp).
2. Fix BeamCalculationReportExporter null check for reports.
3. Verify with M5ChallengerHarness, HPRebar build, HPRebar.Core.Tests, Clean code rules (<=300 lines, <=50 lines/method).
