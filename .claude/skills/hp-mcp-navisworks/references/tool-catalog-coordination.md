# HPNavis MCP — tool catalog: the 10 BIM-coordination seeds (probe disciplines, list / sync / validate search sets, sync colour sets, paint colours, sync clash tests per phase, run canary tests, run a phase in batches, export the clash snapshot with pictures)

Generated from `tools/list` of `HPNavis.Mcp.Server.exe` (34 tools in all) on an isolated registry — the surface a fresh install shows; the user's own registry may add approved tools. Names are `mcp__hprebar-navis__<name>` in Claude Code. `REQ` = required. Every seed takes and reports **millimetres** (the API itself works in the document's units — `units` converts); item ids are instance-guid hashes from earlier results. Seed descriptions end with `[Registry tool v1, <Category>, transaction=…]`; that suffix is stripped here.

## Coordination

### `bim_export_clash_snapshot` — Export the HP clash results of one design phase

*readOnlyHint.* HEAVY (writes files): reads the HP clash tests of one design phase (names HP|LOD<phase>|…) and every result — test, rule, priority group, discipline pair, result GUID, status, distance and centre in mm, the two Revit elements (name, category, Element Id, source file) and the level — and writes the snapshot JSON to the ClashReports folder beside the open model (the folder comes from the model, never from the caller; an unsaved model or a UNC path is refused). With images (default) it first writes the Navisworks clash report (Report tab, HTML (Tabular), all tests combined, every field) of the New/Active/Reviewed/Resolved results into a folder named after the snapshot — one picture per clash, rendered by Navisworks; the Report tab's own settings are restored afterwards. Read-only for the model: nothing in it changes. Run the tests first (bim_run_phase_tests or Clash Detective > Run All); tests that never ran are counted and warned. Needs 'Allow heavy operations' ticked in the bridge window. Returns the file name, the HTML report path and the totals by status and level; turn the files into the workbook (summary, CLASH DESCRIPTION with pictures, by level, every clash, changes since the previous export) with tools/bim-coordinator/build-clash-report.py — the skill /hp-bim-clash-report does both.

| arg | type | default | description |
|---|---|---|---|
| `lod` | string auto \| 200 \| 300 \| 350-400 | "auto" | Design phase to export; auto = the one phase the model's HP tests belong to |
| `images` | boolean | true | Also write the Navisworks HTML report with one picture per reported clash (takes longer: Navisworks renders each picture) |
| `maxPictures` | integer | 500 | Most pictures in the HTML report, taken in test-name order (priority group 1 first); the other reported clashes stay in the snapshot without a picture |

### `bim_list_selection_sets` — List saved selection and search sets

*readOnlyHint.* Read-only inventory of the saved selection/search sets and folders: path, kind (search, explicit = static selection, folder), guid, prune-below-match and condition count; withConditions adds every search condition as text within a 40 000-character budget (sets past it are listed without conditions — ask for one folder). Use it as the record of what existed before bim_sync_search_sets applies anything (the full restore point is the NWD file) and to find old static sets or hand-made search sets. folder narrows to one folder path (e.g. HP BIMCoordinator/MEP); nothing is evaluated, so it is fast.

| arg | type | default | description |
|---|---|---|---|
| `folder` | string |  | Folder path to list, '/'-separated; empty = the whole Sets tree |
| `withConditions` | boolean | false | Include the search conditions as text (bounded) |

### `bim_paint_colors` — Paint the HP colour sets

