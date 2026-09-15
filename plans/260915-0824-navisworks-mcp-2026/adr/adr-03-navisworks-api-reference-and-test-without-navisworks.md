# ADR-03 — Tham chiếu `Autodesk.Navisworks.*.dll` từ thư mục cài và chiến lược test khi máy KHÔNG cài Navisworks

**Ngày:** 2026-09-15 · **Revised 2026-09-15 (red-team):** seed compile-check chuyển sang project test **net48** (`HPNavis.McpBridge.Tests`) — cùng runtime với bridge, Core resolve `net48` tự động, `typeof(object).Assembly` = mscorlib 4.8; bỏ `NavisApiLocator`/skip-path/CI script; test server net10 chỉ giữ kiểm cấu trúc JSON · **Status:** Proposed · **Owner:** HPNavis
**Bằng chứng:** [evidence §E1, E2, E4, E11](../research/evidence-on-machine-2026-09-15.md); registry dump 2026-09-15 (dưới)

## Context

- Không có NuGet chính thức của Autodesk cho Navisworks API (khác AutoCAD `AutoCAD.NET` và Revit `Nice3point.Revit.Api.*`). **[chưa xác minh]** có gói cộng đồng nào đủ tin cậy cho 23.0 — plan **không** phụ thuộc gói không chính thức (rủi ro license/khớp version).
- API có sẵn trong thư mục cài, cùng `.xml` docs cho `Api`, `Automation`, `ComApi`, `Controls`, `Resolver`; **không có** `.xml` cho `Clash`, `Timeliner`, `Takeoff` (E4/E8 — reflection vẫn đọc được member).
- Registry trên máy (Windows PowerShell, 2026-09-15):
  - `HKLM\SOFTWARE\Autodesk\Navisworks API Runtime\23\Navisworks Manage :: Path = C:\Program Files\Autodesk\Navisworks Manage 2026\`, `ProductVersion = 23.0.1432.76`, `AssemblyVersion = 23.0.1432.76`
  - `HKLM\SOFTWARE\Autodesk\Navisworks Manage\23.0\Location :: Path = C:\Program Files\Autodesk\Navisworks Manage 2026\`
- Ba nơi cần API: (1) `HPNavis.McpBridge` — **compile bắt buộc** (plugin); (2) `HPNavis.Mcp.Server` — **không bao giờ** tham chiếu (giống Revit/AutoCAD exe); (3) seed compile-check — AutoCAD làm trong test net10 với DLL từ NuGet cache (`HPAutoCad/HPAutoCad.Mcp.Server.Tests/SeedLibraryTests.cs`); với Navisworks phải là **net48** (§3).
- Ràng buộc: HintPath qua property dò thư mục cài, `Private=false`, không hard-code đường dẫn tuyệt đối trong repo.

## Decision

### 1. `Directory.Build.props` của `HPNavis/` — một chỗ dò thư mục cài

```xml
<!-- HPNavis/Directory.Build.props (kế hoạch) -->
<Project>
  <PropertyGroup>
    <NavisworksMajor>23</NavisworksMajor>            <!-- 2026 = 23.0 -->
    <NavisworksYear>2026</NavisworksYear>
    <!-- 1) override từ CLI/CI: -p:NavisworksInstallDir=... hoặc env HPNAVIS_NAVISWORKS_DIR -->
    <NavisworksInstallDir Condition="'$(NavisworksInstallDir)' == '' And '$(HPNAVIS_NAVISWORKS_DIR)' != ''">$(HPNAVIS_NAVISWORKS_DIR)</NavisworksInstallDir>
    <!-- 2) registry key Autodesk ghi khi cài (verified 2026-09-15) -->
    <NavisworksInstallDir Condition="'$(NavisworksInstallDir)' == ''">$([MSBuild]::GetRegistryValueFromView('HKEY_LOCAL_MACHINE\SOFTWARE\Autodesk\Navisworks API Runtime\$(NavisworksMajor)\Navisworks Manage', 'Path', null, RegistryView.Registry64))</NavisworksInstallDir>
    <!-- 3) mặc định theo ProgramFiles, không hard-code ổ -->
    <NavisworksInstallDir Condition="'$(NavisworksInstallDir)' == ''">$(ProgramW6432)\Autodesk\Navisworks Manage $(NavisworksYear)\</NavisworksInstallDir>
    <NavisworksInstallDir Condition="!HasTrailingSlash('$(NavisworksInstallDir)')">$(NavisworksInstallDir)\</NavisworksInstallDir>
    <NavisworksApiAvailable>false</NavisworksApiAvailable>
    <NavisworksApiAvailable Condition="Exists('$(NavisworksInstallDir)Autodesk.Navisworks.Api.dll')">true</NavisworksApiAvailable>
  </PropertyGroup>
