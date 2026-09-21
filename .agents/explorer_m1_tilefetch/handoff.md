# Handoff Report: HPAutoCad.TileFetch Technical Specification

**Agent**: `explorer_m1_tilefetch`  
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_m1_tilefetch\`  
**Target Plan**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\m1_tilefetch_plan.md`  
**Date**: 2026-09-20  

---

## 1. Observation

1. **Legacy Project Configuration** (`HPGeo/HPGeo.TileFetch/HPGeo.TileFetch.csproj`):
   - Lines 8–20:
     ```xml
     <OutputType>Exe</OutputType>
     <TargetFramework>net8.0</TargetFramework>
     <LangVersion>latest</LangVersion>
     <Nullable>enable</Nullable>
     <ImplicitUsings>enable</ImplicitUsings>
     <RootNamespace>HPGeo.TileFetch</RootNamespace>
     <AssemblyName>HPGeo.TileFetch</AssemblyName>
     <Configurations>Debug;Release</Configurations>
     <Version>0.1.0</Version>
     <UseAppHost>true</UseAppHost>
     <InvariantGlobalization>true</InvariantGlobalization>
     <SatelliteResourceLanguages>en</SatelliteResourceLanguages>
     ```
   - Lines 22–24:
     ```xml
     <ItemGroup>
         <ProjectReference Include="..\HPGeo.Core\HPGeo.Core.csproj" />
     </ItemGroup>
     ```
   - No external NuGet dependencies; purely host-free .NET 8.0.

2. **Legacy Program Implementation** (`HPGeo/HPGeo.TileFetch/Program.cs`):
   - Line 1: `using HPGeo.Core.Imagery;`
   - Line 3: `namespace HPGeo.TileFetch;`
   - Lines 14–47: `public static async Task<int> Main(string[] args)`
     - Lines 16–20: Validates argument count and file existence (`args.Length != 1 || !File.Exists(args[0])`), writes `"usage: HPGeo.TileFetch <request-file>"` to `Console.Error`, returns `TileFetchProtocol.ExitUsage` (1).
     - Lines 23–32: Reads request via `TileFetchProtocol.ReadRequest(args[0])` and resolves provider via `ImageryProviders.Resolve(request.ProviderId)`. On `FormatException | ArgumentException | IOException`, writes `"bad request: " + exception.Message` to `Console.Error`, returns `TileFetchProtocol.ExitUsage` (1).
     - Lines 34–36: Uses thread-safe `SyncProgress` to print `progress <pct>` to `Console.Out` under lock and flushes stdout.
     - Lines 37–38: Instantiates `TileFetcher` and calls `await fetcher.FetchAsync(provider, request.Tiles, progress)`.
     - Lines 39–45: Prints `fail <tile> <reason>` and summary `done <ok> <failed> <cached>`, then flushes stdout.
     - Line 46: Returns `result.Complete ? TileFetchProtocol.ExitOk : TileFetchProtocol.ExitPartial` (0 or 2).

3. **Wire Protocol & Contract** (`HPGeo/HPGeo.Core/Imagery/TileFetchProtocol.cs`):
   - Lines 24–26:
     ```csharp
     public const int ExitOk = 0;
     public const int ExitUsage = 1;
     public const int ExitPartial = 2;
     ```
   - Lines 28–36: `WriteRequest(string path, TileFetchRequest request)` formats request with `provider=`, `cache=`, `ua=`, and `<z>/<x>/<y>`.
   - Lines 68–74: `Format(TileFetchLine line)` formats `progress`, `fail`, and `done` lines.
   - Lines 77–95: `Parse(string? text)` parses stdout lines.

