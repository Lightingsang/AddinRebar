---
phase: 3
title: "HPCivil3d.Mcp.Server hoàn chỉnh: Civil3dHostProfile, 4 core tool, prompts/resources, 12 seed nhúng, tests (structure + compile-check skip quan sát được), publish single-file"
status: pending
priority: P1
effort: "8h"
dependencies: [0, 1]
---

# Phase 3: Server exe + profile + 12 seed + tests

## Context Links
- [ADR-05 (bảng 12 seed, envelope, tests)](adr/adr-05-seed-library-mvp-12.md) · [ADR-06 §1–3](adr/adr-06-server-profile-client-wiring-ribbon-identity.md) · [ADR-03 §3 đơn vị, §5 prompts/resources](adr/adr-03-globals-units-context.md) · [phase-01 spike report](reports/phase-01-spike.md) (NEW — phase 1 sinh) (R6 exception, R9 area, W2 overload/`""`, U6–U8 indexer/`Name`)
- Mẫu (tồn tại): `HPAutoCad/HPAutoCad.Mcp.Server/{Program.cs (8), Hosts/AutocadHostProfile.cs (43), Tools/ExecuteAutocadCodeTool.cs (59), Tools/AutocadContextTool.cs (33), Prompts/AutocadScriptPrompts.cs (72), Resources/AutocadDocumentResources.cs (21), appsettings.json (31), HPAutoCad.Mcp.Server.csproj (:34–35 Compile Remove + EmbeddedResource)}`, seed khuôn `Registry/SeedLibrary/Layer/list_layers/{tool.json,code.cs,examples.json}`, `Registry/SeedLibrary/Drawing/draw_polyline/*` (seed ghi); tests `HPAutoCad/HPAutoCad.Mcp.Server.Tests/{SeedLibraryTests.cs (:25 PackageVersion, :274/:282 SkipWhen), HostProfileTests.cs, AutocadToolsOverPipeTests.cs}`; `HPEtabs/HPEtabs.Mcp.Server.Tests/SeedLibraryCompileTests.cs:79–105,153–163` (usings từ `HostScriptContracts`, refs từ install dir, skip); `HPEtabs/HPEtabs.Mcp.Server.Tests/HPEtabs.Mcp.Server.Tests.csproj:35` (`FakeRevitExecutor.cs` link); `C:\Program Files\Autodesk\AutoCAD 2026\C3D\Sample\Civil 3D API\DotNet\GettingStarted\Class1 step 4..7.cs` (mẫu đọc alignment/surface — E5).

## Overview
Hoàn thiện `HPCivil3d.Mcp.Server` (đã có Program/profile/2 tool tạm từ phase 1): profile đầy đủ theo ADR-06, mô tả `execute_civil3d_code` ≤ 1 800 ký tự, `inspect_type`/`cancel_execution` từ engine, prompts + resources `civil3d://`, 12 seed theo ADR-05 nhúng (`<Compile Remove>` + `EmbeddedResource`) với `_seeds.json` checksum; tests không cần Civil 3D (structure/profile/pipe) + compile-check cần Civil 3D (skip quan sát được); publish single-file; `tools/list` = 24. Song song với phase 2 sau spike.

## Key insights
- Server **không** reference `AeccDbMgd`/`AutoCAD.NET` (ADR-06 §3) → build mọi máy; chỉ `SeedLibraryCompileTests` cần install dir (env `HPCIVIL3D_C3D_DIR` → registry `ACAD-9100:409\Location` → Program Files) và `AutoCAD.NET 25.1.0` trong NuGet cache (có sẵn nhờ HPAutoCad; test tự `Assert.SkipWhen` nếu thiếu).
- Analyzer chứng minh schema ⇔ `args.X("literal")` — seed **không** dùng helper giấu key (như 4 host).
- `test_tool` mặc định dryRun **chạy thật rồi Abort** (AutoCAD) → W1/W2 "tested" có nghĩa; khác ETABS.

