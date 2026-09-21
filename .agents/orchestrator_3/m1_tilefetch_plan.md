# Technical Specification & Implementation Plan: HPAutoCad.TileFetch

**Milestone**: M1 (Domain Core & Companion Project)  
**Deliverable**: `HPAutoCad/HPAutoCad.TileFetch/` (.NET 8.0 Console Companion Utility)  
**Author**: `explorer_m1_tilefetch`  
**Target Audience**: `worker_m1` / Orchestrator  
**Date**: 2026-09-20  

---

## 1. Executive Summary & Purpose

`HPAutoCad.TileFetch` is a lightweight, out-of-process companion console utility designed to download map tiles for satellite imagery features (specifically `-HPGEOIMAGE` and the map tile pipeline in `HPGeoLink`).

### Why Out-of-Process Execution is Mandatory
1. **Firewall Isolation (`WSAEACCES` 10013)**: On corporate and CAD developer workstations, Windows Firewall rules (e.g. `Autocad2026`) frequently block `acad.exe` from making outbound network connections. Direct HTTP calls from inside AutoCAD fail with `SocketError.AccessDenied`. Launching an external child process (`HPAutoCad.TileFetch.exe`) runs with its own process identity and security token, completely bypassing `acad.exe` outbound socket blocks.
2. **AutoCAD Main Thread Responsiveness**: Large multi-tile downloads (e.g. 50–200 tiles) performed within `acad.exe` risk locking the Windows message pump or stalling AutoCAD's drawing thread. Executing in an external process guarantees AutoCAD's UI remains responsive, with progress reported over redirected standard output.
3. **Clean Process Lifecycle**: When a user cancels an operation (via the `ESC` key or UI cancel button), terminating the child process cleanly cancels pending HTTP requests without leaving hanging sockets or corrupted memory inside the AutoCAD host process.

