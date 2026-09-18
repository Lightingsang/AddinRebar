---
phase: 2
title: "Bridge runtime hoàn chỉnh (context Civil, units, serializer lỗi Civil, cửa sổ, ribbon HPCivil3d ▸ MCP ▸ MCP Bridge) + MirrorTests + harness pipe unattended"
status: completed
priority: P1
effort: "8h"
dependencies: [0, 1]
---

# Phase 2: `HPCivil3d.McpBridge` + Loader ribbon production-grade; drift với HPAutoCad bị test bắt

## Context Links
- [ADR-01 §A mirror test](adr/adr-01-code-sharing-with-hpautocad-copy-vs-acadshared.md) · [ADR-03 §3–5](adr/adr-03-globals-units-context.md) · [ADR-04 §1, §3, §5](adr/adr-04-transactions-rebuilds-guard-civil3d.md) (kết luận S-10 → `Rebuild` mở/đóng) · [ADR-06 §1 Ribbon](adr/adr-06-server-profile-client-wiring-ribbon-identity.md) · [phase-01 spike report](reports/phase-01-spike.md) (NEW — phase 1 sinh) (S-04 text, S-06 units, S-07 exception, S-10 rebuild)
- Mẫu: AutoCAD phase 2 (`../260913-0000-autocad-mcp-bridge-2026/phase-02-autocad-bridge-runtime-threading-transactions-context.md`, report `reports/phase-02-bridge-runtime.md`); Ribbon 0.3.0: CLAUDE.md § "Ribbon tab HPAutoCad ▸ MCP ▸ MCP Bridge" + `HPAutoCad/tools/harness/run-ribbon-check.ps1` (12 check + 1 MANUAL); harness pipe `HPAutoCad/tools/harness/{run-bridge-unattended.ps1, pipe-scenarios.py (22 check), bridge.scr}`; `HPAutoCad/HPAutoCad.McpBridge/Service/AutocadContextReader.cs:37–52` (fields), `AutocadResultSerializer.cs:77` (`IsAutocadType` namespace check).

## Overview
Biến bridge spike thành runtime đầy đủ = AutoCAD 2026 runtime (đã verified) + Civil delta: `Civil3dInfo` đếm đủ trong context, `units` Civil + mismatch log, serializer nhận `Autodesk.Civil.*` (namespace check `"Autodesk."`), lỗi Civil `"{Type}: {Message}"`, `Rebuild` theo S-10, cửa sổ trạng thái (1 checkbox — không heavy), ribbon một nút với icon vector, mirror test, và harness pipe unattended ≥ 22 check trên Civil 3D. Xoá spike-only (`HPC3DMCPSPIKE*`, env bypass).

## Key insights
- Mọi fix tinh vi của AutoCAD (wrapper finalizer, undo gộp `HPMCP`, serialize trước `inner.Commit`, `FlushGraphics`) đến Civil **bằng copy** → mirror test là hàng rào duy nhất chống drift (ADR-01 A).
- `AutocadResultSerializer.IsAutocadType` = `ns.StartsWith("Autodesk.AutoCAD")` → `Alignment` (ns `Autodesk.Civil.DatabaseServices`) sẽ rơi vào nhánh generic (walk object graph → getter throw ngoài transaction) → **phải** mở rộng `"Autodesk."` và map `Autodesk.Civil.DatabaseServices.Entity` → `{handle, type, name, style}`; `CogoPoint` → `{handle, type, number, x, y, elevation}`; `Point2d` → `{x, y}`.
- Đơn vị serializer: `Point3d/Point2d` xuất **drawing units** như AutoCAD (seed tự `units.ToMm`) — không tự đổi trong serializer (giữ byte-compat hành vi AutoCAD, mirror test).

