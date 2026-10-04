# Wave 4 — Revit-free ViewModel contracts (Column, Foundation, Beam)

Status: PLANNED (no code changed) · 2026-10-04 · governance: `docs/clean-code/REFACTORING_PLAN.md` §5 Wave 4

## Goal
ViewModels and runner contracts carry no Revit type (`Document`, `Element`, `RebarBarType`, `PlanarFace`, `XYZ`, `Floor`); the Revit objects stay in commands, handlers and services (DEPENDENCY_RULES L2–L4). Behaviour unchanged.

## Scope (non-Kata; Kata waits for the freeze + ADR-0006)
| Id | Where | Today | Target |
|---|---|---|---|
| AUD-012 Column | `ColumnRebar/Model/RebarTypeInfo.cs` (`RebarBarType BarType`), `ColumnRebarSession.Stack` (`ColumnStack`: `Element`, `PlanarFace`) | VM holds Revit objects | `RebarTypeInfo` carries a bar-type id (`long`); the session holds Core sections; the handler keeps the `ColumnStack` |
| AUD-012 Foundation | `FoundationSession` (`Document`, `Floor`, `RebarBarType` list) in `IFoundationRebarRunner` | runner contract and VM get the Revit session | VM gets a Revit-free view (snapshot, bar-type list as `RebarTypeInfo`, spec); handler keeps document + floor |
| AUD-012 Beam | `BeamRebar/Model/RebarTypeInfo.cs`, `BeamRebarSession.Stack` (`BeamStack`: `XYZ`, faces), `Faces` | 18 uses of `Stack` in VM/View, 14 of `RebarTypeInfo` | session holds `BeamContinuousStack` (Core); handler keeps `BeamStack`/faces; bar types by id |
| AUD-037 Column | `TypeDis`, `TypeH`, `TypeV`, `*DowelsType` raw ints — 57 uses in 20 files (Core models included) | int codes | enums with meaning; characterization hashes re-pinned only if the property type change alters them (logged) |
| AUD-013 | `BeamRebarViewModel` TaskDialogs | already gone (grep 2026-10-04: no TaskDialog/MessageBox in any VM) | close the row after a check |

## Phases
| # | Phase | Files (≈) | Testable off-Revit | Status |
|---|---|---|---|---|
| 1 | [Shared bar-type id + Column](phase-01-column-bar-types-and-stack.md) | ~8 | build + Core tests only | planned |
| 2 | [Foundation session split](phase-02-foundation-session.md) | ~9 | build only | planned |
| 3 | [Beam session on Core stack](phase-03-beam-session.md) | ~10 (two batches) | build + Core tests | planned |
| 4 | [Column enums](phase-04-column-enums.md) | ~20 (two batches: Core models, add-in) | Core tests | planned |
| 5 | Close AUD-013, docs, log | 2 | — | planned |

## Key decision — the golden-run gate
`REFACTORING_PLAN.md` §6: "No Revit-level safety net — Wave 0.3 golden runs before any add-in Service/Core change — **blocks W2+ on add-in code**". Waves 2–3 went ahead with "Golden run: CHƯA TEST" on pure moves proven by Core tests and old-vs-new probes. Wave 4 is different: phases 1–3 change how the handler finds the bar type and the stack at run time, which no Core test or probe covers.

## Risks
- Bar type resolved by id at run time (multi-version `ElementId`: `int` R23, `long` R24+) — a wrong lookup creates no rebar or the wrong size; only a Revit run shows it.
- Painters read `Session.Stack` everywhere (Beam 18 uses) — mechanical but wide; build catches type errors, not drawing differences.
- AUD-037 enum change in Core models may change characterization hashes (property type in the hash) — must be shown to be representation-only.

## Verification
Per batch: build R23/R26/R27, Core tests, code review, log entry. Phases 1–3 additionally need a golden run per feature (fixtures from Wave 0.3) — or an explicit user waiver.
