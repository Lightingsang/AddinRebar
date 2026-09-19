# Spike S0-B — two toolkit copies in two AssemblyLoadContexts of one acad.exe (2026-09-19)

**Verdict: loose DLLs = `no-go`; repack the toolkit into each add-in assembly = `go`.** Phase 4 ships the repack target (below) for HPAutoCad, HPCivil3d and HPGeo, plus `ThemeInfo` (S0-A).

Worktree `../AddinRebar-md-spike`: `HPAutoCad.McpBridge` + `HPGeo.AutoCad` get `MaterialDesignThemes 5.3.2`; the bridge window merges `BundledTheme` + `MaterialDesign2.Defaults.xaml` before `AutocadTheme.xaml` and carries a spike `ComboBox` + `PackIcon`; `GeoImportWindow` adds a code-created `BundledTheme` + the Defaults by pack URI before its legacy dictionaries (with the legacy implicit `ComboBox`/`TextBox` styles dropped — the known dark-theme gap). Each window logs on `Loaded` a **`MD-SPIKE` line**: ALC of the add-in's own `typeof(BundledTheme)` vs ALC of the XAML-declared `BundledTheme` (bridge) vs ALC of a toolkit template part found in the visual tree (`SmartHint`), `copiesInProcess` = assemblies named `MaterialDesignThemes.Wpf` in the AppDomain. Driver `s0b-two-alc.ps1` (Windows PowerShell 5.1, same pattern as `HPGeo/tools/dialog-check.ps1`: own acad.exe, SECURELOAD answered, script over COM in a job, windows by pid + title, UIA `ExpandCollapse` on the first enabled ComboBox, screen capture, clean `_.QUIT`, COLORTHEME restored). Order A = `HPMCPBRIDGE` → `HPGEOIMPORT` (dark) → `HPGEOIMPORT` (light); order B = `HPGEOIMPORT` → `HPMCPBRIDGE` → `HPGEOIMPORT`.

## Run 1 — loose DLLs (`Contents\Bridge\MaterialDesignThemes.Wpf.dll`, `Contents\App\…`, each loaded by its own ALC through deps.json)

