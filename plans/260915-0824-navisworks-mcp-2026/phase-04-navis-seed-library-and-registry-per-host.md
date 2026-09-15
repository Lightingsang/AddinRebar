---
phase: 4
title: "Seed library Navisworks (12 seed: 8 read-only, 3 ghi nhẹ, 1 heavy) + compile-check net48 + registry live"
status: pending
priority: P2
effort: "10h"
dependencies: [0, 2, 3]
---

# Phase 4: 12 seed Navisworks + compile-check net48 (ADR-03 §3) + registry live

## Context Links
- [ADR-02 §3](adr/adr-02-navis-transaction-dryrun-and-writable-surface.md) (W1/W1?/W2/W3) · [ADR-03 §3](adr/adr-03-navisworks-api-reference-and-test-without-navisworks.md) (`SeedLibraryTests` trong `HPNavis.McpBridge.Tests` net48) · [ADR-04 §3–4](adr/adr-04-navis-main-thread-busy-heavy-ops-guard-globals.md) (heavy seed-only, `tags:["heavy"]`, `test_tool` từ chối dryRun heavy)
- Bằng chứng API: [evidence §E8, §E8-bis](research/evidence-on-machine-2026-09-15.md) — **mọi member dưới đây đã grep trong `Api.xml`/reflection; chữ ký đúng như ghi**
- Mẫu: `HPAutoCad/HPAutoCad.Mcp.Server/Registry/SeedLibrary/Layer/list_layers/{tool.json,code.cs,examples.json}`; `McpShared/HPRebar.Mcp.Server.Core/Registry/{SeedInstaller.cs, ToolValidator.cs, ToolLibraryStore.cs}`

## Overview
**12 seed** đúng khung "8–12, chia read-only vs ghi": 8 read-only (truy vấn/phân tích/báo cáo), 3 ghi nhẹ W1, 1 heavy W2 (`tags:["heavy"]`, chỉ chạy khi user bật). Không bê seed AutoCAD. Mỗi seed = `tool.json + code.cs + examples.json`, body kết thúc `return`, mm ở biên, `args.X("key", default)` cho mọi input, compile cho **runtime net48** trong `HPNavis.McpBridge.Tests`.

## Requirements
- Functional — bảng seed (chữ ký API theo E8/E8-bis):