</Project>
```

- Bridge project tham chiếu **đúng 4 DLL** cần cho MVP, `Private=false` (Roamer cung cấp lúc chạy; ghi đè version qua `bindingRedirect` 23.0.x của chính Roamer — E2):
  ```xml
  <Reference Include="Autodesk.Navisworks.Api"><HintPath>$(NavisworksInstallDir)Autodesk.Navisworks.Api.dll</HintPath><Private>false</Private></Reference>
  <Reference Include="Autodesk.Navisworks.Clash"><HintPath>$(NavisworksInstallDir)Autodesk.Navisworks.Clash.dll</HintPath><Private>false</Private></Reference>
  <Reference Include="Autodesk.Navisworks.Timeliner"><HintPath>$(NavisworksInstallDir)Autodesk.Navisworks.Timeliner.dll</HintPath><Private>false</Private></Reference>
  <Reference Include="Autodesk.Navisworks.ComApi"><HintPath>$(NavisworksInstallDir)Autodesk.Navisworks.ComApi.dll</HintPath><Private>false</Private></Reference>
  ```
  `Interop.ComApi` chỉ thêm khi seed ComApi xuất hiện (không trong MVP). `Automation`, `Controls`, `Resolver`, `Takeoff`: không tham chiếu.
- Bridge project: `<Error Condition="'$(NavisworksApiAvailable)' != 'true'" Text="Navisworks Manage 2026 API not found at '$(NavisworksInstallDir)'. Install Navisworks, or pass -p:NavisworksInstallDir=<dir> / set HPNAVIS_NAVISWORKS_DIR."/>` trong target `BeforeTargets="ResolveAssemblyReferences"` — lỗi rõ, không phải 200 lỗi CS0246.
- **Multi-version:** `NavisworksMajor`/`NavisworksYear` là property → khi cần 2025 (22.0) thêm `Configurations` `Debug.N25` giống mẫu `R##` của Revit; MVP chỉ 2026 (ADR-05 §4). Không dùng NuGet nên không có version-pin package; pin bằng registry key `API Runtime\23`.

### 2. Server exe và test engine: không tham chiếu Navisworks

`HPNavis.Mcp.Server` + `HPNavis.Mcp.Server.Tests` (profile/pipe/registry tests) tham chiếu chỉ `../McpShared/*` — giống `HPAutoCad.Mcp.Server.csproj:23`. Build/test **luôn** chạy trên máy không cài Navisworks.

### 3. Seed compile-check — trong project test **net48**, cùng runtime với bridge

