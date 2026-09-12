---
title: "Phase 4 — Registry theo host (library/DB/seed/validator theo profile, trường host) + 12 seed tool AutoCAD + compile-check xUnit"
status: planned
priority: P1
effort: 14h
depends_on: [phase-03]
created: 2026-09-13
---

# Phase 4 — Registry per host & AutoCAD seeds

## Context
- [ADR-04](adr/adr-04-registry-per-host-library-and-host-field.md) (chốt: library + DB riêng theo profile, `host` field, seed `SeedLibrary/<Host>/`, categories/reserved theo profile) · [Revit ADR-05/06](../260912-1521-dynamic-revit-mcp-server-2026/adr/) · [research §0.2](research/autocad-dotnet-api-2026-report.md) (NuGet 25.1.0 ship `lib/net8.0` DLL → compile-check được).
- Mã: `Registry/{RegistryOptions,ToolRecord,ToolManager,ToolValidator,ToolLifecycleService,SeedInstaller,DynamicToolRegistrar}.cs`, `Registry/SeedLibrary/**` (21 seed), `Tests/SeedLibraryTests.cs` (wrapper class + `MetadataReference` từ NuGet cache; bài học `ScriptArgs` CS0246 → wrapper phải khớp import bridge), `Tests/Registry/*`.
- Seed contract (CLAUDE.md): script thuần kết thúc `return`, mm ở biên, `args.X("key", default)` cho mọi input, `ScriptArgs` viết đầy đủ tên khi làm tham số helper.

## Overview
Hoàn tất ADR-04 trong mã dùng chung (không viết registry thứ hai), di chuyển seed Revit vào `SeedLibrary/Revit/` (nội dung không đổi), thêm 12 seed AutoCAD vào `SeedLibrary/Autocad/`, mở rộng `SeedLibraryTests` thành theory theo host với reference assemblies AutoCAD 25.1.0. Cuối phase: host `autocad` `tools/list` = 4 + 8 + 12.

## Key insights
- Với library/DB riêng, `search_tools`/registrar "chỉ host này" là **hệ quả cấu trúc**; `host` field + filter trong `LoadAllAsync` là hàng rào thứ hai (file copy tay).
- Seed Revit đã cài ở user (`_seeds.json` checksum theo nội dung) → di chuyển embedded resource không kích hoạt ghi đè.
- `units` phải là kiểu **Core** (`HPRebar.McpBridge.Core.Scripting.ScriptUnits`) để (a) test compile-check không cần reference project bridge (net8.0-windows) và (b) contract script host-neutral. Bridge AutoCAD tạo `ScriptUnits(label, mmPerDrawingUnit)` từ `Insunits` (phase 2 đổi `AutocadUnits` → `ScriptUnits`; nếu phase 2 đã xong trước, đây là refactor 1 file).
- AutoCAD compile-check: wrapper `SeedHost` khai `Document doc; Database db; Editor ed; DocumentCollection app; Transaction tr; ScriptUnits units; CancellationToken ct; Action<string> log; Action<int,int,string> progress; ScriptArgs args;` + usings **đúng** danh sách bridge (architecture §3, không `Autodesk.AutoCAD.Runtime`).

## Requirements
Functional — registry (mã dùng chung)
- `ToolRecord.Host` (default `"revit"`), `HostVersions` (+ `RevitVersions` legacy đọc); `ToolLibraryStore.Write` ghi `host`, `hostVersions`.
- `RegistryOptions`: `LibraryPath`/`DbPath` mặc định lấy từ `IHostProfile` khi config không đặt (bind sau khi có profile: `PostConfigure<RegistryOptions>`).
- `ToolManager.LoadAllAsync`: bỏ qua + warning record `Host != profile.HostId`; `Record`/`RecordAdhoc` điền `RevitVersion` (= host version, tên giữ) như cũ.
- `ToolValidator.Validate(candidate, analysis, existing, newVersion, profile)`: categories, reserved names, `host` phải bằng `profile.HostId` (server điền trước khi validate).
- `ToolLifecycleService.ProposeAsync`: `Host = profile.HostId`, `HostVersions = [bridge.LastStatus?.RevitVersion]`; `_review/<name>.md` in `Host:`; `AnalyzeAsync` gửi `profile.Method("analyze")`.
- `SeedInstaller.LoadSeeds(assembly, hostId)`: prefix `SeedLibrary/<Host>/` (4 phần); `Install(store, logger, hostId)`; csproj `EmbeddedResource` không đổi pattern (folder mới tự vào LogicalName).
- `DynamicToolRegistrar`/`RegistryToolFunction` description: "Registry tools run stored, reviewed C# inside {DisplayName} — prefer them over {ExecuteToolName}".
- `RegistryCli`: `--host <id>` (ưu tiên hơn env), `stats` in `host`.
- `ToolifyPrompts.toolify_run`: persona + contract từ `profile.ScriptContractSummary`.
Functional — 12 seed AutoCAD (`SeedLibrary/Autocad/<Category>/<name>/{tool.json, code.cs, examples.json}`, `status: published`, `author: hprebar`, `host: autocad`, `hostVersions: ["2026"]`)

