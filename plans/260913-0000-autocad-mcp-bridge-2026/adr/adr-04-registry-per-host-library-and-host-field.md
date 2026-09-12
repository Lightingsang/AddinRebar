# ADR-04 — Registry dùng chung engine, tách dữ liệu theo host: library + DB riêng, trường `host` trong `tool.json`, seed theo host

**Ngày:** 2026-09-13 · **Status:** Proposed · **Owner:** HPRebar
**Kế thừa:** [Revit ADR-05 tool = data](../../260912-1521-dynamic-revit-mcp-server-2026/adr/adr-05-typed-tools-are-script-templates.md) · [Revit ADR-06 registry & publish gate](../../260912-1521-dynamic-revit-mcp-server-2026/adr/adr-06-tool-registry-and-publish-gate.md) · [ADR-01 host profile](adr-01-reuse-mcp-server-host-profile.md)

## Context

- Yêu cầu: **một engine registry** (`Registry/*`, `Tools/Registry/*`, `Prompts/ToolifyPrompts.cs`, `RegistryCli`) cho cả hai host; **library tách theo host**; `tool.json` có `host: revit | autocad`; `search_tools` chỉ trả tool của host đang kết nối; `DynamicToolRegistrar` chỉ đăng ký tool của host đó; **22 tool Revit đã cài không bị ảnh hưởng**.
- Hiện trạng (đọc 2026-09-13):
  - `RegistryOptions.LibraryPath` mặc định `%AppData%\HPRebar\McpServer\tools-library`, `DbPath` `…\registry.db` (`Registry/Model/RegistryOptions.cs:18-22`). `ToolLibraryStore.ReadAll` duyệt `Root/<Category>/<name>/` và bỏ qua folder bắt đầu `_` (`ToolLibraryStore.cs:56-70`).
  - `ToolRecord` có `RevitVersions` (`ToolRecord.cs:53`); `RunRecord.RevitVersion`; cột `runs.revit_version` (`ToolRegistryDb.cs:47`). Không có `host`.
  - `SeedInstaller.LoadSeeds` parse `SeedLibrary/<Category>/<name>/<file>` với `parts.Length == 3` (`SeedInstaller.cs:44-45`); csproj `EmbeddedResource LogicalName="SeedLibrary/%(RecursiveDir)…"`. `_seeds.json` lưu checksum theo tên tool.
  - `ToolValidator.Categories` = 7 category Revit; `ReservedNames` = 4 core tool Revit + 8 registry tool (`ToolValidator.cs:22-28`).
  - `ToolManager.RunAsync` gửi `ExecuteRequest(record.Code, record.Transaction, dryRun, timeout, name, args)` qua `IRevitBridgeClient` — host-neutral (`ToolManager.cs:151-170`).
  - Nhiều tiến trình server có thể dùng chung một library + DB (WAL) — hôm nay đã thế với nhiều phiên Claude.
- Code tool phụ thuộc host API: `code.cs` Revit không compile trong AutoCAD và ngược lại. Trộn chung một `tools/list` là sai về mặt ngữ nghĩa lẫn token (NotebookLM Q13).

## Ma trận

| Tiêu chí | **(a) Library + DB riêng theo host** (profile default `tools-library` / `tools-library-autocad`, `registry.db` / `registry-autocad.db`) | **(b) Một root, subfolder `<host>/`** (`tools-library/revit/…`, `tools-library/autocad/…`), một DB có cột `host` | **(c) Một library phẳng, chỉ lọc bằng trường `host`** |
|---|---|---|---|
| 22 tool Revit đã cài không đổi chỗ | ✅ path cũ giữ nguyên | ❌ phải migrate `tools-library/<Category>` → `tools-library/revit/<Category>` (+ `_review/`, `_seeds.json`, cột `folder`) | ✅ |
| Sửa `ToolLibraryStore`/`ToolRegistryDb` | 0 (chỉ default path theo profile) | `ReadAll` thêm 1 cấp; DB migration thêm cột `host`, index, lọc trong `Search/AllStats/RecentRuns` | lọc trong 6 query + `RemoveToolsNotIn` theo host |
| `runId` toàn cục | riêng theo host (không nhầm run Revit/AutoCAD) | chung — `get_run 17` của host khác trả code không chạy được | chung |
| Cross-contamination (file Revit rơi vào library AutoCAD) | chỉ khi user copy tay → `host` field + validator bắt | không | không |
| Watcher, atomic write, CLI | không đổi | không đổi | không đổi |
| Chia sẻ qua git một checkout cho cả hai host | 2 folder cạnh nhau (`Registry:LibraryPath` mỗi entry `.mcp.json`) | 1 folder | 1 folder |

**Chốt: (a)** — zero migration, zero thay đổi query, `runs` tách sạch; cộng **trường `host` bắt buộc** làm hàng rào thứ hai.

## Decision