4. **Caller & Packaging Contracts** (`HPGeo/HPGeo.AutoCad/Imagery/HelperTileFetcher.cs` and `HPGeo/HPGeo.AutoCad/HPGeo.AutoCad.csproj`):
   - `HelperTileFetcher.cs:16-17`: `public const string HelperFolder = "TileFetch"; public const string HelperExe = "HPGeo.TileFetch.exe";`
   - `HelperTileFetcher.cs:80-83`: `using (ct.Register(() => TryKill(process))) { await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false); }`
   - `HPGeo.AutoCad.csproj:48-54`: Target `CopyTileFetchHelper` copies `..\HPGeo.TileFetch\bin\$(Configuration)\net8.0\**\*` into `$(OutDir)TileFetch\`.

5. **AutoCAD Solution** (`HPAutoCad/HPAutoCad.slnx`):
   - Currently includes `HPAutoCad.McpBridge.Loader`, `HPAutoCad.Aec`, `HPAutoCad.Aec.Tests`, `HPAutoCad.McpBridge`, `HPAutoCad.Mcp.Server`, `HPAutoCad.Mcp.Server.Tests`.
   - Lacks `HPAutoCad.Core`, `HPAutoCad.TileFetch`, and `HPAutoCad.Tests`.

---

## 2. Logic Chain

1. **Firewall and Main Thread Protection**:
   - `acad.exe` is blocked from outbound network traffic by Windows Firewall on developer and production workstations (`WSAEACCES` 10013) (Obs. 4).
   - In-process HTTP tile fetching would freeze the AutoCAD message pump during heavy downloads (Obs. 2).
   - An independent out-of-process console executable is mandatory to bypass `acad.exe` network restrictions and keep the CAD UI responsive.

2. **Assembly and Project Configuration**:
   - The utility must run standalone on .NET 8.0 with `OutputType=Exe` and `UseAppHost=true` to generate `HPAutoCad.TileFetch.exe` (Obs. 1).
   - Using `InvariantGlobalization=true` and `SatelliteResourceLanguages=en` keeps the executable lean and eliminates startup overhead (Obs. 1).
   - The project reference must point directly to `..\HPAutoCad.Core\HPAutoCad.Core.csproj` without any CAD dependencies.

3. **Namespace & Code Updates**:
   - Relocating `HPGeo.TileFetch` $\to$ `HPAutoCad.TileFetch` requires namespace update `namespace HPAutoCad.TileFetch;` and domain using `using HPAutoCad.Core.HPGeoLink.Imagery;` (Obs. 2).
   - Error messages must reflect the new binary name: `usage: HPAutoCad.TileFetch <request-file>`.

4. **Process & Communication Protocol**:
   - The process contract uses exit codes 0 (complete success), 1 (usage/request error), and 2 (partial failure with failed tiles listed) (Obs. 2, Obs. 3).
   - Standard output protocol requires atomic, flushed line emissions: `progress <pct>`, `fail <z/x/y> <reason>`, `done <ok> <fail> <cached>`.
   - Process termination via `Kill(entireProcessTree: true)` does not corrupt the tile cache because `TileFetcher` writes to temporary files (`<tile>.tmp`) before atomically renaming them into the cache directory.

---

## 3. Caveats

1. **Prerequisite Dependency**:
   - `HPAutoCad.TileFetch` depends on `HPAutoCad.Core` (`HPAutoCad.Core.HPGeoLink.Imagery`). It cannot compile until `HPAutoCad.Core` is created or scaffolded with the imagery models (`TileFetchProtocol`, `TileFetchRequest`, `TileFetchLine`, `TileFetcher`, etc.).
2. **AutoCAD Packaging Delay**:
   - The MSBuild copy target `CopyTileFetchHelper` and bundle deployment will be integrated in Milestone M2 (`HPAutoCad.csproj`) and Milestone M3 (`HPAutoCad.bundle`). In Milestone M1, verification focuses on standalone compilation, unit testing, and CLI validation.

---

## 4. Conclusion

- A comprehensive, production-ready specification for `HPAutoCad.TileFetch` has been created and documented in:
  `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\m1_tilefetch_plan.md`
- The specification includes:
  - Exact verbatim `.csproj` configuration with .NET 8.0, `UseAppHost=true`, nullability, and core reference.
  - Complete verbatim `Program.cs` code under namespace `HPAutoCad.TileFetch`.
  - Protocol specifications for CLI invocation, request format, stdout message schema, exit codes, and cancellation.
  - Step-by-step instructions for the worker agent to implement and validate the tool.

---

## 5. Verification Method

1. **Static Build Verification**:
   ```powershell
   dotnet build HPAutoCad/HPAutoCad.TileFetch/HPAutoCad.TileFetch.csproj -c Debug
   dotnet build HPAutoCad/HPAutoCad.TileFetch/HPAutoCad.TileFetch.csproj -c Release
   ```
   *Expected Output*: Build succeeds with 0 errors. Native binary `HPAutoCad.TileFetch.exe` exists in `bin/Debug/net8.0/` and `bin/Release/net8.0/`.

2. **CLI Exit Code & Error Message Verification**:
   ```powershell
   $exe = "HPAutoCad/HPAutoCad.TileFetch/bin/Debug/net8.0/HPAutoCad.TileFetch.exe"
   $p = Start-Process -FilePath $exe -NoNewWindow -PassThru -Wait -RedirectStandardError "err.txt"
   Assert ($p.ExitCode -eq 1)
   Assert ((Get-Content "err.txt") -match "usage: HPAutoCad\.TileFetch")
   ```

3. **Solution Integrity Verification**:
   Ensure `HPAutoCad/HPAutoCad.slnx` includes `<Project Path="HPAutoCad.TileFetch/HPAutoCad.TileFetch.csproj" />` and builds cleanly.
