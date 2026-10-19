# Handoff Report: Survey of Clean Code Governance, Solution Architecture, and Test Infrastructure for HPBim

**Date:** 2026-10-05T23:35:00Z  
**Author:** `teamwork_preview_explorer`  
**Working Directory:** `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_survey_cleancode\`  
**Target Deliverable:** `HPBim/` (Independent C# .NET solution `HPBim.slnx`)  
**Mission:** Comprehensive technical survey and architectural blueprint for the new high-level deliverable `HPBim`.

---

## 1. Observation

### 1.1 Clean Code Governance & Mandatory Quality Metrics

Direct observation from `docs/clean-code/HP_CLEAN_CODE_CORE.md`, `docs/clean-code/host-appendix/revit.md`, and `docs/architecture/ARCHITECTURE.md`:

1. **Governance Scope & Binding Status (`HP_CLEAN_CODE_CORE.md:1-6`):**
   - Applies to every new or changed C# line in MCP folders, including `HPBim/`.
   - Prioritization ladder (§1): `simplicity > readability > cohesion > explicit dependencies > testability > formal pattern compliance`.
   - No generic god/junk classes: `*Manager`, `*Helper`, `*Utils`, `*Processor`, `*Data`, `*Info` are strictly forbidden unless no more precise domain name exists (§1, PCC-031, PCC-032, PCC-190).
   - "AI-generated code is unreviewed code until the checklist passes; compiling, looking tidy or using patterns proves nothing" (§1, PCC-017, PCC-271).

2. **Mandatory Size & Complexity Limits:**
   - **Methods (§3 M3, PCC-067, PCC-093, PCC-103):** Review trigger > 20 lines; > 50 lines must be justified in review; > 100 lines must be split unless a flat, cohesive table/switch. Maximum parameters <= 4 (§3 M4). No boolean parameters that switch behaviour (§3 M5).
   - **Classes (§5 C2, PCC-115, PCC-184, PCC-185):** File > 300 lines, ViewModel > 250 lines triggers review; classes must have a single responsibility (§5 C1).
   - **Bracing & Line Length (§4 FM2):** Always brace blocks (including single-line `if`). Lines past ~120 chars trigger review.
   - **File-scoped Namespaces (§4 FM4):** Always file-scoped namespaces matching folder structure.

3. **Three Blocking Runtime Rules (§13, Q-B1..Q-B3):**
   - **Q-B1 (Error - blocks draft):** No commented-out code (CM3).
   - **Q-B2 (Error - blocks draft):** No empty `catch` block without an explanatory reason comment.
   - **Q-B3 (Error - blocks draft):** Script / unit body <= 300 lines (trimmed text).
   - **Warnings (Q-W1..Q-W5):** Method/block > 50 lines (Q-W1), nesting > 3 control-flow levels (Q-W2), vague identifier `data, tmp, temp, obj, res, val, foo, bar` (Q-W3), `bool` parameter on method (Q-W4), swallowing `catch (Exception)` without rethrow, return, or reporting (Q-W5).

4. **Host-Appendix: Revit & Domain Isolation (`docs/clean-code/host-appendix/revit.md`, `REVITADDINAI_CLEAN_CODE_STANDARD.md`):**
   - Rule R5: Core libraries (`HPBim.Core`) must have **0 references** to `Autodesk.*`. All dimensions in Core are in **millimeters (mm)** and angles in degrees. Conversion to feet occurs solely at the adapter boundary via explicit unit conversion (no bare `304.8` literals).
   - Rule R1-R3: Revit API runs only on Revit's main thread; transactions inside `TransactionGroup` with explicit failure policy.
   - Rule RV2: Named Pipe execution supports `auto` (one transaction in group), `manual`, `none` (read-only), and `dryRun: true` (rolls back transaction group).

### 1.2 Repository Conventions & Existing Solution Layouts

Inspection of existing solutions (`HPEtabs.slnx`, `HPExcel.slnx`, `HPRobot.slnx`, `McpShared.slnx`, `HPAutoCad.slnx`):

1. **Solution Format (`.slnx`):**
   Every MCP ecosystem uses the new XML `.slnx` format (pins Configurations `Debug` and `Release`), includes solution items (`global.json`, `Directory.Build.props`, `README.md`), its own projects, and a `/Shared/` folder with relative `ProjectReference`s pointing to `../McpShared/`.
   ```xml
   <Solution>
     <Configurations>
       <BuildType Name="Debug" />
       <BuildType Name="Release" />
     </Configurations>
     <Folder Name="/Solution Items/">
       <File Path="global.json" />
       <File Path="Directory.Build.props" />
       <File Path="README.md" />
     </Folder>
     ...
     <Folder Name="/Shared/">
       <Project Path="../McpShared/HPRebar.Mcp.Contracts/HPRebar.Mcp.Contracts.csproj" />
       <Project Path="../McpShared/HPRebar.McpBridge.Core/HPRebar.McpBridge.Core.csproj" />
       <Project Path="../McpShared/HPRebar.Mcp.Server.Core/HPRebar.Mcp.Server.Core.csproj" />
     </Folder>
   </Solution>
   ```

2. **Global SDK Configuration (`global.json`):**
   Identical across all 10 MCP folders:
   ```json
   {
     "sdk": {
       "version": "10.0.300",
       "rollForward": "latestMinor",
       "allowPrerelease": true
     },
     "test": {
       "runner": "Microsoft.Testing.Platform"
     }
   }
   ```

3. **Directory.Build.props Standard Properties:**
   From `HPExcel/Directory.Build.props` and other deliverables:
   ```xml
   <PropertyGroup>
     <LangVersion>latest</LangVersion>
     <Nullable>enable</Nullable>
     <ImplicitUsings>enable</ImplicitUsings>
     <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
     <NoWarn>$(NoWarn);xUnit1051;NU1701</NoWarn>
   </PropertyGroup>
   ```

4. **Target Frameworks & Polyfill for NetStandard2.0:**
   - In `HPRebar.Core/HPRebar.Core.csproj`: Target is `netstandard2.0`, using `<PackageReference Include="Polyfill" Version="11.0.1" PrivateAssets="all"/>` to unlock modern C# features (`record`, `init`, `required`, pattern matching) while maintaining universal framework compatibility.
   - Test projects (`HPRebar.Core.Tests.csproj`): Target `net8.0` or `net10.0`, `OutputType=Exe`, `UseMicrosoftTestingPlatformRunner=true`, referencing `xunit.v3` (v3.1.0) and `xunit.runner.visualstudio` (v3.1.5).

### 1.3 Dependency Constraints & Named Pipe Protocol

1. **Isolation Rule:**
   - AGENTS.md: "The only permitted dependency direction is MCP folder -> McpShared/; the MCP folders never reference each other".
   - `HPBim/` must ONLY reference `McpShared/`. Zero cross-references to `HPRebar/`, `HPAutoCad/`, `HPEtabs/`, etc.
   - `HPBim.Core` has **0 references** to `Autodesk.Revit.*`.

2. **Named Pipe & Revit MCP Communication Infrastructure:**
   - Pipe Name: `PipeNaming.For(2026)` -> `"hprebar-mcp-r2026"` (`\\.\pipe\hprebar-mcp-r2026`), hosted by `HPRebar.McpBridge` inside Revit 2026.
   - Wire contracts in `HPRebar.Mcp.Contracts.Messages`:
     - `ExecuteRequest(string Code, string Transaction = "auto", bool DryRun = false, int TimeoutSeconds = 30, string? Label = null, JsonElement? Args = null)`
     - `ExecuteResult`: `IsError`, `Value`, `ValueType`, `Message`, `Logs`, `Diagnostics`, `Changed` (counts of Added/Modified/Deleted), `RolledBack`, `TimedOut`, `DurationMs`.
   - Host script environment in Revit (`HostScriptContracts.RevitImports` & `RevitGlobals`):
     - Imports: `System`, `System.Linq`, `System.Collections.Generic`, `Autodesk.Revit.DB`, `Autodesk.Revit.UI`, `Autodesk.Revit.DB.Structure`, `HPRebar.McpBridge.Core.Scripting`.
     - Globals: `doc`, `uidoc`, `app`, `uiapp`, `ct`, `log`, `progress`, `args`.
   - Client implementation: `RevitBridgeClient` in `McpShared/HPRebar.Mcp.Server.Core/Services/RevitBridgeClient.cs` implements `IRevitBridgeClient` and provides NDJSON framing, asynchronous request/response correlation, cancellation token handling, and automatic reconnection.

### 1.4 CLI Conventions & Python Skill Assets

1. **CLI Parsing Convention in McpShared:**
   - `McpShared/HPRebar.Mcp.Server.Core/Registry/RegistryCli.cs` demonstrates a dependency-free, robust CLI parser using simple string helpers (`Option(name, fallback)`, `Flag(name)`, `Positional(index)`).
   - This avoids bulky external packages while maintaining clean, predictable argument parsing.

2. **Domain Source: `vn-autocad-skill-main`:**
   - `layout.py` & `model.py` (Benchmark NhaPho 5x18m):
     - 5 Levels: Tret (+0.150m), Lau 1 (+3.800m), Lau 2 (+7.200m), San thuong (+10.600m), Mai che thang (+13.400m).
     - Grids: X1-X5 (0, 5500, 9500, 13000, 18000 mm), YA-YB (0, 5000 mm).
     - Columns: 10 concrete columns 220x220 mm at all grid intersections.
     - Beams: Main beam D1 (200x400), secondary beam D2/D3 (200x300), ground tie beam GM (250x400).
     - Slabs: S1..S4 (100mm thickness), void opening (9500..13000 mm).
     - Walls: Exterior 220mm, interior partitions 110mm with opening cutouts for Doors D1..D6 and Windows S1..S3.
     - Rooms: Closed polygonal boundaries for Gara, Khach, WC1, Bep, Ban cong, Ngu 1, Ngu 2, WC2, Phong tho, Ngu 3, WC3, San phoi.
     - MEP: Sanitary fixtures (bon cau, lavabo, voi sen, pheu thu) and pipe runs.
   - `standards.py`: TCVN wall thicknesses (220/110), column sizes (220x220, 220x300), cover rules (mong: 50mm, dam/cot: 25mm, san: 15mm), rebar density, bar weights.
   - `qc.py`, `qc_geom.py`, `qc_text.py`: Clash detection, bounding box containment, text/element intersection.
   - `estimate.py`, `build_dutoan.py`: 4 BOQ sections (Ngam & Mong, Khung BTCT than nha, Xay to hoan thien, He thong MEP).
   - `agents/*.md`: 5 specialized AI prompts (`kien-truc-su.md`, `ky-su-ket-cau.md`, `ky-su-co-dien.md`, `kiem-soat-ban-ve.md`, `ky-su-du-toan.md`).

---

## 2. Logic Chain

1. **Why `HPBim/` must be an independent deliverable and solution:**
   - AGENTS.md mandates strict isolation between MCP folders. `HPBim` is a top-level deliverable (like `HPAutoCad`, `HPEtabs`, `HPExcel`).
   - By creating `HPBim/HPBim.slnx`, `HPBim/global.json`, and `HPBim/Directory.Build.props`, `HPBim` maintains its own clean build lifecycle, referencing `../McpShared/` via project references without any coupling to `HPRebar/` or other host folders.

2. **Why `HPBim.Core` targets `netstandard2.0`:**
   - Requirements explicitly specify `HPBim.Core` as `netstandard2.0` with zero references to `Autodesk.Revit.*`.
   - Following `HPRebar.Core`'s proven pattern, adding `<PackageReference Include="Polyfill" Version="11.0.1" PrivateAssets="all"/>` allows C# 12/13 modern features (records, init-only setters, required properties) while remaining portable to any .NET runtime (net48, net8, net10).
   - This ensures all geometric algorithms, polygon area computations, TCVN rules, and QC engines can be tested in milliseconds without launching Revit.

3. **Why `HPBim.Client` targets `net8.0;net10.0` and how it interacts with Revit:**
   - Revit 2026 runs on .NET 8, while CLI and host tools in this repo run on .NET 10. Multi-targeting `net8.0;net10.0` ensures seamless compilation for both CLI execution and direct integration.
   - Rather than reimplementing named pipe communication, `HPBim.Client` leverages `McpShared/HPRebar.Mcp.Contracts` and `RevitBridgeClient` from `McpShared/HPRebar.Mcp.Server.Core` (or a dedicated thin pipe dispatcher) connecting to `\\.\pipe\hprebar-mcp-r2026`.
   - The generation flow follows strict BIM dependency hierarchy:
     $$\text{Levels} \longrightarrow \text{Grids} \longrightarrow \text{Columns/Beams/Floors} \longrightarrow \text{Walls} \longrightarrow \text{Doors/Windows} \longrightarrow \text{Rooms} \longrightarrow \text{MEP}$$
   - Element generation sends C# Roslyn scripts via `ExecuteRequest` to the Revit MCP bridge, with `DryRun = true` safely wrapping operations in a rolled-back `TransactionGroup`.
   - Missing family types (e.g., Column 220x220, Wall 220) are automatically detected and duplicated from existing system types with proper parameter assignments.

4. **Why `HPBim.Cli` provides 4 discrete commands:**
   - `validate`: Runs purely in `HPBim.Core` without connecting to Revit, giving immediate feedback on model syntax, geometric validity, clashes, and TCVN compliance.
   - `generate`: Orchestrates Revit creation through `HPBim.Client` over the named pipe, supporting `--dry-run` for non-destructive pre-flight verification.
   - `verify`: Queries Revit element counts via pipe scripts and validates that actual created BIM elements match 100% of the input JSON specification.
   - `estimate`: Generates the 4-part BOQ Excel/JSON report from the model definition.

5. **Why and how Agent Prompts must be packaged:**
   - Antigravity and Claude Code coding agents require dedicated domain instructions.
   - Porting the 5 Vietnamese construction roles (`kien-truc-su.md`, `ky-su-ket-cau.md`, `ky-su-co-dien.md`, `kiem-soat-ban-ve.md`, `ky-su-du-toan.md`) plus an Orchestrator role into `HPBim/agents/` enables autonomous multi-agent BIM design, validation, generation, and verification through the CLI.

---

## 3. Caveats

1. **Revit MCP Bridge Availability:**
   - Offline validation (`hpbim validate`) and unit tests (`HPBim.Core.Tests`) do NOT require Revit.
   - Live generation (`hpbim generate`) and model verification (`hpbim verify`) require Revit 2026 to be open with the `HPRebar.McpBridge` add-in running and listening on `\\.\pipe\hprebar-mcp-r2026`. If Revit is not running, `BridgeUnavailableException` must be caught and reported gracefully.
2. **Revit In-Process Family Dependencies:**
   - Creating doors and windows requires suitable door/window family symbols in the active Revit project template (or standard architectural template). If a family is missing, the client script should report clear diagnostics or fall back to available hosted symbols.
3. **No Cross-Referencing Rule:**
   - Under no circumstances should `HPBim` reference `HPRebar/` projects directly. All communication with Revit happens exclusively across the Named Pipe boundary.

---

## 4. Conclusion & Solution Blueprint

The proposed structure for `HPBim/` strictly complies with all Clean Code, architectural, and repository standards:

### 4.1 Solution Layout
```
HPBim/
├── global.json                      # .NET SDK 10.0.300, MTP test runner
├── Directory.Build.props            # Nullable, TreatWarningsAsErrors, LangVersion
├── HPBim.slnx                       # Solution in XML format
├── README.md                        # Documentation
├── agents/                          # Packaged AI Agent prompts
│   ├── kien-truc-su.md              # Architectural agent
│   ├── ky-su-ket-cau.md             # Structural agent
│   ├── ky-su-co-dien.md             # MEP agent
│   ├── kiem-soat-ban-ve.md          # QC/Audit agent
│   ├── ky-su-du-toan.md             # BOQ Estimator agent
│   └── dieu-phoi-bim.md             # Lead BIM Orchestrator agent
├── src/
│   ├── HPBim.Core/                  # netstandard2.0 (Zero Revit references)
│   │   ├── HPBim.Core.csproj        # Polyfill 11.0.1
│   │   ├── Geometry/                # Point2D, Point3D, BoundingBox2D, Polygon2D
│   │   ├── Schema/                  # BimModel, LevelDef, AxisGridDef, ColumnDef, WallDef, BeamDef, SlabDef, OpeningDef, RoomDef, MepDef
│   │   ├── Tcvn/                    # TcvnStandards, ConcreteCoverRules, RebarRules
│   │   ├── Qc/                      # QcEngine, ClashDetector, SpatialValidator
│   │   └── Estimate/                # BoqCalculator, BoqExport
│   ├── HPBim.Client/                # net8.0;net10.0 (Connects to Revit Pipe)
│   │   ├── HPBim.Client.csproj      # References HPBim.Core & McpShared
│   │   ├── Pipe/                    # PipeDispatcher, RevitConnection
│   │   ├── Builders/                # LevelBuilder, GridBuilder, StructureBuilder, WallBuilder, OpeningBuilder, RoomBuilder, MepBuilder
│   │   └── Verification/            # ModelVerifier
│   └── HPBim.Cli/                   # net10.0 (Console Executable)
│       ├── HPBim.Cli.csproj
│       ├── Program.cs               # Entry point
│       └── Commands/                # ValidateCommand, GenerateCommand, VerifyCommand, EstimateCommand
└── tests/
    ├── HPBim.Core.Tests/            # net10.0, xUnit v3, MTP
    │   ├── HPBim.Core.Tests.csproj
    │   ├── Fixtures/                # NhaPho5x18mFixture.cs (ground-truth ported from layout.py)
    │   ├── GeometryTests.cs
    │   ├── TcvnValidationTests.cs
    │   ├── QcEngineTests.cs
    │   └── BoqEstimateTests.cs
    └── HPBim.Client.Tests/          # net10.0, xUnit v3, MTP
        ├── HPBim.Client.Tests.csproj
        └── PipeRoundTripTests.cs
```

### 4.2 Exact Project Configuration Specs

1. **`HPBim/global.json`**:
```json
{
  "sdk": {
    "version": "10.0.300",
    "rollForward": "latestMinor",
    "allowPrerelease": true
  },
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}
```

2. **`HPBim/Directory.Build.props`**:
```xml
<Project>
  <PropertyGroup>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <NoWarn>$(NoWarn);xUnit1051;NU1701</NoWarn>
  </PropertyGroup>
</Project>
```

3. **`HPBim/HPBim.slnx`**:
```xml
<Solution>
  <Configurations>
    <BuildType Name="Debug" />
    <BuildType Name="Release" />
  </Configurations>
  <Folder Name="/Solution Items/">
    <File Path="global.json" />
    <File Path="Directory.Build.props" />
    <File Path="README.md" />
  </Folder>
  <Project Path="src/HPBim.Core/HPBim.Core.csproj" />
  <Project Path="src/HPBim.Client/HPBim.Client.csproj" />
  <Project Path="src/HPBim.Cli/HPBim.Cli.csproj" />
  <Project Path="tests/HPBim.Core.Tests/HPBim.Core.Tests.csproj" />
  <Project Path="tests/HPBim.Client.Tests/HPBim.Client.Tests.csproj" />
  <Folder Name="/Shared/">
    <Project Path="../McpShared/HPRebar.Mcp.Contracts/HPRebar.Mcp.Contracts.csproj" />
    <Project Path="../McpShared/HPRebar.McpBridge.Core/HPRebar.McpBridge.Core.csproj" />
    <Project Path="../McpShared/HPRebar.Mcp.Server.Core/HPRebar.Mcp.Server.Core.csproj" />
  </Folder>
</Solution>
```

4. **`HPBim.Core.csproj`**:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
    <RootNamespace>HPBim.Core</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Polyfill" Version="11.0.1" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

5. **`HPBim.Client.csproj`**:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net8.0;net10.0</TargetFrameworks>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>HPBim.Client</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\HPBim.Core\HPBim.Core.csproj" />
    <ProjectReference Include="..\..\..\McpShared\HPRebar.Mcp.Contracts\HPRebar.Mcp.Contracts.csproj" />
    <ProjectReference Include="..\..\..\McpShared\HPRebar.Mcp.Server.Core\HPRebar.Mcp.Server.Core.csproj" />
  </ItemGroup>
</Project>
```

6. **`HPBim.Cli.csproj`**:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>HPBim.Cli</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\HPBim.Core\HPBim.Core.csproj" />
    <ProjectReference Include="..\HPBim.Client\HPBim.Client.csproj" />
  </ItemGroup>
</Project>
```

7. **`HPBim.Core.Tests.csproj`**:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>HPBim.Core.Tests</RootNamespace>
    <IsPackable>false</IsPackable>
    <OutputType>Exe</OutputType>
    <UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="xunit.v3" Version="3.1.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\HPBim.Core\HPBim.Core.csproj" />
  </ItemGroup>
</Project>
```

---

## 5. Verification Method

Once implemented, the following verification commands will independently validate the deliverable:

1. **Solution Build Verification:**
   ```powershell
   dotnet build "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\HPBim.slnx" -c Release
   ```
   *Expected outcome:* 0 errors, 0 warnings (TreatWarningsAsErrors=true).

2. **Core Unit Test Suite Execution (Revit-free):**
   ```powershell
   dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\tests\HPBim.Core.Tests"
   ```
   *Expected outcome:* 100% green tests executed via Microsoft.Testing.Platform runner validating NhaPho5x18mFixture, geometric calculations, TCVN cover/rebar rules, and QC clash detection.

3. **No Forbidden References Verification:**
   ```powershell
   # Ensure HPBim.Core never references Autodesk.Revit
   Get-ChildItem -Recurse "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Core" -Filter "*.cs" | Select-String "Autodesk.Revit"
   # Ensure HPBim only references McpShared and no peer MCP folders
   Get-ChildItem -Recurse "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim" -Filter "*.csproj" | Select-String "ProjectReference"
   ```
   *Expected outcome:* 0 occurrences of `Autodesk.Revit` in `HPBim.Core`; all external ProjectReferences point exclusively to `..\McpShared\`.

4. **CLI Execution Verification:**
   ```powershell
   dotnet run --project "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Cli" -- validate --input NhaPho_5x18m.json
   dotnet run --project "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Cli" -- estimate --input NhaPho_5x18m.json
   ```
