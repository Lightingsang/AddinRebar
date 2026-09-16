# Phase A — Core: implementation + live verification report (2026-09-16)

## Implemented

| Piece | Files | Notes |
|---|---|---|
| `HPAutoCad.Aec` (net8.0-windows) | `Geometry/` Pt, Box, Seg, PlanShape, GeometryTolerance, GeometryMath · `Spatial/` SpatialRelation, SpatialIndex, SpatialPredicates · `Issues/` GeometryIssue, GeometryIssueDetector · `Model/` ToolError, AnalysisResult/EditResult, AecEntityRecord, EntityFilter · `Cad/` HandleResolver, EntityShapeReader, EntityQueryService, DrawingContextReader, MeasureService, SpatialQueryService · `AecTools` facade | 1 754 LOC; pure parts never touch an AutoCAD type; `Shape` renamed `PlanShape` (AutoCAD has a `Shape` entity) |
| `HPAutoCad.Aec.Tests` | GeometryMathTests, SpatialTests, GeometryIssueDetectorTests, ModelTests | 45 tests |
| Seeds | `SeedLibrary/Drawing/get_drawing_context`, `Data/query_entities`, `Data/query_entities_spatial`, `Geometry/measure_geometry`, `Audit/detect_geometry_issues` | thin shims: read `args` → one `AecTools.*` call → envelope; all `transaction: none` |
| Bridge wiring | `HPAutoCad.McpBridge.csproj` (ProjectReference), `BridgeEntry.CompilerReferences` (+ Aec assembly), `ScriptingSelfCheck` probe (`aec tolerance 0.5` in the log), `HostScriptContracts.AutocadImports` (+ `HPAutoCad.Aec`), `AutocadHostProfile.Categories` (+ Geometry, Audit) | McpShared change additive |
| Tests updated | `HPAutoCad.Mcp.Server.Tests` → net10.0-windows + Aec metadata reference; 17 seeds; new shim theory | 83 tests |
| Harness | `tools/harness/aec-tools-live.py` + `run-aec-tools-live.ps1` (isolated registry root, Debug exe by default) | 33 checks |

## Build / tests

| Check | Result |
|---|---|
| `dotnet build HPAutoCad.slnx -c Debug` | 0 warnings, 0 errors; bundle deployed with `Contents\Bridge\HPAutoCad.Aec.dll` |
| `HPAutoCad.Aec.Tests` | 54/54 (run 1: 3 failures fixed — computed members serialised, reversed-chain duplicate matching, connected pairs reported as gaps; +9 `ReviewRegressionTests` after the code review) |
| `HPAutoCad.Mcp.Server.Tests` | 83/83 (every seed: record, validator, guard/analyzer, compile against AutoCAD.NET + Aec) |
| `McpShared` engine | 128 + 60 (net48) |
| `HPRebar.Mcp.Server.Tests` | 109 |

## Live (AutoCAD 2026, `run-aec-tools-live.ps1`, Debug server exe, isolated registry)

Run 1: 30/33 — 3 harness/envelope defects: scene entity count miscounted (19, not 20); `measure_geometry` said `success: true` beside a `NOT_CLOSED` error when nothing was measured (fixed: success = no errors or ≥ 1 item). Run 2: **33/33**. Run 3 (after the `EntityShapeReader` split and the new performance step P): **37/37**. Run 4 (after the code review — scene gained a mirrored arc, a bulge polyline, a hatch and a block with an attribute; step G): 40/42, the two failures were harness expectations (a +1 bulge sweeps below the chord; call count) → run 5: **42/42**.

The template drawing is imperial (INSUNITS Inches, 25.4 mm/unit): every mm figure below came back correct through `units`, so unit handling is exercised, not assumed.

