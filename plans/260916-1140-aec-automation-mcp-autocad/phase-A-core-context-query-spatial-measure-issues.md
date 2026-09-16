---
phase: A
title: "Core — drawing context, entity query, spatial query, measure, geometry issues"
status: completed
priority: P1
effort: "20h"
dependencies: []
---

# Phase A: Core

## Context Links
- [architecture.md](architecture.md) §2 layout, §3 contracts · [ADR-01](adr/adr-01-tools-as-seeds-over-execute-plus-aec-assembly.md) · [ADR-02](adr/adr-02-units-tolerance-handles-envelopes.md)
- Existing seeds to mirror: `Registry/SeedLibrary/Data/get_entities`, `Data/get_drawing_info`; compile wrapper `HPAutoCad.Mcp.Server.Tests/SeedLibraryTests.cs`

## Overview
Foundation assembly `HPAutoCad.Aec` + five read-only tools. After this phase the AI can describe a drawing, page through filtered entities with
geometry in mm, ask spatial questions with tolerance, measure, and get a structured list of geometry defects.

## Requirements
- Functional: the five tools of the brief §5–§9 with the envelopes of ADR-02; `transaction: none`; `limit/offset/mode`; tolerance overrides.
- Non-functional: no full-drawing walk when a type/layer/handle filter is given; `maxCandidates` guard (default 5 000) with `truncated`; every
  service call `ct.ThrowIfCancellationRequested()` in loops; output < 64 KB by construction of the defaults.

## Architecture
```
seed code.cs ──▶ HPAutoCad.Aec.Cad.<Service>(db, tr, ed, units, ct, args-derived options) ──▶ pure HPAutoCad.Aec.{Geometry,Spatial,Issues}
                                                                         └──▶ AutoCAD API only inside Cad/ (SelectionFilter, GeometricExtents, Curve maths)
```
- `Geometry/`: `Pt` (x,y,z), `Box` (min/max + Intersects/Contains/Expand/Distance), `Seg` (a,b + length/direction/closest point/2-D intersection
  with tolerance), `Shape` (kind, closed, segments, bbox, area/centroid for closed 2-D loops), `GeometryTolerance`, `GeometryMath`.
- `Spatial/`: `SpatialIndex` (uniform grid keyed by cell over `Box`, `Query(Box)`), `SpatialRelation` enum, `SpatialPredicates.Evaluate(a, b, relation, tol)`
  + `Distance(a, b)`; `within/contains/inside_polygon` via point-in-polygon on closed shapes, `intersects/crosses/overlaps/touches` via segment maths,
  `nearest/distance_to` via min segment distance.
- `Issues/`: `GeometryIssueDetector.Detect(records, kinds, tol)` → duplicate, near-duplicate, overlapping collinear segments, tiny/zero-length,
  open polyline (single-shape), endpoint gap (pairwise within `EndpointConnection`), self-intersection.
- `Cad/`: `EntityShapeReader` (Line, Polyline (bulges → arc tessellation), Polyline2d/3d, Circle, Arc, Ellipse, Spline (sampled), Hatch (loops),
  Solid, Face, Region (bbox only), BlockReference (bbox + insertion + name + attributes), DBText/MText (bbox + text), Dimension (bbox + text), Leader/MLeader (bbox),
  else bbox-only), `EntityQueryService` (filter → `SelectionFilter` + post-filter → paging → `AecEntityRecord`), `HandleResolver`,
  `DrawingContextReader`, `MeasureService`.

## Tools (seeds)

