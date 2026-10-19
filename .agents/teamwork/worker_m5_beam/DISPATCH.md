# DISPATCH — worker_m5_beam

You are worker_m5_beam (teamwork_preview_worker).
Your working directory is: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m5_beam\
Your parent is orchestrator_3 (conversation ID: 4db7c650-146a-4a05-a1e1-c0fb57e2733b).

## Mandatory Inputs to Read First:
1. ORIGINAL_REQUEST.md at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (check section ## 2026-10-07T22:29:51Z).
2. PROJECT.md at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md.
3. Survey reports:
   - G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_beam_2\handoff.md (TCVN 5574 formulas & ClosedXML export requirements).
   - G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_beam_3\handoff.md (Revit 2026 Rebar API, TransactionGroup & BarTerminationsData).
4. Domain models in `HPRebar.Core/Tcvn5574/Models/` (BeamForceEnvelope.cs, BeamCalculationReportModel.cs, FlexureDesignResult.cs, ShearDesignResult.cs).

## MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Implementation Tasks for Milestone M5:
1. Implement `HPRebar/HPRebar/BeamRebar/Service/ClosedXmlBeamForceReader.cs`:
   - Parses Excel workbook (.xlsx via ClosedXML) containing force envelopes.
   - Reads worksheet rows into `IReadOnlyList<BeamForceEnvelope>` mapping BeamMark, SpanIndex, SpanLengthMm, moments, shears.
   - Robust error handling for missing columns or malformed numbers.
2. Implement `HPRebar/HPRebar/BeamRebar/Service/EtabsBeamForceReader.cs`:
   - Formats / extracts beam force envelopes for continuous beam spans.
3. Implement `HPRebar/HPRebar/BeamRebar/Service/BeamCalculationReportExporter.cs`:
   - Exports calculation verification report to `.xlsx` using ClosedXML.
   - Professional table formatting: Title, Project info, TCVN 5574:2018 Material grades (B15-B40, CB240-T..CB500-V), Geometry, Forces, Flexure checks, Shear checks, Spacing checks, and Final status (ĐẠT / KHÔNG ĐẠT).
4. Update `HPRebar/HPRebar/BeamRebar/Service/RebarCreationService.cs`:
   - Wrap the 5 staged creation transactions in an outer `using (var tg = new TransactionGroup(document, "Beam Rebar")) { tg.Start(); ... tg.Assimilate(); }` so that all 5 phases commit as a single undoable transaction in Revit.
5. Clean Code constraints:
   - Each file <= 300 lines.
   - Each method <= 50 lines.
   - Zero empty catches, zero warnings/errors.
   - Rule D1: Zero ClosedXML or Revit references in `HPRebar.Core` (all ClosedXML usage remains strictly in `HPRebar`).
6. Verification:
   - `dotnet build HPRebar/HPRebar.csproj -c Debug.R26 -p:DeployAddin=false` succeeds with 0 errors.
   - `dotnet build HPRebar/HPRebar.slnx -c Debug.R26 -p:DeployAddin=false` succeeds with 0 errors.
   - `dotnet test HPRebar/HPRebar.Core.Tests` passes 100% (1682 tests).

Write your report to: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m5_beam\handoff.md.
When done, report to orchestrator_3 via send_message.
