## 2026-10-05T23:27:52Z
You are teamwork_preview_explorer assigned to survey the Clean Code governance, solution architecture, and test infrastructure for HPBim.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_survey_cleancode

Read the authoritative request at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (specifically under ## 2026-10-05T23:21:03Z).

Your mission is a thorough technical survey of:
1. Clean Code Governance:
   - docs/clean-code/HP_CLEAN_CODE_CORE.md
   - docs/clean-code/host-appendix/revit.md
   - docs/architecture/ARCHITECTURE.md
   - Mandatory metrics: Methods <= 50 lines, Classes <= 300 lines, no empty catch without comment, no dead code, 3 blocking runtime rules Q-B1..Q-B3.
2. Repository conventions & solution setup:
   - Inspect existing solutions (HPEtabs.slnx, HPExcel.slnx, HPRobot.slnx, McpShared.slnx, HPAutoCad.slnx).
   - Global SDK pin: global.json (.NET SDK 10.0.300, Microsoft.Testing.Platform runner).
   - Directory.Build.props settings (Nullable, TreatWarningsAsErrors, LangVersion, etc.).
   - Target frameworks: HPBim.Core (netstandard2.0), HPBim.Client (net8.0;net10.0), HPBim.Cli (net10.0), HPBim.Core.Tests (net10.0 or net8.0 with xUnit v3 / MTP), HPBim.Client.Tests (net10.0).
3. Dependency constraints:
   - HPBim/ MUST ONLY reference McpShared/. Zero cross-references to other MCP folders.
   - HPBim.Core MUST have zero references to Autodesk.Revit.* and run anywhere.
4. CLI structure and Agent prompt packaging for HPBim.Cli:
   - System.CommandLine or args parsing patterns.
   - Commands: validate, generate, verify, estimate.
   - AI agent prompts/roles packaging.

Deliverables:
- Write your comprehensive survey report to g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_survey_cleancode\handoff.md.
- Update g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_survey_cleancode\progress.md.
- Send a message to parent with summary and file path.
