---
phase: 0
title: "McpShared: hằng/profile/imports/globals/Civil3dInfo additive + engine tests + gate byte-identical 4 host"
status: completed
priority: P1
effort: "3h"
dependencies: []
---

# Phase 0: `McpShared` — chỉ thêm, không đổi hành vi Revit/AutoCAD/Navis/ETABS

## Context Links
- [ADR-02 §2](adr/adr-02-bundle-platform-civil3d-pipe-isolation.md) (pipe/prefix) · [ADR-03 §1–2, §4](adr/adr-03-globals-units-context.md) (globals, imports, `Civil3dInfo`) · [ADR-04 §2](adr/adr-04-transactions-rebuilds-guard-civil3d.md) (guard/analyzer) · [ADR-06 §1](adr/adr-06-server-profile-client-wiring-ribbon-identity.md) (token)
- Mẫu: ETABS phase 0 (`../260916-2152-etabs-mcp-2026/phase-00-mcpshared-additive-etabs-contracts-and-engine-tests.md`, bảng 16 hàng; report `reports/phase-00-report.md`), Navis phase 0. Gate script tồn tại: `plans/260915-0824-navisworks-mcp-2026/reports/snapshot-tools-list.ps1` (2 host) và `plans/260916-2152-etabs-mcp-2026/reports/regression-tools-list-phase-04.py` (3 host + `KNOWN_CHANGED`) — **copy vào `reports/` plan này và mở rộng 4 host** (thêm `etabs`: exe `HPEtabs/HPEtabs.Mcp.Server/bin/Release/net10.0/HPEtabs.Mcp.Server.exe` (build output — bước 1 `dotnet build -c Release` sinh; Release bin ETABS chưa có trên đĩa hôm nay), prefix `HPETABS_MCP_`).
- Seam (đo 2026-09-17, [E8](research/evidence-on-machine-2026-09-17.md)): `McpShared/HPRebar.Mcp.Contracts/PipeNaming.cs:26,46` · `JsonRpc/JsonRpcMethods.cs:38` · `HostScriptContracts.cs:25–34 (AutocadImports), 54 (AutocadGlobals), 75–93 (ETABS block — append sau)` · `Messages/ContextMessages.cs:30 (slot Etabs), 66–73 (AutocadInfo), 106 (EtabsInfo)` · `McpShared/HPRebar.McpBridge.Core/Scripting/GuardProfile.cs:22–45 (Autocad), 85 (Etabs)` · `AnalyzerProfile.cs:14–16, 28` · `McpShared/HPRebar.Mcp.Server.Core/Services/ContextService.cs:52–65 (Shape — không sửa)`.

## Overview
Thêm mọi hằng/profile/DTO mà phase 1–4 cần; chứng minh 4 host cũ **byte-identical** (`tools/list`) và nguyên số test. Không code Civil, không tham chiếu `AeccDbMgd`/`Autodesk.*` trong `McpShared/` (grep = 0). **Bảng dưới là danh sách sửa engine duy nhất của plan** — thêm gì ngoài bảng = dừng, báo user (ràng buộc "adding any dependency / writing outside" của user).

## Danh sách sửa engine (authoritative)

