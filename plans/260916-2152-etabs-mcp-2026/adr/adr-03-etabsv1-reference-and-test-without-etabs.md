# ADR-03 — Tham chiếu `ETABSv1.dll` từ thư mục cài (registry CLSID) + resolve lúc chạy + test khi máy KHÔNG cài ETABS

**Ngày:** 2026-09-16 · **Revised 2026-09-16 (red-team #9, #10, #14f):** bridge tests **cần** ETABS cài (không stub); seed compile-check chuyển sang server tests net10 với `Assert.SkipWhen`; bridge publish **folder**; open item `LocalServer32` đóng · **Status:** Proposed · **Owner:** HPEtabs
**Kế thừa:** [Navis ADR-03](../../260915-0824-navisworks-mcp-2026/adr/adr-03-navisworks-api-reference-and-test-without-navisworks.md) · `HPNavis/Directory.Build.props:13–24`
**Bằng chứng:** [evidence §E1, §E2, §E3, §E5](../research/evidence-on-machine-2026-09-16.md) · [researcher-02 §7](../research/researcher-02-mcpshared-seam-and-autocad-mirror.md) (snippet props dùng key `HKLM\SOFTWARE\Computers and Structures` — **sai**, không tồn tại: E1) · [red-team](../reports/red-team-2026-09-16.md) hàng 9, 10, 14f (fact-check: `LocalServer32` = path thuần, không quote/tham số; có subkey `LocalServer32\22.7.0.0`) · `HPAutoCad/HPAutoCad.Mcp.Server.Tests/SeedLibraryTests.cs:206–272` (mẫu wrapper + skip) · `McpShared/HPRebar.McpBridge.Core/Scripting/ScriptCompiler.cs:34–52` (Roslyn cần `Assembly.Location`)

## Context
- CSI không phát hành NuGet (E5). API: `C:\Program Files\Computers and Structures\ETABS 22\ETABSv1.dll` 2.10.0.0 (E1) — netstandard2.0, refs `netstandard` + `Microsoft.Win32.Registry 5.0.0.0` (inbox Windows), 0 `[ComImport]` (E2).
- Registry: **không** `HKLM\SOFTWARE\Computers and Structures\*` (E1). Có `HKLM\SOFTWARE\Classes\CLSID\{e4f6d00f-51a5-4d65-a09a-ba00fcbf1f82}\LocalServer32` (default) = `C:\Program Files\Computers and Structures\ETABS 22\ETABS.exe` — **giá trị thuần, không quote, không tham số** (red-team fact-check 2026-09-16); subkey `LocalServer32\22.7.0.0` cũng tồn tại (không dùng). Cùng key `Helper.GetPath(clsid)` dùng (E3).
- Ba nơi cần API: (1) `HPEtabs.McpBridge` + `HPEtabs.McpBridge.Tests` — **compile bắt buộc** (bridge tests `ProjectReference` bridge → không build khi thiếu DLL, `<Error>` §1); (2) `HPEtabs.Mcp.Server` + `HPEtabs.Mcp.Server.Tests` — **không bao giờ** `<Reference>`; seed compile-check ở đây tìm DLL **lúc test** → skip quan sát được (red-team #10).
- Bridge là tiến trình của ta (ADR-01) → tự resolve DLL lúc chạy; không redistribute (EULA `[chưa xác minh]`, mặc định không).

## Decision

### 1. `HPEtabs/Directory.Build.props` — một chỗ dò thư mục cài
```xml
<!-- HPEtabs/Directory.Build.props (kế hoạch) -->
<Project>
  <PropertyGroup>
    <EtabsMajor Condition="'$(EtabsMajor)' == ''">22</EtabsMajor>
    <!-- 1) override: -p:EtabsInstallDir=<dir> hoặc env HPETABS_ETABS_DIR -->
    <EtabsInstallDir Condition="'$(EtabsInstallDir)' == '' And '$(HPETABS_ETABS_DIR)' != ''">$(HPETABS_ETABS_DIR)</EtabsInstallDir>
    <!-- 2) COM LocalServer32 của CSI.ETABS.API.ETABSObject (verified 2026-09-16): default value = full path ETABS.exe, plain -->
    <_EtabsExePath Condition="'$(EtabsInstallDir)' == ''">$([MSBuild]::GetRegistryValueFromView('HKEY_LOCAL_MACHINE\SOFTWARE\Classes\CLSID\{e4f6d00f-51a5-4d65-a09a-ba00fcbf1f82}\LocalServer32', '', null, RegistryView.Registry64))</_EtabsExePath>
    <EtabsInstallDir Condition="'$(EtabsInstallDir)' == '' And '$(_EtabsExePath)' != ''">$([System.IO.Path]::GetDirectoryName('$(_EtabsExePath)'))</EtabsInstallDir>
    <!-- 3) mặc định theo ProgramW6432, không hard-code ổ -->
    <EtabsInstallDir Condition="'$(EtabsInstallDir)' == ''">$(ProgramW6432)\Computers and Structures\ETABS $(EtabsMajor)\</EtabsInstallDir>
    <EtabsInstallDir Condition="!HasTrailingSlash('$(EtabsInstallDir)')">$(EtabsInstallDir)\</EtabsInstallDir>
    <EtabsApiAvailable>false</EtabsApiAvailable>
    <EtabsApiAvailable Condition="Exists('$(EtabsInstallDir)ETABSv1.dll')">true</EtabsApiAvailable>
  </PropertyGroup>
</Project>
```
- Bridge + bridge tests: `<Reference Include="ETABSv1"><HintPath>$(EtabsInstallDir)ETABSv1.dll</HintPath><Private>false</Private></Reference>` (bridge) + `<Error Condition="'$(EtabsApiAvailable)' != 'true'" Text="ETABS 22 API not found at '$(EtabsInstallDir)'. HPEtabs.McpBridge and HPEtabs.McpBridge.Tests need ETABS 22 installed; pass -p:EtabsInstallDir=<dir> or set HPETABS_ETABS_DIR. HPEtabs.Mcp.Server(.Tests) build without it."/>` (`BeforeTargets="ResolveAssemblyReferences"`).
- Multi-version: `EtabsMajor` property; MVP 22 (ADR-05).

### 2. Runtime resolution — bridge nạp `ETABSv1.dll` từ thư mục cài
- `Private=false`; `EtabsAssemblyResolver` đăng ký `AssemblyLoadContext.Default.Resolving`: tên `ETABSv1` → env `HPETABS_ETABS_DIR` hoặc key CLSID `LocalServer32` (E3) → `LoadFromAssemblyPath`; log path + FileVersion. Không có → cửa sổ "ETABS 22 not installed (CLSID LocalServer32 missing)", self-check FAIL, app không crash.
- **Load order:** đăng ký resolver trước mọi method chạm `ETABSv1.*` (JIT bind) — `EtabsAttachment` class riêng; `BridgeEntry.Start` gọi resolver đầu tiên.
- `ScriptingSelfCheck`: (a) **assert `typeof(EtabsScriptGlobals).Assembly.Location` khác rỗng** (single-file làm rỗng → Roslyn `CantCreateReferenceToAssemblyWithoutLocation`, `ScriptCompiler.cs:34–52`; red-team #9) → publish **folder** (ADR-05 §3); (b) log path `ETABSv1.dll` + `new Helper().GetOAPIVersionNumber()` (CHM › "cHelper.GetOAPIVersionNumber Method"); (c) sau attach: compile + chạy `return sapModel.GetModelFilename();` tier R → `MCP scripting self-check OK`; chưa attach → "skipped: not attached" (compile `return args.Int("x", 1) + 1;` vẫn chạy).
- `ScriptCompiler` references: `typeof(object)`, `Enumerable`, `List<>`, `ScriptArgs`, `typeof(cSapModel).Assembly`; imports `HostScriptContracts.EtabsImports` (ADR-04 §6). Không `CSiAPIv1.dll`.

### 3. Server exe + server tests: không `<Reference>` ETABS; seed compile-check tìm DLL lúc test
- `HPEtabs.Mcp.Server`, `HPEtabs.Mcp.Server.Tests` chỉ `ProjectReference ../McpShared/*` (mirror `HPNavis/HPNavis.Mcp.Server.Tests/HPNavis.Mcp.Server.Tests.csproj:26–33`, link `FakeRevitExecutor.cs`). Build/test **mọi máy**.
- **`SeedLibraryCompileTests` (server tests, net10)** — mẫu AutoCAD `SeedLibraryTests.cs:206–272`: wrapper class tổng hợp mirror **đúng** `HostScriptContracts.EtabsImports` + `EtabsGlobals` (khai `cSapModel sapModel; cOAPI etabs; ScriptUnits units; CancellationToken ct; Action<string> log; Action<int,int,string> progress; ScriptArgs args;`) + thân seed trong `Run()`; references = `TRUSTED_PLATFORM_ASSEMBLIES` net10 + `typeof(ScriptArgs).Assembly` + **`MetadataReference.CreateFromFile(etabsDll)`** với `etabsDll` = `EtabsApiLocator.Find()` (env `HPETABS_ETABS_DIR` → CLSID `LocalServer32` → `%ProgramW6432%`) → `Assert.SkipWhen(etabsDll is null, "ETABS 22 not installed")`. SKIP quan sát được trên máy không ETABS; PASS trên máy dev. Ngoài compile: chạy `ScriptGuard.Check(code, GuardProfile.Etabs)` = 0 violation; tier theo fixture (regex/fixture đọc từ file trong repo) khớp `tool.json.transaction`/`tags`.
- Khác bridge runtime: BCL net10 vs net8 của bridge — chấp nhận (cùng cảnh AutoCAD); `EtabsTierAnalyzer` thật chạy trong bridge tests (§4).

### 4. Bridge tests — **cần ETABS cài**, không stub
- `HPEtabs.McpBridge.Tests` (net8.0-windows, xunit v3 3.1.0, MTP): `ProjectReference` bridge + Core; cùng `<Error>` §1 khi thiếu DLL — như `HPNavis.McpBridge.Tests`; README nói thẳng: máy không có ETABS chỉ build/test server + server tests.
- Nội dung (phase 2): tier analyzer **bind vào `ETABSv1.dll` thật**, fixture completeness, path policy, snapshot manager (thư mục temp, `Func<int> save` giả), fingerprint diff, units policy, error mapping, serializer, guard collision (member ETABSv1 ∩ base deny), guard cấm `HPEtabs.McpBridge.*`/`McpBridgeHost.Current`, label traversal. **0 skip** trên máy dev; không chạy ETABS.
- Bỏ stub `ETABSv1` in-memory (red-team #10): lệch tên thật, và project không build khi thiếu DLL nên "skip" không quan sát được.

### 5. Không đưa DLL CSI vào repo
Không commit `ETABSv1.dll`/`CSiAPIv1.dll`/`.tlb`; `.gitignore` `*.dll` ngoài `bin/obj`; fixture `etabs-oapi-tiers.txt` (ADR-02 §1) là dữ liệu dẫn xuất — commit được.

## Alternatives rejected
- NuGet cộng đồng; copy DLL vào repo; Uninstall `InstallLocation` làm nguồn chính; `CSiAPIv1.dll` — như bản trước.
- **Stub ETABSv1 + `Assert.Skip` trong bridge tests** (bản sáng): không build được khi thiếu DLL → skip vô nghĩa; stub lệch tên (red-team #10) — loại.
- **Seed compile-check trong bridge tests** (như Navis): cùng lý do; Navis chấp nhận "không build được" vì không có test nào khác cần chạy không-host; ở đây user muốn server tests chạy mọi máy → chuyển sang server tests.

## Consequences
- Máy không ETABS: `dotnet build HPEtabs/HPEtabs.slnx` fail ở bridge + bridge tests với 1 lỗi rõ; `dotnet test HPEtabs/HPEtabs.Mcp.Server.Tests` chạy, seed compile-check **SKIP** có lý do.
- `inspect_type` phản chiếu `ETABSv1` đã nạp trong bridge.
- Phase 1 tạo props + resolver + self-check (+ `EtabsApiLocator` dùng chung server tests); phase 2 tạo bridge tests; phase 3 thêm `SeedLibraryCompileTests` vào server tests.

## Open items `[chưa xác minh]`
- `Microsoft.Win32.Registry 5.0.0.0` bind inbox trên net8 — self-check xác nhận.
- EULA CSI về redistribution — mặc định không.
- ~~Định dạng `LocalServer32`~~ — **đóng**: path thuần (red-team fact-check).

### Kết quả spike 2026-09-17 ([reports/phase-01-spike.md](../reports/phase-01-spike.md))
- **E16 verified:** `ETABSv1.dll` resolve từ `C:\Program Files\Computers and Structures\ETABS 22\` (2.10.0.0) qua CLSID `LocalServer32`; `bin/` và publish folder không có DLL; `Assembly.Location` khác rỗng trên publish folder (bridge publish **folder**, server single-file).
- **E17 verified:** Roslyn compile script `ETABSv1` trong bridge process qua stdio; `Helper` implement `cHelper` **explicit** (gọi qua interface).
- `HPEtabs.McpBridge.Tests` 26 test (cần ETABS cài; module initializer cài resolver vì `ETABSv1.dll` không copy vào bin test); `HPEtabs.Mcp.Server.Tests` 17 (không cần).