## Requirements
Functional
- `Service/Civil3dContextReader` (MODIFY từ phase 1): điền `ContextResult.Autocad` (7 field như AutoCAD) **và** `Civil3d` (11 field ADR-03 §4: `Product`, `IsCivilDocument`, `DrawingUnit`, `CoordinateSystemCode` (`""` → null), `InsunitsMismatch`, đếm `GetAlignmentIds().Count`, `GetSurfaceIds().Count`, `CorridorCollection.Count`, `GetPipeNetworkIds().Count`, `GetPressurePipeNetworkIds(civil).Count`, `GetAllPointIds().Count`); `Units.Length = units.Label`; đọc trong transaction đọc riêng của context (như AutoCAD), < 100 ms trên `Corridor-1.dwg`.
- `Service/Civil3dUnits` (MODIFY): `For(CivilDocument? civil, Database db)` → `DrawingUnits` Meters/Feet → `ScriptUnits("Meters", 1000)`/`("Feet", 304.8)`; `civil == null` → `AutocadInsunits.For(db.Insunits)` + note; mismatch → `Note` + runner `log(...)` + `Civil3dInfo.InsunitsMismatch`.
- `Service/Civil3dResultSerializer` (MODIFY copy): namespace check `Autodesk.` ; case `Autodesk.Civil.DatabaseServices.Entity` → `{handle, type, name, style}`; `CogoPoint` → `{handle, type: "CogoPoint", number, x, y, elevation}` (drawing units); `AlignmentEntity`/`AlignmentSubEntity` (DisposableWrapper, không handle) → `{type, startStation, endStation, length}`; `GeneralSurfaceProperties` → fields; exception `Autodesk.Civil.*Exception` → `"{Type.Name}: {Message}"` (ADR-04 §5); trần 64 KB + `truncated` nguyên.
- `Service/Civil3dScriptRunner` (MODIFY): theo S-10 — nếu ADR-04 §3 mở `Rebuild` dưới `auto`: pre-pass `REBUILD` diagnostic khi `none`/dryRun gặp member `Rebuild|RebuildAll|RebuildSnapshot` (`ScriptAnalyzer` literals — cùng cách Navis `HEAVY`), và guard profile bỏ 3 tên đó (phase 0 sửa → **1 hàng thêm vào bảng phase 0**, gate lại); nếu đóng → không đổi. `civil == null` → chạy với `civil` null + `log("No CivilDocument — plain drawing?")` (S-04 chốt).
- Cửa sổ `View/Civil3dBridgeStatusView.xaml` (MODIFY copy): tiêu đề "HPCivil3d MCP Bridge — Civil 3D 2026", 1 checkbox "Allow AI code execution" (OFF mỗi khởi động, không persist — `BridgeSettingsStore.Load` ép), `AutoStartListener` persist, pipe name, last run, audit tail, link thư mục log — **không** checkbox heavy (ADR-04 §Alternatives); theme `Civil3dTheme{,Light}` theo COLORTHEME (`Civil3dThemeSwitcher`).
- Loader Ribbon (MODIFY copy `McpRibbonTab`, `RibbonIcons`, `RibbonCommandHandler`): tab `HPCivil3d` id `HPCIVIL3D_MCP_TAB`, panel `HPCIVIL3D_MCP_PANEL` "MCP", nút `HPCIVIL3D_MCP_BRIDGE` "MCP Bridge" → `BridgeActions.Run("show")`; tooltip `Command = HPC3DMCPBRIDGE`; disabled + loader-log path khi bridge fail; icon = cùng `DrawingImage` (window + plug; ink `#E6E6E6`/`#3C3C3C` theo COLORTHEME, plug `#0696D7`); tạo khi Ribbon có, re-create sau workspace switch, rebuild sau COLORTHEME (`SystemVariableChanged` → Idle → `EnsureCreated` + `FindTab` guard), gỡ ở `Terminate`, không nhân đôi. Xoá `HPC3DMCPSPIKE*` + env bypass.
- `HPCivil3d.McpBridge.Tests` (NEW, net8.0-windows, xunit v3; **không** ref `AeccDbMgd`/`AutoCAD.NET` để build mọi máy): `MirrorTests` — bảng token (`README.md` phase 1) áp lên từng file trong danh sách byte-identical (Loader 8 `.cs`; Bridge `MainThreadExecutor`, `Civil3dScriptRunner`↔`AutocadScriptRunner`, `Civil3dResultSerializer`↔`AutocadResultSerializer` **trừ** khối Civil được đánh dấu `// civil-only: begin/end` và bị strip trước so, `DatabaseChangeCounter`, `AutocadVersionMap`, `Civil3dThemeSwitcher`↔`AutocadThemeSwitcher`, `View/*.xaml.cs`, `Resources/Themes/*.xaml`, `View/*.xaml`) → `Assert.Equal(normalized(autocad), normalized(civil))` với path HPAutoCad tìm qua `..\..\..\..\HPAutoCad\` từ `AppContext.BaseDirectory` (skip quan sát được nếu repo không có HPAutoCad — không xảy ra trong repo này); `Civil3dUnitsTests` (Meters/Feet/null/mismatch — kiểu `DrawingUnitType` là enum Civil → test dùng `int`/factory nhận `string` để không ref API); guard/analyzer Civil3d qua Core (bổ sung mẫu Civil thật ngoài phase 0: `civil.CogoPoints.SetElevation(id, 1.0)` cho phép, `DataShortcuts.Validate()` cấm).
- Harness pipe (NEW, copy AutoCAD): `run-bridge-unattended.ps1` (Civil 3D qua `Start-AcadWithBridge -Product C3D -Profile '<<C3D_Metric>>'`, `bridge.scr` = `HPC3DMCPBRIDGE`/`HPC3DMCPSTART` + mở `scene\Align-1.dwg`), `pipe-scenarios.py` ≥ 22 check AutoCAD **+ 8 Civil**: context có `civil3d.isCivilDocument=true`, `drawingUnit`, đếm alignments ≥ 1; execute đọc alignment; `units.Label` = Feet trên `Align-1.dwg`; W1 dryRun/commit/`U`; `none`+Add → lỗi; guard `Rebuild`/`DataShortcuts` → `GUARD`; `Autodesk.Civil.*Exception` → message `"SurfaceException: …"` (theo S-07); serializer `Alignment` → `{handle,type,name}`; no-doc `-32003`; busy `-32002`; opt-in `-32001`. `run-ribbon-check.ps1` (copy: tab `HPCivil3d`, 12 check + 1 MANUAL screenshot 2 theme).

Non-functional
- `MirrorTests` xanh = 0 drift; thời gian context < 100 ms; harness ≤ 10 phút; kills only what it started; `-p:DeployBundle=false` khi Civil mở.

## Architecture
[architecture.md §2, §4, §5](architecture.md). Serializer/Context là hai file có "khối Civil" — đánh dấu `// civil-only: begin` … `// civil-only: end` để mirror test strip; mọi thứ ngoài khối phải bằng AutoCAD.

