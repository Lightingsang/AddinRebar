## 2026-10-10T14:50:09Z
[Message] timestamp=2026-10-10T14:50:09Z sender=6b50efdd-bc13-44c7-a3d0-de935480c02b priority=MESSAGE_PRIORITY_HIGH content=You are the Final Forensic Auditor (auditor_final) for the HPRebar & HPRebar.McpBridge MaterialDesign Migration project.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_final\

MANDATORY READING:
Read the authoritative request at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (entry dated 2026-10-10T13:10:58Z).
Read the project scope at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_11\PROJECT.md.
Read TEST_READY.md at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_11\TEST_READY.md.
Read handoffs from:
- worker_m1 (g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m1\handoff.md)
- worker_m2 (g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2\handoff.md)
- worker_m3 (g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3\handoff.md)
- worker_m4 (g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4\handoff.md)
- worker_m5 (g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m5\handoff.md)

Audit Mission & Forensic Checks:
Perform an exhaustive, independent forensic integrity audit on the entire deliverable across all 5 core requirements (R1–R5) and acceptance criteria:
1. R1 Controls Audit: Verify direct usage of MaterialDesignInXamlToolkit v5.3.2 controls and implicit styles across all modules.
2. R2 Redundancy & Preservation Audit: Verify removal of redundant custom HP styles (CardBorderStyle, DataGridHeaderStyle, BadgeBorderStyle, SectionHeaderStyle, etc.) AND 100% preservation of domain configurations (right-aligned numeric inputs, validation error templates, rebar 2D/3D canvases/Helix materials, MVVM commands, Vietnamese text).
3. R3 Theming & Dynamic Switch Audit: Verify MaterialThemeBridge.cs and runtime Dark/Light overlay swapping without PaletteHelper or Application.Current.
4. R4 In-Process CAD/BIM Packaging Audit: Verify ILRepack configuration, Segoe UI font override, strictly 0 Roboto pack URIs across all XAML files, and 0 loose toolkit DLLs in output folders.
5. R5 ThemeGallery & Tests Audit: Verify ThemeGallery headless capability (RenderTargetBitmap), KataSupport 7-parameter record fix, McpBridge view, and ThemeTokenCoverageTests coverage.
6. Clean Code Governance Audit:
   - Rule C2: Files <= 300 lines
   - Rule M3: Methods <= 50 lines
   - Rule Q-B1: Zero dead/commented-out code
   - Rule Q-B2: Zero unannotated empty catch blocks
7. Verification Commands (Run independently):
   - dotnet build HPRebar/HPRebar.csproj -c Debug.R26 -p:DeployAddin=false
   - dotnet build HPRebar/HPRebar.csproj -c Debug.R25 -p:DeployAddin=false
   - dotnet build HPRebar/HPRebar.csproj -c Debug.R27 -p:DeployAddin=false
   - dotnet build HPRebar.McpBridge/HPRebar.McpBridge.csproj -c Debug.R26 -p:DeployAddin=false
   - dotnet build HPRebar.McpBridge/HPRebar.McpBridge.csproj -c Debug.R25 -p:DeployAddin=false
   - dotnet test HPRebar.Core.Tests
   - dotnet test HPRebar.Mcp.Server.Tests

Provide structured evidence and assign a binary verdict:
CLEAN or INTEGRITY VIOLATION.
Write your forensic report in handoff.md in your working directory.
Notify orchestrator_11 (conversation ID: 6b50efdd-bc13-44c7-a3d0-de935480c02b) via send_message.
