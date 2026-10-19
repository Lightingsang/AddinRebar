## 2026-10-18T07:20:14Z
You are worker_m2_gen2.
Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2_gen2\
Parent Conversation ID: 1e55a9de-0d52-405d-83fa-fa58bb05a293

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A Forensic Auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

MANDATORY READING BEFORE WORK:
- ORIGINAL_REQUEST.md: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
- Master Scope: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_25_gen2\PROJECT.md
- Truth Matrix: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\rea-dce-pro\TRUTH_MATRIX.md
- Explorer 1 Report: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m2_gen2_1\handoff.md
- Explorer 2 Report: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m2_gen2_2\handoff.md
- Explorer 3 Report: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m2_gen2_3\handoff.md
- Ready-to-use artifacts in .agents/teamwork/explorer_m2_gen2_3/:
  * proposed_AssetPathResolver.cs
  * proposed_AssetPathResolverTests.cs
  * proposed_M2AssetIntegrityTests.cs
  * verify_m2_integrity.py

EXCLUSIVE WRITE OWNERSHIP:
- You own: HPDcePro/assets/**, HPDcePro/src/**, HPDcePro/tests/**, and your working directory.
- STRICT READ-ONLY INVARIANT: You MUST NOT modify, move, delete, or rename anything in C:\Dce Pro\.

MISSION: Execute Milestone M2 (Standalone Asset & Template Migration)

TASKS:
1. One-Way Asset Migration from C:\Dce Pro into HPDcePro/assets/:
   Execute the migration commands (see explorer_m2_gen2_1 handoff.md) using robocopy or PowerShell:
   - 17 DWGs:
     * C:\Dce Pro\Cad\*.dwg (13 files) -> HPDcePro/assets/Cad/
     * C:\Dce Pro\AutoStr\00.FileDungChung\*.dwg (4 files) + companion templates (.docx, .json) -> HPDcePro/assets/AutoStr/00.FileDungChung/
   - 132 XLSMs:
     * C:\Dce Pro\Excel\File V.2020\ -> HPDcePro/assets/Excel/File V.2020/
     * C:\Dce Pro\Excel\DceSteel\ -> HPDcePro/assets/Excel/DceSteel/
     * C:\Dce Pro\Excel\Menu DCE_2020.xlsm -> HPDcePro/assets/Excel/Menu DCE_2020.xlsm
   - Machine Learning:
     * C:\Dce Pro\MayHoc\mauhoc.db (8.98 MB) + model.zip (26.9 KB) -> HPDcePro/assets/MayHoc/
   - System Configurations:
     * C:\Dce Pro\System\*.txt (170 txt files) -> HPDcePro/assets/System/
   - Language Dictionaries:
     * C:\Dce Pro\Language\**\*.txt (8 files) -> HPDcePro/assets/Language/
   - Icons:
     * C:\Dce Pro\Icon\** (133 files: png, jpg, ico) -> HPDcePro/assets/Icon/
   - CUIX Menus:
     * C:\Dce Pro\*.cuix (7 files) -> HPDcePro/assets/Cuix/
   - Sample Projects:
     * C:\Dce Pro\AutoStr\DuAnMau.autostr -> HPDcePro/assets/AutoStr/DuAnMau.autostr (normalize internal hardcoded paths to relative)
     * C:\Dce Pro\AddinSteelCad\SystemSteel\*.txt (112 txt files) -> HPDcePro/assets/SystemSteel/

2. Implement AssetPathResolver in HPDcePro.Core:
   - Place in HPDcePro/src/HPDcePro.Core/Common/AssetPathResolver.cs
   - Follow the design in Explorer 2 & 3 handoffs and Interface Contract 3 in PROJECT.md.
   - Support multi-tier resolution:
     1. Test seam / AsyncLocal override
     2. Global SetCustomAssetsRoot
     3. Environment variable HPDCEPRO_ASSETS_DIR
     4. Assembly location / parent walking
     5. AppContext.BaseDirectory / ancestor climbing to find "assets" or "HPDcePro/assets"
     6. %APPDATA%/Autodesk/... bundle relative
     7. Fallback directory
   - Check Clean Code rules: <=300 lines/file, <=50 lines/method, 0 unannotated catch, 0 dead code.

3. Decouple HPDcePro/src/** from C:\Dce Pro:
   - HPDcePro.Core/MachineLearning/MauHocDatabaseReader.cs:
     Replace DefaultDatabasePath constant with dynamic property returning AssetPathResolver.ResolveMachineLearningDatabase().
     Update constructor fallback.
     Remove hardcoded references in XML comments.
   - HPDcePro.Core/MachineLearning/RebarTokenType.cs & RebarFeatureExtractor.cs:
     Remove hardcoded references in XML comments.
   - HPDcePro.Core/Serialization/AutoStrConfig.cs:
     Update XML comments.
   - HPDcePro.Desktop/Program.cs:
     Replace DefaultAutoStrSamplePath with AssetPathResolver.ResolveAutoStrSampleProject().
   - HPDcePro.Mcp/Execution/Handlers/AutoStrToolHandler.cs:
     Use AssetPathResolver.ResolveAutoStrSampleProject().
   - HPDcePro.Mcp/Execution/Handlers/MayHocToolHandler.cs:
     Use AssetPathResolver.ResolveMachineLearningDatabase().

4. Test Suite Updates & Additions:
   - Update tests in tests/HPDcePro.Core.Tests/ referencing C:\Dce Pro (AutoStrSerializerExtendedTests.cs, SerializationTests.cs, MachineLearningTests.cs, MauHocDatabaseLiveTests.cs) to use AssetPathResolver.
   - Add AssetPathResolverTests.cs (verifying every asset category resolution).
   - Add M2AssetIntegrityTests.cs (verifying live SQLite query on mauhoc.db, ClosedXML workbook access, and static grep check).

5. Verification Commands:
   - dotnet build HPDcePro/HPDcePro.slnx -c Release --disable-build-servers -m:1
   - dotnet build HPDcePro/HPDcePro.slnx -c Debug --disable-build-servers -m:1
   - dotnet test HPDcePro/HPDcePro.slnx -c Debug --disable-build-servers -m:1
   - Run python .agents/teamwork/explorer_m2_gen2_3/verify_m2_integrity.py
   - Verify zero occurrences of C:\Dce Pro in HPDcePro/src/**.
   - Verify C:\Dce Pro file count and timestamps remain untouched.

6. Report:
   Write your comprehensive handoff report to:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2_gen2\handoff.md
   Include all build output, test results, file counts, and verification logs. Then send_message to parent orchestrator.
