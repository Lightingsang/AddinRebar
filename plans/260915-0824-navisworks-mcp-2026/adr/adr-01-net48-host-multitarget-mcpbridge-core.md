# ADR-01 — Host .NET Framework 4.8: đa mục tiêu `HPRebar.McpBridge.Core` sang `net8.0;net48` (không bridge riêng, không Automation ngoài tiến trình)

**Ngày:** 2026-09-15 · **Revised 2026-09-15 (red-team):** `IReadOnlyCollection<string>` thay `ISet` trên net48; `ScriptAnalyzer.cs` thêm vào danh sách caller; `Net48Tests` không link `PipeRoundTripTests`/`ScriptCompilerTests` (net10-only, partial-name `Assembly.Load`), gate = "mọi test link/mới pass"; resolver allow-list + kiểm `Assembly.Location` ở self-check · **Status:** Proposed (chờ user duyệt plan; chưa sửa repo) · **Owner:** HPNavis
**Kế thừa:** [AutoCAD ADR-06 one MCP one folder](../../260913-0000-autocad-mcp-bridge-2026/adr/adr-06-one-mcp-one-folder.md) · [AutoCAD ADR-05 ALC isolation](../../260913-0000-autocad-mcp-bridge-2026/adr/adr-05-autocad-plugin-packaging-alc-isolation-multi-version.md) · [Revit ADR-01 two-process](../../260912-1521-dynamic-revit-mcp-server-2026/adr/adr-01-two-process-topology.md)
**Bằng chứng:** [research/evidence-on-machine-2026-09-15.md](../research/evidence-on-machine-2026-09-15.md) §E1, E2, E7, E11, E12, E13

## Context

- Navisworks Manage 2026 (`Roamer.exe`) chạy **.NET Framework 4.8** — `Roamer.exe.config:8–10` `supportedRuntime v4.0 sku=.NETFramework,Version=v4.8`; không có `coreclr.dll`/`hostfxr.dll`/`runtimeconfig.json` trong thư mục cài (E1). Revit 2026 và AutoCAD 2026 là .NET 8 → đây là host đầu tiên trong repo không phải .NET 8.
- Phần bridge dùng chung `McpShared/HPRebar.McpBridge.Core` là **`net8.0`** (`HPRebar.McpBridge.Core.csproj:4`), 2 168 dòng trong 23 file: pipe listener + dispatcher (`Pipe/`), Roslyn guard/compiler/analyzer/cache/args/units/inspector (`Scripting/`), `MainThreadQueue` + `McpBridgeHost` (`Host/`), settings/audit/status (`Model/`, `ViewModel/`). Không thể nạp vào Roamer.exe như hiện tại.
- `HPRebar.Mcp.Contracts` đã là `netstandard2.0` (+ Polyfill) — nạp được vào net48. `HPRebar.Mcp.Server.Core` là net10 nhưng chạy **ngoài** Navisworks (exe stdio) — không bị ảnh hưởng.
- Ràng buộc cứng của user: không đổi hành vi Revit/AutoCAD; nếu chọn A phải chứng minh bằng test (96 + 109 + 58 pass, tool surface byte-identical); nếu ADR-01 kết luận phải sửa Core theo cách **không additive** → dừng hỏi.

## Ba phương án

| Tiêu chí | **A — `HPRebar.McpBridge.Core` → `<TargetFrameworks>net8.0;net48</TargetFrameworks>`** | **B — bridge net48 riêng trong `HPNavis/`, chỉ dùng lại Contracts** | **C — không plugin; `Autodesk.Navisworks.Automation` ngoài tiến trình** |
|---|---|---|---|
| Khả thi kỹ thuật | ✅ **đã chứng minh trên máy** (E12 build xanh, E13 chạy Roslyn 5.9 trên 4.8.9181) | ✅ (cùng bằng chứng E12/E13, nhưng trên bản copy) | ❌ Automation API = `OpenFile/AppendFile/SaveFile/Print/CreateCache/ExecuteAddInPlugin/AddPluginAssembly/StayOpen/Visible` — **toàn bộ** member list của `Autodesk.Navisworks.Automation.xml` (E7). Không truy vấn được model tree/properties/selection/search/viewpoint/clash |
| Mã phải viết thêm | csproj: 1 dòng TFM + 1 Polyfill (net48-only); **2 khối `#if NET48`** (`PipeListener.cs:80` ACL pipe, `MainThreadQueue.cs:67` clock); **4 property type** `IReadOnlySet<string>` (`GuardProfile.cs:62,65`, `AnalyzerProfile.cs:25,28`) cần dạng net48-visible | **lặp ~1 900–2 168 dòng** (Pipe 400 + Scripting 900 + Host 350 + Model/VM 500) + viết lại 96 test engine cho bản copy, hoặc chấp nhận bản copy không test | không áp dụng (phương án chết) |
| Hành vi Revit/AutoCAD | ✅ **byte-identical**: mọi thay đổi nằm dưới `#if NET48` / `Condition='$(TargetFramework)'=='net48'`; consumer net8/net10 (HPRebar.McpBridge net8.0-windows7.0, HPAutoCad.McpBridge net8.0-windows, 3 test project net10) chọn asset `net8.0` như cũ | ✅ không đụng | — |
| Drift dài hạn | ✅ một engine, một bộ test; fix guard/compiler/pipe áp dụng cả 3 host | ❌ hai engine; mọi CVE/guard fix làm 2 lần; registry `revit.analyze` semantics dễ lệch | — |
| Rủi ro riêng | binding .NET Framework: assembly bind theo **đúng version** → cần `AssemblyResolve` trong plugin (E13: 4 lần resolve `Immutable 10.0.0.0→10.0.0.1`, `Unsafe 6.0.0.0→6.0.3.0`, `Memory 4.0.2.0→4.0.5.0`, `Metadata 10.0.0.0→10.0.0.1`); `Roamer` ship `Unsafe 6.0.0.0` + `Tasks.Extensions 4.2.0.1` (E2) → side-by-side, vô hại | cùng rủi ro binding + thêm rủi ro drift | — |
| Effort | ~6 h (phase 0) + test net48 | ~14 h + nợ vĩnh viễn | — |

