# AEC Engine for the AutoCAD MCP — Phases A + B: Tools Stay Seeds, the Logic Becomes an Assembly Roslyn Can See

**Date**: 2026-09-16 21:05  
**Severity**: Low (new capability; two engine defects caught by unit tests, one by the live run — all fixed same day)  
**Component**: HPAutoCad.Aec (new) / HPAutoCad.McpBridge / HPAutoCad.Mcp.Server seeds / McpShared Contracts (one import)  
**Status**: Phase A verified live (build 0 warnings; tests 54 + 83 + 128 + 60 + 109; `run-aec-tools-live.ps1` 42/42; regressions 21/21 + 22/22). Code review 7/10 → all three High findings fixed and pinned by `ReviewRegressionTests` the same evening.

## Tình huống

The brief asked for an AEC automation layer on top of the AutoCAD MCP — 37 tools in 9 phases, each with a Tool / Service / Model split,
structured envelopes, tolerances, handles, and explicitly *not* one wide `run_command`. The repo already has a tool mechanism the brief
did not know about: seeds (`tool.json` + `code.cs`) validated by the registry and run through `autocad.execute`, with ADR-05 of the Revit plan
forbidding native tool classes. The first decision was therefore not "how to build 37 tools" but "where does compiled logic live so a seed
can stay a ten-line shim".

## Quyết định