*destructiveHint.* Permanent colours (the Appearance Profiler look, saved in the NWD) from the company sheet ColorSearchSet(DSC), read from the saved sets under Sets > HP BIMCoordinator > Color (create them first with bim_sync_color_sets). mode=preview (default): which set paints how many elements, nothing written. mode=apply: paints in sheet order, every element once with the colour of its last set (a nested element in a later set keeps that set's colour), then reads a sample back from the element geometry. mode=verify: read-back only. mode=reset: removes the permanent colours (and transparency) of the current painting sets' elements only — colours the user set on other elements stay, and so does an old colour on an element that has left every set. success=false when a painting set is missing from the document (run bim_sync_color_sets apply first). <Default> sets (HP_A_All, HP_S_All, communication, conduit, data devices) never paint. Use dryRun with apply/reset to preview the undo; one Undo entry; codes narrows to some sets.

| arg | type | default | description |
|---|---|---|---|
| `mode` | string preview \| apply \| verify \| reset | "preview" | preview = counts only; apply = paint + read-back; verify = read-back; reset = remove the colours of the painting sets' elements |
| `codes` | array<string> |  | Only these colour sets, by code (C17) or sheet name (HP_P_Drainage_RainWater); empty = all |
| `dryRun` | boolean | false | Run the tool, then roll everything back. Use first on a model you care about. |

### `bim_probe_disciplines` — Probe disciplines for the HP clash matrix

*readOnlyHint.* Read-only survey the HP clash matrix depends on, run before bim_sync_search_sets: every appended file with its ISO 19650 role code (AA = ARC, ES = STR, EC/EE/EF/EP = MEP) and discipline, whether the Item > Source File and Element > Category properties exist under the internal names the base sets use, and the Revit categories each discipline really contains with counts. Warnings list files with no known role and base sets (A1..A9, S1..S5, M1..M7) whose categories occur nowhere in their discipline.

| arg | type | default | description |
|---|---|---|---|
| `maxItemsPerDiscipline` | integer | 200000 | Stop counting categories of a discipline after this many elements (truncated is reported) |

### `bim_run_canary_tests` — Run canary tests of the HP clash matrix

*destructiveHint.* HEAVY: runs a few of the HP clash-matrix tests written by bim_sync_clash_tests to prove they catch real clashes before the BIM coordinator runs them all (Clash Detective > Run All). Default picks up to 3 up-to-date tests with non-empty sets, HIGH priority first, one per discipline pair (ARC-STR, MEP-STR, ARC-MEP...); ruleIds picks them explicitly. Needs 'Allow heavy operations' ticked in the bridge window; a run cannot be interrupted or undone and may take minutes — save the file first. Returns each test's result counts by status and its run time; compare with a manual Run of the same test.

| arg | type | default | description |
|---|---|---|---|
| `lod` | string 200 \| 300 \| 350-400 | "350-400" | Design phase whose tests to run: 200, 300 or 350-400 |
| `count` | integer | 3 | How many canaries when ruleIds is empty |
| `ruleIds` | array<string> |  | Run exactly these rules' tests, e.g. ["HP_M2_S5"]; each must be up to date with non-empty sets |
| `dryRun` | boolean | false | Run the tool, then roll everything back. Use first on a model you care about. |

### `bim_run_phase_tests` — Run the HP clash tests of one design phase in batches

*destructiveHint.* HEAVY: runs the HP clash-matrix tests of one design phase (names HP|LOD<phase>|…, in name order: priority group, then pair) one after the other, starting a test only while the slowest one so far still fits in the time budget, and returns nextAfter — call again with after = nextAfter until it is null. A test that fails is listed in failed[] and the batch goes on. onlyNotRun runs only the tests that never ran (resume after Navisworks was closed). Needs 'Allow heavy operations' ticked in the bridge window and the file saved first: runs cannot be interrupted, and a call that fails or times out is rolled back by the bridge, which undoes that call's runs. Returns each test's result counts by status and its run time. Other tests (not HP|…) are never run.

| arg | type | default | description |
|---|---|---|---|
| `lod` | string auto \| 200 \| 300 \| 350-400 | "auto" | Design phase whose tests to run; auto = the one phase the model's HP tests belong to |
| `after` | string |  | Start after this test name: empty for the first batch, then the nextAfter of the previous call |
| `onlyNotRun` | boolean | false | Only the tests that never ran (the offset counts within that shorter list) |
| `budgetSeconds` | integer | 300 | Start no new test once elapsed time plus the slowest test so far would pass this many seconds; the call itself may last up to 600 s |
| `dryRun` | boolean | false | Run the tool, then roll everything back. Use first on a model you care about. |

### `bim_sync_clash_tests` — Sync the HP clash tests for a LOD

*destructiveHint.* One Hard clash test per rule of the HP clash matrix (184 pairs, priority P1/P2/P3) eligible in ONE design phase: 200 basic design = 27 tests @ 50 mm, 300 technical design = 154 @ 30 mm, 350-400 construction drawings = 184 @ 10 mm (the sheet's column G covers both). Name HP|LOD<phase>|P<n>|<ARC-STR>|<A5 Doors>-<S2 Structural Walls> (sort Clash Detective by name; the API cannot create test folders); tests named the earlier ways (no group names, LOD350/LOD400) are renamed with their results kept. Selections = the two base search sets, except M4 pipes which select M4.9 (pipes D >= 32 mm, methodology note 3; run bim_sync_search_sets with apply=true first, it writes M4.9 too). Rules tab: ignore items in same group/block/cell always, coincident snap points only inside one discipline, never same layer or same file; Composite Object Clashing on. apply=false (default) previews create/update/unchanged/skip per rule and lists the HP tests of other phases and duplicates; apply=true writes, upserting by name, and reads every written test back. removeOtherPhases=true (approval needed, whole model only) also deletes those other-phase/duplicate HP tests — their results are lost; tests not named HP|… are never touched. A rule whose set is missing or empty is skipped, never widened. Does not run tests. One Undo entry; dryRun undoes it.

| arg | type | default | description |
|---|---|---|---|
| `lod` | string 200 \| 300 \| 350-400 | "350-400" | Design phase: 200 = basic design (thiết kế cơ sở), 300 = technical design (thiết kế kỹ thuật), 350-400 = construction drawings (bản vẽ thi công); "350" and "400" are read as 350-400 |
| `apply` | boolean | false | false = preview only; true = write the tests |
| `priorities` | array<integer> |  | Only these priorities (1 = HIGH, 2 = MEDIUM, 3 = LOW); empty = all |
| `ruleIds` | array<string> |  | Only these rules, e.g. ["HP_A8_S2"]; empty = all eligible |
| `listUnchanged` | boolean | false | Also list the rules whose test is already up to date |
| `removeOtherPhases` | boolean | false | Delete the HP tests of the other design phases and duplicates (approval required; results are lost) |
| `dryRun` | boolean | false | Run the tool, then roll everything back. Use first on a model you care about. |

### `bim_sync_color_sets` — Sync the HP colour search sets

*destructiveHint.* Builds the colour search sets of the company sheet ColorSearchSet(DSC) under Sets > HP BIMCoordinator > Color, named exactly as the sheet (HP_A_All, HP_E_CableTray(ELV), HP_P_Drainage_RainWater ...). Each set = source file of its discipline AND Revit category AND its system rule (System Type such as "TNM" for rain water, System Classification such as Fire Protection Wet, or the model file for ELV/LV cable trays). Sets without a reliable rule yet are listed as pending and never built. apply=false (default) previews item counts; apply=true creates missing sets, leaves identical ones, reports a set whose saved search differs as a conflict unless allowUpdate=true with the approved codes. Nothing is removed and nothing is coloured here — colour with bim_paint_colors. One Undo entry; dryRun undoes it.

| arg | type | default | description |
|---|---|---|---|
| `apply` | boolean | false | false = preview only; true = write the sets |
| `allowUpdate` | boolean | false | Replace the named saved sets whose search differs from the registry (approval required; needs codes) |
| `codes` | array<string> |  | Only these sets, by code (C03) or sheet name (HP_E_CableTray(ELV)); empty = every set that is not pending |
| `dryRun` | boolean | false | Run the tool, then roll everything back. Use first on a model you care about. |

### `bim_sync_search_sets` — Sync the HP search-set registry

*destructiveHint.* Builds the HP search-set registry as live searches: the 21 base sets of the clash matrix (A1..A9 Architecture, S1..S5 Structure, M1..M7 MEP) plus M4.9 (pipes D >= 32 mm), the set the M4 clash tests select and, with includeExtras, MEP detail sets (cable trays by model, pipes by System Classification, clash scope pipes D >= 32 mm) and auxiliary sets for elements no matrix group covers. Each set = source file has the discipline's ISO role code AND Element > Category is one of the set's Revit categories AND its extra conditions (e.g. A6: Structural <> true), so ARC/STR walls and floors and Electrical/HVAC equipment never mix. apply=false (default) previews item counts and create/unchanged/conflict. apply=true writes under Sets > HP BIMCoordinator: missing sets are created, identical ones left alone, a set whose saved search differs is a conflict left untouched unless allowUpdate=true with the approved codes. A set saved at an earlier registry place (e.g. X.1/X.2 under Not in matrix, now under MEP/Plumbing fixtures) is moved and renamed there instead of duplicated (action moved; moved+conflict when its search also changed). Sets under the top folder that no registry entry owns are reported as orphans; nothing is removed. One Undo entry; dryRun undoes it. Takes about a minute on a 95 MB model.

| arg | type | default | description |
|---|---|---|---|
| `apply` | boolean | false | false = preview only; true = write the sets |
| `allowUpdate` | boolean | false | Replace the named saved sets whose search differs from the registry (BIM lead approval; requires codes); false = report them as conflicts |
| `includeExtras` | boolean | false | Also the detail and auxiliary sets (ignored when codes are given) |
| `codes` | array<string> |  | Only these sets, e.g. ["A8", "S2", "M4.1"]; empty = the 21 base sets (plus extras with includeExtras) |
| `dryRun` | boolean | false | Run the tool, then roll everything back. Use first on a model you care about. |

### `bim_validate_search_sets` — Validate the HP search sets

*readOnlyHint.* Read-only acceptance check of the HP search-set registry against the open model. scope=base (default): the 21 base sets as Navisworks resolves the saved sets (count per set), sets missing from the document or whose saved search differs from the registry, empty sets, elements whose Revit category or source-file discipline is not the set's, elements in two base sets, and every Revit element no base set takes (gaps by discipline|category). scope=extras: detail and auxiliary sets, detail sets leaving their parent or overlapping inside one folder, and parent elements no detail covers. Issues are error / warning / info. About a minute per scope on a 95 MB model.

| arg | type | default | description |
|---|---|---|---|
| `scope` | string base \| extras | "base" | base = the 21 matrix sets + gaps; extras = detail and auxiliary sets |

