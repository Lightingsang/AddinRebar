# RevitAddinAI — Refactoring Plan

> **Status: APPROVED 2026-10-03 — Wave 0 in progress.** ADR-0001…0006 accepted (Kata: option A). Each wave still ends with a report; production code changes only inside a wave batch.
> Inputs: [CLEAN_CODE_AUDIT.md](CLEAN_CODE_AUDIT.md) (AUD-xxx, B-xx) · [REVITADDINAI_CLEAN_CODE_STANDARD.md](REVITADDINAI_CLEAN_CODE_STANDARD.md) · [ARCHITECTURE.md](../architecture/ARCHITECTURE.md) · progress in [REFACTORING_LOG.md](REFACTORING_LOG.md).

## 1. Ground rules

1. **No rewrite.** Small behaviour-preserving batches (PCC-019, PCC-201, PCC-218, PCC-273).
2. **Refactoring ≠ fixing.** Behaviour defects B-01…B-15 go to the separate *Fix track* (§4), each with its own decision, test and commit. A refactoring batch that discovers a bug logs it and does not fix it in the same commit.
3. **No feature work in a refactoring batch**, no refactoring in a feature batch (P6). File moves are committed alone (PCC-213).
4. **Batch size:** one feature (or one shared type), one rule family, ≈ ≤ 10 files changed by hand. Bigger → split.
5. **Stop rule:** a red build, a failing test, or an unexplained behaviour difference stops the wave until the cause is fixed. Never skip, disable or loosen a test to proceed.
6. **Coordination:** Kata files are under active development in another session. Kata batches start only after that work is committed and the user says Kata is stable; Kata Core long-method work (AUD-025/026) waits longest.
7. Waves are ordered as the brief requires; a later wave may start early only for a batch that is independent and approved.

## 2. Batch procedure (every batch)

```
PLAN    → name the batch, findings (AUD/B ids), files, expected diff size, verification steps; log "planned"
EDIT    → smallest change that satisfies the rule; no drive-by edits
BUILD   → dotnet build HPRebar/HPRebar.slnx -c Debug.R26 -p:DeployAddin=false
          + the Revit versions the touched code compiles for (R23/R24 net48, R25, R27 when B-09 is fixed)
TEST    → dotnet test HPRebar/HPRebar.Core.Tests ; dotnet test HPRebar/HPRebar.Mcp.Server.Tests
          (+ TUnit HPRebar.Tests under Debug.R26 once fixtures exist)
REVIEW  → git diff review against CODE_REVIEW_CHECKLIST.md (code-reviewer agent + human for public surface)
VERIFY  → Revit behaviour: golden-run comparison (§3) for any batch touching add-in Service/ or Core used by it
LOG     → REFACTORING_LOG.md entry (findings closed, commands + results, deviations)
COMMIT  → conventional commit `refactor(<feature>): …`, no AI reference; push only when the user asks
```

## 2a. Safe refactoring recipes

Each step is one edit followed by BUILD + TEST (PCC-273); a red step is undone, not patched forward. Tests must be green before the first step. Ids: standard / PCC.

| Smell | Steps |
|---|---|
| **Long method, mixed levels** (M1–M3) | 1. Mark the low-level blocks (loops over geometry, string building, unit conversion). 2. Extract each into a private method named by its intent (verb). 3. Turn nested preconditions into guard clauses at the top (PCC-078). 4. Order the extracted methods below their caller (FM5). Stop when the method reads at one level — a cohesive ~25-line method stays whole (PCC-067). |
| **Long parameter list** (M4–M5) | 1. A `bool` that switches between two jobs → two methods (PCC-063). 2. Parameters that always travel together → one record with a domain name (PCC-060). 3. Update call sites. Never bundle unrelated parameters only to lower the count. |
| **God class** (C1–C3) | 1. List fields and the methods that use each. 2. A cluster of methods sharing a separate set of fields → new class with a domain name (PCC-186). 3. The old class receives it through its constructor; only the feature Command creates it (D1). 4. Revert the split if the halves keep reaching into each other (PCC-114). |
| **Type `switch` in several places** (S1–S2) | Only for a family that does grow (bar shapes, request kinds): 1. Interface for the varying behaviour. 2. One class per branch. 3. Selection in one place (the Command or one factory). A closed `switch` over a stable enum stays. |
| **Revit/static infrastructure inside logic** (P2, D3–D5, T5) | 1. Move the computation into `HPRebar.Core` as a pure function over mm records and test it there — preferred over wrapping the Revit API (`Document` is sealed, R5). 2. Only when a seam is still needed (Excel, files, clock): a role-shaped interface with a stated reason, a thin adapter, a hand-written fake in tests. |

Boy Scout rule (PCC-028) inside a fix or feature batch is limited to the lines the diff already touches — a rename, a brace, a dead local. Anything larger is logged for its wave, never folded into the commit.

## 3. Revit behaviour verification (golden run)

Unit tests do not prove the add-in still produces the same model (PCC-271). For each feature, Wave 0 creates:
- a committed fixture model under `HPRebar/HPRebar.Tests/Fixtures/` (column stack, beam run with a cantilever + secondary beam, foundation slab, Kata run);
- a fixed input spec (JSON) per feature;
- a read-only snapshot script (through the Revit MCP `execute_revit_code`, `transaction: none`) that dumps: rebar count per Partition, total length per bar type and shape, view count/names, dimension count, TextNote count.

**Golden run** = run the feature on the fixture with the fixed spec before the batch, snapshot, undo; after the batch, repeat; snapshots must be identical. Runs use a copy of the fixture, never a user model.

