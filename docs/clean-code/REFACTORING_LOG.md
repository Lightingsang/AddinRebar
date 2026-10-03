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

### 2026-10-03 — Fix track B-01 — Beam bars use the cover entered in the window
- **Findings closed:** B-01
- **Files:** `HPRebar.Core/BeamRebar/Models/BeamContinuousStack.cs` (`WithCover`), `HPRebar/BeamRebar/Service/RebarCreationService.cs` (applies `spec.Stirrups.Cover` to every span before any creator runs), `HPRebar.Core.Tests/BeamRebar/BeamContinuousStackTests.cs` (+6)
- **Change:** span stirrups, additional, side and special bars now use the UI cover (the preview already did); before, they used the 25 mm placeholder read with the stack
- **Build:** Debug.R23 ✅ R26 ✅ R27 ✅ · **Tests:** Core 955/955
- **Golden run:** CHƯA TEST (intended behaviour change: output differs whenever cover ≠ 25 mm)

### 2026-10-03 — Fix track B-05 — Foundation hook aliases removed
- **Findings closed:** B-05
- **Change:** `FoundationHookType.Hook90`, `Hook90Up`, `Hook90Down` deleted — no code or UI used them (the window offers None / Hook90Degrees only), so "Hook90Down makes no hooks" could not be reached; no behaviour change
- **Build/Tests:** as above

### 2026-10-03 — Fix track B-03 — Column view names reach the views
- **Findings closed:** B-03
- **Files:** new `ColumnRebar/Model/ViewNaming.cs`; `IColumnRebarRunner.RunAsync` + `ColumnRebarRequest` + handler carry it; `ColumnRebarOrchestrator.Run(specs, progress, naming)` applies it to `AnnotationSettings`; `ColumnRebarSession.ToViewNaming()`; settings tab + UiStrings
- **Change:** "Detail view name" and "Section prefix" now name the views (`{name}X/Y`, `{name} {n} {prefix}`), blanks fall back to Detail / MC; the session default prefix changed S → MC so untouched runs keep today's names. "Section view name" and "Level prefix" were removed from the tab: the original R01 tool never used them either (`SectionColumnView.cs:112` names sections from DetailViewName + PrefixSection only)
- **Build:** Debug.R23 ✅ R26 ✅ R27 ✅ · **Tests:** Core 955/955 · TUnit project: does not restore (pre-existing NU1605 → AUD-061)
- **Golden run:** CHƯA TEST

### 2026-10-03 — Review follow-up for B-01 / B-03 / B-05
- **Review:** B-01 APPROVE 9/10, B-05 APPROVE 10/10, B-03 APPROVE WITH COMMENTS 8/10 (no High)
- **Applied:** B-03 Medium — view names containing a character Revit refuses (\ : { } [ ] | ; < > ? ` ~) are now refused in `ColumnRebarSession.IsValid` with a message instead of being dropped silently; `ViewNaming` moved to `HPRebar.Core/ColumnRebar/Models` with 9 tests and now owns the naming formulas; naming passed per run to `DetailViewCreator`/`SectionViewCreator` (no more writes into the shared `AnnotationSettings`, `naming` required on `Run`); label "Prefix Section" → "Section Suffix" / "Hậu tố mặt cắt", property `SectionPrefix` → `SectionSuffix`. B-01 Low — Beam `Validate` rejects NaN/Infinity cover before the transaction group opens
- **Not applied:** collapsing the four equal covers of the Beam spec into one (Wave 3, `BeamRebarSession` split); 25 mm placeholder in `BeamStackReader` (only logged now)
- **Build:** Debug.R23 ✅ R26 ✅ R27 ✅ · **Tests:** Core 964/964 · TUnit calls updated (project still blocked by AUD-061)
