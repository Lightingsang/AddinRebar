# HPNavis MCP — tool catalog: the 8 BIM-coordination seeds (probe disciplines, list / sync / validate search sets, sync colour sets, paint colours, sync clash tests per LOD, run canary tests)

Generated from `tools/list` of `HPNavis.Mcp.Server.exe` (32 tools in all) on an isolated registry — the surface a fresh install shows; the user's own registry may add approved tools. Names are `mcp__hprebar-navis__<name>` in Claude Code. `REQ` = required. Every seed takes and reports **millimetres** (the API itself works in the document's units — `units` converts); item ids are instance-guid hashes from earlier results. Seed descriptions end with `[Registry tool v1, <Category>, transaction=…]`; that suffix is stripped here.

## Coordination

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
| `lod` | string 200 \| 300 \| 350 | "350" | LOD whose tests to run |
| `count` | integer | 3 | How many canaries when ruleIds is empty |
| `ruleIds` | array<string> |  | Run exactly these rules' tests, e.g. ["HP_M2_S5"]; each must be up to date with non-empty sets |
| `dryRun` | boolean | false | Run the tool, then roll everything back. Use first on a model you care about. |

### `bim_sync_clash_tests` — Sync the HP clash tests for a LOD

*destructiveHint.* One Hard clash test per rule of the HP clash matrix (184 pairs, priority P1/P2/P3) eligible at the LOD: LOD200 = 27 tests @ 50 mm, LOD300 = 154 @ 30 mm, LOD350 = 184 @ 10 mm; LOD400 is refused (no approved tolerance). Name HP|P<n>|LOD<lod>|<ARC-STR>|<A8-S2>, selections = the two base search sets (run bim_sync_search_sets with apply=true first). apply=false (default) previews create/update/unchanged/skip per rule; apply=true writes them, upserting by name (an existing test keeps its results, statuses and comments; nothing is removed; tests the matrix no longer lists are reported as orphans) and reads every written test back. A rule whose set is missing or finds nothing is skipped, never widened to the whole model. Does not run tests. One Undo entry; dryRun undoes it.

| arg | type | default | description |
|---|---|---|---|
| `lod` | string 200 \| 300 \| 350 \| 400 | "350" | Design stage: 200 basic, 300 technical, 350 construction drawings (400 is refused until a tolerance is approved) |
| `apply` | boolean | false | false = preview only; true = write the tests |
| `priorities` | array<integer> |  | Only these priorities (1 = HIGH, 2 = MEDIUM, 3 = LOW); empty = all |
| `ruleIds` | array<string> |  | Only these rules, e.g. ["HP_A8_S2"]; empty = all eligible |
| `listUnchanged` | boolean | false | Also list the rules whose test is already up to date |
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

*destructiveHint.* Builds the HP search-set registry as live searches: the 21 base sets of the clash matrix (A1..A9 Architecture, S1..S5 Structure, M1..M7 MEP) and, with includeExtras, MEP detail sets (cable trays by model, pipes by System Classification, clash scope pipes D >= 32 mm) and auxiliary sets for elements no matrix group covers. Each set = source file has the discipline's ISO role code AND Element > Category is one of the set's Revit categories AND its extra conditions (e.g. A6: Structural <> true), so ARC/STR walls and floors and Electrical/HVAC equipment never mix. apply=false (default) previews item counts and create/unchanged/conflict. apply=true writes under Sets > HP BIMCoordinator: missing sets are created, identical ones left alone, a set whose saved search differs is a conflict left untouched unless allowUpdate=true with the approved codes. Sets under the top folder that no registry entry owns are reported as orphans; nothing is removed. One Undo entry; dryRun undoes it. Takes about a minute on a 95 MB model.

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

