## 2026-10-09T16:37:17Z
You are Worker W3 M3 for HPBim Wave 3 Remediation Milestone 3: Tách biệt Persistent Document Identity khỏi Dynamic DocumentVersion GUID (R3).
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_worker_w3_m3_1
Your parent conversation ID is: 8b5286ec-5528-473c-b9fe-c3867bdf1bf8

MANDATORY FIRST STEPS:
1. Read g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (under header ## 2026-10-09T14:54:56Z).
2. Read Explorer 2 findings:
   - g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_w3_survey_2\survey_report.md
   - g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_w3_survey_2\handoff.md
3. Read PROJECT.md: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

File Write Ownership:
You EXCLUSIVELY own:
- HPBim/src/HPBim.Client/Recovery/ReconcileService.cs
- Companion partial class in HPBim/src/HPBim.Client/Recovery/ (e.g. ReconcileService.DocSnapshot.cs - CRITICAL: ReconcileService.cs is already at ~290 lines, so extract snapshot matching into a partial class to guarantee every file is <= 300 lines per Rule C2!)
- HPBim/src/HPBim.Core/Models/DocSnapshot.cs (if needed)
- Tests in HPBim/tests/HPBim.Client.Tests/Recovery/ (e.g. DocSnapshotReconcileTests.cs)
Do NOT modify files outside this scope!

Task Requirements:
1. Tách bạch rõ ràng định danh tĩnh của tài liệu Revit (Title, PathName, CentralGuid/Project GUID) khỏi chỉ số revision động (VersionGuid/NumberOfSaves).
2. MatchTargetDocSnapshot: bắt buộc đối soát các trường định danh tĩnh cần thiết (Title, PathName, CentralGuid). Không fail-open khi thiếu path/GUID nếu intent đã có chúng (từ chối receipt nếu intent có path/GUID mà receipt rỗng hoặc không khớp).
3. Ngăn chặn việc từ chối nhầm receipt: trong recovery / ReconcileService, việc VersionGuid thay đổi do chính operation tạo element gây ra KHÔNG ĐƯỢC làm thất bại quá trình recovery của chính operation đó. (VersionGuid thay đổi sau mutation là hành vi tự nhiên của Revit; chỉ kiểm tra định danh tĩnh trong recovery).
4. Implement Dynamic DocumentVersion Resilience Tests:
   - Test kiểm chứng VersionGuid thay đổi sau khi apply element không khiến recovery từ chối receipt hợp lệ của chính operation đó.
   - Test kiểm chứng đối soát định danh tĩnh nghiêm ngặt: sai Title, sai PathName, hoặc sai CentralGuid/ProjectGUID bị từ chối; không fail-open.
5. Verification:
   - Run tests: dotnet test HPBim/tests/HPBim.Client.Tests/HPBim.Client.Tests.csproj
   - Run full HPBim test suite: dotnet test HPBim/HPBim.slnx
   - Ensure 100% tests PASS across the solution.
6. Clean Code Governance:
   - Files <= 300 lines (Rule C2). MUST extract snapshot matching to ReconcileService.DocSnapshot.cs to keep ReconcileService.cs under 300 lines.
   - Methods <= 50 lines (Rule M3).
   - Zero comments (Rule Q-B1).
   - Zero empty catches (Rule Q-B2).
   - Keep netstandard2.0 purity.

Deliverables:
- Write report.md and handoff.md in your working directory.
- Send completion message to parent (8b5286ec-5528-473c-b9fe-c3867bdf1bf8).
