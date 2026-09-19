---
phase: 0
title: "Spikes — ILRepack, two ALCs, net48 resolver"
status: completed
priority: P1
effort: "10h"
dependencies: []
---

# Phase 0: Spikes with measurable gates

> **Done 2026-09-19** — reports [spike-s0a-ilrepack.md](reports/spike-s0a-ilrepack.md) (`go`: merge; +`ThemeInfo`, explicit `FontFamily`), [spike-s0b-two-alc.md](reports/spike-s0b-two-alc.md) (loose = `no-go`, **repack = `go`**), [spike-s0c-navis-resolver.md](reports/spike-s0c-navis-resolver.md) (`go`; repack chosen, allow-list never exercised). Worktree `../AddinRebar-md-spike` (branch `spike/md-spike`, uncommitted) kept as the reference for the exact spike edits until the user deletes it; every deployed host restored from the main tree.

## Overview
Answer the three questions no document can ([compatibility-matrix.md](research/compatibility-matrix.md) gates S0-A/B/C) on throw-away branches. Each spike ends with a report under `reports/` and a verdict `go` / `fallback`; nothing from a spike branch is merged. Phases 2, 4, 5 do not start before their spike's verdict.

## Requirements
- Functional: prove (or refute) that the toolkit renders and switches theme under each risky load mechanism.
- Non-functional: no change to the tracked tree outside `plans/`; each spike ≤ 4h; artefacts are measured (file listings, log lines, screenshots), not impressions.

## Architecture
Spike branches `spike/md-ilrepack`, `spike/md-two-alc`, `spike/md-navis-resolver` off `RebarVersion1`. Each adds the package to exactly one project, one `Window.Resources` merge, one throw-away `ComboBox` + `Button`, runs the gate, records the result, and is deleted.

## Related Code Files
- Read only (nothing is modified on `RebarVersion1`): `HPRebar/HPRebar/HPRebar.csproj:7,29` (repack + ILRepack 2.0.46), `%NUGET%\nice3point.revit.sdk\6.2.3\Sdk\Nice3point.Revit.Repack.targets:28-56` (`RepackBinariesExcludes`, `/union /illink`), `HPAutoCad/HPAutoCad.McpBridge.Loader/BridgeLoadContext.cs:15-41`, `HPGeo/HPGeo.AutoCad.Loader/GeoLoadContext.cs:14-35`, `HPNavis/HPNavis.McpBridge/PluginAssemblyResolver.cs:17-25,59-83`.
- Create (spike branches only): `plans/260919-1910-materialdesign-xaml-adoption/reports/spike-s0a-ilrepack.md`, `spike-s0b-two-alc.md`, `spike-s0c-navis-resolver.md`; helper script `plans/…/reports/list-baml-pack-uris.ps1` (reads `HPRebar.dll` `.g.resources`, prints resource names + every `;component` string found in BAML records).

## Implementation Steps