| Area | Checks (all PASS) |
|---|---|
| Context | units/layout/counts/UCS; layouts listed; S-COL layer with colour 1 |
| query_entities | 4 columns by type+layer; paging count 7 / 3 items / truncated / offset; detail geometry (closed, 4 vertices, 160 000 mm²) + colour/linetype; textContains + property selector + unknown-property warning; singular key + unknown key warning |
| query_entities_spatial | crosses pipe × beam (+ its duplicate) at (3000, 0); within: circle + text in the room; nearest: one column per beam, distance 0; distance_to 50 mm → c3 0 / c4 7; touches with 10 mm yes / 5 mm no; unknown relation → ArgumentException listing the relations |
| measure_geometry | totalLength b1 + b2 + quarter arc = 12 763.8 (exact arc); areas 160 000 + π·300²; open outline → `NOT_CLOSED`, success false; distances points 5 000 / shapes 5 600 / point–beam 700 with closest point; exact `IntersectWith` 1 point at (3000, 0); direction 0° + angle 69.444°; bbox union; closest point on a circle; centroid |
| detect_geometry_issues | duplicate b1/b3, endpoint_gap g1/g2 = 7 mm, overlapping w1/w2, open_polyline 12 mm, self_intersection, zero_length; issue fields complete; restricted types → 0; tolerance 5 mm hides the 7 mm gap |
| errors | invalid handles → `INVALID_HANDLE` ×2 with the valid one returned; erased → `ERASED`; unknown measure → ArgumentException; every run `changed` = 0 |
| geometry through AutoCAD's own maths (step G) | mirrored arc (normal −Z) reported in the upper-left quadrant of its centre with every tessellated vertex on the true circle (the pre-review code reflected it); bulge polyline length π·1000 with the semicircle below the chord; hatch area 2 000 000 mm² and a 4-vertex footprint; block found by `DOOR-*` + attribute text `d01` with `MARK = D01` |
| performance (step P) | 3 000 extra lines on PERF-A/B/C: `query_entities` layer filter over 3 019 entities → 1 000 matched / 100 returned in **92 ms** end to end (11 ms in the bridge); `query_entities_spatial nearest` 1 000 × 1 000 lines **1.6 s** (no `maxDistance` → every target is a candidate; with a radius the grid index prunes); `detect_geometry_issues` over 3 000 lines **158 ms**, 0 false positives |

Regressions: `run-bridge-unattended.ps1` 21/21; `run-server-smoke.ps1` 22/22 (the old published exe, 24 tools, against the new bridge).

## Code review (2026-09-16, `plans/reports/code-review-2026-09-16-aec-phase-a.md`, 7/10)

Fixed the same day: **H1** `SpatialIndex` dropped items spanning > 4096 cells (kept in an oversized list every query checks; huge query boxes fall back to a linear scan); **H2** `Arc` tessellated in the OCS as if the normal were +Z (now AutoCAD's WCS sample points via `GetGeCurve`); **H3** default pages over the 64 KB cap (`detect_geometry_issues` 100/page, detail mode 20 entities × 64 vertices with `vertexCount`/`verticesTruncated`); **M1** nearly-parallel lines took the crossing branch (parallel test by separation over the segment length; `touches` excludes collinear overlaps; `IsWithin` also tests midpoints); **M2** `PlanShape.Approximate` suppresses segment-level verdicts on tessellations and stand-in rectangles; **M4** block-name filters match the effective dynamic name; **M5** `measure_geometry` reports unresolved handles instead of a generic ArgumentException; Lows: hatch loops rebuilt as a polyline (bulges, outermost loop), `Point3dCollection` disposed, clamp order, non-positive tolerance overrides reported, degenerate-segment symmetry, named constants, immutable lists, `EditResult` removed, plan references in comments removed, harness float tolerances + read-only check over every call. Pinned by `ReviewRegressionTests` (9 tests).

## Known limitations (phase A)

- Hatch geometry: only the first polyline loop becomes the footprint; curve loops fall back to the extents rectangle.
- Splines/ellipses/2D-3D polylines are sampled (≤ 512 points) — fine for predicates, not for exact lengths (exact lengths come from AutoCAD anyway).
- `within`/`contains` need a closed target; a polyline that is 12 mm short of closing is not a ring (the room tools of phase F will close such loops with `RoomGap`).
- `endpoint_gap` is between open entities' endpoints; "beam ends 7 mm short of a column outline" is the structural connectivity check of phase E (`query_entities_spatial distance_to` finds it today).
- Text is bounding-box only; block references are bounding box + insertion + attributes.
- The published exe (`HPAutoCad/output/HPAutoCad.Mcp.Server`) is locked by the running `hprebar-autocad` MCP server(s); republish after restarting Claude Code to expose the 5 tools there.
