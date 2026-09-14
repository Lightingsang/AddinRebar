# ADR-06 — Mỗi MCP một folder top-level, mã chung tách ra folder thứ ba `McpShared/`

**Ngày:** 2026-09-14 · **Status:** Proposed (Claude tự chốt theo ràng buộc user 2026-09-14; supersedes phần "server" của [ADR-01](adr-01-reuse-mcp-server-host-profile.md)) · **Owner:** HPRebar
**Kế thừa:** [Revit ADR-01 topology](../../260912-1521-dynamic-revit-mcp-server-2026/adr/adr-01-two-process-topology.md) · [Revit ADR-06 registry](../../260912-1521-dynamic-revit-mcp-server-2026/adr/adr-06-tool-registry-and-publish-gate.md) · `CLAUDE.md` § Repository Layout ("four unrelated deliverables … do not cross-wire them")

## Context

- **Ràng buộc mới (user, 2026-09-14):** mỗi MCP nằm trong một folder top-level riêng, không chung folder với MCP khác. AutoCAD MCP **không** được nằm trong `HPRebar/`; một exe dùng chung cho hai MCP không thoả.
- ADR-01 (2026-09-13) chốt "một exe `HPRebar.Mcp.Server` + host profile" và đặt bridge AutoCAD trong `HPRebar/` → **mâu thuẫn trực tiếp**; phần đó superseded. Yêu cầu không đổi: registry engine, `toolify_run`, CLI approve, script engine (`ScriptGuard/Compiler/Args/Analyzer`), pipe transport dùng **chung mã** — không viết bản thứ hai.
- Hiện trạng đã đọc (2026-09-14):
  - `HPRebar/HPRebar.slnx` liệt kê project bằng path tương đối trong `HPRebar/` (`HPRebar.Mcp.Contracts/…`, `HPRebar.McpBridge.Core/…`, …); `HPRebar/global.json` pin SDK `10.0.300` + runner MTP; repo root **không** có `global.json`/`Directory.Build.props`/`nuget.config`.
  - `HPRebar/.sourcyroot` (rỗng) = scan root của `Sourcy.DotNet`; `build/Modules/*` chỉ dùng `Solutions.HPRebar`, `Projects.HPRebar`, `Projects.HPRebar_McpBridge`, `Projects.HPRebar_Mcp_Server`, `Projects.Installer`, `Projects.Build` — **không** tham chiếu Contracts/Core (`CompileProjectModule.cs:42`, `CreateBundleModule.cs:30`, `PublishServerModule.cs:28-32`, `CreateInstallerModule.cs:30-31`).
  - `ResolveConfigurationsModule.cs:27`: `context.Git().RootDirectory.FindFile(file => file.Extension == ".slnx")` — lấy **`.slnx` đầu tiên dưới git root** → thêm `.slnx` thứ hai ở folder khác là rủi ro pipeline Revit build nhầm solution.
  - `HPRebar.Mcp.Server.csproj` là `Exe`; registry engine (`Registry/*`, `Tools/Registry/*`, `Services/*`) nằm **trong exe**, `public sealed`, `InternalsVisibleTo` test. Seeds nhúng `SeedLibrary/<Category>/<name>/…`.
  - `HPRebar.Mcp.Server.Tests` (159) test pipe thật + fake executor, guard/compiler/args, registry, seeds — phần lớn là test của **mã engine**, không phải của Revit.
  - Bridge Revit đã deploy (commit `9b83aee`) chỉ phụ thuộc JSON trên pipe `hprebar-mcp-r2026` — không phụ thuộc tên assembly server.

## Ma trận 3 cách chia sẻ mã giữa hai folder