| # | Seed | Cat. | tx | API chính | Input (`args`) → Output |
|---|---|---|---|---|---|
| R1 | `get_model_info` | Model | none | `doc.Title/FileName/Units/IsModified/IsClear`, `doc.Models[i].FileName/SourceFileName/Units/Guid`, `Models.RootItems.Count` | → tổng quan + `models[]` + `documentUnits` + `rootItemCount` |
| R2 | `get_selected_item_properties` | Selection | none | `doc.CurrentSelection.SelectedItems`, `ModelItem.PropertyCategories`, `PropertyCategory.Properties`, `DataProperty.Value : VariantData` (`IsDisplayString/IsDouble/…`, `ToDisplayString()`; độ dài → mm qua `units`) | `maxItems=20` → props theo category |
| R3 | `find_items_by_property` | Search | none | `new Search{Locations=SearchLocations.DescendantsAndSelf, PruneBelowMatch=true}`; `SearchCondition.HasPropertyByDisplayName(cat,prop)` + `.EqualValue(VariantData.FromDisplayString(v))` / `.DisplayStringContains(v)` / `.DisplayStringWildcard(v)` / `.CompareWith(SearchConditionComparison.GreaterThan|LessThan, VariantData.FromDouble(d))`; `search.FindAll(doc, false)` → `ModelItemCollection` (cắt `maxResults`) | `category, property, op=equals|contains|wildcard|gt|lt, value, maxResults=200` → `items[]{name,class,path,guid,bboxMm}`. **Không** có tuỳ chọn `select` (đó là ghi) |
| R4 | `list_selection_sets` | Selection | none | `doc.SelectionSets.Value` (`SavedItemCollection`, đệ quy `GroupItem.Children`), `SelectionSet.HasExplicitModelItems/HasSearch`, `SavedItem.DisplayName/Guid` | → cây phẳng `{name, kind, guid, path}` |
| R5 | `list_viewpoints` | Viewpoint | none | `doc.SavedViewpoints.Value`, `SavedViewpoint.Viewpoint.Position` (mm), `.Comments.Count` | → `viewpoints[]` |
| R6 | `get_clash_results` | Clash | none | `doc.GetClash().TestsData.Tests` → `ClashTest.DisplayName/Status/LastRun/Children` → `ClashResult.Status/Distance/Item1/Item2/Center` (mm); `app.HasClashModule=false` → lỗi có nghĩa | `testName?, maxResults=100` → `perTest{counts by status, results[]}` |
| R7 | `get_timeliner_tasks` | Timeliner | none | `doc.GetTimeliner().Tasks` (đệ quy `Children`), `TimelinerTask.DisplayName/PlannedStartDate/PlannedEndDate/ActualStartDate/ActualEndDate/TaskStatus/Selection.DisplayString` | → `tasks[]` phẳng + `level`; rỗng là hợp lệ |
| R8 | `summarize_by_category` | Report | none | `Search` không điều kiện (hoặc `HasCategoryByDisplayName`) → group by `ClassDisplayName` **hoặc** giá trị property `groupBy` (`PropertyCategories.FindPropertyByDisplayName(cat,prop)?.Value.ToDisplayString()`) | `groupBy="Item.Type", maxGroups=200` → `{key,count}[]` |
| W1 | `create_selection_set_from_search` | Selection | auto | `Search` như R3 → `new SelectionSet(search){DisplayName}` (ctor `SelectionSet(Search)` — E8-bis) → `doc.SelectionSets.AddCopy(set)` | `name, category, property, op, value` → `guid, estimatedCount` (`FindAll` cắt 1 000) |
| W2 | `create_viewpoint` | Viewpoint | auto | `doc.CurrentViewpoint.ToViewpoint()` (hoặc `Viewpoint` từ args position/lookAt mm) → `new SavedViewpoint(vp){DisplayName}` → `SavedViewpoints.AddCopy`; `comment?` → `SavedViewpoints.AddComment(item, new Comment(text, CommentStatus.New, author))` | → `guid` |
| W3 | `override_color_by_search` | Selection | auto | `Search` → `ModelItemCollection` → `doc.Models.OverridePermanentColor(items, Color.FromByteRGB(r,g,b))`; `transparency?` → `OverridePermanentTransparency`; `reset=true` → `ResetPermanentMaterials(items)` | → `count` |
| H1 | `create_and_run_clash_test` | Clash | auto, **`tags:["heavy"]`** | `new ClashTest{DisplayName, TestType=ClashTestType.Hard, Tolerance=units.ToDrawing(toleranceMm)}`; `SelectionA.Selection.CopyFrom(searchA.FindAll(doc,false))` (và B) — `ClashSelection.Selection` kiểu **[chưa xác minh — reflection chỉ cho tên member; chốt ở bước 1]**; `TestsData.TestsAddCopy(test)`; `TestsData.TestsRunTest(test)` | `name, toleranceMm, a:{category,property,op,value}, b:{…}` → `{status counts, elapsedMs}`; heavy OFF → `HEAVY` |

  `get_model_tree` bỏ (R1 `rootItemCount` + R3 đủ); `append_model_file` bỏ (append ad-hoc khi heavy ON; user không yêu cầu seed); `export_clash_report` không làm (không API public — R6 trả JSON đủ để viết báo cáo).
- Non-functional: `code.cs` < 80 dòng; `tool.json` `host:"navis"`, `hostVersions:["2026"]`, `destructive` đúng (R* false; W*/H1 true), H1 `timeoutSeconds: 600`; description nêu mm; `examples.json` ≥ 2; `_seeds.json` checksum như AutoCAD.

## Architecture
- Nhúng `Registry/SeedLibrary/<Category>/<name>/` như AutoCAD; `SeedInstaller(hostAssembly)` cài vào `%AppData%\HPNavis\McpServer\tools-library\`. `ToolLibraryStore.TryRead` không validate → seed 600 s nạp được; `ToolManager.RunAsync` clamp theo `profile.MaxTimeoutSeconds` (phase 0 #12).
- Heavy marker = `tags` chứa `"heavy"` (`ToolRecord.Tags`, sống qua rewrite). `test_tool` (dryRun mặc định) trên H1 → bridge từ chối trước khi chạy (ADR-02 §1) → `test_tool` trả lỗi rõ; `run_tool` heavy ON là đường kiểm. `propose_tool` heavy: **ngoài MVP** (analyze heavy OFF → validator từ chối).
- **`SeedLibraryTests` trong `HPNavis.McpBridge.Tests` (net48):** đọc `Registry/SeedLibrary/**` từ source tree (relative path); wrapper = imports `HostScriptContracts.NavisImports` + globals **`NavisScriptGlobals` thật** (ProjectReference bridge) + references `typeof(object).Assembly`, `typeof(Enumerable).Assembly`, `typeof(List<>).Assembly`, `typeof(Autodesk.Navisworks.Api.Document).Assembly`, `typeof(Autodesk.Navisworks.Api.Clash.ClashTest).Assembly`, `typeof(Autodesk.Navisworks.Api.Timeliner.TimelinerTask).Assembly`, `typeof(ScriptArgs).Assembly` (net48 tự nhiên); guard `GuardProfile.Navis` + `NavisHeavyGate` heavy OFF cho R*/W*, heavy ON cho H1, **và** assert H1 bị `HEAVY` khi OFF; assert không seed nào chứa `Descendants` mà thiếu `Prune`/`Take`. Mỗi seed 1 Theory case + 1 structural.
- `HPNavis.Mcp.Server.Tests` (net10) giữ `SeedLibrary_JsonAndStructure_AreValid` (không cần API): schema, examples, checksum, category ∈ profile, `tags∋heavy ⇔ code có W2 member` (regex), `timeoutSeconds ≤ 600`.