## 4. Fix track (behaviour defects — needs a decision per item)

| Id | Defect | Proposed fix | Decision needed |
|---|---|---|---|
| B-09 | R27 compile break (Beam, Foundation) | Use the version-gated `CreateFromCurves`/hook path KataRebar already has; gate `Curve.Intersect` | none — restores a supported version (recommended first) |
| B-01 | Beam span cover fixed 25 mm | Span cover from spec | **decided: fix** |
| B-02 | Beam Views tab ignored | Map Views tab into `BeamAnnotationSettings` | **decided: wire** |
| B-03 | Column view-name/prefix fields ignored | Map into `AnnotationSettings` | **decided: wire** |
| B-05 | Foundation `Hook90Down` makes no hooks | Drop the option | **decided: remove** |
| B-07 | Localized string comparisons | BuiltInParameter integer values / type ids | none (correctness) |
| B-04, B-06, B-08, B-10, B-11, B-12, B-14, B-15 | see audit §2 | small local fixes, mostly absorbed by Waves 5–6 | none |
| B-13 | MCP queued-request cancellation | verify live first | none |

## 5. Waves

| Wave | Goal | Findings | Size | Entry gate | Exit gate |
|---|---|---|---|---|---|
| **0 Safety baseline** | Know exactly what builds, what passes, what Revit produces | AUD-055, 056, 058, 059 | M (needs Revit) | plan approved | baseline numbers logged; fixtures + golden snapshots committed; CLAUDE.md counts corrected |
| **1 Naming + formatting** | Cheap clarity; remove dead code | AUD-010, 045–050, 052–054 | S | W0 exit | names/dead code fixed per feature; format-only commits separate |
| **2 Methods** | Split long methods, parameter objects, flag args | AUD-022–030 | L | W1 exit | no method > 100 lines without a written reason; Core tests unchanged and green |
| **3 Classes / SRP / cohesion** | Move stranded pure logic to Core; split overloaded classes | AUD-007, 016, 018, 020, 021, 031–034 | L | W2 exit for the feature | moved logic has Core tests; VMs ≤ 250 lines or justified |
| **4 SOLID contracts** | Revit-free VM contracts; meaningful enums; role interfaces | AUD-012, 013, 015, 035–037 | M | W3 exit, ADR-0006 decided for Kata | DEPENDENCY_RULES L2–L4 hold per feature |
| **5 Dependencies / static / DI** | Constructor injection at seams; remove harmful statics; shared request queue | AUD-005, 011, 014, 019, 039–044; B-08 | M | ADR-0003/0004/0005 accepted | S1 grep = allowlist only; no I/O in constructors |
| **6 Coupling / architecture / composition** | Shared kernel, break cycles, one representation per rule | AUD-001–004, 006, 008, 009, 017; ADR-0006 | L | W5 exit; Kata stable | DEPENDENCY_RULES F1 grep clean; no duplicate infrastructure types |
| **7 Testability** | Seams where tests need them; presentation tests; bridge tests | AUD-038, 056, 060 | M | W6 exit; new test project approved | VM tests for each feature; TUnit runs (not skips) in CI-less local run |
| **8 Project / folder organisation** | Consistent files/folders; docs final | AUD-051, 057; CLAUDE.md feature-folder paragraph for `Shared/` | S | W7 exit | moves in own commits; docs updated; checklist pass |

### Wave 0 — batches
| Batch | Work | Notes |
|---|---|---|
| 0.1 | Build matrix `Debug.R23…R27` (`-p:DeployAddin=false`), record errors/warnings per config | R27 expected to fail (B-09) |
| 0.2 | Run every test project; record real counts; correct CLAUDE.md "Current State" numbers + "five features" | doc change only |
| 0.3 | Fixture models + fixed specs + golden snapshot script (§3) | **needs Revit 2026 open**; Claude can drive it through the Revit MCP if the bridge is on |
| 0.4 | `.editorconfig` mirroring the current style (no reformat yet) | config file → Planning Mode approval |
| 0.5 | Fill Core test gaps that later waves rely on: `BeamContinuousStack`, `Polyline3.Simplify`, `Point3`/`Tolerance` | tests only |

### Wave 1–8 batch order (per wave: Column → Foundation → Beam → MCP → Kata)
Column first (best existing tests incl. TUnit), Foundation second (smallest), Beam third (largest Beam debt), MCP bridge fourth, Kata last (active development).

## 6. Risks

| Risk | Impact | Mitigation | Blocks? |
|---|---|---|---|
| No Revit-level safety net today | refactors could change created rebar silently | Wave 0.3 golden runs before any add-in Service/Core change | Yes for W2+ on add-in code |
| Concurrent Kata development | merge conflicts, refactoring a moving target | Kata batches last, only after user confirms stability | Kata only |
| Multi-version builds (R23/R24 net48, R27 net10) | a refactor compiles on R26 only | build matrix per batch for touched code | No |
| ILRepack + namespace moves | merged DLL resolves wrong types | build + theme gallery + golden run after Shared moves | No |
| Over-refactoring | churn without value | every batch cites audit ids; "numbers are triggers" rule | No |
| Revit locks deployed DLL | deploy fails | `-p:DeployAddin=false` for compile checks | No |

## 7. Out of scope

Feature changes; McpShared engine internals; other hosts (AutoCAD, Civil 3D, Navisworks, ETABS…); installer/build pipeline (`HPRebar/build/` not audited).