## Requirements
Functional
- `Hosts/Civil3dHostProfile.cs` (MODIFY): mọi giá trị ADR-06 §1 (`HostId civil3d`, `DisplayName "Civil 3D"`, `ServerName "HPCivil3d MCP"`, `ProductFolder "HPCivil3d"`, `EnvPrefix "HPCIVIL3D_MCP_"`, `DefaultVersion 2026`, `ValidVersions [2026]`, `MethodPrefix Civil3dPrefix`, `ResourceScheme "civil3d"`, `Categories` 10, `CoreToolNames` 4, `ScriptImports = Civil3dImports`, `ScriptContractSummary` (globals + `civil` + đơn vị hai lớp + không rebuild + U undo), `CliExecutable "HPCivil3d.Mcp.Server.exe"`, `BridgeNotConnectedHint` ADR-06, `TimeoutSemanticsHint null`, `MaxTimeoutSeconds` mặc định).
- `Tools/ExecuteCivil3dCodeTool.cs` (MODIFY): tên `execute_civil3d_code`, `Destructive`; mô tả ≤ 1 800 ký tự nêu: globals (`civil` = `CivilDocument`, có thể null trong drawing không Civil — theo S-04), `tr` của bridge, đơn vị (mm phẳng qua `units`; station/elevation drawing unit; `drawingUnit` từ `civil.Settings…DrawingUnits`), `transaction` auto/none, dryRun thật, bị chặn: rebuild/data shortcut/survey/UI/file export, undo gộp `U`, ví dụ 3 dòng đọc alignment. `Tools/Civil3dContextTool.cs` (`get_civil3d_context`, `ReadOnly`; mô tả nêu `civil3d.*` fields + `autocad.*`).
- `Prompts/Civil3dScriptPrompts.cs` (NEW): `civil3d_query_template` (few-shot: alignments, surface elevation, cogo points — `units.ToMm`), `civil3d_modify_template` (COGO add dưới auto + dryRun trước; style phải tồn tại), `toolify_run` từ engine.
- `Resources/Civil3dDocumentResources.cs` (NEW): `civil3d://document/info`, `civil3d://selection` (copy AutoCAD, scheme từ profile).
- `Registry/SeedLibrary/**` (NEW, 12 thư mục theo ADR-05 bảng: `Document/get_civil_document_info`, `Alignment/list_alignments`, `Alignment/get_alignment_geometry`, `Profile/list_profiles`, `Surface/list_surfaces`, `Surface/get_surface_elevation`, `Corridor/list_corridors`, `Pipe/list_pipe_networks`, `Parcel/list_parcels`, `Point/list_cogo_points`, `Point/create_cogo_points`, `Alignment/create_alignment_from_polyline`) mỗi cái `tool.json` (`host:"civil3d"`, `tags:["seed","civil3d"]`, `transaction`, `inputSchema` với `maximum ≤ 500` cho `limit`/`maxSamples`/`partLimit`, mô tả nêu đơn vị), `code.cs` (body kết `return`; `args.X("literal", default)`; per-item try/catch `Autodesk.Civil.CivilException`; `drawingUnit` + `lengthUnit:"mm"` trong envelope; caller error → `ArgumentException`), `examples.json` (≥ 2); `_seeds.json` checksum như AutoCAD. Member API **chỉ** từ [addendum](research/reflection-addendum-verified-signatures.md) + kết luận spike (indexer/`Name`/exception/W2 overload).
- `HPCivil3d.Mcp.Server.csproj` (MODIFY): `<Compile Remove="Registry\SeedLibrary\**\*.cs"/>` + `<EmbeddedResource Include="Registry\SeedLibrary\**\*" LogicalName="SeedLibrary/%(RecursiveDir)%(Filename)%(Extension)"/>`; `ModelContextProtocol` cùng version 4 exe kia; `InvariantGlobalization`; `appsettings.json` copy AutoCAD (`Bridge.HostVersion 2026`).
- `HPCivil3d.Mcp.Server.Tests` (NEW, net10, xunit v3): `HostProfileTests` (mọi giá trị ADR-06 §1; `PipeName == "hpcivil3d-mcp-2026"`; `Categories` 10 đúng thứ tự; mô tả execute ≤ 1 800; `BridgeNotConnectedHint` nêu `/product C3D`), `Civil3dToolsOverPipeTests` (round trip 4 core tool qua pipe với `FakeRevitExecutor` linked — copy `AutocadToolsOverPipeTests`; `get_civil3d_context` giữ `civil3d` + `autocad`, bỏ `revitVersion`), `SeedLibraryStructureTests` (12 seed; category ∈ profile; `ToolValidator` pass; `args` keys ⇔ schema (analyzer qua `ScriptAnalyzer` local, không bridge); transaction đúng bảng ADR-05; `limit` `maximum ≤ 500`; `tags` chứa `civil3d`; **unit-label rule**: mọi property số trong `inputSchema`/`examples` output mẫu có hậu tố `Mm` hoặc thuộc `{x,y,z,station*,elevation*,rim*,sump*,slope*,area*,delta*,direction*,number,count,*Count,index,order,offset,limit,taxId,radius…}` — danh sách trong test, mở rộng có review; mô tả seed chứa `mm` hoặc `drawing unit`; cấm `Rebuild`/`DataShortcuts`/`ExportTo`/`CreateFrom*` trong `code.cs` (grep); `_seeds.json` checksum khớp), `SeedLibraryCompileTests` (mỗi `code.cs` compile bằng Roslyn với usings = `Civil3dImports`, globals class mirror `Civil3dScriptGlobals` (fields 11 tên/kiểu — kiểu Civil chỉ `CivilDocument`), refs = `AutoCAD.NET 25.1.0` từ NuGet cache (`FindAutocadReference` copy) + `AeccDbMgd.dll`/`AecBaseMgd.dll`/`AeccPressurePipesMgd.dll` từ install dir + Core → 0 error; `ScriptGuard.Check(code, GuardProfile.Civil3d)` pass; `Assert.SkipWhen(dir is null, "Civil 3D 2026 not installed (AeccDbMgd.dll not found)")` — skip **quan sát được** trên máy không có Civil 3D; `-p:` không dùng — test tự dò như ETABS `:153–163`).
- Publish: `dotnet publish HPCivil3d/HPCivil3d.Mcp.Server -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=false -o HPCivil3d/output/HPCivil3d.Mcp.Server` → ≈ 7.4 MB; `python McpShared/tools/mcp-call.py <exe> tools/list` (registry cách ly) → **24** tool.

