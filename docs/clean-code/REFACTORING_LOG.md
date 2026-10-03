# RevitAddinAI — Refactoring Log

> Append-only. One entry per batch of [REFACTORING_PLAN.md](REFACTORING_PLAN.md), newest at the bottom. Never edit a past entry except to add a "Correction:" line.

## Entry template

```markdown
### <YYYY-MM-DD> — Wave <n>.<batch> — <short title>
- **Findings closed:** AUD-… / B-…
- **Files:** <count> (list the important ones)
- **Change:** what moved/renamed/split, in one paragraph
- **Build:** `<command>` → Pass/Fail (configs)
- **Tests:** `<command>` → <passed>/<total> (before → after)
- **Golden run:** identical / differences (explained) / CHƯA TEST (reason)
- **Review:** checklist sections passed; findings fixed
- **Deviations:** anything not as planned, bugs discovered (logged as B-xx, not fixed here)
- **Commit:** <hash> `<message>`
```

## Entries

### 2026-10-03 — Governance setup (no production code)
- **Findings closed:** none (baseline created: AUD-001…AUD-060, B-01…B-15)
- **Files:** docs only — `docs/clean-code/*` (7), `docs/architecture/ARCHITECTURE.md`, `DEPENDENCY_RULES.md`, `adr/*` (7), CLAUDE.md governance section, AGENTS.md regenerated; evidence in `plans/261003-2133-pragmatic-clean-code-governance/`
- **Change:** extracted 293 PCC rules from the full book (15 chapters), mapped the Revit product line, audited it, proposed target architecture, standard, workflow, checklist and wave plan
- **Build:** not run (no code changed; concurrent session in the repo)
- **Tests:** not run (counts in the audit come from grep)
- **Golden run:** n/a
- **Review:** n/a
- **Deviations:** ADR-0001…0006 left *Proposed* pending user approval; refactoring not started by instruction
- **Commit:** not committed (awaiting user)
- Correction: committed 2026-10-03 as `ae693d0` `docs: add Pragmatic Clean Code governance for HPRebar` (not pushed)

### 2026-10-03 — Wave 0.1 — Build matrix baseline
- **Findings closed:** none (confirms B-09)
- **Build:** `dotnet build HPRebar/HPRebar.slnx -c Debug.<v> -p:DeployAddin=false` → R23 ✅ R24 ✅ R25 ✅ R26 ✅ — R27 ❌ 10 errors (8 × CS0103 `RebarHookOrientation` in BeamMainBarCreator:82-83, BeamSideBarCreator:73-74, BeamSpecialBarCreator:65-66, FoundationRebarCreationService:64-65; CS1615 + CS0019 `Curve.Intersect(out)` BeamSupportFinder:269,273)
- **Warnings baseline:** 0 C# compiler warnings in production code; ILRepack/Polyfill merge warnings (R23/R24 11 distinct, R25 4, R26 merge + 6 xUnit analyzer warnings xUnit2013 ×4 / xUnit2029 ×2 in test projects)
- **Deviations:** Revit 2026 was open with a working model → deploy disabled; no file touched
- **Commit:** with Wave 0 docs commit

### 2026-10-03 — Wave 0.2 — Test baseline + doc counts
- **Findings closed:** AUD-058
- **Tests:** HPRebar.Core.Tests 919/919, HPRebar.Mcp.Server.Tests 109/109, McpShared Server.Core.Tests 743/743, McpBridge.Core.Net48Tests 113/113. TUnit HPRebar.Tests **not run** (loads Revit in-process; user's Revit session busy) — 21 tests, all skip without `.rvt` fixture
- **Change:** CLAUDE.md + AGENTS.md counts (Core 448 → 949 after 0.5, engine 164 → 743, net48 62 → 113, TUnit 16 → 21, "Four features" → five); standard §13 updated
- **Commit:** with Wave 0 docs commit

### 2026-10-03 — Wave 0.5 — Core test gaps (tests only)
- **Findings closed:** part of AUD-056 (Core primitives)
- **Files:** `HPRebar.Core.Tests/BeamRebar/BeamGeometryPrimitivesTests.cs` (Polyline3 length/Simplify/Translate, Point3, Tolerance), `BeamContinuousStackTests.cs` (Validate, FindSpanAt) — production untouched
- **Tests:** HPRebar.Core.Tests 919 → 949, 0 failed
- **Deviations:** Beam `Point3.Equals`/`GetHashCode` inconsistency found (added to B-06); deliberately not pinned by a test
- **Commit:** see git log

### Wave 0 — open batches
- **0.3 fixtures + golden run:** deferred — needs a Revit session not used by the Kata work (Revit was open on the user's `THCPHCS2…_detached` model). Next step when the user frees Revit or approves a second Revit instance on a scratch template.
- **0.4 `.editorconfig`:** deferred to the start of Wave 1 so the concurrent Kata session's editor does not start reformatting files mid-work.

### 2026-10-03 — Fix track B-09 — Revit 2027 build (removed APIs gated)
- **Findings closed:** B-09; first type in `Shared/` (ADR-0004)
- **Files:** new `HPRebar/HPRebar/Shared/Revit/RebarCurveFactory.cs`; `BeamMainBarCreator`, `BeamSideBarCreator`, `BeamSpecialBarCreator`, `BeamSupportFinder` (private gated `FirstIntersection`), `FoundationRebarCreationService`; CLAUDE.md/AGENTS.md R27 paragraph + `Shared/` paragraph
- **Change:** 4 `CreateFromCurves` + 1 `Curve.Intersect(out)` under `#pragma CS0618` replaced by `#if REVIT2026_OR_GREATER` paths; a null rebar now throws a clear exception (before: NRE, or a null silently added to the created list)
- **Build:** Debug.R23 ✅ R24 ✅ R25 ✅ R26 ✅ R27 ✅ (was 10 errors), 0 C# warnings
- **Tests:** Core 949/949, Mcp.Server 109/109
- **Golden run:** CHƯA TEST — R23–R25 identical calls; R26 switches to `BarTerminationsData` (same call KataRebar runs live on R26); to verify with Wave 0.3 fixtures
- **Review:** code-reviewer APPROVE 8.5/10. Applied: docs, Foundation summary, using order. Deferred: `KataRebarCurveFactory.Create` duplicates the shared factory → point Kata at it in Wave 6 (concurrent Kata work); `FirstIntersection` accepts only `Overlap` on R26+ like the old branch did — candidates are near-perpendicular (dot ≤ 0.25) so collinear/end-contact cases are not expected; pre-existing fallback to the candidate start point when lines are disjoint in 3D logged for Wave 3