| Tool | Category | Inputs (top-level keys) | Output |
|---|---|---|---|
| `get_drawing_context` | Drawing | `includeLayouts` (bool), `includeLayers` (bool, capped 200) | drawing, path, autocadVersion, units, insunits, measurement, activeSpace, activeLayout, currentLayer, ucs (origin/x/y), extentsMm, annotationScale, dimStyle, textStyle, ltScale, viewports, counts (entities model/paper, layers, blockDefinitions, xrefs, layouts) |
| `query_entities` | Data | `filter` {types[], layers[] (wildcards), colors[], linetypes[], blockNames[], textContains, handles[], visibleOnly, space}, `limit`, `offset`, `mode` (summary/detail), `properties[]` | analysis envelope; items = records (handle, type, layer, color, linetype, space, bboxMm, + detail: geometry, text, blockName, attributes) |
| `query_entities_spatial` | Data | `source` {filter}, `target` {filter}, `relation`, `maxDistance`, `tolerance`, `limit`, `maxCandidates` | matches [{sourceHandle, targetHandle, relation, distanceMm?, points?}] |
| `measure_geometry` | Geometry | `measure` (length/totalLength/area/perimeter/centroid/boundingBox/angle/distance/closestPoint/intersections), `handles[]`, `points[]` ({x,y,z} mm) | value(s) in mm/mm²/deg + per-handle items |
| `detect_geometry_issues` | Audit | `filter` (as query), `handles[]`, `issueTypes[]`, `tolerance`, `limit`, `maxCandidates` | issues [{issueId, type, severity, handles, locationMm, valueMm, toleranceMm, description}] |

## Related Code Files
- Create: `HPAutoCad/HPAutoCad.Aec/HPAutoCad.Aec.csproj`, `Geometry/*.cs`, `Spatial/*.cs`, `Issues/*.cs`, `Model/*.cs`, `Cad/*.cs`
- Create: `HPAutoCad/HPAutoCad.Aec.Tests/` (xUnit v3, net10.0-windows)
- Create: 5 seed folders under `HPAutoCad.Mcp.Server/Registry/SeedLibrary/{Drawing,Data,Geometry,Audit}/`
- Modify: `HPAutoCad.McpBridge.csproj` (ProjectReference Aec), `BridgeEntry.cs` (`CompilerReferences` += Aec; self-check touches Aec), `HPAutoCad.slnx`,
  `AutocadHostProfile.cs` (categories + Geometry, Audit), `McpShared/.../HostScriptContracts.cs` (imports), `SeedLibraryTests.cs` (count, Aec metadata ref, TFM),
  `HPAutoCad.Mcp.Server.Tests.csproj` (net10.0-windows + reference), harness (`run-server-smoke.ps1` names), docs.

## Implementation Steps
1. Scaffold `HPAutoCad.Aec` + tests; pure geometry with tests first (distance, intersection, bbox, polygon area/centroid/contains, tolerance).
2. Spatial index + predicates with tests (each relation, tolerance edge cases).
3. Issue detector with tests (each issue kind on synthetic shapes).
4. Cad adapters: shape reader, query service, handle resolver, context reader, measure service.
5. Wire the bridge (reference, compiler references, imports, self-check) — build, deploy bundle.
6. Seeds ×5 + update seed tests/harness pins; `dotnet test` both projects.
7. Live smoke through the stdio server against the open drawing: each tool with two argument sets; fix; record in `reports/phase-A-live.md`.

## Success Criteria
- [x] `dotnet build HPAutoCad.slnx -c Debug` 0 warnings; `HPAutoCad.Aec.Tests` 45 green; `SeedLibraryTests` green with 17 seeds (83 total)
- [x] Each of the 5 tools ran live in AutoCAD 2026 with a real answer — [reports/phase-A-core-live.md](reports/phase-A-core-live.md) (33/33)
- [x] `query_entities` on a 3 019-entity drawing with a layer filter: 92 ms end to end (11 ms in the bridge); spatial nearest 1 000 × 1 000 lines 1.6 s (no radius → full target scan; with `maxDistance` the grid index is used); issue detection over 3 000 lines 158 ms, 0 false positives (harness step P)
- [x] Existing 24 tools unchanged (`run-server-smoke.ps1` 22/22, `run-bridge-unattended.ps1` 21/21)

## Risk Assessment
- Roslyn cannot see the Aec assembly inside AutoCAD (ALC) → `ScriptingSelfCheck` calls one Aec method at start so the log shows it; the compiler
  references the assembly object the bridge loaded, so the same ALC instance is used.
- `net10.0-windows` for the seed test project: OS-specific TFM only; the tests already run on Windows only (AutoCAD DLLs).
- Arc tessellation error for predicates: chord tolerance derived from `PointEquality`; exact curve ops (`IntersectWith`, `GetClosestPointTo`)
  used in `MeasureService` where precision matters.