**Kết luận: A.** C bị loại bằng bằng chứng file (E7). B chỉ hợp lý nếu A không additive — nhưng A additive được 100 % (mục Decision §3).

## Decision

1. **`McpShared/HPRebar.McpBridge.Core` đa mục tiêu `net8.0;net48`.** **Hành vi** asset `net8.0` không đổi (mọi dòng bị đổi nằm trong `#if NET48`; nguồn chỉ **thêm** member `GuardProfile.Navis`, `AnalyzerProfile.Navis`); `net48` là target **mới**. Consumer hiện có không đổi `ProjectReference`.
2. **Chỉ sửa dưới điều kiện TFM** — danh sách đóng, lấy từ probe E12 (mọi thứ khác compile nguyên văn):
   | Vị trí | net8.0 (giữ nguyên) | net48 (thêm) |
   |---|---|---|
   | `HPRebar.McpBridge.Core.csproj` | `PackageReference` như cũ | `+ Polyfill 11.0.1 PrivateAssets=all Condition net48`, `<PolyPublic>` không cần (chỉ dùng nội bộ) |
   | `Pipe/PipeListener.cs:78–80` | `PipeOptions.Asynchronous \| PipeOptions.CurrentUserOnly` | `PipeSecurity` với 1 `PipeAccessRule(WindowsIdentity.GetCurrent().User, FullControl, Allow)` + overload ctor có `pipeSecurity` (net48 có sẵn). Cùng ý nghĩa ACL "chỉ user hiện tại" |
   | `Host/MainThreadQueue.cs:67` | `Environment.TickCount64` | `Stopwatch.GetTimestamp() * 1000 / Stopwatch.Frequency` (monotonic, cùng hợp đồng "không bị nhảy wall-clock") |
   | `Scripting/GuardProfile.cs:62,65`, `Scripting/AnalyzerProfile.cs:25,28` | `public IReadOnlySet<string>` | `#if NET48 public IReadOnlyCollection<string> … #else public IReadOnlySet<string> … #endif` — cùng backing `HashSet<string>`; **read-only ở cả hai TFM** (không dùng `ISet`: script import `HPRebar.McpBridge.Core.Scripting` và Core là compiler reference → `GuardProfile.Navis.DeniedMembers.Clear()` sẽ compile). Caller trong Core chỉ gọi `.Contains` — `ScriptGuard.cs:104,125` **và** `ScriptAnalyzer.cs:102,127` — LINQ `Contains` trên `IReadOnlyCollection` dispatch về `ICollection<T>.Contains` O(1); không đổi code gọi. Bề mặt net8 **không đổi** |
