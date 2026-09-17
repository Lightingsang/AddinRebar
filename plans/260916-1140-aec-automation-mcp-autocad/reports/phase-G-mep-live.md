# Phase G — MEP: implementation + live verification report (2026-09-17)

## Implemented

| Piece | Files | Notes |
|---|---|---|
| Model (pure) | `HPAutoCad.Aec/Mep/MepModel.cs` — `MepRunKind` pipe|duct|tray, `MepNodeKind` equipment|fixture|terminal|fitting (`MustBeServed`), `MepRun` (open chain, system from the caller's layer map else the layer), `MepNode`, `MepEndpoint` (state joined|tee|node|open, `ConnectedTo`, nearest run/node within the near-miss reach), `MepNetwork` (runs, the nodes it touches, open ends, systems, `DuplicateOverlapMm`), `MepDuplicate`, `MepOutcome` | |
| Graph (pure) | `Mep/MepNetworkBuilder.cs` — a run end connects when within `tolerance.endpointConnection` of another run's end (joined), a run's body — another's or its own loop, never the segment it sits on — (tee) or a node (node); a node is attached to every run that ends on it **or passes through it** (inline valve / pump / VAV); union-find over runs: direct connections join, a node joins only runs of the same system (supply and return meet at an AHU without merging; a node belongs to every network it touches); crossings counted, never connected; duplicates = same system, parallel within `parallelAngle`, offset ≤ `endpointConnection`, shared run > `tolerance.duplicate` (collinear or a copy a few mm off); `DoubleLineDucts` heuristic (parallel duct within 1 500 mm over half the length); networks longest first `N-nnn`; `MaxElements` 20 000; `ct` in every loop, `SpatialIndex` everywhere | |
| Checks (pure) | `Mep/MepChecks.cs` — `near_miss` (one per facing pair at the midpoint), `open_end`, `disconnected_run` (both ends open, nothing touching it, nothing near — one issue), `orphan_node` (warning for equipment/fixture/terminal, info for a fitting; none when the drawing has no runs), `duplicate_run`, `mixed_system` (runs of several systems joined directly; 4 handles + 5 systems listed); ids `MEP-nnn` after `AuditIssue.Ordered`; invariant culture | |
| Read adapter | `Cad/MepService.cs` — MEP discipline classification in one pass, or two when the filter names no layers/types (the rule set's MEP layers + types; its fitting block names on INSERTs), no text index (no MEP rule reads text), `examined` = both passes; `filter.space: all` refused; closed shapes on run layers and runs ≤ `tinySegment` set aside and counted; `ParseSystems` (blank / duplicate names refused; first declared match wins), `Describe` (network: handles ≤ 16, open ends ≤ 8; endpoint: `connectedTo` ≤ 4 + `connectedCount`) | |
| Facade + seeds | `AecTools.Mep.cs` — `MepDetectNetwork` (`MaxNetworkLimit` 30), `MepConnectivityCheck` (`MaxIssueLimit`), `MepEndpointCheck` (`MaxEndpointLimit` 150, `includeConnected`); shared `detection` block (`MepDetectionKeys`: nearMissMm — must be above endpointConnection —, systems) + `tolerance` + `maxCandidates`; warnings for no runs / narrowed scan / truncation / closed or tiny runs / double-line duct drafting; seeds `MEP/mep_detect_network`, `mep_connectivity_check`, `mep_endpoint_check` (all `none`); category MEP | server 55 tools (4 + 8 + 43 seeds) |
| Rule set | `Rules/aec-classification.default.json` v4: pipe / duct / tray runs accept ARC + SPLINE and are `closed: false`; the generic `CO-*` fitting pattern replaced by `CO-ONG*` | |
| Tests | `MepTests` (6: joins / tees / nodes / crossings, near miss + checks named once, systems + mixed, inline nodes + AHU never merging systems, offset copy / ring main / self-loop / facing ends / no runs / double-line ducts, envelopes at every cap incl. a page of `mixed_system` issues) → `HPAutoCad.Aec.Tests` 197; `SeedLibraryTests` 43 seeds + MEP detection pin → `HPAutoCad.Mcp.Server.Tests` 241 | |
| Harness | `aec-tools-live.py` step V (9 checks; scene + an MEP set at (60 000, 0): a pipe main with a tee branch, a branch 50 mm short, a lost run, a copy over the main, an inline gate-valve block on the main, a duct served by a diffuser block, an orphan diffuser → 64 entities) | |

## Build / tests

| Check | Result |
|---|---|
| `dotnet build HPAutoCad.slnx -c Debug` | 0 warnings; bundle deployed |
| `HPAutoCad.Aec.Tests` | 197/197 |
| `HPAutoCad.Mcp.Server.Tests` | 241/241 (43 seeds) |

## Live (AutoCAD 2026, 2026-09-17)

`run-aec-tools-live.ps1` **83/83** · `run-aec-edit-tools-live.ps1` **89/89** (regression).

| Step | Verified |
|---|---|
| V network | 5 networks: the pipe main + tee branch + overlapping copy (`N-001`, 15 m, 2 m drawn twice, 3 open ends, the inline valve attached as its node), the duct with its diffuser (node), the old pipe polyline, the lost run, the short branch (its open end names the main at 50 mm); 3 nodes, 1 orphan (the far diffuser), 1 duplicate, 0 crossings. `detection.systems {CHW: [M-PIPE*], SA: [M-DUCT*]}` → networks CHW / SA; `tolerance.endpointConnection 60` → the short branch joins the main (4 networks); `nearMissMm 10` (= endpointConnection) → ArgumentException |
| V connectivity | `near_miss` 50 mm (short → main), `open_end` ×5, `disconnected_run` ×2 (the lost run, the old polyline), `duplicate_run` 2 000 mm, `orphan_node` warning for the far diffuser only; `MEP-001` first; on the diffuser layer alone (no runs) → warned, no orphan issues |
| V endpoints | 10 open ends listed, the near miss first with its gap; 14 endpoints in all (`byState` tee 3, node 1, open 10); pipe layer + `includeConnected` → 12, the tee ends name the main |

## Review round (2026-09-17, `plans/reports/code-review-2026-09-17-aec-phase-g.md`, 5.5/10 → fixed)

| # | Finding | Fix | Pinned by |
|---|---|---|---|
| H1 | a node a run passes through (inline valve, pump, VAV) was an orphan; a branch ending inside a VAV on the main made two networks | nodes attach to every run touching them (vertex inside / segment within tolerance / crossing); same-system runs at a node join | `Inline_nodes_are_attached…`; live V (valve) |
| H2 | a node unioned every system: supply + return + ducts through one AHU = one "mixed" network + a `mixed_system` per equipment | union-find over runs; a node joins runs of one system only and belongs to every network it touches; `mixed_system` only for direct joins | `Inline_nodes…` (4 networks through one AHU, no mixed) |
| M1 | a copy 2 mm off-axis was a tee at both ends, never a duplicate | duplicates by projection: parallel, offset ≤ endpointConnection, shared run > duplicate; `duplicateOverlapMm` per network and in the summary | `An_offset_copy_is_a_duplicate…`; live V (2 000 mm) |
| M2 | two facing open ends = two `near_miss` issues | paired: one issue, both handles, midpoint | `…facing_ends_are_one_near_miss` |
| M3 | O(networks × endpoints) open-end scan without `ct`; segment pairs without a bounds check | open ends indexed per run once; `ct` in every loop; segment-pair bounds pre-check in the parallel/crossing tests | — |
| M4 | `connectedTo` uncapped (64 KB at 200 endpoints), `mixed_system` pages over the cap | `connectedTo` ≤ 4 + `connectedCount`, `MaxEndpointLimit` 150, `mixed_system` 4 handles + 5 systems; size tests at every cap incl. a page of `mixed_system` | `Envelopes_at_the_caps…` |
| M5 | no runs → every fixture an `orphan_node` warning | warned by all three tools; no orphan issues without runs | live V (diffuser layer alone) |
| M6 | double-line ducts read as two side networks, nothing said | `doubleLineDucts` heuristic + warning; descriptions say single-line (centreline) drafting | `…double-line ducts` |
| M7 | a run's own body excluded: a ring main 60 mm from closing was "connects to nothing" | the own body counts through its other segments and far end (never the segment the end sits on): tee onto itself, near miss to itself | `…a_ring_main_finds_its_own_far_end…` |
| L1–L9 | input-order duplicate handles; first-match `SystemOf` undocumented, blank names; `examined` = pass 1, text index built twice; ARC/SPLINE ducts and trays, `CO-*`; zero-length runs; `space: all`; `nearMissMm == endpointConnection` disabling near misses; `mixed_system` location, `bySystem` cut, stale comment, `systems` schema; state rank | ordinal pair + handle tie-break; documented, refused; summed, `TextIndex.Empty`; rule set v4; `tinyRunsIgnored`; refused; `>` required; first run's midpoint, `systemsTotal`, comment, `additionalProperties`; documented | tests / seeds |

Decision (recommended by the review, taken): double-line ducts are not paired into centrelines — the tools read single-line drafting and warn when a plan looks double-line.

## Known limitations (phase G)

- Single-line (centreline) drafting: a duct or pipe drawn as two side lines is two runs (warned when it looks that way).
- A 2D plan has no heights: crossings are never connections; a riser drawn as a circle at the end of a pipe is an `open_end` unless the circle is on an equipment/fixture layer.
- A node "touched" by a run passing through it is attached to that run even when the symbol merely overlaps the line.
- Duplicates are pairwise stretches; three stacked copies report three pairs.
- 400 stacked sampled arcs over one another still take minutes (every segment pair of overlapping bounds is tested); split the plan or filter the layers.
