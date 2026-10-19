# Task Dispatch — reviewer_m3_2

## 2026-10-17T11:10:00Z

- **Agent Name**: `reviewer_m3_2`
- **Role**: Milestone M3 Functional & Build/Test Reviewer
- **Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m3_2\`
- **Parent Conversation ID**: `a7a2dbbb-6b7d-4279-98cd-1216d8280d80`

### Mandatory First Steps:
1. Read the authoritative original request at:
   `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (headers `## 2026-10-16T23:55:10Z` and `## 2026-10-17T06:46:41Z`)
2. Read the master project scope:
   `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_17\PROJECT.md`
3. Read the worker's changes and handoff:
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_foundation_slab_stair\changes.md`
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_foundation_slab_stair\handoff.md`

### Your Mission:
Review the Milestone M3 implementation focusing on Functional Completeness & Build/Test Verification:
1. **Functional Verification**:
   - Verify coverage of all 15 AutoCAD commands and aliases in `GoiLenh_HPKata.lsp` (`vmbm`, `rm`, `vmd`, `vmc`, `ve_mong_coc_giat_cap`, `vesan`, `vesanpl`, `vesan4d`, `ssan`, `dsan`, `chiasan`, `tksan`, `vethang`, `velanhto`, `vemc`).
   - Verify genuine AutoCAD controllers without simulation stubs: ModelSpace placement, dynamic Table generation, DWG XRecord serialization (`KATA_FOUNDATION_DATA`, `KATA_SLAB_DATA`, `KATA_STAIR_DATA`, `KATA_LINTEL_DATA`).
   - Verify Revit generators (`FoundationRebarGenerator.cs`, `SlabRebarGenerator.cs`, `StairRebarGenerator.cs`) and schedule service with genuine 3D rebar curves, covers, and idempotent cleanup.
   - Verify ClosedXML workbook generator for sheets `Mong`, `San`, and `Thang`.
2. **Build & Test Verification**:
   - Run: `dotnet build HPKata/HPKata.slnx --disable-build-servers -m:1` (must pass with 0 errors, 0 warnings).
   - Run: `dotnet test HPKata/HPKata.slnx --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-$([guid]::NewGuid())"` (all 743 tests must pass 100% green).
3. **Verdict**:
   Provide an explicit verdict: **APPROVE** or **REQUEST_CHANGES**.

### Deliverable:
Write `review.md` and a concise `handoff.md` in your working directory. Notify parent with verdict and report path.