### Packaging Location
In the unified single-bundle architecture, `HPAutoCad.TileFetch.exe` and its runtime dependencies will reside under:
`%AppData%\Autodesk\ApplicationPlugins\HPAutoCad.bundle\Contents\App\TileFetch\`

---

## 2. Analysis of Legacy `HPGeo.TileFetch`

The legacy utility is located at `HPGeo/HPGeo.TileFetch/` and consists of two files:
- `HPGeo.TileFetch.csproj` (27 lines)
- `Program.cs` (55 lines)

### Key Architectural Characteristics:
- **Host-Free**: Relies solely on standard .NET 8 libraries and domain models from `HPGeo.Core` (`HPGeo.Core.Imagery`). Zero dependencies on AutoCAD APIs or external NuGet packages.
- **Minimal Footprint**: Uses `InvariantGlobalization=true` and `SatelliteResourceLanguages=en` to eliminate ICU data bloat and satellite assemblies.
- **UseAppHost**: Explicitly enables `UseAppHost=true` so the compiler emits a native Windows PE launcher executable (`HPGeo.TileFetch.exe`) rather than requiring `dotnet.exe <dll>`.
- **Synchronous Progress Reporting**: Implements a custom `SyncProgress` (`IProgress<int>`) that formats and flushes progress lines directly to `Console.Out` under lock synchronization, avoiding dependency on a WPF/Windows Forms `SynchronizationContext`.
- **Atomic Cache Updates**: Integrates with `TileFetcher`, which downloads tiles and writes them via temporary files (`<tile>.tmp`) before atomic renaming (`File.Move(..., overwrite: true)`), ensuring hard kills never produce zero-byte or corrupt tile cache entries.

---

## 3. Project Configuration: `HPAutoCad.TileFetch.csproj`

### Target Location
`HPAutoCad/HPAutoCad.TileFetch/HPAutoCad.TileFetch.csproj`

### Verbatim XML Content
```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <!-- The tile download as its own process. acad.exe may be denied the network (a per-program firewall rule blocks
             it on the dev machine), and a process of its own is also the only place a download can never freeze the
             drawing. Ships in the bundle under Contents\App\TileFetch\ (copied there by HPAutoCad's build); the
             add-in starts it with a request file and reads its stdout. Host-free: HPAutoCad.Core only. -->
        <OutputType>Exe</OutputType>
        <TargetFramework>net8.0</TargetFramework>
        <LangVersion>latest</LangVersion>
        <Nullable>enable</Nullable>
        <ImplicitUsings>enable</ImplicitUsings>
        <RootNamespace>HPAutoCad.TileFetch</RootNamespace>
        <AssemblyName>HPAutoCad.TileFetch</AssemblyName>
        <Configurations>Debug;Release</Configurations>
        <Version>0.1.0</Version>
        <UseAppHost>true</UseAppHost>
        <InvariantGlobalization>true</InvariantGlobalization>
        <SatelliteResourceLanguages>en</SatelliteResourceLanguages>
    </PropertyGroup>

    <ItemGroup>
        <ProjectReference Include="..\HPAutoCad.Core\HPAutoCad.Core.csproj" />
    </ItemGroup>

</Project>
```

### Detailed Property Analysis
| Property | Value | Rationale |
|---|---|---|
| `<OutputType>` | `Exe` | Emits an executable binary rather than a class library. |
| `<TargetFramework>` | `net8.0` | Matches AutoCAD 2026 (.NET 8.0) runtime. Does not require `net8.0-windows` since it has no UI/WPF components. |
| `<LangVersion>` | `latest` | Enables modern C# features (primary constructors, collection expressions, pattern matching). |
| `<Nullable>` | `enable` | Enforces compile-time null safety. |
| `<ImplicitUsings>` | `enable` | Enables implicit namespace imports for `System`, `System.IO`, `System.Threading.Tasks`, etc. |
| `<RootNamespace>` | `HPAutoCad.TileFetch` | Adheres to repository naming conventions. |
| `<AssemblyName>` | `HPAutoCad.TileFetch` | Produces `HPAutoCad.TileFetch.exe` and `HPAutoCad.TileFetch.dll`. |
| `<Configurations>` | `Debug;Release` | Standard solution configurations. |
| `<UseAppHost>` | `true` | **Critical**: Generates native Windows PE executable wrapper (`HPAutoCad.TileFetch.exe`), allowing `ProcessStartInfo` to launch the binary directly without requiring `dotnet.exe`. |
| `<InvariantGlobalization>` | `true` | Strips internationalization/ICU tables, speeding up cold start and reducing process memory footprint. |
| `<SatelliteResourceLanguages>` | `en` | Suppresses creation of satellite resource folders. |
| `<ProjectReference>` | `..\HPAutoCad.Core\HPAutoCad.Core.csproj` | Pure domain dependency providing imagery contracts, `TileFetcher`, and `TileFetchProtocol`. Zero CAD references. |

---

## 4. Source Code Specification: `Program.cs`

### Target Location
`HPAutoCad/HPAutoCad.TileFetch/Program.cs`

### Verbatim C# Implementation
```csharp
using HPAutoCad.Core.HPGeoLink.Imagery;

namespace HPAutoCad.TileFetch;

/// <summary>
/// <c>HPAutoCad.TileFetch.exe &lt;request-file&gt;</c>: downloads the tiles the request names into the cache it names,
/// printing <c>progress N</c> / <c>fail z/x/y reason</c> / <c>done ok failed cached</c> lines
/// (<see cref="TileFetchProtocol"/>). The provider id is the only thing that decides a URL. Exit 0 = every tile
/// in the cache, 2 = some failed (listed), 1 = bad request. Cancelled by killing the process; the cache stays
/// consistent because every tile is written through a temp name.
/// </summary>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length != 1 || !File.Exists(args[0]))
        {
            await Console.Error.WriteLineAsync("usage: HPAutoCad.TileFetch <request-file>");
            return TileFetchProtocol.ExitUsage;
        }
        TileFetchRequest request;
        IImageryProvider provider;
        try
        {
            request = TileFetchProtocol.ReadRequest(args[0]);
            provider = ImageryProviders.Resolve(request.ProviderId);
        }
        catch (Exception exception) when (exception is FormatException or ArgumentException or IOException)
        {
            await Console.Error.WriteLineAsync("bad request: " + exception.Message);
            return TileFetchProtocol.ExitUsage;
        }

        var stdout = Console.Out;
        var sync = new object();
        var progress = new SyncProgress(p => { lock (sync) { stdout.WriteLine(TileFetchProtocol.Format(new TileFetchLine.Progress(p))); stdout.Flush(); } });
        using var fetcher = new TileFetcher(cacheRoot: request.CacheRoot, userAgent: request.UserAgent);
        var result = await fetcher.FetchAsync(provider, request.Tiles, progress);
        lock (sync)
        {
            foreach (var failure in result.Failures)
                stdout.WriteLine(TileFetchProtocol.Format(new TileFetchLine.Failed(failure.Tile, failure.Reason)));
            stdout.WriteLine(TileFetchProtocol.Format(new TileFetchLine.Done(result.Tiles.Count, result.Failures.Count, result.FromCache)));
            stdout.Flush();
        }
        return result.Complete ? TileFetchProtocol.ExitOk : TileFetchProtocol.ExitPartial;
    }

    /// <summary>IProgress that reports on the calling thread (Progress&lt;T&gt; would need a synchronization context).</summary>
    private sealed class SyncProgress(Action<int> onReport) : IProgress<int>
    {
        public void Report(int value) => onReport(value);
    }
}
```

### Namespace & Import Changes from Legacy
1. **Namespace**: `HPGeo.TileFetch` $\longrightarrow$ `HPAutoCad.TileFetch`
2. **Domain Import**: `using HPGeo.Core.Imagery;` $\longrightarrow$ `using HPAutoCad.Core.HPGeoLink.Imagery;`
3. **Usage String**: `"usage: HPGeo.TileFetch <request-file>"` $\longrightarrow$ `"usage: HPAutoCad.TileFetch <request-file>"`
4. **Documentation**: References updated to `HPAutoCad.TileFetch.exe`.

---

## 5. Execution Protocol & Communication Contracts

The communication contract between the AutoCAD host add-in (`HelperTileFetcher`) and the helper process (`HPAutoCad.TileFetch.exe`) is governed by `TileFetchProtocol`.

### 5.1 Invocation Protocol
```bash
HPAutoCad.TileFetch.exe "<request-file-path>"
```
- Exactly 1 command line argument containing the absolute path to a request file.
- If `args.Length != 1` or `!File.Exists(args[0])`:
  - Stderr output: `usage: HPAutoCad.TileFetch <request-file>`
  - Exit code: `1` (`TileFetchProtocol.ExitUsage`).

### 5.2 Request File Specification
A UTF-8 encoded text file written by `TileFetchProtocol.WriteRequest`:
```text
provider=esri
cache=C:\Users\username\AppData\Local\HPGeo\tiles
ua=HPAutoCad.TileFetch
19/245823/417490
19/245824/417490
19/245823/417491
19/245824/417491
```
- Line 1: `provider=<providerId>` (e.g. `esri`, `google`, `bing`, `osm`).
- Line 2: `cache=<cacheRootPath>` (directory path where tiles are stored).
- Line 3: `ua=<userAgent>` (HTTP User-Agent header; defaults to `HPAutoCad.TileFetch` if blank).
- Subsequent lines: `<zoom>/<x>/<y>` (tile coordinate tuples).

**Security Guardrail**: Arbitrary URLs are never accepted over the wire. The helper resolves URLs exclusively via `ImageryProviders.Resolve(request.ProviderId).TileUrl(tile)`.

### 5.3 Standard Output Protocol (Stdout)
Each stdout line is formatted via `TileFetchProtocol.Format` and flushed immediately:
1. **Progress Line**:
   ```text
   progress <percent>
   ```
   Example: `progress 45`  
   Emitted as each tile completes: `(doneCount * 100) / totalTiles`.
2. **Failure Line**:
   ```text
   fail <zoom>/<x>/<y> <reason>
   ```
   Example: `fail 19/245823/417490 HTTP 404 Not Found`  
   Newlines in `<reason>` are replaced with spaces to guarantee single-line emission.
3. **Completion Line**:
   ```text
   done <okCount> <failedCount> <cachedCount>
   ```
   Example: `done 4 0 2`  
   Emitted once at the conclusion of the fetch.

### 5.4 Exit Code Matrix
| Exit Code | Constant | Meaning | Action by Caller |
|---|---|---|---|
| `0` | `TileFetchProtocol.ExitOk` | Success: All tiles are in cache (`result.Complete == true`). | Proceed to stitch and render. |
| `1` | `TileFetchProtocol.ExitUsage` | Error: Bad arguments, missing file, corrupt syntax, unknown provider. | Throw `InvalidOperationException` with stderr diagnostic. |
| `2` | `TileFetchProtocol.ExitPartial` | Partial: Process finished, but one or more tiles could not be fetched. | Inspect stdout `fail` lines; render available tiles or notify user. |

### 5.5 Cancellation & Cache Integrity
- When cancelled via `CancellationToken` in AutoCAD, `HelperTileFetcher` calls `process.Kill(entireProcessTree: true)`.
- **Atomic Cache Protection**: `TileCache.WriteAsync` writes bytes to a temp file (`<path>.<guid>.tmp`) before executing `File.Move(temp, path, overwrite: true)`. If the process is terminated abruptly:
  - No zero-byte or partially-written `.tile` files can exist in the cache directory.
  - On next read, `TileCache.LooksLikeImage` verifies magic bytes (`0xFF 0xD8 0xFF` for JPEG or `0x89 0x50 0x4E 0x47` for PNG). Any invalid or truncated tile is automatically evicted.

---

## 6. Solution & Downstream Integration

### 6.1 Solution File (`HPAutoCad/HPAutoCad.slnx`)
`HPAutoCad.TileFetch` must be registered in `HPAutoCad.slnx`. Add the project entry:
```xml
  <Project Path="HPAutoCad.TileFetch/HPAutoCad.TileFetch.csproj" />
