# Phase 4 — Registry theo profile + 12 seed AutoCAD, verified with AutoCAD 2026 (2026-09-14)

**Cách chạy:** `dotnet test` ba folder (McpShared 92, HPRebar 106, HPAutoCad 46) → publish exe → `pwsh HPAutoCad/tools/harness/run-server-smoke.ps1` (nay gọi cả 12 seed bằng tên qua stdio với AutoCAD 2026 sống).

## Registry engine theo profile (`McpShared`)

| Việc | Kết quả |
|---|---|
| `ToolValidator.Validate(…, IHostProfile)` | categories + reserved names (8 registry + `profile.CoreToolNames`) + `host` phải bằng `profile.HostId`; overload 4 tham số = Revit (test cũ không đổi). 3 test mới `RegistryProfileTests` |
| `ToolLifecycleService.ProposeAsync` | category theo `profile.Categories`; validate với profile; `Host`/`HostVersions` đã có từ phase 0 |
| `DynamicToolRegistrar` | "Registry tools run stored, reviewed C# inside {DisplayName} — prefer them over {ExecuteToolName}" |
| `ToolifyPrompts` | persona/categories/contract từ profile (`ScriptContractSummary`) |
| `RegistryCli stats` | in `host: autocad (AutoCAD)` + root |
| Meta tool `[Description]` (attribute tĩnh) | wording host-neutral: "the host application", "the execute tool (execute_revit_code / execute_autocad_code)", `inspect_type` ví dụ cả hai host, `search_tools.category` liệt kê cả hai bộ |

**Ảnh hưởng Revit (gọi tên rõ):** `tools/list` Revit vẫn 34 tool, **tên và schema (type/required/properties) y hệt phase 0**; đổi **chữ** description của 7 engine tool (`get_run`, `inspect_type`, `search_tools`, `cancel_execution`, `run_tool`, `propose_tool`, `test_tool`) + title `inspect_type` ("Inspect a host API type") + description của 4 tham số (`analyze`, `typeName`, `category`, `code`/`sourceRunId`). `reports/phase-04-tools-list-revit.json` vs `phase-00-tools-list-after.json`. `%AppData%\HPRebar\McpServer\tools-library` không có file nào mới hơn phase 0.

## 12 seed AutoCAD (`HPAutoCad/HPAutoCad.Mcp.Server/Registry/SeedLibrary/`)

| # | Seed | Category | tx | Live (Drawing1 từ acad.dwt, Inches) |
|---|---|---|---|---|
| 1 | `list_layers` | Layer | none | ✅ layer 0, `entityCount` đúng |
| 2 | `list_block_definitions` | Block | none | ✅ 0 (bản vẽ trống) |
| 3 | `get_entities` | Data | none | ✅ LINE 1234,5 mm, bbox mm |
| 4 | `list_layouts` | Layout | none | ✅ Model + Layout1/2, tabOrder, isCurrent |
| 5 | `get_drawing_info` | Data | none | ✅ insunits Inches, mmPerUnit 25,4, extents, counts |
| 6 | `get_selected_entities` | Generic | none | ✅ `[]` khi không chọn |
| 7 | `draw_polyline` | Drawing | auto | ✅ 4 đỉnh, 15 000 mm, trên layer mới |
| 8 | `draw_circle` | Drawing | auto | ✅ dryRun `rolledBack`, area 785 398 mm² |
| 9 | `add_text` | Annotation | auto | ✅ MText width 2 000 mm |
| 10 | `create_layer` | Layer | auto | ✅ `MCP-TEST` màu 1, lineweight 50, `changed.added=1` |
| 11 | `insert_block` | Block | auto | ✅ lỗi rõ "Block 'NO-SUCH-BLOCK' is not defined…" (bản vẽ không có block; live với block thật → phase 5) |
| 12 | `add_linear_dimension` | Annotation | auto | ✅ RotatedDimension 3 000 mm |

- Điểm/toạ độ dạng object `{x, y}` (mm) thay vì `[x, y]` trong plan — `ScriptArgs.List` trả `ScriptArgs` mỗi phần tử, `.Double("x")` đọc thẳng, không cần `Raw`.
- Layer optional: seed **không** tự tạo layer (lỗi rõ "run create_layer first") — dự đoán được; prompt `autocad_modify_template` mới tự tạo.
- Không seed nào `StartTransaction` (guard cấm); `SeedLibraryTests` khẳng định `UsesTransaction == false`.

## Compile-check (`HPAutoCad.Mcp.Server.Tests/SeedLibraryTests.cs`, 46 test, 0 skip)
Wrapper = `HostScriptContracts.AutocadImports` + field đúng `AutocadScriptGlobals`; refs `AcMgd/AcCoreMgd/AcDbMgd` 25.1.0 từ NuGet cache (metadata only) + `ScriptArgs`; 12/12 compile lần đầu, guard AutoCAD sạch, args ↔ schema khớp; negative CS1061 + `ed.GetPoint` bị guard.

## Smoke stdio (run 4): 20/21 → 21/21 sau khi nới query search
`tools/list` = **24**; `search_tools "list layers"` → `list_layers, create_layer, list_layouts, …`; 12 seed gọi bằng tên như MCP tool thật (dyn tool có `dryRun`); `%AppData%\HPAutoCad\McpServer\tools-library` 12 folder + `_seeds.json`; `registry stats` in host. Lưu ý ranking FTS: query một chữ "layer" xếp `list_layers` ngoài top 5 (description khác nhắc "layer" nhiều hơn) → follow-up: boost name match (engine, cả hai host).

## Follow-up
- `get_run`/DB cột `revit_version` giữ tên (schema); alias `hostVersion` trong output nếu phase 5 thấy AI nhầm.
- FTS name boost (trên).
