# Baseline — 261004-1005 hp-clean-code-ai-tools

Read-only baseline for brief §5. No source / csproj / slnx edited, nothing deployed, nothing committed. No `Debug.R2x`/`Release.R2x` build, no build of `HPRebar/HPRebar` or `HPRebar.McpBridge`.

## Environment

| Item | Value |
|---|---|
| Date | 2026-10-04 (10:11–10:16 +07) |
| Git HEAD | `ffad05bdc6aa3caef0e30fe3dd1fc7c156861058` (branch RebarVersion1; working tree dirty only in agent memory / plans / golden-run scripts) |
| `dotnet --version` | 10.0.300 (every solution folder pins SDK 10.0.300 + MTP runner in its own `global.json`) |
| Python | 3.14 (`C:\Python314`) |

## Tests (sequential, Debug, run from the owning folder)

| Project | Cmd dir | Total | Pass | Fail | Skip | MTP duration | Wall (build+test) |
|---|---|---:|---:|---:|---:|---:|---:|
| HPRebar.Mcp.Server.Core.Tests | McpShared | 743 | 743 | 0 | 0 | 7.7 s | 21 s |
| HPRebar.McpBridge.Core.Net48Tests | McpShared | 113 | 113 | 0 | 0 | 9.2 s | 19 s |
| HPRebar.Mcp.Server.Tests | HPRebar | 109 | 109 | 0 | 0 | 10.5 s | 16 s |
| HPAutoCad.Mcp.Server.Tests | HPAutoCad | 280 | 280 | 0 | 0 | 14.5 s | 23 s |
| HPCivil3d.Mcp.Server.Tests | HPCivil3d | 106 | 106 | 0 | 0 | 7.2 s | 14 s |
| HPNavis.Mcp.Server.Tests | HPNavis | 49 | 49 | 0 | 0 | 4.8 s | 10 s |
| HPEtabs.Mcp.Server.Tests | HPEtabs | 81 | 81 | 0 | 0 | 11.3 s | 18 s |
| HPSap2000.Mcp.Server.Tests | HPSap2000 | 79 | 54 | 0 | 25 | 2.4 s | 8 s |
| HPRobot.Mcp.Server.Tests | HPRobot | 97 | 97 | 0 | 0 | 5.5 s | 12 s |
| HPExcel.Mcp.Server.Tests | HPExcel | 90 | 90 | 0 | 0 | 18.1 s | 27 s |
| HPPowerBi.Mcp.Server.Tests | HPPowerBi | 96 | 96 | 0 | 0 | 3.9 s | 10 s |
| HPTekla.Mcp.Server.Tests | HPTekla | 96 | 83 | 0 | 13 | 2.6 s | 8 s |
| **Total** | | **1 939** | **1 901** | **0** | **38** | | ~3 min |

Skips (all visible, host not installed on this machine):
- HPSap2000 25 = `SeedLibraryCompileTests.*` — "SAP2000 not installed (SAP2000v1.dll not found)".
- HPTekla 13 = `SeedCompilationTests.*` — "Trimble Tekla Structures 2025 not installed (Tekla Open API assemblies not found)".

Build warnings seen (incremental builds, so possibly incomplete): xUnit1051 ×3 in `McpShared/HPRebar.Mcp.Server.Core.Tests` (`MainThreadQueueTests.cs:198,210`, `ScriptCompilerTests.cs:36,98`), xUnit2013 ×1 `HPRebar/HPRebar.Mcp.Server.Tests/Registry/ToolRegistryTests.cs:442`.

Logs: `reports/test-logs/<project>.log`, `reports/test-logs/summary.tsv`.

## tools/list

Isolation: each exe started with `<PREFIX>Registry__LibraryPath` / `<PREFIX>Registry__DbPath` → `reports/tmp-registry/<host>/` (prefixes from `*HostProfile.cs`: HPREBAR_ (`McpShared/.../Hosts/HostProfile.cs:20`), HPAUTOCAD_, HPCIVIL3D_, HPNAVIS_, HPETABS_, HPSAP2000_, HPROBOT_, HPEXCEL_, HPPOWERBI_, HPTEKLA_ — all `_MCP_`). `tmp-registry/` deleted after each run (verified absent).

Hashes: **tools** = sha256 of `json.dumps(result.tools, sort_keys=False, ensure_ascii=False, separators=(',',':'))` (UTF-8); **raw** = sha256 of `mcp-call.py` stdout bytes (indented JSON, `PYTHONIOENCODING=utf-8`, CRLF line ends from Windows text mode — raw hash is only comparable on Windows with the same helper).

### A. Debug bin, built from HEAD by the test runs above — **use this as the before-snapshot**

Files: `reports/tools-list-baseline-debug/<host>.json`.