## Related Code Files
- Modify (trong `HPCivil3d/`): `HPCivil3d.McpBridge/{Civil3dBridgeEntry.cs, Service/Civil3dContextReader.cs, Service/Civil3dUnits.cs, Service/Civil3dResultSerializer.cs, Service/Civil3dScriptRunner.cs, View/Civil3dBridgeStatusView.xaml(.cs), Resources/Themes/*.xaml}`, `HPCivil3d.McpBridge.Loader/{Ribbon/McpRibbonTab.cs, Ribbon/RibbonIcons.cs, Ribbon/RibbonCommandHandler.cs, BridgeLoaderCommands.cs (xoá spike), BridgeActions.cs}`.
- Create: `HPCivil3d.McpBridge.Tests/{HPCivil3d.McpBridge.Tests.csproj, MirrorTests.cs, MirrorTokenTable.cs, Civil3dUnitsTests.cs, Civil3dGuardTests.cs}`; `tools/harness/{run-bridge-unattended.ps1, pipe-scenarios.py, bridge.scr, run-ribbon-check.ps1}`; `HPCivil3d.slnx` += test project.
- Nếu S-10 mở `Rebuild`: Modify `McpShared/HPRebar.McpBridge.Core/Scripting/GuardProfile.cs` (bỏ 3 tên khỏi `Civil3d`) + test phase 0 + gate byte-identical lại — **ghi thành hàng #8 bảng phase 0** trước khi sửa.
- Reports: `reports/phase-02-bridge-runtime.md`, `reports/phase-02-harness-run.log`, `reports/phase-02-ribbon-check.md` (+ 2 screenshot), `reports/code-review-phase-02.md`, `reports/test-report-phase-02.md`.

