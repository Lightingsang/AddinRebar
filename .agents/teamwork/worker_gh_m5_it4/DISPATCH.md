## 2026-10-10T15:31:52Z
You are worker_gh_m5_it4 (Milestone M5 Iteration 4 Remediation Worker).
Your working directory is:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_gh_m5_it4

MANDATORY FIRST STEP:
Read the user requirements from:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
Specifically section ## 2026-10-10T06:56:11Z.

Also read:
- Master project index: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md
- Test Infra specification: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\TEST_INFRA.md
- Full Forensic Auditor Veto Report: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_gh_m5_it3_1\handoff.md
- Explorer 1 Remediation Blueprint (WPF & Clipboard): g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_gh_m5_it4_1\wpf_clipboard_remediation.md
- Explorer 2 Remediation Blueprint (Canvas & Image Export): g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_gh_m5_it4_2\canvas_image_remediation.md
- Explorer 3 Remediation Blueprint (Seed Dimensions & Holistic 15-target Remediation): g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_gh_m5_it4_3\seed_and_holistic_remediation.md

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

EXCLUSIVE WRITE OWNERSHIP:
You exclusively own:
- `HPGrasshopper/HPGrasshopper.E2E.Tests/**`
- `run-e2e-grasshopper-tests.ps1`
- `TEST_READY.md`
- `PROJECT.md`

TASK OBJECTIVE:
Apply the comprehensive drop-in remediation designs from Explorers 1, 2, and 3 to eliminate 100% of residual pseudo-tests across `HPGrasshopper.E2E.Tests`:

1. Apply WPF & Clipboard Remediation (from explorer_gh_m5_it4_1):
   - Add `Harness/GrasshopperTestBridgeRunner.cs` implementing `IMcpBridgeRunner`.
   - In `Tiers/Tier1_NamedPipeAndWpfTests.cs`: replace `TC_T1_WPF_04`.
   - In `Tiers/Tier2_NamedPipeAndWpfBoundariesTests.cs`: replace `TC_T2_WPF_04` and `TC_T2_WPF_05`.

2. Apply Canvas & Image Export Remediation (from explorer_gh_m5_it4_2):
   - Update `Harness/GrasshopperE2EHarness.cs` (enhance `ExportCanvasImage` and add `ExtractComponentMessages`).
   - In `Tiers/Tier1_EditAndExportToolsTests.cs`: replace `TC_T1_IMG_03`.
   - In `Tiers/Tier2_EditAndExportBoundariesTests.cs`: replace `TC_T2_IMG_05`.
   - In `Tiers/Tier1_CanvasAndPlayerToolsTests.cs`: replace `TC_T1_RDC_05`.

3. Apply Seed Dimensions & Holistic Hardening (from explorer_gh_m5_it4_3):
   - In `Tiers/Tier2_RegistryAndSeedBoundariesTests.cs`: replace `TC_T2_SED_03` (and apply hardening to `TC_T2_SED_01`, `TC_T2_REG_05`).
   - In `Tiers/Tier1_RegistryAndSeedsTests.cs`: apply hardening to `TC_T1_SED_04`.
   - In `Tiers/Tier1_EditAndExportToolsTests.cs`: apply hardening to `TC_T1_IMG_01`, `TC_T1_IMG_02`, `TC_T1_IMG_05`.
   - In `Tiers/Tier2_CanvasAndPlayerBoundariesTests.cs`: apply hardening to `TC_T2_RPL_02`.
   - In `Tiers/Tier3_CrossFeatureInteractionTests.cs`: apply hardening to `TC_T3_INT_09`.

4. Clean Code Governance Compliance:
   - Rule C2: EVERY .cs file in `HPGrasshopper.E2E.Tests` strictly <= 300 lines.
   - Rule M3: EVERY method in `HPGrasshopper.E2E.Tests` strictly <= 50 lines (target <= 40 lines).
   - Rule Q-B1: Zero commented-out or dead code.
   - Rule Q-B2: Zero unannotated empty catch blocks.
   - Run AST audit: `python scratch/verify_clean_code_ast.py "HPGrasshopper/HPGrasshopper.E2E.Tests" 40`.

5. Verification Commands (YOU MUST RUN):
   - `dotnet build HPGrasshopper/HPGrasshopper.slnx -c Release` (0 errors, 0 warnings).
   - `dotnet test HPGrasshopper/HPGrasshopper.E2E.Tests -c Release` (181/181 PASS, 0 popups).
   - `powershell -ExecutionPolicy Bypass -File .\run-e2e-grasshopper-tests.ps1` (181/181 PASS).
   - `dotnet test HPGrasshopper/HPGrasshopper.McpBridge.Tests -c Release` (110/110 PASS).
   - `dotnet test HPGrasshopper/HPGrasshopper.Mcp.Server.Tests -c Release` (77/77 PASS).

6. Update Documentation & Deliver Handoff:
   - Update `TEST_READY.md` and `PROJECT.md`.
   - Write comprehensive handoff report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_gh_m5_it4\handoff.md`.
   - Call `send_message` when done.