| Tiêu chí | **(a) `HPAutoCad/` `ProjectReference` chéo sang `../HPRebar/…`** (Contracts, McpBridge.Core, và registry qua ref exe `HPRebar.Mcp.Server`) | **(b) Folder thứ ba `McpShared/`** (Contracts, McpBridge.Core, `HPRebar.Mcp.Server.Core` mới = registry + pipe client + meta tools + bootstrap; hai MCP cùng reference) | **(c) NuGet local feed** (pack mã chung từ (a) hoặc (b) thành `.nupkg`, `HPAutoCad/` dùng `PackageReference` + `nuget.config`) |
|---|---|---|---|
| Không cross-wire hai deliverable | ❌ AutoCAD phụ thuộc trực tiếp folder Revit; sửa registry = sửa trong `HPRebar/` | ✅ Hai MCP chỉ phụ thuộc thư viện trung lập; chiều phụ thuộc MCP → shared, không bao giờ MCP → MCP | ✅ (nhưng nguồn gói vẫn phải ở (a) hoặc (b)) |
| Build độc lập từng folder | 🟡 `HPAutoCad.slnx` build được nhưng kéo theo build project trong `HPRebar/`; ref tới project **Exe** (`HPRebar.Mcp.Server`) để lấy registry — hợp lệ với .NET SDK nhưng kéo cả `Program.cs`, `appsettings.json`, seed Revit vào output AutoCAD | ✅ `McpShared.slnx` (libs + tests) · `HPRebar.slnx` (ref `../McpShared/…`) · `HPAutoCad.slnx` (ref `../McpShared/…`) | ✅ mạnh nhất, nhưng mỗi thay đổi mã chung = `dotnet pack` + bump version + restore ở 2 nơi |
| 159 test Revit vẫn xanh | ✅ không đổi gì | ✅ test engine **di chuyển** theo mã (git mv) vào `McpShared/…Tests`; tổng số test không đổi (gate: tổng 2 suite = 159 + mới) | 🟡 test engine phải nằm cạnh nguồn gói |
| Chi phí di dời | ~2h (chỉ csproj mới) | **~8h**: git mv 2 project + tách `Server.Core` khỏi exe + `.slnx` ×3 + `global.json` ×2 + fix `ResolveConfigurationsModule` + move test + CLAUDE.md | (b) + ~4h feed/versioning + quy trình 2 bước mỗi lần sửa |
| Wire-compat bridge Revit đã deploy | ✅ | ✅ Contracts chỉ đổi chỗ, không đổi field; pipe `hprebar-mcp-r2026` giữ | ✅ |
| Seed / registry per exe | 🟡 seed Revit nhúng trong exe được ref → phải lọc | ✅ mỗi exe nhúng seed của mình; engine đọc từ `hostAssembly` | ✅ |
| Rủi ro dài hạn | ❌ "registry chung" thực chất là "registry của Revit được mượn" — vi phạm tinh thần CLAUDE.md | 🟡 phải giữ `McpShared/` **không** có mã host-specific (rule + review) | 🟡 version drift giữa hai MCP khi quên bump |

**Khuyến nghị: (b)** — thoả ràng buộc mới đúng nghĩa, chi phí một lần, và là dạng "extract `Server.Core`" mà ADR-01 đã dự phòng ("C trở thành bước cơ học"). (c) để sau nếu hai MCP tách repo.

## Decision

1. **Ba folder top-level** (tên đề xuất, user đổi được — `plan.md` "Quyết định Claude tự chốt"):
   - `HPRebar/` — Revit add-in + **Revit MCP** (exe `HPRebar.Mcp.Server` mỏng, bridge `HPRebar.McpBridge`, tests Revit-specific, `build/`, `install/`).
   - `HPAutoCad/` — **AutoCAD MCP** (exe `HPAutoCad.Mcp.Server`, `HPAutoCad.McpBridge.Loader`, `HPAutoCad.McpBridge`, `HPAutoCad.Mcp.Server.Tests`, `HPAutoCad.slnx`, `global.json`).
   - `McpShared/` — **thư viện host-neutral, không phải MCP**: `HPRebar.Mcp.Contracts` (git mv), `HPRebar.McpBridge.Core` (git mv), `HPRebar.Mcp.Server.Core` (mới, tách từ exe), `HPRebar.Mcp.Server.Core.Tests` (git mv test engine), `McpShared.slnx`, `global.json`, `README.md` (rule: không `Autodesk.*`, không tên host trong text).