## Implementation Steps
1. Context reader + units + `Civil3dInfo` đầy đủ; đo thời gian trên `Corridor-1.dwg`.
2. Serializer Civil cases + exception mapping (kiểu từ S-07); đánh dấu `civil-only` blocks.
3. Runner theo ADR-04 §3 sau S-10 (mở/đóng `Rebuild`); nếu mở → sửa phase 0 #8 + gate.
4. Cửa sổ + theme; Ribbon Civil (ids, title, command); xoá spike-only; build deploy (Civil đóng).
5. `HPCivil3d.McpBridge.Tests`: `MirrorTokenTable` (từ README phase 1) → `MirrorTests` (đỏ nếu tự sửa 1 dòng runner Civil — kiểm bằng cách thử); `Civil3dUnitsTests`; guard tests.
6. Harness pipe + ribbon check; chạy ×2; `reports/phase-02-*`.
7. Code review → fix → mirror test + harness lại.

## Từ review phase 1 ([code-review-phase-01.md](reports/code-review-phase-01.md), 7.5/10) — việc chuyển sang phase 2
- **M2 marker `civil-only`:** chỉ 3/8 file drift có `// civil-only: begin/end` (ContextReader, ResultSerializer, ScriptRunner). Còn `Civil3dBridgeEntry` (10 hunk), `Civil3dScriptGlobals`, `ScriptingSelfCheck`, `Loader/BridgeLoadContext.cs:16,30`, `Loader/Ribbon/McpRibbonTab.cs:8`, `Bundle/PackageContents.xml`. MirrorTests quyết quy ước trước khi viết: marker cho hunk nhỏ, **file được liệt kê "Civil-owned"** (không mirror) cho `Civil3dBridgeEntry`/`Civil3dScriptGlobals`/`ScriptingSelfCheck`; XML/`launchSettings.json` so bằng token + regex version (`<Version>`/`AppVersion` strip — L13: bảng token pin `0.3.0→0.1.0`, áp **theo thứ tự**).
- **M4 description:** `ExecuteCivil3dCodeTool`, `Civil3dHostProfile`, README §Script contract phải nói `DrawingUnits` mặc định **Feet** khi DWG không có Civil settings và `insunitsMismatch` cảnh báo (ADR-03 revised); `get_civil3d_context` trả `insunitsMismatch` — description nhắc AI đọc context trước khi ghi toạ độ.
- **L7 wording cửa sổ:** XAML còn "in this AutoCAD session" / "when AutoCAD opens" / "· AutoCAD 2026" (5 literal) → đổi "Civil 3D" + thêm 5 literal vào bảng token (mirror giữ).
- **L12 harness:** `Stop-Acad` thử `_.QUIT _N` qua COM (pid-guard) với 20 s grace trước `Stop-Process -Force` (Drawing Recovery entries); sweep `.dwl` đã có trong `finally`.
- Đã xử lý ngay trong phase 1 (không mang sang): M1 bỏ hẳn nhánh `HPCIVIL3D_MCP_SPIKE` (MainThreadExecutor byte-identical AutoCAD sau token; S-10b/c của spike kiểm **guard từ chối** `Rebuild`); M3 `Answer-SecureLoad` chỉ trả lời dialog của acad.exe harness start **và** nêu tên `HPCivil3d.McpBridge`; L5 bỏ mã plan trong comment/docstring; L6 F5 profile `/product C3D`; L8 `finally` xoá `HPCIVIL3D_MCP_Registry__*`, `HP_HARNESS_ACAD_PID`; L9 `CogoPoints.Count` O(1); L10 `Count()` log Debug; L11 mismatch so tương đối 1e-4 (US survey feet = feet).

## Todo List
- [x] Context/units/`Civil3dInfo` (phase 1 + `Civil3dUnitTable`) · [x] Serializer Civil (AlignmentEntity/SubEntity, CogoPoint, StyleBase) + lỗi `{Type}: {Message}` · [x] Runner — `Rebuild*` deny giữ (ADR-04), engine không đổi · [x] Cửa sổ wording Civil 3D + theme · [x] Ribbon verified live 12/12 + 1 MANUAL (spike bypass đã xoá ở phase 1) · [x] `HPCivil3d.McpBridge.Tests` 41 (Mirror 28 + UnitTable 13; guard tests đã có ở McpShared `Civil3dProfileTests`) · [x] Harness pipe 31 check ×3 (run 1/3/4) · [x] Ribbon check ×2 (run 2 sạch) · [x] Report ([reports/phase-02-bridge-runtime.md](reports/phase-02-bridge-runtime.md)) · [x] Review 8/10 (0 High, 3 M, 8 L → 10 fixed, L5 giữ như AutoCAD, I12 → phase 3) + tester 47/41 xanh, 206 engine, mutation, logs khớp → [reports/code-review-phase-02.md](reports/code-review-phase-02.md), [reports/test-report-phase-02.md](reports/test-report-phase-02.md)

