# DISPATCH — explorer_m3_3

## Task Assignment
- Role: Explorer 3 for Milestone M3
- Working directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m3_3\`
- Original Request path: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md`
- Scope Document: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_14\plan.md`
- Re-audit Report: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\plans\reports\261012-2212-hpgrasshopper-reaudit.md` (Findings R8, R9)

## Focus
Investigate Domain Seeds, Rebar Tolerances & Context/Compiler Lifecycle (R8, R9):
1. Analyze `HPGrasshopper/HPGrasshopper.McpBridge/Registry/SeedLibrary/Revit/generate_rebar_curves/code.cs` (lines 35–46, 72–80):
   - Check diameter resolution: why does it pick the nearest diameter without a tolerance policy (e.g. requesting 16mm when only 12mm exists in model creates 12mm without warning/error)?
   - Check normal calculation: why does collinear segments in vertical/inclined planes fail or default improperly?
2. Analyze tool descriptions for `directshape_from_geometry/tool.json` and `query_revit_geometry/tool.json`: align descriptions with actual capabilities.
3. Analyze `HPGrasshopper/HPGrasshopper.McpBridge/Coordinator/GrasshopperContextExtractor.cs` and `InvalidateCompiler`:
   - Differentiate live vs default fallback context clearly when UI thread is unavailable.
   - Wire `InvalidateCompiler` into production lifecycle when references/assemblies change.
4. Formulate concrete implementation recommendations for Worker, ensuring C2 <= 300 lines, M3 <= 50 lines, 0 McpShared changes.
5. Write your report to `handoff.md` and notify orchestrator via `send_message`.

## 2026-10-12T17:15:22Z
You are explorer_m3_3.
Your working directory is:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m3_3\

Read your dispatch instructions at:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m3_3\DISPATCH.md
Read the original request at:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
Read the scope document at:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_14\plan.md
Read the re-audit report at:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\plans\reports\261012-2212-hpgrasshopper-reaudit.md

Your focus is Findings R8 & R9: Domain Seeds (Rebar Tolerance & Collinear Normal) & Context/Compiler Lifecycle.
Formulate a concrete fix strategy for generate_rebar_curves/code.cs, tool.json descriptions, GrasshopperContextExtractor.cs, and InvalidateCompiler wiring.
Write your report to `handoff.md` and notify orchestrator (be638180-e3a9-46c4-834a-a5beaaf11a17) via send_message.
