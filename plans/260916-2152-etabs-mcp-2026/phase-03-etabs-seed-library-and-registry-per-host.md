---
phase: 3
title: "Seed library 12 (8 R + 3 W + 1 D) + registry per host + compile-check trong server tests (skip quan sát được) + structure test"
status: completed
priority: P2
effort: "4h"
dependencies: [0, 2]
---

# Phase 3: 12 seed ETABS + registry live

## Context Links
- [ADR-02 §1–4](adr/adr-02-no-transaction-snapshot-tiers-and-opt-ins.md) (tier fixture; D seed-only; `PREVIEW`; path policy) · [ADR-04 §4–6](adr/adr-04-sta-worker-busy-cancel-units-guard-globals.md) (units, ret, `ArgumentException`, va chạm `GetProperty`/`File`) · [ADR-03 §3](adr/adr-03-etabsv1-reference-and-test-without-etabs.md) (compile-check trong server tests) · [ADR-05 §1](adr/adr-05-identity-registry-packaging.md) · [red-team](reports/red-team-2026-09-16.md) hàng 2, 4, 6, 10, 11, 14f
- Mẫu: `HPNavis/HPNavis.Mcp.Server/Registry/SeedLibrary/**`, `HPAutoCad/HPAutoCad.Mcp.Server.Tests/SeedLibraryTests.cs:206–272` (wrapper + skip), `HPNavis/HPNavis.Mcp.Server.Tests/SeedLibraryStructureTests.cs`.
- OAPI: [researcher-01 §3–§6](research/researcher-01-etabs-oapi-facts.md); [evidence §E6, §E8](research/evidence-on-machine-2026-09-16.md). **Member không có trong fact sheet = `[chưa xác minh]` — bước 1 tra CHM index (E6) + fixture tier (phase 2) và ghi topic vào `tool.json.notes` trước khi code.**