- **ADR-01**: every AEC tool is a seed; the logic is a new assembly `HPAutoCad.Aec` (net8.0-windows) the bridge references, adds to
  `BridgeEntry.CompilerReferences`, and imports through `HostScriptContracts.AutocadImports` (the only `McpShared` change — one string).
  The bridge ships it in `Contents\Bridge\` and `ScriptingSelfCheck` calls one Aec method at start, so a stale bundle shows in the log
  instead of as CS0246 on the first AEC call.
- **ADR-02**: mm at the boundary whatever INSUNITS is (the imperial template of the harness proved the conversion), `GeometryTolerance`
  record with every threshold, handles as the only identity, camelCase envelopes (`success / summary / items / count / offset / truncated /
  warnings / errors[{code, message, handle}]`), `ArgumentException` for input that makes a run meaningless (the registry's stability window
  ignores those), `errors[]` for partial problems, `limit ≤ 500` + `offset` so a result stays under the bridge's 64 KB cap.
- Pure layers (`Geometry/`, `Spatial/`, `Issues/`, `Model/`) never mention an AutoCAD type, so `HPAutoCad.Aec.Tests` runs without acad.exe
  even though the assembly targets `net8.0-windows` (the test projects are `net10.0-windows` for that reference alone).

## Những gì đã đổ vỡ và tại sao

- `Shape` collides with `Autodesk.AutoCAD.DatabaseServices.Shape` once both namespaces are imported — renamed `PlanShape` before a single
  script hit the ambiguity.
- `ToolError` had a `Handle` property *and* a `Handle(...)` factory (CS0102) → `ForHandle`.
- `Pt`/`Box` computed properties (`LengthXY`, `Width`, `Center`…) serialised into every point of every response; `[JsonIgnore]` on computed
  members, caught by a serialisation test.
- Duplicate detection missed a reversed line: the reversed-chain mapping used cyclic offsets that only make sense for rings — open chains map
  `i → n-1-i`. Two chains that already meet at one end were also reported as an `endpoint_gap` at their other ends; connected pairs are now
  exempt.
- Live run 1 (30/33): `measure_geometry` said `success: true` beside a `NOT_CLOSED` error because a summary object counted as success;
  now success = no errors or ≥ 1 item. The other two were my own scene count (19 entities, not 20).
- Bash heredocs halve backslashes (again): a `'\\n'` char literal reached a C# file as a real newline. Patch scripts go through the Write tool.

## Verification

| Check | Result |
|---|---|
| `dotnet build HPAutoCad.slnx -c Debug` | 0 warnings, bundle deployed with `HPAutoCad.Aec.dll` |
| `HPAutoCad.Aec.Tests` / `HPAutoCad.Mcp.Server.Tests` | 54 / 83 (17 seeds compile against AutoCAD.NET + Aec; shim rule pinned; 9 review regressions) |
| `McpShared` 128 + 60 · `HPRebar.Mcp.Server.Tests` 109 | unchanged, green |
| `run-aec-tools-live.ps1` (AutoCAD 2026) | 42/42: context, filters/paging/detail, 5 spatial relations ± tolerance, 10 measures with exact curve maths, 6 issue kinds, error codes, and performance: layer query over 3 019 entities 92 ms, nearest 1 000 × 1 000 1.6 s, issue scan of 3 000 lines 158 ms |
| Regressions | bridge 21/21, smoke 22/22 (old published exe against the new bridge) |

## Review round (same evening)

The code-reviewer reproduced three defects on the built DLL without AutoCAD: the grid index silently dropped any item spanning more than
4096 cells (a site boundary among door swings — found or not depending on insertion order), arcs were tessellated from OCS angles as if
the normal were +Z (every mirrored door swing came out reflected), and the default page sizes of two tools exceeded the 64 KB result cap
(the AI would have received truncated text instead of JSON). All three plus five mediums (nearly-parallel lines "crossing" kilometres
away, chords tessellated into tiny-segment warnings, dynamic block names, masked handle errors) were fixed and pinned by
`ReviewRegressionTests`; the live scene gained a normal −Z arc, a bulge polyline, a hatch and a block with an attribute so the fixes are
observable in AutoCAD, not only in xUnit.

## Phase B the same night — classification as data, relationships as geometry

`classify_aec_entities` turns records into AEC objects through a JSON rule set (26 embedded rules across AIA/NCS, plain-English and
Vietnamese layer names; a project overrides them with one file beside the server registry) — best rule wins, the others are alternatives,
nearby text marks are evidence, and rotated rectangles report their own sides. `get_entity_relationships` names both ends by AEC type and
derives connected (end-of-run gap with a confidence that decays to 0.5 at the tolerance), intersect, near, inside/contains, touching and the
axis relations. Live 51/51 on the harness scene after one defect: `AecObject.Record` was `required` *and* `[JsonIgnore]`, which
System.Text.Json refuses for the whole object — the first response came back as "value is not serializable". A serialisation test now
runs the bridge's own options over classified objects.

The phase-B review (6.5/10) found what a unit test on a clean scene cannot: `locationMm` fell back to `(0,0,0)` when an intersection had no
crossing point (one shape inside the other); `*DAM*` also matched `M-DAMPER`, `*ONG*` took `S-MONG` and `ONGGIO`, `*COT*` took `COTAS`, so the
default rule set now anchors every stem to a name boundary and a 21-row theory pins the hostile layer list; `count` was `limit + 1` rather than
the total; the default classify page of 100 rich objects breached the 64 KB cap. All fixed the same night — relationships now count what holds
and de-duplicate symmetric pairs in the loop, axis relations report `angleDeg`, pages cap at 50 / 250 with the schema `maximum` pinned to the
engine constant, rule files refuse misspelt keys and null lists — and the live run rose to 55/55 with three new checks that would have caught
each of the reviewer's findings.

## Phase C — writing back, atomically

Six write tools followed the same night. The contract that mattered was *atomic*: the bridge gives a script one transaction and no
nested rollback, so "one bad item voids the batch" cannot be done by undoing — it is done by validating every item before the first
`AppendEntity`, refusing the whole batch with the errors listed (nothing written), and reserving a thrown exception for a failure *while*
writing, which the bridge turns into an abort. `atomic: false` writes what it can and reports the rest per item, in input order, with the
handle each item produced. Two live surprises: `Hatch.Area` is not readable inside the transaction that created the hatch (the summary now
falls back to the boundary's own area), and a top-level `using var` compiles in the seed test's method wrapper but not as a Roslyn script —
the harness's Wblock script had to spell out `try/finally`. `detectBoundary` is geometric on purpose: `Editor.TraceBoundary` wants a command
context the bridge does not have, while "the smallest closed entity containing the point" needs only the phase-A shape reader. 40/40 live,
including `U` over COM reverting a three-entity batch after a REGEN boundary.

The phase-C review (6/10) found the two-phase promise honoured only by *create*: *update* validated its `set` while writing and threw
`ArgumentException` naming the first error; phase 1 upgraded every entity to write, so a refused batch still reported `changed.modified = N`;
`Hatch.SetDatabaseDefaults` was called *after* the caller's layer and colour and reset both; an attribute reference on a locked layer
leaked `eOnLockedLayer` — five of those and the registry would have quarantined the tool; and the envelope breached 64 KB at the schema
maxima (findReferences × 500 with attributes uncapped, every item error copied twice). The answers were mechanical once named: read-open
phase 1 with a `Validate` that never mutates, `Upgrade` + `Apply` in phase 2 with `changed` accumulated, an owner check that keeps block
definitions out of reach, caps measured by a serialisation test at 200 refused items, and one policy for single-item ops — refuse the
whole op. Associativity turned out to need nothing but the right order (`Associative = true` before `AppendLoop(ids)`): the managed API has
no persistent-reactor call, and the live run shows the hatch following its moved boundary. 62/62 after the round.

## Phase D — standards as data, one audit, marks in the drawing

CAD standards are the least universal thing in a drawing office, so the checker is a JSON file: a layer-naming regex with exemptions,
which entity types belong on which layers, whether colour/linetype/lineweight may be overridden, allowed styles and text heights, block
names, unused layers. The checker itself is pure — records plus the symbol tables read once — so the eleven checks have unit tests on
synthetic layers and entities, and an `unused_layer` verdict is only given when the whole drawing was examined. `audit_aec_drawing` runs the
geometry detector and the standards checker over one query and returns them in one severity-first order with their own ids, so a page at
`offset 3` is the same page tomorrow. `create_issue_markup` closes the loop: the issue objects a tool returned go back in and come out as
revision clouds with a leader reading the id, on a markup layer created on demand and coloured by severity — the original geometry is only
read for its extents. 63/63 and 67/67 live.

## Lessons

- **Atomic without nested transactions = validate everything first, throw only while writing**: the refusal envelope carries every error and writes nothing; the throw path lets the bridge keep its one-undo-entry promise.
- **A "no native tools" rule is a gift when the logic can still be compiled**: the registry, quarantine, `tools/list_changed`, the read-only
  guarantee of `transaction: none` and the seed compile tests all came for free; the price was one assembly reference and one import string.
- **Test the envelope, not just the maths**: the three unit-test failures and the one live failure were all about *shape of the answer*
  (serialised members, success flag), not geometry — exactly what an LLM consumer trips over.
- **`nearest` without a radius is a full scan by design**; the grid index only prunes with `maxDistance`. 1.6 s for a million pairs is fine for
  phase A, and the tool description tells the AI to pass a radius.
- **`required` + `[JsonIgnore]` is a runtime error in System.Text.Json**, not a compile error; any DTO the bridge serialises needs a serialisation test with the bridge's options.
- The published exe is locked by every running `hprebar-autocad` MCP server (one per Claude Code session); the harness defaults to the Debug
  exe so new seeds are verified without republishing.
