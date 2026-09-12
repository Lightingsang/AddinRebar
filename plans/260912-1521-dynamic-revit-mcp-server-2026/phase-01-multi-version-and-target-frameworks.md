# Phase 01 — Multi-version & Target Frameworks

## Context Links
- [architecture.md §6 ghi chú R25/R26](architecture.md) · [ADR-01 bảng project/TFM](adr/adr-01-two-process-topology.md) · [ADR-02 tên pipe `hprebar-mcp-r{RevitVersion}`](adr/adr-02-ipc-named-pipe.md)
- `CLAUDE.md` §"HPRebar — Current State": BeamRebar/FoundationRebar không compile R27; R23/R24 = net48
- `research/roslyn-scripting-in-revit-report.md` §1 (Revit 2026 .NET 8 + ALC), Risk "Version conflict Roslyn vs Dynamo"
- Verified: `HPRebar/HPRebar.Tests/HPRebar.Tests.csproj:6-8` (`Configurations Debug.R25;Debug.R26;Release.R25;Release.R26`) — mẫu cho bridge; `HPRebar/HPRebar/ColumnRebar/Service/ThemeSwitcher.cs:70-73` (comment `// Multi-version:` + `#if REVIT2024_OR_GREATER`)
- Skill: `/bs:revit-addin` → `references/multi-version-strategy.md`

## Overview
- **Status (thực tế):** built 2026-09-12 — `PipeNaming`, `BridgeOptions` (+validate-on-start), appsettings `Bridge` section; bridge xanh Debug.R25 + Debug.R26; 0 `#if REVIT`.
- **Priority:** P1 · **Effort:** 2h
- Chốt bridge chỉ R25 + R26 (cùng net8.0-windows7.0), server version-agnostic, pipe name mang Revit version, quy ước `// Multi-version:` cho tương lai R27.

## Key Insights
- R25 và R26 đều net8 → cùng binary shape; khác biệt Revit API giữa 2025/2026 không chạm code bridge (chỉ dùng `Document`, `Transaction`, `UIDocument.Selection`, `UIThemeManager`, `UIApplication.SelectionChanged` — tất cả có từ ≤2024).
- **Out of scope R23/R24 (net48):** không có `AssemblyLoadContext` → Roslyn + `Microsoft.CodeAnalysis` load thẳng vào AppDomain Revit, đụng Dynamo; `IsRepackable=false` khiến DLL Roslyn nằm chung `Addins\` folder → xung đột version chắc chắn hơn. Roslyn scripting chạy được trên net48 nhưng rủi ro không đáng cho v1.
- **Out of scope R27 (net10):** HPRebar chính chưa compile R27 (10 lỗi `RebarHookOrientation`/`Curve.Intersect`); TFM net10 khác → chỉ mở lại khi HPRebar xanh R27.
- Server không biết Revit version — chỉ biết tên pipe. Cấu hình `Bridge:RevitVersion` (mặc định 2026) → pipe `hprebar-mcp-r2026`. Đổi qua env `HPREBAR_MCP_Bridge__RevitVersion=2025` (prefix env đã đặt ở phase 0 bước 4).
- Breaking-change matrix liên quan duy nhất: `ElementId.Value` (long, 2024+). R25/R26 đều ≥2024 → dùng `.Value` thẳng, **không cần `#if`** trong v1. Contracts dùng `long` cho mọi id.
- Bridge `Configurations` sửa tay trong csproj (IDE làm hỏng — CLAUDE.md).

## Requirements
**Functional**
- Bridge build xanh cả `Debug.R25` và `Debug.R26`; solution build R23/R24/R27 bỏ qua bridge, không lỗi.
- Pipe name sinh từ một hàm chung trong Contracts, dùng ở cả server lẫn bridge (DRY).
**Non-functional**
- Không có `#if REVIT` nào trong bridge v1 trừ khi kèm `// Multi-version:`; grep phải ra 0 hoặc chỉ dòng có tag.

## Architecture
| Project | Configurations | TFM (SDK tự chọn) | Ghi chú |
|---|---|---|---|
| `HPRebar.Mcp.Contracts` | `Debug;Release` | netstandard2.0 | id = `long`, không `ElementId` |
| `HPRebar.Mcp.Server` | `Debug;Release` | net10.0 | `BridgeOptions.RevitVersion=2026` |
| `HPRebar.McpBridge` | `Debug.R25;Debug.R26;Release.R25;Release.R26` | net8.0-windows7.0 | SDK emit `REVIT2025`/`REVIT2026`, `REVIT2024_OR_GREATER` |
| `HPRebar.McpBridge.Tests` (phase 5) | như bridge | net8.0-windows7.0 | TUnit in-process |

Pipe naming flow: `appsettings.json Bridge:RevitVersion` → `BridgeOptions` → `PipeNaming.For(2026)` = `"hprebar-mcp-r2026"` ← bridge: `PipeNaming.For(uiapp.Application.VersionNumber)` (string "2026" → int).