- **Vấn đề với mẫu AutoCAD:** `HPAutoCad.Mcp.Server.Tests` (net10) compile seed bằng `TRUSTED_PLATFORM_ASSEMBLIES` của runtime net10 + `typeof(ScriptArgs).Assembly.Location` (`SeedLibraryTests.cs:223–232`) — với Navisworks đó là BCL **sai runtime** (script dùng `Random.Shared`/`Span` sẽ pass test, fail trong Roamer), và một project net10 không `ProjectReference` được asset `net48` của Core.
- **Quyết định:** `SeedLibraryTests` sống trong **`HPNavis/HPNavis.McpBridge.Tests` (net48, xunit)** — project này cũng chứa test thuần của bridge (`NavisHeavyGate`, `NavisChangeCounter`, clamp). Ở net48: `typeof(object).Assembly` **là** `mscorlib` 4.8, `ProjectReference` Core resolve `net48` tự động, `Directory.Build.props` cung cấp cùng `HintPath` `Autodesk.Navisworks.Api/Clash/Timeliner` (`Private=false`) như bridge → wrapper = **đúng** môi trường bridge (imports `HostScriptContracts.NavisImports`, globals `NavisScriptGlobals` **lấy từ bridge project qua `ProjectReference`** — không khai lại, hết drift; guard `GuardProfile.Navis` + pre-pass `NavisHeavyGate` heavy OFF cho seed thường, heavy ON cho seed `tags:["heavy"]`, và khẳng định seed heavy **bị** chặn khi heavy OFF).
- **Máy không cài Navisworks:** project net48 này **không build** (cùng `<Error>` rõ ràng của §1 như bridge) — đó là câu trả lời trung thực cho "compile seed khi không có Navisworks": **không thể** nếu không có DLL Autodesk; không skip mềm, không NuGet trái EULA. `HPNavis.Mcp.Server.Tests` (net10) không cần API và giữ **một** test luôn chạy: `SeedLibrary_JsonAndStructure_AreValid` (tool.json schema, examples.json, `_seeds.json` checksum, tên/category ∈ `NavisHostProfile.Categories`, `tags` chứa `heavy` ⇔ code gọi W2 — kiểm bằng regex, không cần API).
- Bỏ: `NavisApiLocator.cs`, skip-with-reason, `tools/ci-build-without-navisworks.ps1` (repo không có CI; ba MCP đều verify trên máy dev). README ghi: máy không có Navisworks chỉ build/test `HPNavis.Mcp.Server` + `HPNavis.Mcp.Server.Tests`.

### 4. Không đưa DLL/ref-asm của Autodesk vào repo

Không commit `Autodesk.Navisworks.*.dll`, không sinh reference assembly (Refasmer) vào repo — EULA Autodesk không cho tái phân phối; `.gitignore` của `HPNavis/` thêm `*.dll` ngoài `bin/obj` như phòng hờ.

## Alternatives rejected

- **NuGet cộng đồng cho Navisworks API:** không rõ nguồn/EULA, không khớp `23.0.1432.76`; tiêu chuẩn repo là "verified by" — loại cho MVP; có thể xem lại nếu Autodesk/ Nice3point phát hành gói chính thức.
- **Hard-code `C:\Program Files\Autodesk\Navisworks Manage 2026\`:** vi phạm ràng buộc; registry + env + ProgramW6432 fallback đủ.
- **Copy API vào `HPNavis/lib/`:** vi phạm EULA, phình repo 8 MB.
- **Skip mềm seed compile trong project net10 + `NavisApiLocator`:** compile với BCL net10 là sai runtime; thêm plumbing cho CI không tồn tại. Loại (red-team 2026-09-15).

## Consequences

- Máy không có Navisworks: `dotnet build HPNavis/HPNavis.slnx` **fail ở bridge và bridge-tests** với thông điệp rõ → build/test riêng `HPNavis/HPNavis.Mcp.Server` + `HPNavis/HPNavis.Mcp.Server.Tests`, hoặc set `HPNAVIS_NAVISWORKS_DIR`. Ghi vào `HPNavis/README.md`.
- `inspect_type` (TypeInspector) phản chiếu `Autodesk.Navisworks.Api/Clash/Timeliner/ComApi` đã nạp trong Roamer — không cần XML docs.
- Phase 0 không đụng ADR này; phase 1 tạo `Directory.Build.props`; phase 2 tạo `HPNavis.McpBridge.Tests`; phase 4 thêm `SeedLibraryTests` vào đó.
