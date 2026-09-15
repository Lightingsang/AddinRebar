# Phase 0 — `McpShared` đa mục tiêu `net8.0;net48` + hằng/profile Navis — Kết quả

**Ngày:** 2026-09-15 · **Trạng thái:** Implemented + Built + Tested + Reviewed (code-reviewer 8.5/10 → 7 finding đã fix; tester độc lập 9/9 gate) — không có phần Verified live: phase 0 không chạm host nào · **Đã commit** `fb65f25` + commit 0b (xem cuối).

## Đã làm (khớp bảng authoritative 14 mục)

| # | File | Kết quả |
|---|---|---|
| 1 | `HPRebar.McpBridge.Core.csproj` | `net8.0;net48` + Polyfill 11.0.1 (net48 only) |
| 2 | `Pipe/PipeListener.cs` | `#if NET48`: `PipeSecurity.SetOwner(identity.Owner)` + `PipeAccessRule(Owner, FullControl)`; `#else` giữ `CurrentUserOnly` |
| 3 | `Host/MainThreadQueue.cs` | `#if NET48`: clock `Stopwatch`; `#else` `TickCount64` |
| 4 | `Scripting/GuardProfile.cs` | `#if NET48` `IReadOnlyCollection<string>`; **+** `GuardProfile.Navis` (W3, SQL, Expressions/Delegate, Automation/Interop/ComApi/Data, Forms/Win32) |
| 5 | `Scripting/AnalyzerProfile.cs` | như #4; **+** `AnalyzerProfile.Navis(["Transaction"], ["BeginTransaction"])` |
| 6–9 | Contracts | `PipeNaming.NavisHost` + case; `JsonRpcMethods.NavisPrefix`; `NavisImports`/`NavisGlobals`; `ContextResult.Navis : NavisInfo?` + `ModelSummary` |
| 10 | `IHostProfile`/`HostProfile` | `MaxTimeoutSeconds` (default 120, copy trong `WithHostAssembly`) |
| 11–13 | `ExecuteCodeService.cs:59`, `ToolManager.cs:173`, `ToolValidator.cs:52` | clamp/validate theo `profile.MaxTimeoutSeconds`; message validator dùng số của profile (Revit/AutoCAD: "between 5 and 120." — nguyên văn) |
| 14 | `McpShared.slnx` | + `HPRebar.McpBridge.Core.Net48Tests` |
| + | `HPRebar.McpBridge.Core.Net48Tests/` (mới) | link `ScriptGuardTests`, `MainThreadQueueTests`, `Fakes/FakeRevitExecutor`; mới `ScriptCompilerNet48Tests` (8), `PipeListenerNet48Tests` (4) |
| + | `HPRebar.Mcp.Server.Core.Tests/NavisProfileTests.cs` (mới) | 24 test: hằng navis, guard Navis (11 mẫu deny + 1 mẫu cho qua), analyzer, `MaxTimeoutSeconds` default/`WithHostAssembly`, validator 120/600, `ExecuteCodeService` clamp (4 case), `run_tool` clamp (2 case), `Shape` navis, serialize không có key `navis` |
| + | `HPRebar.Mcp.Server.Core.Tests/MainThreadQueueTests.cs` | 1 assertion `IsCompletedSuccessfully` → `Status == RanToCompletion` (tương đương; để link được vào net48) |
| + | `McpShared/README.md` | bảng TFM + project test mới + consumer HPNavis (planned) |

**Diff `McpShared/HPRebar.McpBridge.Core/*.cs`: 0 dòng xoá / 76 dòng thêm** — mọi dòng net8 giữ nguyên văn trong `#else`.

## Kiểm tra đã chạy

| Kiểm tra | Kết quả |
|---|---|
| `dotnet build McpShared.slnx -c Debug` | ✅ 0 error; 1 warning pre-existing `McpBridgeStatusViewModel.cs(161,31) CS8604` chỉ trên net48 (BCL .NET Framework không có nullable annotation) — ngoài danh sách sửa, để nguyên |
| `dotnet test HPRebar.Mcp.Server.Core.Tests` | ✅ **121/121** (baseline 96 + 25 mới, gồm lower-bound `MaxTimeoutSeconds`) |
| `dotnet test HPRebar.McpBridge.Core.Net48Tests` | ✅ **53/53** (gồm assert owner SID + DACL của pipe, race-free second-listener, default `Stopwatch` clock), runner log `HPRebar.McpBridge.Core.Net48Tests.exe (net48|x64)` — MTP + xunit.v3 chạy net48 **không cần fallback** (mục `[chưa xác minh]` ADR-01 §4 → verified) |
| `dotnet test HPRebar/HPRebar.Mcp.Server.Tests` | ✅ 109/109 (nguyên số) |
| `dotnet test HPAutoCad/HPAutoCad.Mcp.Server.Tests` | ✅ 58/58 (nguyên số) |
| `tools/list` Revit exe rebuild (registry root cách ly) | ✅ 33 tool, **byte-identical** before/after (`phase-00-tools-list-*-revit.json`) |
| `tools/list` AutoCAD exe rebuild | ✅ 24 tool, **byte-identical** |
| Bằng chứng rebuild | `HPRebar.Mcp.Server.Core.dll` bên cạnh exe chứa `MaxTimeoutSeconds` ×3, `Contracts.dll` chứa `NavisInfo`; SHA Core dll đổi giữa hai lần snapshot `after` (`758CF6A9…` → `B437201A…` sau fix review) trong khi `tools/list` vẫn identical → gate không so exe với chính nó (không có `before` cho Core dll: baseline được đo trước khi phát hiện exe là apphost stub) |
| Tester độc lập (`reports/tester-phase-00-gates.md`) | ✅ 9/9 gate, 339 test tổng (trước fix review) |
| `dotnet build HPAutoCad/HPAutoCad.slnx -c Debug -p:DeployBundle=false` | ✅ 0 error 0 warning |
| `dotnet build HPRebar/HPRebar.slnx -c Debug.R26 -p:DeployAddin=false` | ✅ 0 error; 24 warning = ILRepack `EXEC : warning` pre-existing của add-in, 0 từ McpShared |
| `git diff` Core `.cs` | ✅ 0 dòng xoá |