3. **Tính additive — kiểm chứng bằng:** (a) bảng **duy nhất** ở `phase-00` "Danh sách sửa engine (authoritative)" là nguồn sự thật; `git diff` của Core chỉ chạm `PipeListener.cs`, `MainThreadQueue.cs`, `GuardProfile.cs`, `AnalyzerProfile.cs` + `.csproj`; mọi dòng **bị xoá/đổi** nằm trong khối `#if NET48`/`#else`, phần còn lại là **thêm** member (`GuardProfile.Navis`, `AnalyzerProfile.Navis`); `AuditEntry`, `BridgeSettings`, `IMcpBridgeRunner`, `McpBridgeStatusViewModel` **không đổi** (ADR-04 revised); (b) `dotnet test McpShared/HPRebar.Mcp.Server.Core.Tests` = 96 pass, `HPRebar/HPRebar.Mcp.Server.Tests` = 109, `HPAutoCad/HPAutoCad.Mcp.Server.Tests` = 58; (c) snapshot `tools/list` Revit (34) và AutoCAD (24) byte-identical trước/sau — **trên exe vừa build** (`dotnet build -c Release` rồi `mcp-call.py bin/Release/net10.0/<exe> tools/list` với registry root cách ly qua env, không cần Revit/AutoCAD mở; ghi SHA-256 exe cạnh snapshot) — không dùng exe đã publish trong `output/` (bản Revit publish 2026-09-12 cũ hơn nguồn 2026-09-14; so nó với chính nó vô nghĩa); (d) `dotnet build HPRebar/HPRebar.slnx -c Debug.R26 -p:DeployAddin=false` và `dotnet build HPAutoCad/HPAutoCad.slnx -c Debug -p:DeployBundle=false` xanh. Nếu bất kỳ mục nào đỏ → **dừng, báo user** (đúng điều kiện "sửa Core không additive").
4. **Test cho target net48:** project mới `McpShared/HPRebar.McpBridge.Core.Net48Tests` (net48; xunit.v3 3.1.0 ship lib `net472` — `~/.nuget/packages/xunit.v3/3.1.0/lib`, `xunit.v3.core`, `xunit.v3.runner.inproc.console`, verified 2026-09-15; **[chưa xác minh]** runner Microsoft.Testing.Platform khởi động exe net48 qua `dotnet test` — fallback `xunit` v2 + `xunit.runner.visualstudio` cho riêng project này). Nội dung:
   - **Link nguyên văn:** `ScriptGuardTests.cs` (1 Fact + 23 InlineData), `MainThreadQueueTests.cs` (11 + 5), `Fakes/FakeRevitExecutor.cs`.
   - **Không link** `PipeRoundTripTests.cs` (import `HPRebar.Mcp.Server.Models/Services` — Server.Core net10, `PipeRoundTripTests.cs:3–4,25,33`) và `ScriptCompilerTests.cs`/`ScriptArgsAndAnalyzerTests.cs` (`Assembly.Load("System.Runtime")` partial-name — ném `FileNotFoundException` trên .NET Framework; verified bằng Windows PowerShell 5.1: `[Assembly]::Load('System.Runtime')` thất bại, full name thành công).
   - **Mới (net48-only, ~10 test):** `ScriptCompilerNet48Tests` (references = `typeof(object).Assembly`, `typeof(Enumerable).Assembly`, `typeof(List<>).Assembly`, `typeof(ScriptArgs).Assembly`; compile/run/cache/args/analyzer như E13) và `PipeListenerNet48Tests` (`PipeListener` + `FakeRevitExecutor` + `NamedPipeClientStream` thô viết NDJSON: ping/context/execute round trip — bài kiểm duy nhất của shim `PipeSecurity`).
   - **Gate:** "mọi test link + mới pass trên .NET Framework 4.8" (≈ 50 case); **không** con số tối thiểu tuỳ tiện.
   - Shim `PipeSecurity` net48 phải mirror điều client .NET kiểm: `NdjsonPipeTransport.cs:47–50` dùng `PipeOptions.CurrentUserOnly` phía client → .NET so **owner SID** của pipe với `WindowsIdentity.GetCurrent().Owner`. Server net48: `security.SetOwner(identity.Owner)` + `PipeAccessRule(identity.Owner, FullControl, Allow)` (không dùng `.User`). Hệ quả (cả 3 host): Roamer chạy elevated + server không elevated → owner khác → từ chối kết nối (ghi docs). Kiểm thật ở phase 3 (exe net10 ↔ bridge net48 thật) và phase 5.