1. **Đường dẫn theo profile** (`IHostProfile.LibraryPathDefault`, `DbPathDefault`): Revit giữ **nguyên** `tools-library` + `registry.db`; AutoCAD `tools-library-autocad` + `registry-autocad.db` (cùng root `%AppData%\HPRebar\McpServer\`). `Registry:LibraryPath`/`DbPath` trong config vẫn override được như cũ (cho git checkout).
2. **`tool.json` thêm `host`** (`ToolRecord.Host`, mặc định `"revit"` khi thiếu → 22 tool cũ đọc được không sửa file). `ToolLibraryStore.ReadAll` **không** lọc (giữ đơn giản); `ToolManager.LoadAllAsync` **bỏ qua + log warning** record có `Host != profile.HostId` (không upsert DB, không đăng ký). `ToolValidator` thêm error "tool.host must be `<profile.HostId>`" (proposal do server điền, AI không truyền `host`). `_review/<name>.md` in thêm dòng `Host:`.
3. **`hostVersions` thay `revitVersions` cho record mới**: thêm `ToolRecord.HostVersions`; `RevitVersions` giữ để đọc file cũ (`[JsonIgnore]` khi ghi nếu rỗng). `ToolLifecycleService` điền `HostVersions` từ `bridge.LastStatus.RevitVersion` (field wire giữ tên — xem ADR-01 #5). `RunRecord.RevitVersion` + cột `revit_version` **giữ nguyên tên**, ngữ nghĩa = host version; ghi vào `docs/` khi phase 5, đổi tên khi có phase dọn dẹp riêng (không migration DB trong plan này).
4. **Seed theo host, layout đối xứng**: `Registry/SeedLibrary/<Host>/<Category>/<name>/{tool.json,code.cs,examples.json}` — 21 seed Revit **di chuyển** vào `SeedLibrary/Revit/` (git mv, nội dung không đổi → checksum `_seeds.json` không đổi → không ghi đè library user), 12 seed AutoCAD vào `SeedLibrary/Autocad/`. `SeedInstaller.LoadSeeds(assembly, hostId)` parse `parts.Length == 4` và lọc `parts[0] == hostId`; `SeedLibraryTests` chạy theo host với reference assembly tương ứng (`RevitAPI.dll` / `AcDbMgd.dll`+`AcMgd.dll`+`AcCoreMgd.dll` từ NuGet cache).
5. **Categories theo profile**: Revit giữ 7 category hiện có; AutoCAD: `Drawing`, `Layer`, `Block`, `Annotation`, `Layout`, `Data`, `Generic`. `ToolValidator.Validate(candidate, analysis, existing, newVersion, profile)`.
6. **Reserved names theo profile**: `profile.CoreToolNames ∪ RegistryToolNames` (8 tên chung là hằng trong `ToolValidator`).
7. **Text nêu host trong registry tool/prompt dùng chung** → trung tính hoặc lấy từ `IHostProfile` qua DI (prompt `toolify_run` nhận `IHostProfile` tham số DI — SDK cho phép, NotebookLM Q08 [23]); `RegistryToolFunction` description dùng `profile.ExecuteToolName` + `profile.DisplayName`.
8. **`search_tools`/`get_tool`/`run_tool`/`get_run`/`propose_tool`/`test_tool`/`publish_tool`/`manage_tool`/CLI**: **không đổi logic** — mỗi tiến trình server chỉ thấy library của host mình; filter host là hệ quả của (1)+(2), không phải code mới trong từng tool.
9. **CLI**: `HPRebar.Mcp.Server.exe registry <cmd>` đọc cùng env `HPREBAR_MCP_Host`; thêm cờ `--host autocad` để tiện gõ tay (ưu tiên cờ > env > default `revit`). `registry stats` in `host:` ở dòng đầu.

## Vòng lặp registry ánh xạ 1:1 (không viết lại bước nào)

| Bước (đã verified Revit) | Mã tái dùng nguyên | Điểm chạm host |
|---|---|---|
| `execute_autocad_code` trả `runId` + `hint` | `Services/ExecuteCodeService` (tách từ `ExecuteRevitCodeTool`), `ToolManager.RecordAdhoc`, `LooksReusable` | tên tool + description (host tool class) |
| `get_run` (code + literals) | `Tools/Registry/RunHistoryTools`, `ToolLifecycleService.AnalyzeAsync` → `autocad.analyze` | `profile.Method("analyze")` |
| prompt `toolify_run` | `Prompts/ToolifyPrompts` | persona text từ `IHostProfile` |
| `propose_tool` → `ToolValidator` + analyze | `Tools/Registry/ToolLifecycleTools`, `ToolValidator` | categories/reserved/host từ profile |
| `test_tool` (examples, dryRun) | `ToolLifecycleService.TestAsync` → `ToolManager.RunAsync` | — |
| `publish_tool` → `pending_approval` + `_review/<name>.md` | `ToolLifecycleService.PublishAsync` | review file ghi `Host:` |
| `registry approve <name> --by <who>` | `Registry/RegistryCli` | `--host` |
| `tools/list_changed` | `ToolLibraryStore` watcher → `ToolManager.LoadAllAsync` → `DynamicToolRegistrar.Sync` | — |
| `search_tools` → gọi theo tên | `ToolRegistryQueryTools`, `RegistryToolFunction` | description text |
| `runs`, stability, quarantine ≥ 5 run & > 40 % | `ToolRegistryDb`, `StabilityScorer`, `ToolManager.Record` | — |

## Consequences

- Hai library, hai DB, hai `_seeds.json`; `registry export/import` làm việc trên host hiện tại. Share qua git: hai folder hoặc hai checkout.
- Phase 4 phải chạy lại **toàn bộ** `ToolRegistryTests`/`ToolLifecycleTests`/`SeedLibraryTests` cho cả 2 host (theory theo profile) — gate không cho hồi quy Revit.
- Một Claude session với cả hai entry `.mcp.json` → 2 server process, 2 registry, tool trùng tên giữa host (`get_selected_elements` Revit vs `get_selected_entities` AutoCAD) không đụng nhau vì khác server; host AI hiển thị prefix server (`hprebar-revit:` / `hprebar-autocad:`).