| Host | Exe | Tools | sha256 tools | sha256 raw |
|---|---|---:|---|---|
| Revit | HPRebar/HPRebar.Mcp.Server/bin/Debug/net10.0/HPRebar.Mcp.Server.exe | 33 | `40a4d950edc0d5e8c31624b36546eb0ce54f08c1091c0ccca7596a09a760c14b` | `8840d39686b76f2b901aba6dd06b5de499506bf7052704cf3b2b3432f7d874fc` |
| AutoCAD | HPAutoCad/HPAutoCad.Mcp.Server/bin/Debug/net10.0/… | 62 | `bdbb2729a41607cb7346979e5ea281dc9741ec80ec41fc8b4889a21447bc318a` | `b87df8f3c75bc217caef825404b96a5943d3fd2ec9ca33c079f7da9ccb6d185c` |
| Civil 3D | HPCivil3d/…/bin/Debug/net10.0/… | 24 | `4e1f49a3b5a44cc4bcf6e6c11b95ba64902cd21e17332a5b4f0551a3fe54639a` | `6e75c290f4f0bb56e39aad35a40d9d0587e2bc5df06140103a2cb36fbfc12678` |
| Navisworks | HPNavis/…/bin/Debug/net10.0/… | 24 | `d16b96554479b49343766b2f04f52471a574a566dfae23b75a1e27f71a9c9ecc` | `026143b7a00e869f753adc6c1991a028ebed5e6dc64f5cf80b204e5f06732e67` |
| ETABS | HPEtabs/…/bin/Debug/net10.0/… | 25 | `eee44fbad2eb7eb4f9a2286168284cdb169a45bbb9b5d26a621a8cec9d0d425c` | `1ffeba1acd0f047e782cd564b3be3e48473033a979e623fafee3c248b6cc337e` |
| SAP2000 | HPSap2000/…/bin/Debug/net10.0/… | 24 | `15c37be0340a0347ed1528b571526f113b1fea22e32584e60ce55c254056b611` | `120e06a2538e09664929ef4d29381d8516474f1d96c7cf26f2b00a67d4d16e05` |
| Robot | HPRobot/…/bin/Debug/net10.0/… | 24 | `36f71f0952073d4deea4d3424ec6f5df095eff77229b86c6adcd422c9dbea171` | `cd4842e685fbd1efb17b005809e4c6a898a3b7c0f0ba62040034902a49b7fe08` |
| Excel | HPExcel/…/bin/Debug/net10.0/… | 24 | `85a5bfd87c2705154ef5c9f2755183b879f401e4942a03e12a3f5556157a2b39` | `63af21f260a1e5b4438c0f12338bdddea2dae1aee90c0e65c2eac1a480be4352` |
| Power BI | HPPowerBi/…/bin/Debug/net10.0/… | 22 | `ff85e88ee87a6d8505d4cf1a415f158fbab9754388649b69684c5f70f6217d11` | `40f746efcf85a3fdca18fe945a90b10543261079a09f7f11342f2bfc771177eb` |
| Tekla | HPTekla/…/bin/Debug/net10.0/… | 24 | `ed7ba4b6c0f816314b69cc3541c99efed6a54ed2e5b5add3c0b0b231a9ceb17f` | `196b3195a658fd387a733c6fff08d6b1bf84aba4b6a9e8f26143547037435654` |

### B. Published exes (`<Host>/output/<Server>/<Server>.exe`) — reference only, STALE

Files: `reports/tools-list-baseline/<host>.json`. Tekla has no published exe (no `HPTekla/output/`), so B has 9 rows.

| Host | Exe mtime | Tools | sha256 tools | = Debug HEAD? |
|---|---|---:|---|---|
| Revit | 2026-09-12 | 33 | `6648c6fa6aa8c4e52da5c56464e46bcf827fc761e65c8b2bf92ee44723f68138` | **No** — predates the host-neutral engine descriptions (`get_run`, `inspect_type`, `publish_tool`, `search_tools` differ) |
| AutoCAD | 2026-09-17 | 62 | `bdbb2729…` | yes |
| Civil 3D | 2026-09-18 | 24 | `4e1f49a3…` | yes |
| Navisworks | 2026-09-15 | 24 | `d16b9655…` | yes |
| ETABS | 2026-09-17 | 24 | `6a2385c4a4244e6b942e3d417be0f8344f6e071ddd0dbc8158444425e4282d5e` | **No** — lacks `connect_etabs` (added by 37c538b, 2026-09-22) |
| SAP2000 | 2026-09-22 | 24 | `15c37be0…` | yes |
| Robot | 2026-09-22 | 24 | `36f71f09…` | yes |
| Excel | 2026-09-22 | 24 | `85a5bfd8…` | yes |
| Power BI | 2026-09-22 | 22 | `ff85e88e…` | yes |