| # | File | Loại | Nội dung | Ảnh hưởng 4 host |
|---|---|---|---|---|
| 1 | `Contracts/PipeNaming.cs` | thêm sau `:26` + case sau `:46` | `public const string Civil3dHost = "civil3d";` (doc: "Autodesk Civil 3D — an AutoCAD vertical on the same acad.exe; its own pipe so AutoCAD 2026 and Civil 3D 2026 can serve at once") + `Civil3dHost => "hpcivil3d-mcp-" + version` (= nhánh mặc định `:47` — thêm để tự tài liệu) | không |
| 2 | `Contracts/JsonRpc/JsonRpcMethods.cs` | thêm sau `:38` | `public const string Civil3dPrefix = "civil3d.";` | không |
| 3 | `Contracts/HostScriptContracts.cs` | append cuối file | `Civil3dImports = { "System", "System.Linq", "System.Collections.Generic", "Autodesk.AutoCAD.ApplicationServices", "Autodesk.AutoCAD.DatabaseServices", "Autodesk.AutoCAD.EditorInput", "Autodesk.AutoCAD.Geometry", "Autodesk.AutoCAD.Colors", "Autodesk.Civil", "Autodesk.Civil.ApplicationServices", "Autodesk.Civil.DatabaseServices", "Autodesk.Civil.DatabaseServices.Styles", "Autodesk.Civil.Settings", "HPRebar.McpBridge.Core.Scripting" }` (**không** `HPAutoCad.Aec`, không `DataShortcuts`/`AeccUiMgd`); `Civil3dGlobals = { "doc", "db", "ed", "app", "tr", "units", "civil", "ct", "log", "progress", "args" }`; doc comment: `civil` = `CivilDocument`, `units` từ Civil `DrawingUnits` | không |
| 4 | `Contracts/Messages/ContextMessages.cs` | thêm slot sau `:30` + record sau `:116` | `public Civil3dInfo? Civil3d { get; set; }`; `public sealed record Civil3dInfo(string Product, bool IsCivilDocument, string? DrawingUnit, string? CoordinateSystemCode, bool InsunitsMismatch, int AlignmentCount, int SurfaceCount, int CorridorCount, int PipeNetworkCount, int PressureNetworkCount, int CogoPointCount);` — **11 field** (ADR-03 §4) | null → bị bỏ (`BridgeJson` `WhenWritingNull`) |
| 5 | `Core/Scripting/GuardProfile.cs` | thêm sau `:85` (khối Etabs) | `Civil3d` theo ADR-04 §2 — **nối** từ `Autocad.DeniedIdentifiers/DeniedMembers/DeniedNamespaces/DeniedMembersOnIdentifier` (các list đã public từ trước — kiểm 2026-09-17) + phần Civil; superset bằng cấu trúc, không lặp literal; 0 API mới | không |
| 6 | `Core/Scripting/AnalyzerProfile.cs` | thêm sau `:30` | `Civil3d = new AnalyzerProfile(transactionTypeNames: [], transactionMethodNames: ["StartTransaction", "StartOpenCloseTransaction"])` — copy `Autocad` với doc comment | không |
| 7 | test (không code) | chứng minh | `ContextService.Shape` với `HostId="civil3d"`: bỏ `revitVersion/isFamily`, giữ `autocad` **và** `civil3d` cùng lúc (context Civil điền cả hai slot — ADR-03 §4); không `Civil3d` → không key | — |
| 8 | `Core/Scripting/ScriptGuard.cs` | **thêm sau code review 2026-09-17** (1 override, +18 dòng) | `VisitMemberBindingExpression`: `a?.b` là `MemberBindingExpression`, không phải `MemberAccessExpression` → mọi member bị cấm lọt ở dạng null-conditional (`corridor?.Rebuild()`, `db.TransactionManager?.StartTransaction()`, `tr?.Commit()`) ở **cả 5 host** (reviewer probe xác nhận). Xử lý y như `VisitMemberAccessExpression`; receiver của quy tắc `tr.*` = `ConditionalAccessExpression` gần nhất. Lỗ có sẵn từ trước (không do diff này). **Ngoài bảng gốc — user có thể revert 1 override**; precedent = fix `global::` ETABS phase 0 #15 | script hợp lệ không dùng `?.` trên member bị cấm → không đổi; script cố lách → giờ bị chặn; `tools/list` không đổi (gate lại pass) |
| 9 | `Core/Scripting/GuardProfile.cs` | **sau review** (+5 tên vào #5) | file-member còn thiếu, kiểm bằng reflection: `ImportPoints`, `ExportPoints` (CogoPointCollection), `CreateFromDEM` (GridSurface), `CreateSolidsAtDepthToFile`, `CreateSolidsAtSurfaceToFile` (TinSurface); comment `ExportTo` sửa đúng (nhận `Database` — ghi style sang bản vẽ khác, không phải path) | không |
| — | **Không đổi** | | `IHostProfile`/`HostProfile` (hint đã có từ ETABS — Civil chỉ set giá trị trong profile riêng), `RequestDispatcher`, `McpBridgeHost`, `BridgeClient`, `ExecuteRequest/Result`, `AnalyzeRequest`, `ScriptGuard` base, `ScriptCompiler`, `ScriptUnits` (ctor public đủ), `ContextService.Shape`, `ToolValidator`, `ToolManager`, `McpServerHost.ConfigureOptions` (`HostVersion` từ `profile.DefaultVersion` — đã có), meta tool descriptions, `Net48Tests` | |

Ghi chú: ít hơn ETABS (16) vì Civil dùng lại **mọi** seam AutoCAD; không hint mới (`BridgeNotConnectedHint` là giá trị trong `Civil3dHostProfile`, phase 3), không DTO wire mới ngoài `Civil3dInfo`.

## Requirements
- Functional: #1–#6 hiện thực; #7 chứng minh bằng test; test mới xanh.
- Non-functional: `git diff McpShared -- '*.cs'` chỉ **thêm** (0 dòng sửa/xoá — kiểm bằng `git diff --numstat`); 7 suite cũ nguyên số (`HPRebar.Mcp.Server.Core.Tests`, `HPRebar.McpBridge.Core.Net48Tests`, `HPRebar.Mcp.Server.Tests` 109, `HPAutoCad.Mcp.Server.Tests`, `HPAutoCad.Aec.Tests`, `HPNavis.Mcp.Server.Tests` 49, `HPEtabs.Mcp.Server.Tests` 81 — số Navis/ETABS bridge tests cần host cài: chạy nếu máy có); `tools/list` **4 host** byte-identical trên exe rebuild Release; 4 build Debug host xanh.

## Architecture
- Contracts net48 asset (Navis) tự nhận `Civil3dInfo` — record thuần.
- `Civil3dInfo` positional record như `AutocadInfo` (`ContextMessages.cs:66`); `IsCivilDocument=false` cho phép `context` trả về khi drawing không phải Civil (ADR-03 §4).

## Related Code Files
- Modify (thêm dòng): #1–#6 (6 file).
- Create (test, NEW): `McpShared/HPRebar.Mcp.Server.Core.Tests/Civil3dProfileTests.cs` (mirror `EtabsProfileTests.cs` / `NavisProfileTests.cs:48–223`): `PipeNaming.For("civil3d", 2026) == "hpcivil3d-mcp-2026"` và == nhánh mặc định trước khi thêm case (so với `"hp" + "civil3d" + "-mcp-2026"`); `PipeNaming.For("autocad", 2026)` không đổi; `JsonRpcMethods.For(Civil3dPrefix, "execute") == "civil3d.execute"`, `Suffix` == `"execute"` (≡ AutoCAD); `Civil3dImports` chứa 5 namespace Civil + không chứa `HPAutoCad.Aec`/`DataShortcuts`/`AeccUiMgd`; `Civil3dGlobals` = AutoCAD + `civil` (thứ tự); `GuardProfile.Civil3d`: **cấm** `civil.CorridorCollection.RebuildAll()`, `corridor.Rebuild()`, `surface.RebuildSnapshot()`, `DataShortcuts.SetWorkingFolder("x")`, `Autodesk.Civil.DataShortcuts.DataShortcuts.GetWorkingFolder()` (namespace), `CivilApplication.SurveyProjects`, `surface.ExportToDEM("f", "c", 1.0, default)`, `TinSurface.CreateFromLandXML(db, "s", "f")`, `style.ExportTo(db2, default)`, `new Autodesk.Civil.AeccUiMgd.Roadway.X()`, `Autodesk.AECC.Interop.Land.AeccApplication a`, mọi mẫu AutoCAD (`ed.GetPoint`, `tr.Commit()`, `StartTransaction`, `SendStringToExecute`); **cho phép** `civil.GetAlignmentIds()`, `(Alignment)tr.GetObject(id, OpenMode.ForRead)`, `surface.FindElevationAtXY(1, 2)`, `civil.CogoPoints.Add(new Point3d(0,0,0), "d", true)`, `Alignment.Create(civil, opts, "A", "", "0", "S", "L")`, `corridor.IsOutOfDate`, `corridor.RebuildAutomatic`, `alignment.ImportLabelSet("x")`, `civil.Styles.AlignmentStyles.Count`; `AnalyzerProfile.Civil3d.UsesTransaction("var t = db.TransactionManager.StartTransaction();") == true`; **#7** `Shape` qua `ContextService` với profile giả `HostId="civil3d"` (như `EtabsTestProfile.cs`) → JSON không có `revitVersion`/`isFamily`, có `autocad` + `civil3d` khi cả hai set, không `civil3d` khi null; `Civil3dInfo` round-trip camelCase (`alignmentCount`…).
- Create: `reports/phase-00-baseline.md` (số 7 suite + số tool `tools/list` 4 host + SHA `HPRebar.Mcp.Server.Core.dll` **đo lúc chạy**) + `reports/phase-00-tools-list-{before,after}-{revit,autocad,navis,etabs}.json` + `reports/snapshot-tools-list.ps1` (copy Navis script, 4 host, path `reports/` plan này).

## Implementation Steps
1. **Baseline:** `dotnet build -c Release` 4 server exe (`HPRebar/HPRebar.Mcp.Server`, `HPAutoCad/HPAutoCad.Mcp.Server`, `HPNavis/HPNavis.Mcp.Server`, `HPEtabs/HPEtabs.Mcp.Server`) → `pwsh plans/260917-1633-civil3d-mcp-2026/reports/snapshot-tools-list.ps1 -Tag before` (registry cách ly qua `<PREFIX>Registry__LibraryPath/DbPath`); chạy 7 suite → `reports/phase-00-baseline.md`.
2. #1–#4 Contracts; `dotnet build McpShared/McpShared.slnx`.
3. #5–#6 Core profiles.
4. `Civil3dProfileTests.cs` (#7 trong đó); `cd McpShared && dotnet test HPRebar.Mcp.Server.Core.Tests` (không `--nologo`).
5. **Gate:** 7 suite = baseline (+ test mới); rebuild Release 4 exe → `snapshot-tools-list.ps1 -Tag after` → `diff` 4 cặp JSON rỗng; SHA Core.dll khác; 4 build Debug host xanh (`HPRebar.slnx -c Debug.R26 -p:DeployAddin=false`, `HPAutoCad.slnx -c Debug -p:DeployBundle=false`, `HPNavis.slnx -c Debug -p:DeployPlugin=false`, `HPEtabs.slnx -c Debug`).
6. `reports/phase-00-report.md`; code review (`code-reviewer`) → fix cùng ngày → gate lại.

## Todo List
- [x] `reports/phase-00-baseline.md` + 4 `tools/list` before + SHA (33/62/24/24; 164/62/109/280/225/49/81)
- [x] #1–#4 Contracts (+58 dòng, 0 xoá)
- [x] #5–#6 Core (+35 dòng, 0 xoá)
- [x] `Civil3dProfileTests.cs` — **28** test
- [x] Gate byte-identical 4 host (4 JSON identical, Core.dll SHA đổi) + 7 suite nguyên số, Core.Tests 192 ×3
- [x] Report `reports/phase-00-report.md` · review `reports/code-review-phase-00.md` 8/10 → H1 (`?.`), M2 (+5 tên), L3 (comment) fixed cùng ngày; L4 (deny-list mutable qua cast — "not a sandbox", chấp nhận MVP, ghi known gap) · test report `reports/test-report-phase-00.md` (+ đính chính 5 suite host)
- [x] Gate lần 2 sau fix: Core.Tests **206** (=192+5+9), Net48 **71** (=62+9), 5 suite host nguyên số, 4 host `tools/list` byte-identical (Core.dll `28779CBB…`), 4 build Debug xanh

## Success Criteria
- [x] `cd McpShared && dotnet test HPRebar.Mcp.Server.Core.Tests` → **206** = 164 + 28 (Civil3d) + 9 (`?.` all profiles) + 5 (file members), 0 fail, 0 skip (192 ×3 trước review; 206 sau).
- [x] `cd McpShared && dotnet test HPRebar.McpBridge.Core.Net48Tests` → **71** = 62 + 9 (`ScriptGuardTests.cs` linked).
- [x] 109 · 280 · 225 · 49 · 81 = baseline (2 lần: sau implement, sau review).
- [x] 4 cặp JSON byte-identical (2 lần); Core.dll SHA `3A3A774F…` → `086DF637…` → `28779CBB…`.
- [x] `git diff --numstat` → 8 file, cột xoá = 0 (+150 dòng: 6 file bảng + `ScriptGuard.cs` +18 + `ScriptGuardTests.cs` +34) + `Civil3dProfileTests.cs` mới; grep `Autodesk.` trong csproj = 2 hit đều là comment có sẵn.
- [x] 4 build Debug host 0 error (2 lần).

## Risk Assessment
- `GuardProfile.Civil3d` lặp literal AutoCAD (≈ 40 tên) → drift với `Autocad` khi ai sửa AutoCAD list: test pin "mọi member `Autocad` cấm thì `Civil3d` cũng cấm" (chạy `ScriptGuard.Check` cùng 8 mẫu AutoCAD trên cả hai profile).
- Merge với plan khác đang sửa `HostScriptContracts.cs` (AEC đã complete 2026-09-17 → diff sạch; kiểm `git status` trước).
- `Civil3dInfo` 11 field — nếu review đòi thêm (`ParcelCount`…) → seed R1, không vào context (ADR-03 §4).

## Security Considerations
- Guard Civil chỉ **thêm** deny; base list không đổi. Không path máy trong text.

## Next Steps
- Phase 1 (scaffold + spike) cần #1–#6 để build bridge/server Civil.
