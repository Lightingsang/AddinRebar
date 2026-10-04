# Phase 3 — seed quality tests with a baseline allowlist

## Context
[baseline seed scan](reports/baseline.md) (blocking: Excel `read_table` ×2 only); [map-02 §2–§3](reports/map-02-autocad-civil.md) (mirror never scans `*.Mcp.Server.Tests` / `SeedLibrary/`; allowlist must not sit under `SeedLibrary/` — embedded); [map-03 §2–§3](reports/map-03-com-standalone.md) (E9 test checks no code quality; all test projects reference Bridge.Core); [map-05 §3](reports/map-05-powerbi.md) (no seeds).

## Overview
Priority P1 · Planned · tests only (10 `*.Mcp.Server.Tests`), no seed edited, no csproj edited.

## Key decisions
- **Thin per-server test, no shared linked file** (K1 vs P7): a shared helper in a test project would need a `<Compile Include>` link in 10 csproj files; the logic that matters (the walker) is already shared via `ScriptQuality.Find`. Each test ≈ 30 lines: load seeds with the public `SeedInstaller.LoadSeeds`/existing loader, run `ScriptQuality.Find`, subtract the allowlist, assert no `error`; warnings printed, never asserted (review triggers).
- **Allowlist** = a static array in the test file, only non-empty in Excel (a text file would need `CopyToOutputDirectory` = csproj edit): one entry `("Data/read_table", "Q-B2", "<sha256 of code.cs, first 12 hex>", "reason")`. An entry whose hash no longer matches fails the test ("seed changed - make it clean, then delete the entry") -> new/edited seeds must be clean (acceptance 4).
- Excel `read_table` stays allowlisted (contract); fixing it means editing `HPExcel/tools/generate-seed-library.py:330,342` + regenerate — out of scope.
- Civil 3D: test file in `HPCivil3d.Mcp.Server.Tests` (not scanned by the mirror test); Civil seeds come from the generator, so a future fix goes there.
- Power BI: test accepts an empty seed library and checks any future seed (**pending D1**).

## Related files
Create: `<Host>/<Host>.Mcp.Server.Tests/SeedQualityTests.cs` x10 (HPRebar, HPAutoCad, HPCivil3d, HPNavis, HPEtabs, HPSap2000, HPRobot, HPExcel, HPPowerBi, HPTekla). No other file.

## Steps
1. Write the Revit test first; run; check it finds 0 errors on 21 seeds.
2. Copy the pattern to the 9 other servers (namespace + loader per project).
3. Excel: add the two allowlist entries (one per catch, same hash) from `sha256sum`.
4. Mutation check on a scratch copy of one seed (not committed): add `// var x = 1;` → test red; restore.
5. Run all 10 server tests + `HPCivil3d.McpBridge.Tests`.

## Build / test commands
`cd <Host> && dotnet test <Host>.Mcp.Server.Tests` ×10; `cd HPCivil3d && dotnet test HPCivil3d.McpBridge.Tests`.

## Success criteria
Acceptance 4: green in all 10; Excel entries match current hashes; a changed allowlisted seed fails.

## Commit split (P6)
`test(mcp): check every embedded seed with the script-quality walker` (one commit, tests only).

## Risks
Tests that skip without the host (SAP2000, Tekla compile tests) — the quality test reads text only and never skips; seed loader differences per project (reuse each project's existing loader).

## Rollback
Delete the 10 test files.
