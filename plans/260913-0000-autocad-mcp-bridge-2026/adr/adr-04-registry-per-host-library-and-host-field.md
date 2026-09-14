# ADR-04 — Registry: một engine trong `McpShared/`, dữ liệu tách theo exe (root `%AppData%\<Product>\McpServer\`), seed nhúng trong từng exe, `host` là metadata tự mô tả

**Ngày:** 2026-09-13 · **Revised 2026-09-14** theo [ADR-06](adr-06-one-mcp-one-folder.md) (hai exe riêng → "tách theo host" trở thành "tách theo exe"; bản 2026-09-13 với `tools-library-autocad` cạnh `tools-library` và `SeedLibrary/<Host>/` **bị thay**) · **Status:** Proposed · **Owner:** HPRebar
**Kế thừa:** [Revit ADR-05 tool = data](../../260912-1521-dynamic-revit-mcp-server-2026/adr/adr-05-typed-tools-are-script-templates.md) · [Revit ADR-06 registry & publish gate](../../260912-1521-dynamic-revit-mcp-server-2026/adr/adr-06-tool-registry-and-publish-gate.md)

## Context

- Yêu cầu không đổi: **một engine registry** (`Registry/*`, `Tools/Registry/*`, `Prompts/ToolifyPrompts`, `RegistryCli`) cho cả hai MCP; `search_tools`/registrar chỉ thấy tool của host mình; 22 tool Revit đã cài không bị ảnh hưởng; seed AutoCAD compile-check trong xUnit.
- ADR-06: engine sống ở `McpShared/HPRebar.Mcp.Server.Core`; mỗi MCP có **exe riêng** (`HPRebar.Mcp.Server`, `HPAutoCad.Mcp.Server`). Không còn "một exe chọn host bằng env" → không cần lọc host trong `search_tools`/registrar bằng mã: mỗi tiến trình chỉ nhìn thấy dữ liệu của mình.
- Hiện trạng (đọc 2026-09-13/14): `RegistryOptions.DefaultRoot` = `%AppData%\HPRebar\McpServer` (`Registry/Model/RegistryOptions.cs:15`); `ToolLibraryStore.ReadAll` duyệt `Root/<Category>/<name>/` (`ToolLibraryStore.cs:56-70`); `SeedInstaller.LoadSeeds(assembly)` đọc resource `SeedLibrary/<Category>/<name>/<file>` của **assembly được truyền vào** (`SeedInstaller.cs:26-50`, mặc định `typeof(SeedInstaller).Assembly` — sau khi tách, mặc định này phải là **assembly exe** chứ không phải `Server.Core`); `ToolValidator.Categories`/`ReservedNames` là hằng Revit (`ToolValidator.cs:22-28`); `ToolRecord.RevitVersions`, `RunRecord.RevitVersion`, cột `runs.revit_version`; `ToolManager.RunAsync` host-neutral.

## Ma trận (revised)