## Related Code Files
- Create: `HPNavis/HPNavis.Mcp.Server/Registry/SeedLibrary/{Model,Selection,Search,Viewpoint,Clash,Timeliner,Report}/<name>/{tool.json,code.cs,examples.json}` ×12; `HPNavis/HPNavis.McpBridge.Tests/SeedLibraryTests.cs`; `HPNavis/HPNavis.Mcp.Server.Tests/SeedLibraryStructureTests.cs`.
- Modify: `HPNavis.Mcp.Server.csproj` (EmbeddedResource). **Không** đổi `ToolRecord`, `RegistryJson`, `AnalyzeRequest`.

## Implementation Steps
1. Chốt chữ ký còn `[chưa xác minh]` (`ClashSelection.Selection` kiểu; `Comment` ctor; `SearchConditionComparison` tên enum) bằng reflection PowerShell như E8 — ghi vào E8-bis trước khi viết seed.
2. Viết R1–R8 + W1–W3; chạy live qua `execute_navis_code` (bridge phase 2) trên `gatehouse_pub.nwd` (R1–R5, R8, W1–W3) và `Getting Started` (R6/R7 sau H1 + 1 task tạo bằng script `auto`).
3. Viết H1; kiểm `HEAVY` khi OFF, chạy thật khi ON (đo thời gian, RAM).
4. `SeedLibraryTests` (net48) + `SeedLibraryStructureTests` (net10): máy dev 0 skip.
5. Registry live: `search_tools "clash"` → R6, H1; `run_tool get_model_info`; `test_tool create_viewpoint` (dryRun) → `RolledBack=true`; `test_tool create_and_run_clash_test` → từ chối rõ; `run_tool create_and_run_clash_test` heavy ON → ok, 600 s không bị cắt 120; `_seeds.json` upgrade (đổi 1 seed → reinstall) như AutoCAD.
6. `reports/phase-04-seeds.md`.

## Todo List
- [ ] Chốt chữ ký `[chưa xác minh]` → E8-bis
- [ ] R1–R8, W1–W3 + examples + live
- [ ] H1 + heavy gate live
- [ ] `SeedLibraryTests` net48 + structure test net10
- [ ] Registry live (search/run/test/upgrade/600 s)
- [ ] Report

## Success Criteria
- [ ] `dotnet test HPNavis/HPNavis.McpBridge.Tests` (máy dev): 15 (phase 2) + 12 compile + 12 structural + 1 heavy-gate = **≥ 40 pass, 0 skip**; `dotnet test HPNavis/HPNavis.Mcp.Server.Tests`: ≥ 10 + 1 pass.
- [ ] `tools/list` Navis = 12 + 12 = **24**; Revit 34 / AutoCAD 24 không đổi (snapshot phase 0, exe rebuild).
- [ ] Mỗi R*/W* seed có `runId` live trong report (`run_tool` hoặc `test_tool` dryRun trên gatehouse/Getting Started); H1: `test_tool` → từ chối có thông điệp; `run_tool` heavy ON → `status counts` + `elapsedMs` (ghi); heavy OFF → `HEAVY`.
- [ ] `grep -L '"host": "navis"' …/SeedLibrary/*/*/tool.json` rỗng; `grep -l '"heavy"' …/tool.json` = đúng 1 (H1); `grep -rn 'Descendants' …/code.cs` chỉ ở seed có `PruneBelowMatch`/`Take(`.
- [ ] Không có `Contains(` trên `SearchCondition` (tên đúng `DisplayStringContains`), không có `EqualValue("` với string (phải `VariantData`).

## Risk Assessment
- `VariantData` nhiều kiểu → helper trong R2 dùng `IsDisplayString/IsDouble/IsInt32/IsBoolean/IsDateTime` + `ToDisplayString()`; kiểm live.
- `TestsRunTest` đồng bộ? (S-08) — nếu async → H1 chỉ `TestsAddCopy` + hướng dẫn user Run; mô tả sửa.
- `Getting Started` không có task → R7 rỗng hợp lệ; harness tạo 1 task bằng script để có dữ liệu.
- H1 vượt 600 s trên sample lớn → harness dùng 2 category nhỏ; ghi thời gian.