```

### 6.2 Add-In Packaging Integration (Milestone M2)
In Milestone M2, `HPAutoCad/HPAutoCad.csproj` will include the build target to copy `HPAutoCad.TileFetch` into its output:
```xml
<ItemGroup>
    <!-- Build order only: the tile-fetch helper is a separate process, never a compile-time reference. -->
    <ProjectReference Include="..\HPAutoCad.TileFetch\HPAutoCad.TileFetch.csproj" ReferenceOutputAssembly="false" Private="false" />
</ItemGroup>

<Target Name="CopyTileFetchHelper" AfterTargets="Build" Condition="'$(DesignTimeBuild)' != 'true'">
    <ItemGroup>
        <_HelperFiles Include="$(MSBuildThisFileDirectory)..\HPAutoCad.TileFetch\bin\$(Configuration)\net8.0\**\*" />
    </ItemGroup>
    <Error Condition="'@(_HelperFiles)' == ''" Text="HPAutoCad.TileFetch output missing - build HPAutoCad.TileFetch first" />
    <Copy SourceFiles="@(_HelperFiles)" DestinationFiles="@(_HelperFiles->'$(OutDir)TileFetch\%(RecursiveDir)%(Filename)%(Extension)')" SkipUnchangedFiles="true" />
</Target>
```

### 6.3 Caller Resolution (`HelperTileFetcher.cs` in M2)
In `HPAutoCad/HPGeoLink/Service/HelperTileFetcher.cs`:
```csharp
public const string HelperFolder = "TileFetch";
public const string HelperExe = "HPAutoCad.TileFetch.exe";