### S0-A — ILRepack merge of the toolkit into `HPRebar.dll` (R26, then R24)
1. Branch. Add `MaterialDesignThemes 5.3.2` to `HPRebar/HPRebar/HPRebar.csproj` (it pulls `MaterialDesignColors` + `Microsoft.Xaml.Behaviors.Wpf`). Leave `IsRepackable=true`.
2. `ColumnRebarView.xaml` (spike copy): merge `<md:BundledTheme BaseTheme="Dark" PrimaryColor="Blue" SecondaryColor="Orange"/>` + `MaterialDesign2.Defaults.xaml` **before** `Theme.xaml`; `Style="{StaticResource MaterialDesignWindow}"`; drop one `ComboBox` (3 items) and one `Button` on the Geometry tab.
3. `dotnet build HPRebar/HPRebar.slnx -c Debug.R26 -p:DeployAddin=false` (Revit may be open). Measure on `HPRebar/HPRebar/bin/Debug.R26/HPRebar.dll`: (a) no loose `MaterialDesignThemes.Wpf.dll` beside it; (b) `HPRebar.g.resources` contains `themes/materialdesign2.defaults.baml`, `themes/generic.baml` (merged), `resources/roboto/roboto-regular.ttf`; (c) `list-baml-pack-uris.ps1` finds **zero** `MaterialDesignThemes.Wpf;component` strings and ≥ 1 `HPRebar;component/themes/…` — this is the BAML/pack-URI question; (d) `HPRebar.dll` carries `ThemeInfoAttribute(…, SourceAssembly)` (needed for `generic.baml`); (e) size delta (expect ≈ +10 MB).
4. Close Revit, build with deploy, open Revit 2026 → Column Rebar. Gate: window opens, `ComboBox` popup opens styled, `Button` is `MaterialDesignRaisedButton`, `PackIcon` renders (the #1340 failure mode), no `XamlParseException` in `%AppData%\HPRebar\logs`. Then toggle Revit's theme (Options ▸ Colors) and call `SetTheme` from a spike button: brushes animate, no exception.
5. Repeat step 3 with `-c Debug.R24` (net48 asset, `Microsoft.Xaml.Behaviors` net462). Revit 2024 is not installed on the dev machine → build-only + resource listing; runtime on 2024 stays `[chưa xác minh]` and is recorded as such.
6. Fallback if (c) or 4 fails: `RepackBinariesExcludes=MaterialDesignThemes.Wpf.dll;MaterialDesignColors.dll;Microsoft.Xaml.Behaviors.dll` (loose DLLs beside `HPRebar.dll`, resolved by Revit 2025+'s per-add-in ALC; on R23/R24 this reopens the other-add-ins clash — then the net48 verdict is **no-go** and phase 2 ships MaterialDesign on R25/R26 only). Second fallback: ILRepack 2.0.48 (released 2026-09-01) if the failure is a known BAML bug.

### S0-B — two isolated ALCs in one acad.exe (HPAutoCad bridge + HPGeo)
1. Branch. Add the package to `HPAutoCad/HPAutoCad.McpBridge` and `HPGeo/HPGeo.AutoCad`; merge `BundledTheme` + `MaterialDesign2.Defaults.xaml` into `AutocadBridgeStatusView.xaml` and `GeoImportWindow.xaml` (its `ComboBox` is the known gap). Build both with `-p:DeployBundle=false`, then with AutoCAD closed deploy both bundles.
2. Start AutoCAD 2026 (`SECURELOAD` Always Load), run `HPMCPBRIDGE` then `HPGEOIMPORT`, close, run them in the **other order**, and once with `COLORTHEME` flipped in between. Gate: both windows render their own MaterialDesign theme; the HPGeo `ComboBox` popup opens and is dark; no `XamlParseException` / `FileLoadException` in `%LocalAppData%\HPAutoCad\McpBridge\logs\loader.log` and `%LocalAppData%\HPGeo\…\hpgeo-*.log`; `loader.log` shows `load MaterialDesignThemes.Wpf 5.3.2.0` **once per ALC**.
3. If either window gets the other ALC's types (symptom: `XamlParseException` "…is not a valid value for property 'Style'" or `InvalidCastException` on `MaterialDesignThemes.Wpf.*`), record the order that fails.
4. Fallback: repack the toolkit into each bridge assembly (`ILRepack` `PackageReference` + a `RepackToolkit` target after build, `/internalize`, output replaces `HPAutoCad.McpBridge.dll`) so the simple name `MaterialDesignThemes.Wpf` never exists in the process; re-run step 2. The `Aec` assembly and Roslyn stay loose (untouched).
5. Also record: does `SystemVariableChanged` fire for `COLORTHEME` on the main thread (needed by phase 1's live switch)? One log line answers it.

### S0-C — net48 `PluginAssemblyResolver` vs a foreign toolkit copy (Navisworks)
1. Branch. Add the package to `HPNavis/HPNavis.McpBridge`; add `MaterialDesignThemes.Wpf`, `MaterialDesignColors`, `Microsoft.Xaml.Behaviors` to `PluginAssemblyResolver.AllowList`; merge the theme into `NavisBridgeStatusView.xaml`. Build with `-p:DeployPlugin=false`, then deploy with Roamer closed.
2. Plant a throw-away plugin folder `%AppData%\Autodesk\Navisworks Manage 2026\Plugins\SpikeForeign\` holding `MaterialDesignThemes.Wpf.dll` **4.9.0** + `MaterialDesignColors 2.1.4` (downloaded to the scratchpad, never into the repo) and a 20-line `EventWatcherPlugin` that loads them at start (`Assembly.LoadFrom`).
3. `pwsh HPNavis/tools/harness/run-ribbon-check.ps1 -WithNoDoc`. Gate: our window opens with 5.3.2 (`PluginAssemblyResolver.Resolved` log shows `MaterialDesignThemes.Wpf, Version=5.3.2.0 -> …HPNavis.McpBridge\MaterialDesignThemes.Wpf.dll`), the planted 4.9.0 is never handed to us and our 5.3.2 is never handed to the foreign plugin (version-family gate), the foreign `NavisworksMCPPlugin` still loads silently, `MCP scripting self-check OK` still logged.
4. Known trap to record: the CLR resolves a **simple-name** `Assembly.Load("MaterialDesignThemes.Wpf")` (what WPF's `BaseUriHelper` issues for `;component` URIs) against whichever copy is already loaded — if the foreign 4.9.0 was loaded first, our BAML would bind to it. Measure it: start order foreign-first vs ours-first.
5. Fallback: repack the toolkit into `HPNavis.McpBridge.dll` (same target as S0-B fallback, net48 ILRepack) — then no allow-list entry is needed and the foreign copy is irrelevant. Remove `SpikeForeign\` in `finally`.

## Success Criteria
- [x] `reports/spike-s0a-ilrepack.md`: resource listing + pack-URI count (224/224 patched) + off-Revit harness 15/15 on R26 **and** R24 with ComboBox-open / light screenshots; in-Revit run deferred to phase 2 (would overwrite the installed add-in).
- [x] `reports/spike-s0b-two-alc.md`: both orders, loose (mixing observed in order B) and repacked (8/8 × 2), loader/bridge/HPGeo log excerpts, screenshots.
- [x] `reports/spike-s0c-navis-resolver.md`: foreign 4.9.0 loaded first, loose 6/7 (the FAIL is a grep — no resolver line because Roamer probes the folder itself) and repacked 6/6.
- [ ] Spike worktree deleted (`git worktree remove --force ../AddinRebar-md-spike; git branch -D spike/md-spike`) — user's call; `git status` on `RebarVersion1` is clean except `plans/`.

## Risk Assessment
- ILRepack `/union` merges identically named types — the toolkit has `MaterialDesignThemes.Wpf.Converters.*`; HPRebar has no converter namespace, but `/union` is on for every input — check the build log for "Merging types" collisions. Mitigation: none needed unless the log shows one.
- `/illink` (Nice3point default) could trim toolkit types referenced only from BAML → `XamlParseException` at runtime. Mitigation: S0-A step 4 exercises `ComboBox` + `PackIcon` + `MaterialDesignWindow`; if trimmed, the fallback is loose DLLs, not disabling `/illink` for the whole add-in.
- A spike that passes on the dev machine (Revit 2026, AutoCAD 2026, Navisworks 2026, no foreign MaterialDesign copies present) still says nothing about a user's machine with another MaterialDesign add-in loaded first — S0-C step 4 covers Navisworks; for Revit R23/R24 the merge is the mitigation (no simple name exported); for Revit 2025+ the per-add-in ALC is.

## Next Steps
Phase 1 can start in parallel with S0-B/S0-C (it is host-neutral code); phase 2 waits for S0-A, phase 4 for S0-B, phase 5 for S0-C.