| Order | Window | `MD-SPIKE` result |
|---|---|---|
| A | bridge (loaded its copy first) | code = xaml = template = `HPAutoCad.McpBridge`, `same=true`, `packIcon=true` |
| A | HPGeo dark, then light (its copy loaded second) | template `SmartHint` ALC = `HPGeo.AutoCad`, `same=True`, `copiesInProcess=2` — both windows |
| B | HPGeo dark (its copy loaded first) | `same=True`, `copiesInProcess=1` |
| B | bridge (its copy loaded second) | `same=true`, `copiesInProcess=2` |
| **B** | **HPGeo light (opened after the bridge's copy loaded)** | **template `SmartHint` ALC = `HPAutoCad.McpBridge` → `same=False`** — the BAML-created toolkit objects of HPGeo's window came from the *other* add-in's copy |

Both orders: windows rendered, no exception in `loader.log` / `mcpbridge-*.log` / `hpgeo-*.log`, acad quit cleanly (`s0b-order-a/`, `s0b-order-b/`, `session-logs.txt`). The mixing is silent today because the two copies are the same version; it breaks the moment code compares types across the boundary — a window whose XAML-declared `BundledTheme` came from the other copy fails `x is IMaterialDesignThemeDictionary` (the theme switch), and two add-ins on different toolkit versions would mix templates. Rule observed: **WPF's BAML type resolution picks the last-loaded assembly of that simple name, `EnterContextualReflection` does not change it.**

## Run 2 — repacked (`ILRepack` post-build target merges `MaterialDesignThemes.Wpf` + `MaterialDesignColors` + `Microsoft.Xaml.Behaviors` into the add-in assembly, loose files deleted; `HPAutoCad.McpBridge.dll` 10 546 176 B, `HPGeo.AutoCad.dll` 10 678 784 B)

| Order | Result |
|---|---|
| A | **8 / 8**: bridge `same=true packIcon=true copiesInProcess=0`; HPGeo dark + light `same=True copiesInProcess=0`; 0 `load MaterialDesignThemes.Wpf` loader lines; ComboBox popups expanded via UIA; no errors; clean quit |
| B | **8 / 8**, same lines |

Screenshots `s0b-repacked-order-{a,b}/`: `bridge-dark.png` (toolkit ComboBox + `PackIcon` in the header, MD checkboxes, legacy cards intact), `geo-dark-combo-open.png` (**dark MaterialDesign popup** on the province ComboBox — the documented HPGeo gap closed), `geo-light-combo-open.png`.

## Repack target (spike form; phase 4 makes it a shared snippet per csproj)

```xml
<PackageReference Include="ILRepack" Version="2.0.46" PrivateAssets="all" ExcludeAssets="all" GeneratePathProperty="true" />
<Target Name="RepackMaterialDesign" AfterTargets="CopyFilesToOutputDirectory" Condition="Exists('$(OutDir)MaterialDesignThemes.Wpf.dll')">
  <PropertyGroup>
    <_RepackExe>$(PkgILRepack)\tools\ILRepack.exe</_RepackExe>
    <_RepackLib>@(ReferencePath->'%(RelativeDir)'->Distinct()->'/lib:&quot;%(Identity) &quot;', ' ')</_RepackLib>
  </PropertyGroup>
  <Exec Command="&quot;$(_RepackExe)&quot; /union /parallel /noRepackRes $(_RepackLib) /out:&quot;$(OutDir)$(AssemblyName).dll&quot; &quot;@(IntermediateAssembly->'%(FullPath)')&quot; &quot;$(OutDir)MaterialDesignThemes.Wpf.dll&quot; &quot;$(OutDir)MaterialDesignColors.dll&quot; &quot;$(OutDir)Microsoft.Xaml.Behaviors.dll&quot;" />
  <Delete Files="$(OutDir)MaterialDesignThemes.Wpf.dll;$(OutDir)MaterialDesignColors.dll;$(OutDir)Microsoft.Xaml.Behaviors.dll;$(OutDir)MaterialDesignThemes.Wpf.xml" />
</Target>
```
Primary input = the **obj** DLL, so an incremental build re-merges from a fresh compile and never merges twice; the copy-local step re-copies the toolkit DLLs when they are missing, the `Exists` condition then fires again. Roslyn, `HPAutoCad.Aec`, WebView2 stay loose. The bridge's `deps.json` still lists the toolkit; nothing requests it after the merge. `[assembly: ThemeInfo(None, SourceAssembly)]` added to both.

## Consequences for the plan
- Phase 4: repack in `HPAutoCad.McpBridge`, `HPCivil3d.McpBridge` (mirror + re-pin; Civil never co-loads with the others but the pattern stays identical), `HPGeo.AutoCad`; `MirrorTests` must classify the target text (it lives in the two csproj that are already sha-pinned).
- Phase 3 (Revit MCP bridge, loose DLLs inside Revit's per-add-in ALC): the same hazard exists if two Revit add-ins ship the toolkit → **repack there too** (`IsRepackable=false` stays for Roslyn; the toolkit-only target above is independent). Phase 5 (Navisworks, net48, one AppDomain): same reasoning → repack rather than allow-list; S0-C measures the foreign-copy case anyway.
- The spike's HPGeo diagnostics also proved `defaultsMerged=44` (the MD2 Defaults chain loads by pack URI inside the isolated ALC) and that dropping HPGeo's implicit `ComboBox`/`TextBox` styles is enough for the toolkit templates to apply.

## Environment restored
Both bundles rebuilt and redeployed from the main tree (`HPAutoCad.McpBridge.dll` 68 096 B, `HPGeo.AutoCad.dll` 193 024 B, no toolkit DLL beside them); user COLORTHEME and `%AppData%\HPGeo\settings.json` restored by the driver.