2. **`HPRebar.Mcp.Server.Core` chứa** (tách từ `HPRebar/HPRebar.Mcp.Server/`, git mv giữ history): `Services/*` (`RevitBridgeClient`, `NdjsonPipeTransport`, `ResultFormatter`, `BridgeExceptions`, `RegistryStartup`, + `ExecuteCodeService`, `ContextService` mới), `Models/BridgeOptions`, `Registry/*` **trừ** `SeedLibrary/**`, `Tools/Registry/*`, `Tools/{InspectTypeTool,CancelExecutionTool}`, `Prompts/ToolifyPrompts`, `Resources/ToolRegistryResources`, `Hosts/IHostProfile` (seam compile-time; **không** còn env switch `HPREBAR_MCP_Host`), `Bootstrap/McpServerHost.cs` (`CreateBuilder(args, envPrefix, profile, hostAssembly)` = thân `Program.cs` hiện tại). **Ở lại exe Revit:** `Program.cs` (~15 dòng), `appsettings.json`, `Hosts/Revit/*` (4 core tool/prompt/resource Revit + `RevitHostProfile`), `Registry/SeedLibrary/**` (21 seed, **không đổi chỗ**).
3. **Tên assembly/namespace giữ nguyên** trong phase 0 (`HPRebar.Mcp.Contracts`, `HPRebar.McpBridge.Core`, `HPRebar.Mcp.Server.Core`) — chỉ đổi folder. Rename sang tiền tố trung lập (`HPMcp.*`) là bước dọn dẹp tách riêng sau MVP (một commit atomic, ~90 file `using`), để phase 0 là **git mv + csproj path** với rename detection sạch và gate test rõ.
4. **Solution / SDK:** `McpShared/McpShared.slnx` (Debug/Release); `HPRebar/HPRebar.slnx` sửa 3 path project sang `../McpShared/…` (+ project test engine rời đi); `HPAutoCad/HPAutoCad.slnx` mới. `global.json` copy y nguyên vào `McpShared/` và `HPAutoCad/` (mỗi folder tự pin SDK). Path `..\` trong `.slnx` **`[unverified]`** — kiểm bước 1 phase 0; fallback: Directory `McpShared/` vẫn dùng được qua `.sln` cổ điển nếu `.slnx` từ chối (đây là hành vi chuẩn MSBuild, khả năng thấp).
5. **Pipeline Revit (`HPRebar/build/`) — 1 sửa bắt buộc:** `ResolveConfigurationsModule` dùng `Solutions.HPRebar.FullName` (Sourcy) thay `FindFile(".slnx")` để không bắt nhầm `HPAutoCad.slnx`/`McpShared.slnx`. `CreateBundleModule`, `PublishServerModule`, `CompileProjectModule` không đổi (verified không ref Contracts/Core). Sourcy scan root vẫn `HPRebar/`.
6. **Pipeline AutoCAD:** MVP **không** có `build/` ModularPipelines — dùng `dotnet build/publish` + MSBuild target `DeployBundle` (ADR-05); `HPAutoCad/.run/` optional. Thêm pipeline khi cần `pack` (sau MVP).
7. **Registry per exe** (chi tiết ADR-04 revised): root mặc định `%AppData%\<Product>\McpServer\` với `Product` từ `IHostProfile.ProductFolder` (`HPRebar` giữ nguyên; `HPAutoCad` mới); seed nhúng trong từng exe với prefix `SeedLibrary/` **không đổi**; CLI `registry …` gọi trên exe tương ứng, không cần `--host`.
8. **`.mcp.json`:** `hprebar-revit` không đổi; `hprebar-autocad` → `HPAutoCad/output/HPAutoCad.Mcp.Server/HPAutoCad.Mcp.Server.exe`, env prefix `HPAUTOCAD_MCP_` (`Bridge__HostVersion=2026` tuỳ chọn, default 2026).
9. **`CLAUDE.md` § Repository Layout** thêm 2 dòng (`HPAutoCad/`, `McpShared/`) + sửa mô tả `HPRebar/`; § Current State/MCP cập nhật lệnh test (`dotnet test McpShared/HPRebar.Mcp.Server.Core.Tests`). Thực hiện ở **phase 0 bước cuối**, không trong plan này.

## Alternatives rejected

- **(a)** — vi phạm "do not cross-wire"; ref project Exe kéo seed/config Revit vào AutoCAD.
- **(c)** — đúng về cách ly nhưng thêm quy trình pack/bump cho hai MCP đang co-develop trong một repo; giữ làm bước sau nếu tách repo.
- **Giữ ADR-01 (một exe, host switch)** — trái ràng buộc user; phần `IHostProfile` vẫn được giữ như seam trong `Server.Core`, chọn ở compile-time.
- **Đặt mã chung trong `HPRebar/` và coi `HPRebar/` là "shared"** — chính là (a).

## Consequences

- Phase 0 trở thành **phase di dời** (không đổi hành vi): gate = `McpShared.slnx` + `HPRebar.slnx` build xanh; tổng test engine + Revit = 159 + mới; publish exe Revit → `tools/list` = 34; bridge Revit **không** redeploy.
- Mỗi MCP có exe, registry root, settings/audit/log folder, pipe name, env prefix riêng; không có "instance chọn host".
- `McpShared/` cần rule chống rò rỉ host: không `PackageReference` Autodesk, không `Autodesk.*` trong `using`, text lỗi/description lấy tên host từ `IHostProfile`. Kiểm bằng test đơn giản (`McpShared` assemblies không ref `RevitAPI`/`AcDbMgd`).
- CLAUDE.md § "four unrelated deliverables" → sáu mục (thêm `HPAutoCad/`, `McpShared/`), với ghi chú `McpShared/` là dependency chung được phép của hai MCP.