## Overview
12 seed nhúng trong server (`Registry/SeedLibrary/<Category>/<name>/`), cài lần đầu vào `%AppData%\HPEtabs\McpServer\tools-library\`, nâng cấp theo `_seeds.json`. Tool surface → **24**. Compile-check trong **`HPEtabs.Mcp.Server.Tests`** (net10, Roslyn + wrapper mirror imports/globals + metadata ref `ETABSv1.dll` tìm lúc test → `Assert.SkipWhen`); structure test luôn chạy. W seed verify bằng `test_tool realRun=true` (preview không chạy — red-team #4).

## Seed contract (mirror Navis, ETABS-specific)
Thân script kết thúc `return`; input qua `args.X("literal", default)`; kN/mm vì bridge ép present units (seed **không** gọi `SetPresentUnits`); mọi `ret` kiểm → `InvalidOperationException($"ETABS returned {ret} from X")`; caller error → `ArgumentException`; duyệt bị chặn `args.Int("limit", 200)` (≤ 500) + `ct.ThrowIfCancellationRequested()`; **không** `Helper`, không member D ngoài seed D, không path-taking member ngoài policy; trường dẫn xuất có nhãn (`momentKNm`, `fcMPa`, `lengthMm`); envelope camelCase `{success, summary, items, count, offset, truncated, warnings, errors[{code,message,name}]}`; helper nhận args viết đầy đủ `HPRebar.McpBridge.Core.Scripting.ScriptArgs`; **không** dùng identifier `File`/`GetProperty` (base guard — ADR-04 §5). `tool.json`: `transaction` `none` (R) / `auto` (W/D); `tags:["destructive"]` + `timeoutSeconds` 600 cho D; tier khai phải khớp fixture (structure test).

## Danh sách seed (12)

| # | Tên | Cat | Tier | Args | Member OAPI (nguồn) | Envelope items |
|---|---|---|---|---|---|---|
| R1 | `get_model_info` | Model | R `none` | — | `GetModelFilename/GetModelFilepath` (researcher-01 §3), `GetModelIsLocked` (CHM › "cSapModel.GetModelIsLocked Method"), `GetPresentUnits/GetDatabaseUnits` (CHM › "cSapModel.SetPresentUnits Method"; `GetDatabaseUnits` `[chưa xác minh]`), `GetVersion` `[chưa xác minh]`, `PointObj/FrameObj/AreaObj.GetNameList` (E13b) | `modelName, modelPath, isLocked, presentUnits, databaseUnits, etabsVersion, counts{points,frames,areas}` |
| R2 | `get_stories_and_grids` | Geometry | R | `limit` | `Story.GetStories` `[chưa xác minh]`; `GridSys.GetNameList` `[chưa xác minh]` | `stories[{name, elevationMm, heightMm}], gridSystems[]` |
| R3 | `get_structural_objects` | Geometry | R | `kind` (frame\|area\|point), `story`, `nameLike`, `limit`, `offset` | `FrameObj.GetNameList/GetPoints/GetSection`, `PointObj.GetCoordCartesian`, `AreaObj.GetNameList/GetPoints` `[chưa xác minh]`; **section của area** qua `DatabaseTables.GetTableForDisplayArray` (CHM › "cDatabaseTables Interface") hoặc **bỏ** (`cAreaObj.GetProperty` bị base guard chặn — red-team #11) | `items[{name, label, story, kind, section?, endpointsMm | verticesMm, lengthMm}]`, paging |
| R4 | `get_materials_and_sections` | Property | R | `limit` | `PropMaterial.GetNameList/GetMaterial`, `PropFrame.GetNameList/GetTypeOAPI/GetRectangle` `[chưa xác minh]` | `materials[], frameSections[]` |
| R5 | `get_load_definitions` | Load | R | — | `LoadPatterns.GetNameList/GetLoadType`, `LoadCases.GetNameList/GetTypeOAPI`, `RespCombo.GetNameList/GetCaseList` `[chưa xác minh]` | `patterns[], cases[], combos[]` |
| R6 | `get_joint_reactions` | Results | R | `caseOrCombo` (req), `pointNames[]`\|`story`, `limit` | `AnalysisResultsSetup.DeselectAllCasesAndCombosForOutput/SetCaseSelectedForOutput/SetComboSelectedForOutput`, `AnalysisResults.JointReact` `[chưa xác minh]` — **R vì `cAnalysisResultsSetup.*` trong allow-list** (ADR-02 §1d); chưa chạy analysis → `ret≠0` → `InvalidOperationException` "run_analysis first" | `items[{point, case, fxKN, fyKN, fzKN, mxKNm, myKNm, mzKNm}]` |
| R7 | `get_frame_forces` | Results | R | `caseOrCombo` (req), `frameNames[]`, `limit` | như R6 + `AnalysisResults.FrameForce` `[chưa xác minh]` | `items[{frame, station, case, pKN, v2KN, v3KN, tKNm, m2KNm, m3KNm}]` |
| R8 | `get_modal_results` | Results | R | `limit` | `AnalysisResults.ModalPeriod`, `ModalParticipatingMassRatios` `[chưa xác minh]` | `modes[{mode, periodS, frequencyHz, ux, uy, rz}]` |
| W1 | `draw_frame_by_coords` | Geometry | W `auto` | `x1..z2` (mm, req), `section`, `name` | `FrameObj.AddByCoord` `[chưa xác minh]`; validate section (`PropFrame.GetNameList`) → `ArgumentException` | `createdCount, affectedNames[], snapshot` |
| W2 | `assign_frame_section` | Property | W | `frameNames[]` (req), `section` (req) | `FrameObj.SetSection` `[chưa xác minh]` | `modifiedCount, affectedNames[], snapshot` |
| W3 | `assign_frame_load` | Load | W | `frameNames[]`, `pattern` (req), `type`, `valueKNperM`\|`valueKN`, `dir`, `replace` | `FrameObj.SetLoadDistributed/SetLoadPoint` `[chưa xác minh]`; `LoadPatterns.GetNameList` validate | `modifiedCount, affectedNames[], snapshot` |
| D1 | `run_analysis` | Analysis | D `auto`, `tags:["destructive"]`, 600 s | `cases[]`, `deleteResultsFirst` | `Analyze.SetRunCaseFlag` `[chưa xác minh]`, `Analyze.DeleteResults` (CHM › "cAnalyze.DeleteResults Method"), `Analyze.RunAnalysis` (CHM › "cAnalyze.RunAnalysis Method"), `Analyze.GetCaseStatus` `[chưa xác minh]`; **mô tả:** "choose timeoutSeconds from the last GUI analysis time (up to 600); a timeout does not abort the analysis — ETABS keeps running it and later calls return busy; the model is saved and snapshotted first" (red-team #6) | `success, ranCases[{name, status}], durationMs, snapshot` |

Tuỳ chọn (không đếm, không MVP): `unlock_model` (D, `confirm:true`), `export_table` (D — path-taking; cần path policy + `args.Str`), `restore_model_snapshot` (D, phụ thuộc E9).

## Requirements
- Functional: 12 seed compile (máy dev); `tools/list` = 24; D1: `test_tool` → `PREVIEW` fail (seed-only), `propose_tool` bản sao → `PREVIEW`/`DESTRUCTIVE`; R seed `run_tool` dưới `none`; W seed `test_tool realRun=true` (snapshot, `tested`) rồi `run_tool`; proposal `transaction:none` chứa W → `etabs.analyze` từ chối.
- Non-functional: `code.cs` < 120 dòng; không literal đơn vị ngoài kN/mm; `_seeds.json` checksum.

## Architecture
```
HPEtabs/HPEtabs.Mcp.Server/Registry/SeedLibrary/{Model,Geometry,Property,Load,Analysis,Results}/<name>/{tool.json, code.cs, examples.json} + _seeds.json
HPEtabs/HPEtabs.Mcp.Server.Tests/SeedLibraryCompileTests.cs     per seed: wrapper class (imports = HostScriptContracts.EtabsImports; fields = EtabsGlobals typed cSapModel/cOAPI/ScriptUnits/CancellationToken/Action/ScriptArgs) → Roslyn compile với TRUSTED_PLATFORM_ASSEMBLIES + ScriptArgs + MetadataReference(ETABSv1.dll từ EtabsApiLocator.Find()) → Assert.SkipWhen(dll null, "ETABS 22 not installed"); + ScriptGuard.Check(code, GuardProfile.Etabs) == 0; + tier theo fixture (đọc HPEtabs.McpBridge/Resources/etabs-oapi-tiers.txt qua đường tương đối) khớp tool.json
HPEtabs/HPEtabs.Mcp.Server.Tests/SeedLibraryStructureTests.cs   tool.json schema; examples.json; _seeds.json checksum; category ∈ Categories; tags destructive ⇔ tier D; transaction none ⇔ tier R; args key ⇔ code (regex `args\.\w+\("key"`); grep cấm: SetPresentUnits|Helper|OpenFile|ExportFile|ImportFile|ImportProp|CSVFile|MergeAnalysisResults|ShowTablesInExcel|Save\( ngoài D
```

## Related Code Files
- Create: 12 thư mục seed + `_seeds.json`; `SeedLibraryCompileTests.cs`, `SeedLibraryStructureTests.cs` (server tests); `live-verify.py --phase seeds`.
- Modify: `HPEtabs.Mcp.Server.csproj` (EmbeddedResource glob), README.

## Implementation Steps
1. Tra CHM index + fixture cho mọi member `[chưa xác minh]` → `tool.json.notes`; member không có topic → đổi/bỏ (báo user nếu < 12).
2. 8 seed R; compile-check; `run_tool` từng seed trên model bỏ đi (👤; R6–R8 sau D1 hoặc GUI analysis).
3. 3 seed W: `test_tool` → `PREVIEW` (fail, ghi rõ); `test_tool realRun=true` → chạy thật + snapshot + `tested`; `run_tool` → `Changed.Added` đúng.
4. D1: `test_tool` → `PREVIEW`; `propose_tool` bản sao → từ chối; checkbox ON (👤) → `run_tool run_analysis` (`timeoutSeconds` theo GUI) → `ranCases`; OFF → `-32001`, tool **vẫn published** sau 5 lần.
5. Structure tests; `_seeds.json`; `tools/list` = 24.
6. `reports/phase-03-seeds.md`.

## Todo List
- [x] Chữ ký OAPI qua reflection trên DLL → `tool.json.notes` (thay tra CHM)
- [x] 8 R + 3 W + 1 D
- [x] `SeedLibraryCompileTests` (25, `Assert.SkipWhen` không ETABS) + `SeedLibraryStructureTests` (38)
- [x] Registry live 12 seed ×3 (`-Phase seeds`: 20 + 9 mỗi lần, 2026-09-17)
- [x] Report `reports/phase-03-seeds.md`

## Success Criteria
- [x] `dotnet test HPEtabs/HPEtabs.Mcp.Server.Tests` → máy dev **81** pass, 0 skip (17 + 25 compile/tier + 39 structure); máy không ETABS: 25 SKIP "ETABS 22 not installed" (thiết kế `Assert.SkipWhen`, chưa chạy trên máy như vậy).
- [x] `tools/list` registry cách ly → **24** tool; `tools-library\` 12 thư mục / 6 category; `_seeds.json`.
- [x] `run-live-verify.ps1 -Phase seeds` → `seeds` 20/20 + `seedsdestructive` 9/9, ×3 (R ×8 `run_tool`; W ×3 `test_tool realRun=true` + `run_tool` + snapshot; D1 `test_tool` → preview, ON `run_analysis` 14 s + reactions/forces/modal, OFF `-32001` ×5 vẫn published).
- [x] `propose_tool` RunAnalysis → "is destructive … cannot be stored as a tool"; `none` + SetSection → "declared transaction: none, but … writes".
- [x] Structure grep: cấm + `RunAnalysis|DeleteResults|Save|Delete*` chỉ D1 (`Only_the_destructive_seed_names_destructive_members_and_no_seed_takes_a_path`).

## Risk Assessment
- Chữ ký `ref`/`out` mảng (researcher-01 #20–#21) đoán sai → bước 1 + compile-check trên máy dev.
- R6/R7 cần kết quả → thứ tự harness (D1 trước, checkbox ON tạm).
- `test_tool realRun=true` ghi model bỏ đi 3 lần → snapshot `prerun` 3 file; user khôi phục từ `presave` nếu cần.
- Mảng lớn → `limit` + cap 200.

## Security Considerations
- D1 duy nhất gọi member D; `tags:["destructive"]` + 600 s; audit `started`; không path-taking member trong 12 seed.

## Next Steps
- Phase 4 harness `--phase full` + registry loop MISS/quarantine.