**Consequence for the byte-identical gate (§2 criterion 6):** compare builds of the same configuration from before/after source (table A procedure: `dotnet build <Host>.Mcp.Server -c Debug` or the test run, then snapshot), never the published exes — 2 of 9 are stale against HEAD. Revit count is 33 (not the 34 in CLAUDE.md) because the isolated registry carries only the 21 seeds, not the user-approved `set_mark_from_comments`.

### Existing snapshot scripts (E11)

`plans/260915-0824-navisworks-mcp-2026/reports/snapshot-tools-list.ps1` (+ ETABS / Civil 3D copies, differing only in host list and output dir): same isolation idea, but (1) host list hard-coded to 2–3 hosts, (2) Release bin paths, (3) re-serialises with `ConvertTo-Json` after **sorting by name** — order-independent, so not a byte-identical check, (4) writes into its own plan folder, (5) records the sha256 of `HPRebar.Mcp.Server.Core.dll` (useful: proves the engine was rebuilt). Verdict: reusable as a pattern only; `reports/snapshot-tools-list-baseline.py` already covers all 10 hosts byte-exactly — extend it with the Core.dll hash rather than copying the .ps1 a fourth time.

## Seed scan (`reports/scan-seed-quality.py`)

> **Syntactic approximation** (regex + brace counting, strings stripped crudely). Not the planned Roslyn walker. Use for order of magnitude and allowlist seeding only.

Seed files under `<Host>.Mcp.Server/Registry/SeedLibrary/`:

| Host | Files | code.cs |
|---|---:|---:|
| HPRebar | 63 | 21 |
| HPAutoCad | 150 | 50 |
| HPCivil3d | 36 | 12 |
| HPNavis | 36 | 12 |
| HPEtabs | 36 | 12 |
| HPSap2000 | 36 | 12 |
| HPRobot | 36 | 12 |
| HPExcel | 36 | 12 |
| HPPowerBi | **0** | **0** |
| HPTekla | 36 | 12 |
| **Total** | **465** | **155** |

HPPowerBi has **no `Registry/SeedLibrary/`** folder (the csproj only conditionally embeds it, `HPPowerBi.Mcp.Server.csproj:28-29`); its 10 domain tools are compiled C# classes in `HPPowerBi/HPPowerBi.Mcp.Server/Tools/` (`PowerBi*Tool.cs`). A seed-quality test there has nothing to scan — relevant to §2 criterion 4 ("green in all 10 servers").

Counts (hits, not files):

| Host | code.cs | R-B1 | R-B2 | R-B3 | R-W1 | R-W2~ | R-W3 | R-W4 | R-W5 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| HPRebar | 21 | 0 | 0 | 0 | 3 | 1 | 1 | 2 | 0 |
| HPAutoCad | 50 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| HPCivil3d | 12 | 0 | 0 | 0 | 0 | 12 | 0 | 0 | 14 |
| HPNavis | 12 | 0 | 0 | 0 | 0 | 0 | 1 | 0 | 0 |
| HPEtabs | 12 | 0 | 0 | 0 | 0 | 0 | 2 | 0 | 0 |
| HPSap2000 | 12 | 0 | 0 | 0 | 0 | 0 | 2 | 0 | 0 |
| HPRobot | 12 | 0 | 0 | 0 | 0 | 2 | 7 | 0 | 0 |
| HPExcel | 12 | 0 | 2 | 0 | 0 | 2 | 2 | 0 | 0 |
| HPPowerBi | 0 | — | — | — | — | — | — | — | — |
| HPTekla | 12 | 0 | 0 | 0 | 0 | 1 | 3 | 0 | 0 |
| **Total** | 155 | **0** | **2** | **0** | 3 | 18 | 18 | 2 | 14 |

Rules: R-B1 commented-out code, R-B2 empty catch without comment, R-B3 > 300 lines (blocking); R-W1 brace block > 50 lines, R-W2~ > 3 nested control-flow braces (approximate), R-W3 vague declared name from the explicit list in the script (`data, tmp, temp, obj, res, result2, val, item2, foo, bar, x1, x2, stuff, thing, info2`), R-W4 `bool` parameter in a local function, R-W5 `catch (Exception…)` with neither `throw` nor `return`.

Blocking hits (only one file):

| File | Rule | Lines |
|---|---|---|
| HPExcel/HPExcel.Mcp.Server/Registry/SeedLibrary/Data/read_table/code.cs | R-B2 | 13, 25 (confirms E8) |

Top 10 largest code.cs (none near 300 → R-B3 baseline is 0; confirms E7):