## Success Criteria
- [x] `dotnet test HPCivil3d/HPCivil3d.McpBridge.Tests` → ≥ 25 test, 0 fail, 0 skip (trên máy này); cố ý đổi 1 ký tự trong `Civil3dScriptRunner.cs` ngoài khối civil-only → đúng 1 `MirrorTests` fail (rồi hoàn lại). → **47 test (sau review), 0 fail, 0 skip; mutation 1 ký tự → đúng 1 fail (hoàn lại, ×2: runner + DatabaseChangeCounter).**
- [x] `pwsh -File HPCivil3d/tools/harness/run-bridge-unattended.ps1` → ≥ 30/30 check ×2, không skip; Civil 3D tự thoát; `%LocalAppData%\HPCivil3d\McpBridge\logs\` có `MCP scripting self-check OK`. → **31/31 ×4 (run 1, 3, 4, 5; `powershell` 5.1), Civil 3D tự thoát graceful 18–19 s.**
- [x] `pwsh -File HPCivil3d/tools/harness/run-ribbon-check.ps1` → 12 PASS + 1 MANUAL (2 screenshot trong `reports/`): tab `HPCivil3d` đúng một lần, còn một sau workspace/COLORTHEME round trip, nút mở cửa sổ, click 2 vẫn 1 cửa sổ, loader.log không failure. → **12/12 + 1 MANUAL (run 2, run 3), screenshot 2 theme trong `HPCivil3d/output/ribbon-check/`, icon xem tay OK.**
- [x] `get_civil3d_context` (qua server tạm) trả `civil3d{...11 field}` + `autocad{...}` + `units.length` = `Feet`/`Meters`; thời gian < 1.5 s khi script đang chạy (busy path). → **C1 PASS (`civil3d` 11 field, `units.length` = Meters); busy path < 1.5 s → phase 4.**
- [x] `grep -rn "HPC3DMCPSPIKE\|HPCIVIL3D_MCP_SPIKE" HPCivil3d` = 0. → **0.**
- [x] 7 suite + phase-0 tests không hồi quy; `tools/list` 4 host không đổi (không sửa engine trừ khi #8). → **`McpShared/` không đổi (`git diff --stat` rỗng) → gate 4 host không cần chạy lại.**

## Risk Assessment
| Risk | Mitigation |
|---|---|
| Mirror test quá cứng (khoảng trắng/`using` order) → đỏ vô ích | normalize: CRLF→LF, trim trailing, strip `civil-only` blocks, áp token table; giữ danh sách file trong test làm nguồn duy nhất |
| Serializer walk object Civil (`DisposableWrapper`) ném ngoài transaction | serialize **trước** `inner.Commit()` (đã có); type-case rõ; fallback `{type}` cho `DisposableWrapper` chưa map |
| Ribbon tạo trước khi Civil nạp xong `AeccUi*.arx` → tab mất khi workspace Civil load | `EnsureCreated` trên Idle + `FindTab` guard (đã chống ở AutoCAD workspace switch); ribbon check đo workspace `Civil 3D` ↔ `Drafting & Annotation` |
| COLORTHEME trong Civil khác (`Civil 3D` workspace dark mặc định) | theme switcher đọc COLORTHEME như AutoCAD; screenshot 2 theme |

## Security Considerations
- Opt-in OFF mỗi khởi động; không persist; audit `%AppData%\HPCivil3d\McpBridge\audit\`; guard Civil (ADR-04); không bypass sau phase 2.

## Next Steps
- Phase 3 (server/profile/seed/tests) chạy song song được sau phase 1 (chỉ cần spike kết luận ADR-05 R6/R9/W2); phase 4 cần phase 2 + 3.