| # | name | Category | transaction | args (mm ở biên) | return |
|---|---|---|---|---|---|
| 1 | `list_layers` | Layer | none | `includeCounts` (bool, false) | `[{name,colorIndex,isOff,isFrozen,isLocked,lineweight,entityCount?}]` |
| 2 | `list_block_definitions` | Block | none | `pattern` (str, "*"), `includeAnonymous` (false) | `[{name,entityCount,referenceCount,hasAttributes}]` |
| 3 | `get_entities` | Data | none | `layer?`, `type?` (DXF: LINE/LWPOLYLINE/CIRCLE/INSERT/TEXT/MTEXT/DIMENSION…), `limit` (200), `space` (model/current) | `{count,truncated,items:[{handle,type,layer,bboxMm:{min,max}}]}` (dùng `ed.SelectAll(SelectionFilter)`) |
| 4 | `list_layouts` | Layout | none | — | `[{name,tabOrder,isModel,viewportCount,isCurrent}]` |
| 5 | `get_drawing_info` | Data | none | `countByType` (true) | `{fileName,insunits,measurement,extentsMm,currentLayout,currentLayer,layerCount,countsByType}` |
| 6 | `get_selected_entities` | Generic | none | `limit` (200) | `[{handle,type,layer}]` từ `ed.SelectImplied()` |
| 7 | `draw_polyline` | Drawing | auto | `points` ([[x,y],…] mm, ≥ 2), `closed` (false), `layer?`, `colorIndex?` | `{handle,lengthMm,vertexCount}` (2 điểm vẫn là LWPOLYLINE; ghi rõ trong description) |
| 8 | `draw_circle` | Drawing | auto | `center` ([x,y] mm), `radiusMm`, `layer?` | `{handle}` |
| 9 | `add_text` | Annotation | auto | `text`, `position` ([x,y] mm), `heightMm` (2.5×scale?), `rotationDeg` (0), `layer?`, `mtext` (false), `widthMm?` | `{handle,type}` (DBText \| MText) |
| 10 | `create_layer` | Layer | auto | `name`, `colorIndex` (7), `lineweight?`, `ifExists` (skip/update) | `{name,created,updated}` |
| 11 | `insert_block` | Block | auto | `blockName`, `position` ([x,y] mm), `scale` (1), `rotationDeg` (0), `layer?`, `attributes` ({tag:value}, {}) | `{handle,attributesSet}` (lỗi rõ nếu block không tồn tại) |
| 12 | `add_linear_dimension` | Annotation | auto | `p1`, `p2`, `dimLinePoint` ([x,y] mm), `layer?`, `dimStyle?` | `{handle,measurementMm}` (`AlignedDimension` hoặc `RotatedDimension` theo `aligned` bool, default rotated ngang/dọc) |

Mỗi seed ≥ 2 example khác args; description ≥ 20 ký tự nêu units; `timeoutSeconds` 30–60; code dùng `tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db) …)`, `units.ToDrawing`, `args.*`, `log`, kiểm `ct` trong vòng lặp; không `ed.Get*`, không Commit.
Non-functional
- `SeedLibraryTests`: `[Theory] MemberData(Seeds)` với key `<Host>/<Category>/<name>`; compile bằng reference set theo host (Revit: như hiện tại; AutoCAD: mọi `lib/net8.0/*.dll` của `autocad.net`, `autocad.net.core`, `autocad.net.model` **25.1.0** trong NuGet cache + `ScriptArgs`/`ScriptUnits` từ Core); `Assert.SkipWhen` khi thiếu cache.
- Test registry chạy với cả hai profile (fixture tham số `hostId`): seed install chỉ cài seed của host; `LoadAllAsync` bỏ record host khác; validator từ chối category lạ và `host` sai; `propose_tool` điền `host`.

## Architecture
Xem [ADR-04 §Decision](adr/adr-04-registry-per-host-library-and-host-field.md) và [architecture.md §5–6](architecture.md).

## Related code files
- **Tái dùng nguyên:** `ToolLibraryStore`, `ToolRegistryDb`, `StabilityScorer`, `Tools/Registry/*` (logic), `RegistryStartup`, `ToolLifecycleTools`, `RunHistoryTools`, `RunToolTool`, `ToolRegistryQueryTools`.
- **Tách ra chung / sửa:** `RegistryOptions`, `ToolRecord`, `ToolManager`, `ToolValidator`, `ToolLifecycleService`, `SeedInstaller`, `DynamicToolRegistrar`, `ToolifyPrompts`, `RegistryCli`, `HPRebar.Mcp.Server.csproj` (không đổi pattern EmbeddedResource; kiểm LogicalName 4 cấp), `Registry/SeedLibrary/**` (git mv → `Revit/`), `Tests/SeedLibraryTests.cs`, `Tests/Registry/{ToolRegistryTests,ToolLifecycleTests}.cs` (fixture theo host), Core `Scripting/ScriptUnits.cs` (mới, host-neutral) + phase 2 `AutocadScriptGlobals.units` kiểu `ScriptUnits`.
- **Viết mới:** 36 file seed AutoCAD; `Tests/RegistryHostFilterTests.cs`; `Tests/SeedLibraryAutocadReferenceLocator` (helper trong `SeedLibraryTests`).

