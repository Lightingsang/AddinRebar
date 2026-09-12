# ADR-01 — Topology 2 tiến trình: MCP Server (net10 console) ↔ Revit Bridge Add-in (net8, trong Revit)

**Ngày:** 2026-09-12 · **Status:** Accepted (user-confirmed default) · **Owner:** HPRebar

## Context

- Revit 2026 chạy .NET 8; add-in Nice3point target `net8.0-windows7.0`, load trong tiến trình `Revit.exe` qua AssemblyLoadContext riêng (`EnableDynamicLoading=true`, `HPRebar/HPRebar/HPRebar.csproj:8`; `research/roslyn-scripting-in-revit-report.md` §1).
- MCP C# SDK: sách dùng `net10.0`, `ModelContextProtocol` 1.3.0 + `Microsoft.Extensions.Hosting` 10.0.3 — NotebookLM Q09 [2-5]. **Verified 2026-09-12:** NuGet mới nhất `ModelContextProtocol` 2.2.0, `Microsoft.Extensions.Hosting` 10.0.12; sample chính thức `samples/QuickstartWeatherServer` target `net8.0` → SDK multi-target, TFM **không** phải lý do tách tiến trình. Server vẫn chọn `net10.0` (global.json pin SDK 10.0.300, khớp sách).
- Transport sách recommend cho desktop integration là **stdio**: host AI (Claude Desktop / VS Code / Claude Code) *tự launch server làm child process* và quản lý lifecycle — NotebookLM Q07 [6, 7, 23, 30-33]. `Revit.exe` không thể là child process của host AI.
- Quan hệ client↔server là 1:1 trên một transport — NotebookLM Q01 [5, 8].
- Repo tham khảo đều tách: MCP server ngoài (Node) ↔ plugin trong Revit qua TCP — `research/revit-bridge-reference-report.md` Summary.

## Decision

Bốn project, hai tiến trình:

| Project | TFM | Vai trò |
|---|---|---|
| `HPRebar.Mcp.Server/` | `net10.0` console, `OutputType=Exe` | MCP server thật: `Host.CreateApplicationBuilder(…)` + `AddConsole(LogToStandardErrorThreshold = Trace)` (theo SDK sample chính thức, verified 2026-09-12; sách dùng `CreateEmptyApplicationBuilder(null)` — Q07 [16-18], Q08 [4]) → `AddMcpServer().WithStdioServerTransport().WithToolsFromAssembly()…`. Không reference Revit API — chỉ nói chuyện với bridge qua IPC. |
| `HPRebar.McpBridge.Core/` | `net8.0` | Phần bridge không đụng Revit: `Pipe/` (listener, dispatcher, writer, `IRevitExecutor`), `Model/` (settings, status, store), `Scripting/` (Roslyn guard/compiler/cache — phase 4). Không reference Revit → xUnit test được (rule CLAUDE.md). |
| `HPRebar.McpBridge/` | `net8.0-windows7.0`, Nice3point.Revit.Sdk, R25/R26 | Add-in riêng trong Revit. Listener IPC + ExternalEvent + Roslyn executor. Có `.addin` manifest riêng. |
| `HPRebar.Mcp.Contracts/` | `netstandard2.0` | Record cho envelope IPC (request/response/notification, error codes, DTO context/inspect/execute). Không reference Revit lẫn MCP SDK. |

Bridge là **project add-in riêng**, không phải feature folder trong `HPRebar/HPRebar/`:
- Roslyn (`Microsoft.CodeAnalysis.*`, ~15 MB) không được ILRepack vào `HPRebar.dll` (`IsRepackable=true`) — phình DLL sản phẩm chính, tăng rủi ro xung đột ALC với Dynamo (`roslyn-scripting-in-revit-report.md` Risk).
- Tính năng "AI chạy code trong Revit" cần bật/tắt và cài đặt độc lập với add-in nghiệp vụ rebar.
- Release cadence khác nhau (MCP SDK còn thay đổi nhanh).
- Bên trong `HPRebar.McpBridge/` vẫn theo Feature Folder Convention của CLAUDE.md: root chỉ `McpBridgeCommand.cs`, `McpBridgeExternalEventHandler.cs`, `McpBridgeRequest.cs` + `Model/ Service/ View/ ViewModel/`.

## Alternatives rejected

1. **MCP server in-process trong Revit qua Streamable HTTP** (`ModelContextProtocol.AspNetCore` + `MapMcp()` — NotebookLM Q07 [19-21]). Loại: kéo ASP.NET Core vào tiến trình Revit; SDK target net10 vs add-in net8; host AI phải cấu hình `type: http` thay vì stdio mặc định; sách recommend stdio cho local desktop (Q07 [6, 23, 30]).
2. **Node/TypeScript server như repo tham khảo.** Loại: tách ngôn ngữ, mất DI/attribute model của C# SDK (Q04, Q08); user yêu cầu C#.
3. **Feature folder `HPRebar/HPRebar/McpBridge/`.** Loại vì ILRepack/Roslyn ở trên. Vẫn là phương án B nếu sau này cần 1 installer duy nhất — ghi ở phase-02.

## Consequences

- Server phải chịu được trạng thái "Revit chưa mở / bridge chưa bật": tool ném `McpException("Revit bridge not connected …")` — thông điệp verbatim tới AI (NotebookLM Q02 [15]) thay vì crash.
- Hai lifecycle độc lập: server sống theo host AI (stdio: client đóng stdin = shutdown — Q03 [9]); bridge sống theo Revit + toggle ribbon.
- Logging server **chỉ ra stderr/file** (`LogToStandardErrorThreshold: Trace`, Q08 [14-18]); bridge log qua Serilog file như HPRebar.
- Cần ADR riêng cho IPC contract (ADR-02) và phase packaging 2 artefact (server = dotnet tool/exe; bridge = Autodesk bundle).