public static string? DefaultExePath()
{
    try
    {
        var dir = Path.GetDirectoryName(typeof(HelperTileFetcher).Assembly.Location);
        if (dir is null) return null;
        var path = Path.Combine(dir, HelperFolder, HelperExe);
        return File.Exists(path) ? path : null;
    }
    catch (Exception)
    {
        return null;
    }
}
```

---

## 7. Verification & Testing Strategy

### 7.1 Static Build Verification
```powershell
# Debug build
dotnet build HPAutoCad/HPAutoCad.TileFetch/HPAutoCad.TileFetch.csproj -c Debug
# Release build
dotnet build HPAutoCad/HPAutoCad.TileFetch/HPAutoCad.TileFetch.csproj -c Release
```
**Verification Criteria**:
- Builds with 0 errors and 0 warnings.
- Emits native executable: `HPAutoCad/HPAutoCad.TileFetch/bin/Debug/net8.0/HPAutoCad.TileFetch.exe`.
- Emits required dependencies: `HPAutoCad.TileFetch.dll`, `HPAutoCad.TileFetch.runtimeconfig.json`, `HPAutoCad.TileFetch.deps.json`, `HPAutoCad.Core.dll`.

### 7.2 CLI Protocol Verification
```powershell
$exe = "HPAutoCad/HPAutoCad.TileFetch/bin/Debug/net8.0/HPAutoCad.TileFetch.exe"