Non-functional
- `dotnet build HPCivil3d/HPCivil3d.slnx -c Debug -p:DeployBundle=false -p:Civil3dInstallDir=X:\nowhere\` → server + server tests build (bridge fail 1 error như phase 1) — chứng minh server không phụ thuộc Civil API.
- Seed `code.cs` ≤ 120 dòng mỗi file; envelope mẫu trong `examples.json` < 64 KB (test đo).

## Architecture
[architecture.md §2 (server), §6](architecture.md). Seed → `civil3d.execute` → bridge; server không biết Civil API.

## Related Code Files
- Modify: `HPCivil3d/HPCivil3d.Mcp.Server/{Hosts/Civil3dHostProfile.cs, Tools/ExecuteCivil3dCodeTool.cs, Tools/Civil3dContextTool.cs, HPCivil3d.Mcp.Server.csproj, appsettings.json}`, `HPCivil3d.slnx` (+ tests).
- Create: `Prompts/Civil3dScriptPrompts.cs`, `Resources/Civil3dDocumentResources.cs`, `Registry/SeedLibrary/**` (12 × 3 file + `_seeds.json`), `HPCivil3d.Mcp.Server.Tests/{HPCivil3d.Mcp.Server.Tests.csproj, HostProfileTests.cs, Civil3dToolsOverPipeTests.cs, SeedLibraryStructureTests.cs, SeedLibraryCompileTests.cs, Civil3dApiLocator.cs}`.
- Reports: `reports/phase-03-server-seeds.md`, `reports/phase-03-tools-list-civil3d.json`, `reports/code-review-phase-03.md`, `reports/test-report-phase-03.md`.

## Implementation Steps
1. Profile + 2 tool đầy đủ + prompts + resources; `HostProfileTests`, `Civil3dToolsOverPipeTests` xanh.
2. 10 seed đọc (R1–R10) theo ADR-05 + spike kết luận; `SeedLibraryStructureTests` xanh (không cần Civil).
3. 2 seed ghi (W1, W2); `SeedLibraryCompileTests` (Civil cài) xanh; thử `-p:Civil3dInstallDir=X:\nowhere\` → skip quan sát được (log dòng skip).
4. `_seeds.json`; build; `tools/list` = 24 (registry cách ly); mô tả execute đếm ký tự.
5. Smoke live (Civil 3D mở, bridge phase 2 hoặc phase 1): `run-server-smoke.ps1` (copy AutoCAD, ≥ 9 check: tools/list 24, context, R1/R2/R5 thật, W1 dryRun) — chứng minh seed **chạy** trước phase 4; ghi `reports/phase-03-server-seeds.md`.
6. Publish single-file; `HPCivil3d/README.md` cập nhật (`.mcp.json` entry mẫu — user tự thêm).
7. Code review → fix → tests + smoke lại.

## Todo List
- [ ] Profile/tools/prompts/resources · [ ] R1–R10 · [ ] W1–W2 · [ ] Structure tests · [ ] Compile tests (+ skip path quan sát) · [ ] `tools/list` 24 · [ ] Smoke ≥ 9 · [ ] Publish · [ ] Report + review

## Success Criteria
- [ ] `dotnet test HPCivil3d/HPCivil3d.Mcp.Server.Tests` → ≥ 60 test, 0 fail, 0 skip trên máy này; với `HPCIVIL3D_C3D_DIR=X:\nowhere\` → đúng N (= số compile test) **skipped** với text "Civil 3D 2026 not installed", 0 fail.
- [ ] `python McpShared/tools/mcp-call.py HPCivil3d/output/HPCivil3d.Mcp.Server/HPCivil3d.Mcp.Server.exe tools/list --env HPCIVIL3D_MCP_Registry__LibraryPath=<tmp> --env HPCIVIL3D_MCP_Registry__DbPath=<tmp>/registry.db` → 24 tool (4 + 8 + 12), `execute_civil3d_code` description ≤ 1 800 ký tự; lưu `reports/phase-03-tools-list-civil3d.json`.
- [ ] `pwsh -File HPCivil3d/tools/harness/run-server-smoke.ps1` → ≥ 9/9 với Civil 3D thật (R1, R2, R5 số hợp lý; W1 dryRun `changed.added` đúng, `rolledBack`).
- [ ] `grep -rn "Rebuild\|DataShortcuts\|ExportTo\|CreateFrom" HPCivil3d/HPCivil3d.Mcp.Server/Registry/SeedLibrary --include=code.cs` = 0; `grep -rn "AeccDbMgd\|AutoCAD.NET" HPCivil3d/HPCivil3d.Mcp.Server/*.csproj` = 0.
- [ ] `tools/list` 4 host cũ không đổi (không sửa engine ở phase này).

## Risk Assessment
| Risk | Mitigation |
|---|---|
| Seed dùng member không có (indexer, `Feature.Name`) → compile pass trong test nhưng runtime lỗi | chỉ member trong addendum + spike S-08; smoke bước 5 chạy R1–R5 thật trước phase 4 |
| Envelope R8 `includeParts` vượt 64 KB trên network lớn | `partLimit ≤ 500` + `truncated`; test đo `examples` |
| `list_parcels` không có area → AI thất vọng | mô tả seed nói thẳng "area not exposed by the .NET API" + `warnings`; S-09 có thể mở đường |
| Mô tả execute > 1 800 ký tự | test pin; cắt ví dụ |

## Security Considerations
- Server không API host; seed không path/file/rebuild; `host:"civil3d"` trong `tool.json` là rào copy tay giữa registry root AutoCAD ↔ Civil.

## Next Steps
- Phase 4 harness full dùng exe publish + 12 seed + registry loop.