| Tiêu chí | **(a) Root riêng theo exe** — `%AppData%\HPRebar\McpServer\` (không đổi) / `%AppData%\HPAutoCad\McpServer\` — library `tools-library`, DB `registry.db` cùng tên trong root riêng | **(b) Một root chung `%AppData%\HP\McpServer\<host>\`** | **(c) Root chung, một DB, cột `host`** |
|---|---|---|---|
| 22 tool Revit đã cài không đổi chỗ | ✅ | ❌ migrate | ❌ migrate |
| Sửa engine | chỉ `DefaultRoot` lấy từ `IHostProfile.ProductFolder` | thêm 1 cấp | migration DB + lọc 6 query |
| Nhầm lẫn `runId` giữa host | không thể (DB riêng) | không thể | có thể |
| Khớp "mỗi MCP một folder" (ADR-06) | ✅ mỗi exe một root, đối xứng với folder mã nguồn | 🟡 | 🟡 |

**Chốt: (a).**

## Decision

1. **Root theo product:** `IHostProfile.ProductFolder` (`"HPRebar"` / `"HPAutoCad"`) → `RegistryOptions` mặc định `%AppData%\<ProductFolder>\McpServer\tools-library` và `…\registry.db` (bind qua `PostConfigure<RegistryOptions>` trong `McpServerHost.CreateBuilder`); config `Registry:LibraryPath`/`DbPath` vẫn override được (git checkout). Revit: giá trị **y hệt** hôm nay.
2. **Seed nhúng trong từng exe:** `Registry/SeedLibrary/<Category>/<name>/…` — prefix `SeedLibrary/` và parse 3 cấp **không đổi**; `SeedInstaller.Install(store, logger, hostAssembly)` nhận assembly exe (`RegistryStartup` lấy từ `IHostProfile.HostAssembly`). 21 seed Revit **không di chuyển**; 12 seed AutoCAD vào `HPAutoCad/HPAutoCad.Mcp.Server/Registry/SeedLibrary/**` với cùng csproj pattern `EmbeddedResource LogicalName="SeedLibrary/%(RecursiveDir)…"`.
3. **`host` trong `tool.json` — giữ, làm metadata tự mô tả (không phải cơ chế lọc):** `ToolRecord.Host` (ghi khi tạo record mới = `profile.HostId`; thiếu → coi là host của exe đang đọc, để 22 file cũ không cần sửa). `ToolManager.LoadAllAsync` **bỏ qua + warning** record có `Host` khác `profile.HostId` (hàng rào khi user copy tay folder tool Revit vào library AutoCAD); `ToolValidator` từ chối proposal có `host` sai (server tự điền, AI không truyền). `_review/<name>.md` in `Host:`. Chi phí: ~10 dòng.
4. **`hostVersions`:** thêm `ToolRecord.HostVersions`; `RevitVersions` giữ để đọc file cũ. `RunRecord.RevitVersion` + cột `revit_version` giữ tên (ngữ nghĩa = host version; đổi tên khi rename `HPMcp.*`).
5. **Categories/Reserved names theo profile:** `ToolValidator.Validate(candidate, analysis, existing, newVersion, profile)`; Revit giữ 7 category hiện có; AutoCAD `Drawing, Layer, Block, Annotation, Layout, Data, Generic`. Reserved = `profile.CoreToolNames ∪ RegistryToolNames`.
6. **Text host trong tool/prompt dùng chung** lấy từ `IHostProfile` (DI): `RegistryToolFunction` description ("inside {DisplayName} — prefer them over {ExecuteToolName}"), `toolify_run` persona (`profile.ScriptContractSummary`), `RevitBridgeClient` thông điệp "not connected".
7. **CLI:** `HPRebar.Mcp.Server.exe registry …` / `HPAutoCad.Mcp.Server.exe registry …` — cùng `RegistryCli` trong `Server.Core`, đọc profile của exe; **không** có `--host`.
8. **Nhiều tiến trình cùng exe** (nhiều phiên Claude) vẫn chia sẻ một root/DB (WAL) như hôm nay; hai exe khác nhau không bao giờ chạm root của nhau.

## Vòng lặp registry ánh xạ 1:1 (không viết lại bước nào)

| Bước (đã verified Revit) | Mã tái dùng nguyên (nay ở `McpShared/HPRebar.Mcp.Server.Core`) | Điểm chạm host |
|---|---|---|
| `execute_autocad_code` trả `runId` + `hint` | `Services/ExecuteCodeService` (tách từ `ExecuteRevitCodeTool`), `ToolManager.RecordAdhoc`, `LooksReusable` | tool class mỏng trong exe AutoCAD |
| `get_run` (code + literals) | `Tools/Registry/RunHistoryTools`, `ToolLifecycleService.AnalyzeAsync` → `profile.Method("analyze")` = `autocad.analyze` | — |
| prompt `toolify_run` | `Prompts/ToolifyPrompts` | persona từ `IHostProfile` |
| `propose_tool` → `ToolValidator` + analyze | `Tools/Registry/ToolLifecycleTools`, `ToolValidator` | categories/reserved/host từ profile |
| `test_tool` (examples, dryRun) | `ToolLifecycleService.TestAsync` → `ToolManager.RunAsync` | — |
| `publish_tool` → `pending_approval` + `_review/<name>.md` | `ToolLifecycleService.PublishAsync` | review file ghi `Host:` |
| `registry approve <name> --by <who>` | `Registry/RegistryCli` (gọi qua exe AutoCAD) | — |
| `tools/list_changed` | `ToolLibraryStore` watcher → `ToolManager.LoadAllAsync` → `DynamicToolRegistrar.Sync` | — |
| `search_tools` → gọi theo tên | `ToolRegistryQueryTools`, `RegistryToolFunction` | description text |
| `runs`, stability, quarantine ≥ 5 run & > 40 % | `ToolRegistryDb`, `StabilityScorer`, `ToolManager.Record` | — |

## Alternatives rejected

- **Bản 2026-09-13** (`tools-library-autocad` cạnh `tools-library`, `SeedLibrary/<Host>/`, `--host`): chỉ cần khi một exe phục vụ nhiều host; ADR-06 bỏ mô hình đó → loại, giảm churn (không git mv 63 file seed Revit).
- **Bỏ hẳn `host` field:** rẻ hơn 10 dòng nhưng mất hàng rào khi copy tay + mất thông tin trong `_review`. Giữ.
- **DB chung có cột `host`:** migration + lọc query; vô nghĩa khi exe đã tách.

## Consequences

- Phase 4 chỉ còn: profile-driven validator/categories/text, `host`/`hostVersions`, `SeedInstaller(hostAssembly)`, root theo product, 12 seed AutoCAD, tests theo profile. Không di chuyển seed Revit.
- Test engine (`McpShared/HPRebar.Mcp.Server.Core.Tests`) dùng `TestHostProfile` + seed giả nhúng trong test assembly; test seed thật của mỗi host nằm trong test project của exe đó (`HPRebar/HPRebar.Mcp.Server.Tests/SeedLibraryTests.cs` giữ nguyên; `HPAutoCad/HPAutoCad.Mcp.Server.Tests/SeedLibraryTests.cs` bản AutoCAD với reference `AcDbMgd/AcMgd/AcCoreMgd`).
- Tool trùng tên giữa hai host (`get_selected_elements` Revit / `get_selected_entities` AutoCAD) không đụng nhau: khác server, host AI hiển thị prefix `hprebar-revit:` / `hprebar-autocad:`.