# Test 1: No arguments -> exit code 1 and usage message on stderr
$p = Start-Process -FilePath $exe -NoNewWindow -PassThru -Wait -RedirectStandardError "stderr.txt"
if ($p.ExitCode -eq 1 -and (Get-Content "stderr.txt") -match "usage: HPAutoCad\.TileFetch") {
    Write-Host "PASS: Test 1 (ExitUsage on missing args)"
}

# Test 2: Non-existent file -> exit code 1
$p = Start-Process -FilePath $exe -ArgumentList "does-not-exist.txt" -NoNewWindow -PassThru -Wait
if ($p.ExitCode -eq 1) {
    Write-Host "PASS: Test 2 (ExitUsage on missing file)"
}

# Test 3: Malformed request -> exit code 1
"provider=invalid`ncache=C:\temp`n19/1/2" | Out-File -Encoding utf8 "bad.txt"
$p = Start-Process -FilePath $exe -ArgumentList "bad.txt" -NoNewWindow -PassThru -Wait -RedirectStandardError "bad_err.txt"
if ($p.ExitCode -eq 1 -and (Get-Content "bad_err.txt") -match "bad request") {
    Write-Host "PASS: Test 3 (ExitUsage on bad provider)"
}
```

### 7.3 Automated Unit Tests (`TileFetchHelperTests.cs` in `HPAutoCad.Tests`)
Ported to `HPAutoCad.Tests`:
- `Request_file_round_trips_and_lines_parse_both_ways`: Validates protocol serialization/deserialization.
- `The_helper_exe_refuses_a_missing_or_malformed_request_with_exit_1`: Executes the compiled `HPAutoCad.TileFetch.exe` and verifies exit code 1.
- `Live_helper_fetches_four_real_tiles_into_a_temp_cache_through_the_host_side_fetcher`: Gated live test (`HPAUTOCAD_LIVE_TILES=1`).

---

## 8. Step-by-Step Instructions for Worker Agent

When implementing Milestone M1:

1. **Verify Prerequisites**:
   Ensure `HPAutoCad/HPAutoCad.Core/` exists and compiles successfully (contains `HPAutoCad.Core.HPGeoLink.Imagery`).

2. **Create Project Directory**:
   Create directory: `HPAutoCad/HPAutoCad.TileFetch/`

3. **Create Project File**:
   Write `HPAutoCad/HPAutoCad.TileFetch/HPAutoCad.TileFetch.csproj` using the exact XML in Section 3.

4. **Create Program Source**:
   Write `HPAutoCad/HPAutoCad.TileFetch/Program.cs` using the exact C# code in Section 4.

5. **Register in Solution**:
   Add `<Project Path="HPAutoCad.TileFetch/HPAutoCad.TileFetch.csproj" />` to `HPAutoCad/HPAutoCad.slnx`.

6. **Compile & Validate**:
   Execute:
   ```powershell
   dotnet build HPAutoCad/HPAutoCad.TileFetch/HPAutoCad.TileFetch.csproj -c Debug
   dotnet build HPAutoCad/HPAutoCad.TileFetch/HPAutoCad.TileFetch.csproj -c Release
   ```
   Confirm `HPAutoCad.TileFetch.exe` is generated in `bin/Debug/net8.0/` and `bin/Release/net8.0/`.

7. **Test CLI Invariant**:
   Execute `HPAutoCad.TileFetch.exe` with no arguments and verify exit code is `1` and stderr begins with `usage: HPAutoCad.TileFetch <request-file>`.