## Sai lệch so với plan (đã sửa plan)

- Tiêu chí "SHA **exe** after ≠ before" sai artefact: `.exe` là apphost stub, không đổi khi rebuild; đổi thành SHA `HPRebar.Mcp.Server.Core.dll` + grep member mới. `snapshot-tools-list.ps1` sửa theo.
- Revit `tools/list` = **33** trên registry root cách ly (CLAUDE.md "34" gồm `set_mark_from_comments` user đã approve trong registry live) — sửa số trong phase 0.
- Bước 2 (quan sát 4+2 lỗi trước khi shim) bỏ qua vì probe E12 đã ghi đúng lỗi; áp shim thẳng.
- `MainThreadQueueTests.cs` phải sửa 1 assertion để link được vào net48 (`Task.IsCompletedSuccessfully` không có trên .NET Framework) — test file, tương đương, 96 test cũ vẫn pass.
- Phát hiện lỗ hổng base deny-list mới: `((Func<int>)(() => 1)).Method` trả `MethodInfo` mà không nêu tên type → thêm `Method` vào bước 0b (user đã duyệt làm 0b trong commit riêng).

## Code review (code-reviewer, 8.5/10, DONE_WITH_CONCERNS) — đã xử lý

| # | Finding | Xử lý |
|---|---|---|
| M1 | Test net48 không assert owner SID/DACL của pipe (điểm cốt lõi của shim) | ✅ `Ping_round_trips…` giờ assert `GetOwner == WindowsIdentity.Owner`, đúng 1 rule `FullControl/Allow` cho Owner |
| M2 | Race hai listener cùng tên (test có thể fail 5 s) | ✅ chờ pipe xuất hiện trong `\\.\pipe\` trước khi start listener thứ hai (mẫu net10) |
| L1 | Clock net48 `GetTimestamp()*1000/Frequency` tràn `long` với QPC tần số cao; nhánh không có test | ✅ `Stopwatch.StartNew().ElapsedMilliseconds` (static); test `Default_monotonic_clock_ages_a_request…` |
| L2 | 3 warning xUnit1051 trong test mới | ✅ truyền `TestContext.Current.CancellationToken` |
| L3 | Thiếu `before` SHA; report chưa tồn tại | ✅ report ghi rõ bằng chứng mtime/grep + SHA đổi giữa 2 lần after |
| L4 | `WindowsIdentity` không dispose | ✅ `using var identity` |
| L5 | `MaxTimeoutSeconds < 5` làm `Math.Clamp` ném | ✅ `init` guard `ArgumentOutOfRangeException` + test |
| L6 | Chưa assert `AutocadHostProfile.MaxTimeoutSeconds == 120` | ⏭ hoãn — đụng `HPAutoCad/` (phase 0 không chạm); ghi vào phase kế tiếp có sửa HPAutoCad |
| Info | mutability qua cast trên cả hai TFM; `NavisImports` import `Clash`/`Timeliner` cứng (Simulate không có Clash); `IHostProfile` thêm abstract member (chỉ `HostProfile` implement) | ghi nhận; `NavisImports` → phase 1/2 quyết định bỏ import khi assembly thiếu |

## Commit

- Phase 0: `fb65f25` (`feat(mcpshared): multi-target the bridge engine to net48 and add the Navisworks profile`).
- **0b (user duyệt, commit riêng):** deny-list gốc `ScriptGuard.cs` + `System.Linq.Expressions` (namespace), `Expression`/`Delegate` (identifier), `CreateDelegate`/`Compile`/`Method` (member); `GuardProfile.Navis` bỏ các mục trùng; 5 case mới trong `ScriptGuardTests` (link sang net48 → 58). Gate: 126 + 58 + 109 + 58 pass; `tools/list` 33/24 identical; 21 seed Revit + 12 seed AutoCAD không dính guard mới (suite host pass nguyên số). CLAUDE.md security model cập nhật.