## Implementation steps
1. `ScriptUnits` (Core) + test; cập nhật phase-2 globals nếu đã có `AutocadUnits`.
2. `ToolRecord` (+Host/HostVersions), `RegistryOptions` defaults theo profile, `ToolManager` filter, `ToolValidator(profile)`, `ToolLifecycleService` (host, review file, analyze method), `DynamicToolRegistrar` text, `ToolifyPrompts` persona, `RegistryCli --host`.
3. `git mv Registry/SeedLibrary/<Category>` → `Registry/SeedLibrary/Revit/<Category>`; `SeedInstaller` 4 cấp + host filter; chạy test Revit (`SeedLibraryTests` với key mới) → 21 seed vẫn pass; kiểm `_seeds.json` checksum không đổi (test: checksum của seed sau move == trước move — snapshot trong test bằng hằng hash cho 2–3 seed).
4. Viết 12 seed AutoCAD (mỗi seed: tool.json schema subset, code.cs, examples.json). Thứ tự: 5 read trước (1–6) để phase 5 HIT có nhiều lựa chọn, rồi 7–12.
5. `SeedLibraryTests` theory theo host + reference locator AutoCAD (`autocad.net*/25.1.0/lib/net8.0/*.dll`); `Compile_check_rejects_api_misuse_and_accepts_real_api` bản AutoCAD (`db.NoSuchMember` CS1061; `ed.SelectAll()` OK); guard-clean cho mọi seed với `GuardProfile.Autocad` (deny `ed.GetPoint` phải bắt được trong test negative).
6. Registry tests theo host: fixture `hostId` ∈ {revit, autocad}; `RegistryHostFilterTests` (record `host: revit` trong library autocad → bị bỏ qua + warning; `propose_tool` với category `Architecture` trên host autocad → error).
7. Publish exe; `mcp_call.py` host autocad: `tools/list` = 24; `search_tools "layer"` → `list_layers`, `create_layer`; `run_tool list_layers` dryRun với AutoCAD 2026 (bridge phase 2) → kết quả thật; `registry stats --host autocad`.
8. Host revit không đổi: `tools/list` = 34; `registry stats` (revit) không đổi.

## Todo
- [ ] 1 ScriptUnits · [ ] 2 registry host-aware · [ ] 3 move seed Revit + test · [ ] 4 12 seed AutoCAD · [ ] 5 SeedLibraryTests theo host · [ ] 6 registry tests theo host · [ ] 7 publish + smoke autocad · [ ] 8 smoke revit không đổi

## Success criteria
- `dotnet test HPRebar/HPRebar.Mcp.Server.Tests`: mọi test cũ pass; 12 seed AutoCAD compile-check pass (không skip trên máy dev vì cache có 25.1.0); registry tests pass cho cả hai host.
- `tools/list` autocad = 24; revit = 34.
- `%AppData%\HPRebar\McpServer\tools-library-autocad\` có 12 folder + `_seeds.json`; `tools-library\` (Revit) **không thay đổi** (`git diff` không áp dụng; kiểm bằng hash folder trước/sau).
- Mỗi `tool.json` AutoCAD có `host: "autocad"`; `registry show <name> --host autocad` in đúng.

## Risks
| Risk | Mitigation |
|---|---|
| Compile-check pass nhưng runtime fail trong AutoCAD (bài học `ScriptArgs` Revit) | wrapper import = bridge import **đúng từng dòng** (test đọc danh sách import từ một hằng dùng chung `AutocadScriptImports` trong Core? — Core không nên biết AutoCAD; đặt hằng trong `AutocadHostProfile` server và bridge cùng đọc? Bridge không ref server → **hằng trong Contracts** `HostScriptContracts.AutocadImports` (string[]) dùng bởi bridge, server description, và test) |
| `AcMgd.dll` cần `System.Windows.*` khi compile (WPF types trong chữ ký) | chỉ lỗi nếu script đụng thành viên đó; thêm `WindowsBase`/`PresentationFramework` từ `Microsoft.WindowsDesktop.App` 8 ref pack nếu CS0012 xuất hiện |
| Move seed Revit làm `git` history khó đọc | `git mv` giữ rename detection; commit riêng "refactor(registry): seed library per host" |
| Seed `insert_block` cần block có sẵn trong template | example dùng block tạo trong cùng test (`test_tool` tạo block trước?) — không; example ghi `expected: error when block missing` là hợp lệ; live verify dùng drawing có block |
| `RevitVersions` trong tool.json cũ | đọc được, `HostVersions` ưu tiên; không ghi lại file seed Revit |

## Security
Seed AutoCAD qua cùng guard/opt-in/timeout/audit; `Destructive` cho 7–12; `none` cho 1–6.

## Next steps
Phase 5 live verify 3 kịch bản registry trên AutoCAD + hồi quy Revit.