## Related Code Files
**To create (proposed)**
- `HPRebar/HPRebar.Mcp.Contracts/PipeNaming.cs` — `public static class PipeNaming { public const string Prefix = "hprebar-mcp-r"; public static string For(int revitVersion) => Prefix + revitVersion; }`
- `HPRebar/HPRebar.Mcp.Server/Models/BridgeOptions.cs` — `SectionName="Bridge"`, `RevitVersion=2026`, `PipeName => PipeNaming.For(RevitVersion)` (phase 2 bổ sung timeouts/limits)
**To modify (proposed)**
- `HPRebar/HPRebar.McpBridge/HPRebar.McpBridge.csproj` — `<Configurations>` 4 config
- `HPRebar/HPRebar.slnx` — xác nhận mapping bridge R25/R26 (đã thêm phase 0) đúng
- `HPRebar/HPRebar.Mcp.Server/appsettings.json` — `"Bridge": { "RevitVersion": 2026 }`
- `HPRebar/HPRebar.Mcp.Server/Program.cs` — `builder.Services.Configure<BridgeOptions>(builder.Configuration.GetSection(BridgeOptions.SectionName))`
**To delete:** none

## Implementation Steps
1. Bridge csproj: thay `<Configurations>` bằng hai dòng như `HPRebar.Tests.csproj:7-8` (`Debug.R25;Debug.R26` + `$(Configurations);Release.R25;Release.R26`). Không thêm R23/R24/R27.
2. Thêm comment XML ngay trên `<Configurations>`: lý do loại R23/R24 (net48, không ALC, Roslyn không ILRepack) và R27 (chờ HPRebar xanh). Không nhắc phase/plan trong comment.
3. `.slnx`: đối chiếu khối bridge — 4 `BuildType` + 6 `Build Project="false"`; `HPRebar.McpBridge.Tests` (phase 5) copy y hệt khối `HPRebar.Tests:52-58`.
4. Contracts `PipeNaming.cs` (namespace `HPRebar.Mcp.Contracts`). Đơn vị test phase 5: `For(2026) == "hprebar-mcp-r2026"`.
5. Server `Models/BridgeOptions.cs` + bind trong `Program.cs`; `appsettings.json` thêm section `Bridge`. Validate `RevitVersion` trong `[2025, 2026]` bằng `builder.Services.AddOptions<BridgeOptions>().Bind(...).Validate(o => o.RevitVersion is 2025 or 2026)`.
6. Bridge: nơi tạo pipe (phase 2 `PipeListener`) lấy version từ `uiapp.Application.VersionNumber` (string) → `int.Parse` → `PipeNaming.For`. Ghi vào phase 2 step; phase này chỉ chốt contract.
7. Quy ước: mọi `#if REVIT…` trong bridge kèm `// Multi-version: <topic>` dòng trên. v1 kỳ vọng 0 block. Ghi vào `Application.cs`? Không — chỉ áp dụng khi phát sinh.
8. Build gate: `dotnet build HPRebar.slnx -c Debug.R25 -p:DeployAddin=false` và `-c Debug.R26`; `-c Debug.R27` và `-c Debug.R23` phải xanh (bridge skip). Lưu ý `Debug.R27` có thể fail sẵn vì BeamRebar (CLAUDE.md) — nếu vậy chỉ build bridge theo project path để chứng minh không phải lỗi mới: `dotnet build HPRebar/HPRebar.McpBridge/HPRebar.McpBridge.csproj -c Debug.R27` phải báo "configuration not found", không phải lỗi C#.

## Todo List
- [ ] Bridge `<Configurations>` 4 config + comment lý do
- [ ] `.slnx` mapping bridge đối chiếu
- [ ] `PipeNaming.cs` trong Contracts
- [ ] `BridgeOptions.RevitVersion` + validate + `appsettings.json`
- [ ] Build R25 + R26 xanh; R23 skip bridge

## Success Criteria
- `dotnet build HPRebar.slnx -c Debug.R25 -p:DeployAddin=false` → exit 0.
- `dotnet build HPRebar.slnx -c Debug.R26 -p:DeployAddin=false` → exit 0.
- `dotnet build HPRebar.slnx -c Debug.R23 -p:DeployAddin=false` → exit 0 và output không chứa `HPRebar.McpBridge ->`.
- `grep -rn "#if REVIT" HPRebar/HPRebar.McpBridge --include=*.cs` → 0 dòng, hoặc mỗi dòng có `// Multi-version:` ngay trên.
- `HPREBAR_MCP_Bridge__RevitVersion=2024 dotnet run --project HPRebar/HPRebar.Mcp.Server` → host fail validation lúc start (stderr), không phải im lặng.

## Risk Assessment
| Risk | L×I | Mitigation |
|---|---|---|
| User sau này muốn R24 → phải thêm net48 path, Roslyn xung đột | M×M | Ghi rõ trong csproj comment + `plan.md` Unresolved #3; nếu cần: ADR-03 alt 1 (compile trong server) là đường đi cho net48 |
| `Nice3point.Revit.Api.*` `2025.*` không có version khớp Roslyn net8 deps | L×M | Bước 8 build R25 sớm |
| Solution config R27 fail do BeamRebar → nhầm là lỗi bridge | H×L | Bước 8 build bridge theo project path để phân biệt |
| Env var override sai version → server nối pipe không tồn tại | M×L | Validate `[2025,2026]` + thông điệp `McpException` "bridge not connected … pipe hprebar-mcp-r2025" nêu tên pipe |

## Security Considerations
- Tên pipe cố định theo version, không nhận từ input AI → không thể ép server nối pipe lạ. Env override chỉ user máy đó đặt được.

## Next Steps
- Phase 2: `PipeListener` dùng `PipeNaming.For(version)`; `BridgeOptions` mở rộng timeouts/limits.
- Khi HPRebar xanh R27: mở phase phụ "Compatibility R27" cho bridge (net10 TFM, Roslyn version).