5. **`AssemblyResolve` là việc của plugin Navis, không của Core — và phải hẹp.** `HPNavis.McpBridge` cài handler trong static ctor của `EventWatcherPlugin` (trước khi chạm type Roslyn nào): (a) **allow-list** simple name (`Microsoft.CodeAnalysis`, `Microsoft.CodeAnalysis.CSharp`, `Microsoft.CodeAnalysis.Scripting`, `Microsoft.CodeAnalysis.CSharp.Scripting`, `System.Collections.Immutable`, `System.Reflection.Metadata`, `System.Runtime.CompilerServices.Unsafe`, `System.Memory`, `System.Buffers`, `System.Numerics.Vectors`, `System.Threading.Tasks.Extensions`, `System.Text.Encoding.CodePages`, `System.Text.Json`, `System.Text.Encodings.Web`, `Microsoft.Bcl.AsyncInterfaces`, `HPRebar.Mcp.Contracts`, `HPRebar.McpBridge.Core`, `CommunityToolkit.Mvvm`, `Serilog`, `Serilog.Sinks.File`); (b) chỉ trả lời khi `args.RequestingAssembly == null` (Roslyn reflective load) **hoặc** `RequestingAssembly.Location` nằm trong thư mục plugin — plugin khác trong Roamer hỏi `System.Text.Json 8.0.0.0` sẽ **không** nhận bản 10 của ta; (c) log requester + đường dẫn ở Debug. **Không có "eager `LoadFrom` 24 DLL" làm fallback** — .NET Framework bind theo đúng version, nạp trước không thoả reference `10.0.0.0` khi file là `10.0.0.1` (E13 chứng minh chính lỗi này). **Kiểm ở `ScriptingSelfCheck`:** mọi assembly `Microsoft.CodeAnalysis*`, `System.Collections.Immutable`, `System.Reflection.Metadata` đã nạp phải có `Location` trong thư mục plugin; sai → self-check FAIL, listener không start (plugin lạ `NavisworksMCPPlugin` cũng có thể có resolver riêng — E3; phase 1 S-10 chạy với plugin đó **bật** để so log resolve).
6. **Phạm vi ảnh hưởng net48 lên từng thành phần (đánh giá theo yêu cầu):**
   | Thành phần | Ảnh hưởng | Bằng chứng |
   |---|---|---|
   | `ScriptGuard` | không đổi mã; `string.Contains(string, StringComparison)` ← Polyfill | E12 §5 |
   | `ScriptCompiler` (Roslyn) | không đổi mã; `Convert.ToHexString`/`SHA256.HashData` ← Polyfill; `InteractiveAssemblyLoader` dùng desktop loader; cache hit hoạt động | E12 §5, E13 (`compiled=1 cacheHit2=True`) |
   | `ScriptArgs` | không đổi; `System.Text.Json 10.0.12` có `net462` | E11 |
   | `AuditLogger` / `BridgeSettingsStore` | không đổi; `record`/`init` ← Polyfill `IsExternalInit`; Serilog.Sinks.File 7 có `net462` | E11, E12 |
   | `PipeListener` | ACL: `PipeSecurity` thay `CurrentUserOnly` | E12 §3 |
   | `MainThreadQueue` | clock shim | E12 §3 |
   | `McpBridgeStatusViewModel` | không đổi; CommunityToolkit.Mvvm 8.4.0 `netstandard2.0` | E11 |
   | `TypeInspector`, `ScriptAnalyzer`, `ScriptCache`, `ScriptUnits`, `RequestDispatcher`, `McpBridgeHost` | không đổi | E12 §5 |

## Alternatives rejected

- **A′ — hạ Core xuống `netstandard2.0` đơn TFM:** consumer net8 sẽ mất `PipeOptions.CurrentUserOnly` và `Environment.TickCount64` (netstandard2.0 cũng không có) → **đổi hành vi Revit/AutoCAD**, vi phạm ràng buộc. Loại.
- **A″ — tách `HPRebar.McpBridge.Core.Scripting` thành lib netstandard2.0 riêng** để Navis chỉ dùng phần script: vẫn phải lặp Pipe/Host/Model hoặc đa mục tiêu chúng → không rẻ hơn A, thêm 1 assembly. Loại (YAGNI).
- **B:** lặp ~2 000 dòng + 96 test; drift guard/compiler giữa host. Loại.
- **C:** E7. Loại.
- **AppDomain riêng cho bridge (isolation kiểu ALC của AutoCAD):** .NET Framework cho phép AppDomain với `ConfigurationFile` riêng (binding redirects riêng) — nhưng object `Document`/`ModelItem` của Navisworks không marshal qua AppDomain; script phải chạy trong AppDomain chính. Loại; `AssemblyResolve` là đủ (E13).

## Consequences

- Phase 0 là refactor **không đổi hành vi** trên `McpShared/`; gate = §3 (a)–(d). `McpShared.slnx` thêm project test net48; `HPRebar.slnx`/`HPAutoCad.slnx` không đổi.
- Build `McpShared` cần reference assemblies net48 (`Microsoft.NETFramework.ReferenceAssemblies.net48` có trong cache — E11; SDK tự thêm khi target net48).
- Bridge Navis (`HPNavis.McpBridge`) là **net48** + `UseWPF` (WPF net48 hợp lệ) hoặc WinForms; không có ALC/loader tách đôi như AutoCAD — một assembly + thư mục dependency phẳng, `AssemblyResolve` thay ALC.
- Roslyn 5.9 (`LangVersion` mới nhất) compile script cho runtime net48: script không dùng được API .NET 8-only (`Span` slicing phức tạp, `Random.Shared`, `Parallel`…); `HostScriptContracts.NavisImports` và mô tả tool phải nói rõ "runtime .NET Framework 4.8".
- Nếu Autodesk chuyển Navisworks sang .NET 8/10 ở bản sau, chỉ cần đổi TFM của `HPNavis.McpBridge`; Core đã có cả hai.
