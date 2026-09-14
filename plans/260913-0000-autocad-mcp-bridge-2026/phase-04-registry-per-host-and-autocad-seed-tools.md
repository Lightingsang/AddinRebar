---
title: "Phase 4 — Registry theo profile trong `McpShared` (categories/validator/text/host field) + 12 seed AutoCAD nhúng trong exe AutoCAD + compile-check xUnit"
status: built + tested + verified (2026-09-14) — 12 seeds compile-checked (46 tests) and all 12 run live in AutoCAD 2026 over stdio (reports/phase-04-seeds-registry.md); tools/list 24 / Revit 34 names+schemas unchanged (descriptions host-neutral)
priority: P1
effort: 12h (actual ≈ 3h)
depends_on: [phase-03]
created: 2026-09-13
revised: 2026-09-14 (ADR-06/ADR-04 revised — seed Revit không di chuyển; root theo product; không `--host`)
---

# Phase 4 — Registry per profile & AutoCAD seeds

> Revised 2026-09-14: engine registry đã ở `McpShared/HPRebar.Mcp.Server.Core` (phase 0). Không còn `SeedLibrary/<Host>/`, `tools-library-autocad`, `--host`: mỗi exe nhúng seed của mình với prefix `SeedLibrary/` **không đổi**, root `%AppData%\<Product>\McpServer\` riêng (ADR-04 revised). 21 seed Revit **không di chuyển**.

## Context
- [ADR-04 revised](adr/adr-04-registry-per-host-library-and-host-field.md) · [ADR-06](adr/adr-06-one-mcp-one-folder.md) · [Revit ADR-05/06](../260912-1521-dynamic-revit-mcp-server-2026/adr/) · [research §0.2](research/autocad-dotnet-api-2026-report.md) (NuGet 25.1.0 ship `lib/net8.0` DLL → compile-check được).
- Mã (nay trong `McpShared/HPRebar.Mcp.Server.Core`): `Registry/{RegistryOptions,ToolRecord,ToolManager,ToolValidator,ToolLifecycleService,SeedInstaller,DynamicToolRegistrar}.cs`; test mẫu `HPRebar/HPRebar.Mcp.Server.Tests/SeedLibraryTests.cs` (wrapper class + `MetadataReference` từ NuGet cache; bài học `ScriptArgs` CS0246 → wrapper phải khớp import bridge).
- Seed contract (CLAUDE.md): script thuần kết thúc `return`, mm ở biên, `args.X("key", default)` cho mọi input, `ScriptArgs` viết đầy đủ tên khi làm tham số helper.

## Overview
Hoàn tất phần profile-driven của registry trong `McpShared` (categories, reserved names, `host` field, text từ profile), viết 12 seed AutoCAD vào `HPAutoCad/HPAutoCad.Mcp.Server/Registry/SeedLibrary/`, `SeedLibraryTests` bản AutoCAD với reference assemblies 25.1.0. Cuối phase: `HPAutoCad.Mcp.Server.exe` `tools/list` = 4 + 8 + 12.

## Key insights
- Với root/DB riêng theo exe, "chỉ tool của host này" là **hệ quả cấu trúc**; `host` field + skip trong `LoadAllAsync` là hàng rào thứ hai (file copy tay).
- `units` là kiểu **Core** (`HPRebar.McpBridge.Core.Scripting.ScriptUnits`, đã tạo phase 0) để (a) test compile-check không cần reference project bridge (net8.0-windows) và (b) contract script host-neutral.
- AutoCAD compile-check: wrapper `SeedHost` khai `Document doc; Database db; Editor ed; DocumentCollection app; Transaction tr; ScriptUnits units; CancellationToken ct; Action<string> log; Action<int,int,string> progress; ScriptArgs args;` + usings **đúng** `HostScriptContracts.AutocadImports` (Contracts) — một nguồn cho bridge, description và test.
- Test engine (`McpShared/…Core.Tests`) dùng `TestHostProfile` + seed giả; test seed thật nằm cạnh exe của host.

## Requirements
Functional — registry (`McpShared/HPRebar.Mcp.Server.Core`, mã dùng chung)
- `ToolRecord.Host` (đã có từ phase 0; default = host của exe khi thiếu), `HostVersions` (+ `RevitVersions` legacy đọc); `ToolLibraryStore.Write` ghi `host`, `hostVersions`.
- `ToolManager.LoadAllAsync`: bỏ qua + warning record `Host != profile.HostId`; `Record`/`RecordAdhoc` điền `RevitVersion` (= host version, tên giữ) như cũ.
- `ToolValidator.Validate(candidate, analysis, existing, newVersion, profile)`: categories/reserved theo profile; `host` phải bằng `profile.HostId` (server điền trước khi validate).
- `ToolLifecycleService.ProposeAsync`: `Host = profile.HostId`, `HostVersions = [bridge.LastStatus?.RevitVersion]`; `_review/<name>.md` in `Host:`; `AnalyzeAsync` gửi `profile.Method("analyze")`.
- `DynamicToolRegistrar`/`RegistryToolFunction` description: "Registry tools run stored, reviewed C# inside {DisplayName} — prefer them over {ExecuteToolName}".
- `ToolifyPrompts.toolify_run`: persona + contract từ `profile.ScriptContractSummary`.
- `RegistryCli`: `stats` in `host` + root; không cờ `--host`.
Functional — 12 seed AutoCAD (`HPAutoCad/HPAutoCad.Mcp.Server/Registry/SeedLibrary/<Category>/<name>/{tool.json, code.cs, examples.json}`, `status: published`, `author: hprebar`, `host: autocad`, `hostVersions: ["2026"]`)

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
| 9 | `add_text` | Annotation | auto | `text`, `position` ([x,y] mm), `heightMm`, `rotationDeg` (0), `layer?`, `mtext` (false), `widthMm?` | `{handle,type}` (DBText \| MText) |
| 10 | `create_layer` | Layer | auto | `name`, `colorIndex` (7), `lineweight?`, `ifExists` (skip/update) | `{name,created,updated}` |
| 11 | `insert_block` | Block | auto | `blockName`, `position` ([x,y] mm), `scale` (1), `rotationDeg` (0), `layer?`, `attributes` ({tag:value}, {}) | `{handle,attributesSet}` (lỗi rõ nếu block không tồn tại) |
| 12 | `add_linear_dimension` | Annotation | auto | `p1`, `p2`, `dimLinePoint` ([x,y] mm), `layer?`, `dimStyle?`, `aligned` (false) | `{handle,measurementMm}` |

Mỗi seed ≥ 2 example khác args; description ≥ 20 ký tự nêu units; `timeoutSeconds` 30–60; code dùng `tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db) …)`, `units.ToDrawing`, `args.*`, `log`, kiểm `ct` trong vòng lặp; không `ed.Get*`, không Commit.
Non-functional
- `HPAutoCad/HPAutoCad.Mcp.Server.Tests/SeedLibraryTests.cs`: `[Theory] MemberData(Seeds)` đọc resource `SeedLibrary/` của **exe AutoCAD**; compile bằng mọi `lib/net8.0/*.dll` của `autocad.net`, `autocad.net.core`, `autocad.net.model` **25.1.0** trong NuGet cache + `ScriptArgs`/`ScriptUnits` từ Core; `Assert.SkipWhen` khi thiếu cache; guard-clean với `GuardProfile.Autocad`; well-formed record (name regex, category ∈ profile, transaction, timeout, `host == "autocad"`).
- `McpShared/…Core.Tests/Registry/*`: theory theo `TestHostProfile` (2 profile giả: categories khác nhau) — seed install chỉ cài seed của `hostAssembly` truyền vào; `LoadAllAsync` bỏ record host khác; validator từ chối category lạ và `host` sai; `propose_tool` điền `host`.
- `HPRebar/HPRebar.Mcp.Server.Tests/SeedLibraryTests.cs` (Revit) **không đổi** trừ `using` (đã phase 0).

## Architecture
Xem [ADR-04 revised §Decision](adr/adr-04-registry-per-host-library-and-host-field.md) và [architecture.md §5–6](architecture.md).

## Related code files
- **Tái dùng nguyên (`McpShared`):** `ToolLibraryStore`, `ToolRegistryDb`, `StabilityScorer`, `Tools/Registry/*` (logic), `RegistryStartup`, `ToolLifecycleTools`, `RunHistoryTools`, `RunToolTool`, `ToolRegistryQueryTools`.
- **Tách ra chung / sửa (`McpShared`):** `ToolRecord` (HostVersions), `ToolManager` (host mismatch skip), `ToolValidator(profile)`, `ToolLifecycleService` (host, review file, analyze method), `DynamicToolRegistrar` text, `ToolifyPrompts` persona, `RegistryCli` stats; Contracts `HostScriptContracts.AutocadImports` (đã tạo phase 0; điền danh sách); tests `McpShared/…Core.Tests/Registry/*` (theo profile giả), `RegistryHostFieldTests`.
- **Viết mới (`HPAutoCad/`):** 36 file seed; `HPAutoCad.Mcp.Server.Tests/SeedLibraryTests.cs` (+ helper locator NuGet cache AutoCAD).

## Implementation steps
1. `ToolRecord.HostVersions`, `ToolManager` skip, `ToolValidator(profile)`, `ToolLifecycleService` (host, review, analyze method), `DynamicToolRegistrar` text, `ToolifyPrompts` persona, `RegistryCli` stats — chạy `dotnet test McpShared/…Core.Tests` + `dotnet test HPRebar/HPRebar.Mcp.Server.Tests` (Revit: 21 seed vẫn well-formed/compile; `registry stats` Revit không đổi).
2. `HostScriptContracts.AutocadImports` = danh sách usings (architecture §3); bridge phase 2 và description phase 3 đọc từ đây (nếu đã hard-code → đổi sang hằng).
3. Viết 12 seed AutoCAD (mỗi seed: tool.json schema subset, code.cs, examples.json). Thứ tự: 1–6 (read) trước, rồi 7–12.
4. `HPAutoCad/…Tests/SeedLibraryTests.cs`: theory + reference locator (`autocad.net*/25.1.0/lib/net8.0/*.dll`); `Compile_check_rejects_api_misuse_and_accepts_real_api` bản AutoCAD (`db.NoSuchMember` CS1061; `ed.SelectAll()` OK); guard negative (`ed.GetPoint` bị bắt).
5. Registry tests theo profile giả trong `McpShared` + `RegistryHostFieldTests` (record `host: revit` trong root AutoCAD → bị bỏ qua + warning; `propose_tool` với category `Architecture` trên profile AutoCAD → error).
6. Publish exe AutoCAD; `mcp_call.py <exe> tools/list` = 24; `search_tools "layer"` → `list_layers`, `create_layer`; `run_tool list_layers` dryRun với AutoCAD 2026 (bridge phase 2) → kết quả thật; `HPAutoCad.Mcp.Server.exe registry stats`.
7. Revit không đổi: `tools/list` = 34; `HPRebar.Mcp.Server.exe registry stats` không đổi; `%AppData%\HPRebar\McpServer\` hash không đổi.

## Todo
- [x] 1 registry profile-driven (validator/lifecycle/registrar/toolify/CLI; meta tool wording host-neutral) · [x] 2 AutocadImports (đã có từ phase 0/2, wrapper test đọc từ Contracts) · [x] 3 12 seed · [x] 4 SeedLibraryTests AutoCAD (46, 0 skip) · [x] 5 registry tests theo profile (`RegistryProfileTests` 3) · [x] 6 publish + smoke (24 tool, 12 seed live, `registry stats`) · [x] 7 Revit: 34 tool, tên/schema y hệt phase 0, library không đổi; **description 7 engine tool đổi chữ** (host-neutral)
- Sai lệch: điểm dạng `{x,y}` thay vì `[x,y]`; seed không tự tạo layer (lỗi rõ); `ToolRecord.RevitVersions`/cột DB giữ nguyên; FTS name boost → follow-up.

## Success criteria
- `dotnet test McpShared/HPRebar.Mcp.Server.Core.Tests`, `dotnet test HPRebar/HPRebar.Mcp.Server.Tests`, `dotnet test HPAutoCad/HPAutoCad.Mcp.Server.Tests`: 0 fail; 12 seed AutoCAD compile-check pass (không skip trên máy dev vì cache có 25.1.0).
- `tools/list` AutoCAD = 24; Revit = 34.
- `%AppData%\HPAutoCad\McpServer\tools-library\` có 12 folder + `_seeds.json`; `%AppData%\HPRebar\McpServer\tools-library\` **không thay đổi** (hash trước/sau).
- Mỗi `tool.json` AutoCAD có `host: "autocad"`; `HPAutoCad.Mcp.Server.exe registry show <name>` in đúng.

## Risks
| Risk | Mitigation |
|---|---|
| Compile-check pass nhưng runtime fail trong AutoCAD (bài học `ScriptArgs` Revit) | wrapper import = `HostScriptContracts.AutocadImports` — một nguồn cho bridge, description, test |
| `AcMgd.dll` cần `System.Windows.*` khi compile (WPF types trong chữ ký) | chỉ lỗi nếu script đụng thành viên đó; thêm `WindowsBase`/`PresentationFramework` từ `Microsoft.WindowsDesktop.App` 8 ref pack nếu CS0012 xuất hiện |
| Seed `insert_block` cần block có sẵn trong template | example `expected: error when block missing` là hợp lệ; live verify dùng drawing có block |
| `RevitVersions` trong tool.json cũ | đọc được, `HostVersions` ưu tiên; không ghi lại file seed Revit |
| Test engine trong `McpShared` không có seed thật | seed giả nhúng trong test assembly (2–3 tool) đủ cho install/upgrade/checksum |

## Security
Seed AutoCAD qua cùng guard/opt-in/timeout/audit; `Destructive` cho 7–12; `none` cho 1–6.

## Next steps
Phase 5 live verify 3 kịch bản registry trên AutoCAD + hồi quy Revit.