| Lines | File |
|---:|---|
| 137 | HPRebar/…/Generic/create_line_based_element/code.cs |
| 102 | HPRebar/…/Data/ai_element_filter/code.cs |
| 101 | HPRebar/…/Architecture/create_point_based_element/code.cs |
| 99 | HPRebar/…/Annotation/create_dimensions/code.cs |
| 93 | HPRebar/…/Architecture/create_surface_based_element/code.cs |
| 88 | HPSap2000/…/Geometry/get_structural_objects/code.cs |
| 88 | HPRebar/…/View/operate_element/code.cs |
| 86 | HPExcel/…/Data/read_table/code.cs |
| 77 | HPEtabs/…/Geometry/get_structural_objects/code.cs |
| 76 | HPCivil3d/…/Pipe/list_pipe_networks/code.cs |

Observed false-positive classes (spot-checked; full list in `reports/seed-scan-output.md`):
- R-W5: all 14 Civil 3D hits are `catch (Exception ex) { Fail(Classify(ex), ex.Message, handle); }` — the per-item `errors[]` pattern the seed contract asks for (e.g. `Alignment/list_alignments/code.cs:36`). "Returns an error" must count a call that records into `errors`, or the rule warns on every Civil seed.
- R-W3: `x1/x2` hits (ETABS/SAP2000/Robot/Tekla `draw_*_by_coords:1-2`) are coordinate names mirroring schema keys — drop `x1`/`x2` from the list. `data` hits (Navis `summarize_by_category:23`, Revit `get_material_quantities:16`) are genuine.
- R-W2~: Civil `list_pipe_networks` (10 lines flagged) — brace counting overstates; real walker should count control-flow statements, not braces.
- R-W4: Revit `create_level:28` `void Plan(bool wanted, …)` — a genuine flag-argument local function.

## Other facts

| Item | Value |
|---|---|
| `.editorconfig` | only `HPRebar/.editorconfig` (none at repo root, none in McpShared, HPAutoCad, HPCivil3d, HPNavis, HPEtabs, HPSap2000, HPRobot, HPExcel, HPPowerBi, HPTekla) → 10 solution folders lack one, not 9 (McpShared counts) |
| Solutions | 11 `.slnx`: McpShared + 10 host folders (`HPGeo/` has no `.slnx` at its root) |
| Live registry roots | Isolation env honoured by the tools/list runs (each wrote only into `tmp-registry/`). But `%AppData%\HPExcel\McpServer\registry.db` mtime = 10:14:09, inside the HPExcel test window (≈10:13:56–10:14:23). HPExcel tests build the real host with default options (`ExcelCatalogCompletenessTests.cs:61`, `ExcelServerIntegrationChallengeTests.cs:182`) — **inference, unverified**: those tests open the user's live Excel registry DB. Several long-running MCP server processes (from other Claude sessions) are also alive and could be the writer. Log as a candidate test-isolation defect, not fixed here. |

## Not run

| Item | Reason |
|---|---|
| Any `Debug.R2x` / `Release.R2x` build, `HPRebar/HPRebar`, `HPRebar.McpBridge` | Forbidden by the brief (Revit running, DLL locks) |
| Tekla published-exe tools/list | No `HPTekla/output/` folder; Debug bin used instead (table A) |
| SAP2000 seed compile tests (25), Tekla seed compile tests (13) | Host not installed — skipped by the tests themselves |
| Release-bin tools/list (the old scripts' path) | Not needed; Debug bin from HEAD is the same-source before-snapshot |
| Host bridge tests (`*.McpBridge.Tests`, Navis net48 bridge tests, etc.) | Outside §5 list |

## Commands used

```bash
# tests (sequential; script: reports/run-baseline-tests.sh)
cd McpShared && dotnet test HPRebar.Mcp.Server.Core.Tests
cd McpShared && dotnet test HPRebar.McpBridge.Core.Net48Tests
cd <Host>    && dotnet test <Host>.Mcp.Server.Tests      # Host in HPRebar HPAutoCad HPCivil3d HPNavis HPEtabs HPSap2000 HPRobot HPExcel HPPowerBi HPTekla

# tools/list (script: reports/snapshot-tools-list-baseline.py published|debug), per host:
PYTHONIOENCODING=utf-8 python McpShared/tools/mcp-call.py <exe> tools/list \
  --env <PREFIX>Registry__LibraryPath=<reports>/tmp-registry/<host>/tools-library \
  --env <PREFIX>Registry__DbPath=<reports>/tmp-registry/<host>/registry.db

# seed scan
python plans/261004-1005-hp-clean-code-ai-tools/reports/scan-seed-quality.py --details > reports/seed-scan-output.md

git rev-parse HEAD; dotnet --version
```
