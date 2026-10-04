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

### 2026-10-03 — Fix track B-02 — Beam Views tab reaches the run
- **Findings closed:** B-02 (fix track complete: B-01, B-02, B-03, B-05, B-09)
- **Files:** new `BeamRebar/Model/BeamViewOptions.cs` (in `BeamRebarSpec.Views`); `BeamRebarSession.ToSpec`/`Validate`; `BeamAnnotationSettings.ForRun` (per-run copy, no shared mutation); `BeamRebarOrchestrator` honours create-elevation / create-sections / sections-per-span / names / dimensions / tables flags and sizes `PlannedCount` from them; `DetailViewCreator.ApplyScale` sets the elevation scale unless the view template controls it (then logs); new `HPRebar.Core/Shared/RevitViewNames` (forbidden view-name characters, now shared by Column `ViewNaming` and Beam validation — ADR-0004 admission: 2 features) + 6 tests
- **Behaviour:** an untouched window keeps today's output (session default sections per span 2 → 3, the value the run always used); the scale 1:50 shown on the tab is now applied when no template controls the scale — this changes the elevation scale of untouched runs in documents whose template leaves scale free
- **Not changed:** `UseRealRebar` session property is bound nowhere (dead; Wave 1)
- **Build:** Debug.R23 ✅ R25 ✅ R26 ✅ R27 ✅ · **Tests:** Core 970/970 · **Golden run:** CHƯA TEST

### 2026-10-03 — Review follow-up for B-02
- **Review:** code-reviewer APPROVE WITH FOLLOW-UPS 7.5/10 (no Critical/High); flags, `ForRun` completeness, `ApplyScale` (VIEW_SCALE is a template parameter) and R1–R3 confirmed
- **Applied:** progress plan counts the sections the run really cuts (`SectionViewCreator.PlannedCount` — one per cantilever) and the dimensions of the views it really draws (`DimensionCreator.PlannedCount(onElevation, sections)`); view-name validation checks only the names of enabled views; session and settings defaults read `BeamViewOptions.Default` (no repeated literals); checkbox relabelled "Create Section Bar Tables" / "Tạo Bảng Thống Kê Thép Mặt Cắt" — no rebar tags were ever drawn
- **Logged, not fixed (pre-existing):** B-16 (R4 stale elements), B-17 (table row height scale), B-18 (template picking), B-19 (table rows by cut index)
- **Deferred:** dead `UseRealRebar`, unused `faces` constructor, uncalled `TagRebarOnElevation` (Wave 1); `BeamAnnotationSettings` → record with `with` (Wave 3); `RevitViewNames` sub-namespace (cosmetic)
- **Build:** Debug.R23 ✅ R26 ✅ R27 ✅ · **Tests:** Core 970/970 · **Golden run:** CHƯA TEST

### 2026-10-03 — Braces on single-line blocks written during the fix track
- **Rule:** FM2 / PCC-092 — four `if (…) return/continue/+=` one-liners added by this session's own fix commits (`RevitViewNames`, `BeamSupportFinder.FirstIntersection`, `DetailViewCreator.ApplyScale`, `BeamRebarSession.Validate`); not caught by two reviews, found by comparing against an external checklist
- **Scope:** only lines this session wrote; older one-liners elsewhere are left for `.editorconfig` + `dotnet format` (Wave 0.4 / Wave 1)
- **Build:** Debug.R23 ✅ R26 ✅ R27 ✅ · **Tests:** Core 970/970 · no behaviour change

### 2026-10-03 — Governance docs: ideas merged from an external clean-code skill
- **Source:** a user-supplied skill `.claude/skills/revitaddinai-clean-code` (22 condensed rules, 10-step workflow); the repo set stays the single source of truth and the skill was removed after the merge
- **Merged:** FM2 (brace one-liners explicitly, ~120-char line trigger, PCC-067), new FM5 member order / step-down (PCC-202–204), S1 + checklist: fix defects in place (PCC-126), REFACTORING_PLAN §2a safe refactoring recipes (5 smells, build + test per step, PCC-273) and the Boy Scout limit (PCC-028), TOOL_DEVELOPMENT_WORKFLOW "warning signs that a step was skipped"
- **Not merged (conflicts with the repo):** its own PCC-001…022 numbering (would collide with PCC-001…293), `RevitAddinAI.sln` build, NUnit `[TestCase]` and mocks for Revit (T5: hand-written fakes, `Document` is sealed), mandatory interface wrappers around the Revit API (replaced by "move the logic to Core first")

### 2026-10-03 — Fix track B-16 + B-18 — Beam view templates and annotation types
- **Findings closed:** B-16 (R4), B-18 (R7); B-07 narrowed to "Linear Dimension Style" in Beam + the Column copies
- **Files:** `BeamRebar/Model/BeamAnnotationSettings.cs` (keeps `ElementId`s, `ForRun(document, views)` fetches them again; templates limited to Section / Detail / Elevation view types, discipline compared by integer), `Service/DetailViewCreator.cs` (`ApplyTemplate` assigns only when `IsValidViewTemplate`, else logs; shared by `SectionViewCreator`), `Service/BeamRebarOrchestrator.cs`
- **Behaviour:** a template / dimension type / text type deleted while the window was open is left out with a log line instead of rolling back the run (dimensions or tables are then skipped); a project whose first "Structural" template is not a section-family template no longer throws — the first structural section/detail/elevation template is used, else the first of those of any discipline, else none; non-English Revit now finds the structural template
- **Review:** code-reviewer APPROVE WITH CHANGES; applied M1 (ids + re-fetch instead of `IsValidObject` on held wrappers — undo invalidates a wrapper of an element that still exists), M2 (Elevation templates), L3 (helper named `Resolve`); M3/L1 recorded in B-07/B-17; L2 (summary dialog does not list skipped types) not done
- **Build:** Debug.R23 ✅ R24 ✅ R25 ✅ R26 ✅ R27 ✅ · **Tests:** Core 970/970 · **Golden run:** CHƯA TEST

### 2026-10-03 — Wave 0.4 — `.editorconfig`
- **File:** new `HPRebar/.editorconfig` (`root = true`): 4-space indent, Allman braces, file-scoped namespaces, `csharp_prefer_braces` (FM2), `max_line_length = 140`, `_camelCase` private fields, PascalCase constants and `static readonly` — all `suggestion`/`silent`; line endings and BOMs deliberately not set (mixed tree, normalising = churn)
- **Effect:** no build change (Debug.R26 ✅, 0 new warnings); `dotnet format whitespace --verify-no-changes` on Core reports 49 drifts → applied per feature in Wave 1 format-only commits

### 2026-10-03 — Wave 1 · Column · dead members (AUD-046, Column part)
- **Entry:** Wave 0 not fully closed (0.3 fixtures wait for the user's Revit choice); started under plan §1.7 (independent batch, user approved "tiếp tục" on Wave 1) — compile-verified deletions only
- **Deleted:** `ColumnFaces.Cylindricals/TopLevel/BottomLevel` (written by `ColumnStackReader`, read nowhere) + `LevelOf`; `ColumnStack.Summary` (pre-UI smoke-test dump); `DowelStyles` on both dowel tab VMs (bound nowhere); Column `RevitUnits.Display`
- **Behaviour:** none — the reader no longer queries two level parameters and the cylindrical faces it threw away (`RequireSingleSolid` still runs through `GetTop/GetBottom`)
- **Build:** Debug.R23 ✅ R26 ✅ R27 ✅ · **Tests:** Core 970/970 · TUnit still blocked (AUD-061) · **Golden run:** CHƯA TEST (no fixtures)

### 2026-10-03 — Wave 1 · shared type · Revit's bar-position limit (AUD-010, first half)
- **Change:** new `HPRebar.Core/Shared/RevitRebarLimits.MaxBarPositions` (1002) replaces the three Core constants (`StirrupDistributionCalculator`, `BeamStirrupDistributionCalculator`, `FoundationValidationCalculator.MaxRebarCountPerLayer`), `ColumnSpecEditor.MaxBarPositions` and the `1002` literals in `BeamRebarSession` / `BeamStirrupCreator`
- **Behaviour:** value unchanged; the Beam validation message said "Revit's 1000 limit" while checking 1002 — it now prints the constant
- **Open:** the repeated hook default `Math.Max(30d, 200)` in Core Beam (rest of AUD-010) — next Beam batch
- **Build:** Debug.R23 ✅ R26 ✅ R27 ✅ · **Tests:** Core 970/970

### 2026-10-03 — Wave 1 · Column · format only
- `dotnet format whitespace --folder` over `HPRebar/ColumnRebar`, `HPRebar.Core/ColumnRebar`, `HPRebar.Core.Tests/ColumnRebar`: one file changed (`TestSections` initializer indentation); `.editorconfig` gains `csharp_indent_case_contents_when_block = false` so braced `case` blocks keep today's layout (the first run re-indented `SpliceCalculator`, reverted)
- **Tests:** Core 970/970

### 2026-10-03 — Wave 1 · Foundation · dead code and names (AUD-046/047/049, Foundation part)
- **Deleted:** `FoundationGeometrySnapshot` aliases (`LengthMm`, `WidthMm`, `ThicknessMm`, `TopElevation`, `BottomElevation`, `NormalZ`, `DirectionX/Y/Z` — read only by their own test asserts); `FoundationRebarSpec.EnableTopMat` alias and the four per-layer hook overrides nothing sets (`GetHookLength(diameter)` keeps HookLength-or-15d); `FoundationBoundaryCalculator.ComputeEffectiveBoundary` + both `ValidateBoundary` overloads (called only by tests; the rules live and are tested in `FoundationValidationCalculator`) with their 8 test methods / 11 cases; the never-supplied `onBarCreated` callback and the computed-and-dropped `barId` in `FoundationRebarCreationService`
- **Renamed:** `normX`/`normY` (named after the other axis than the value they held) → `normalOfXBars`/`normalOfYBars`; loop → `foreach`
- **Behaviour:** none
- **Build:** Debug.R23 ✅ R26 ✅ R27 ✅ · **Tests:** Core 959/959 (970 − 11 deleted cases of deleted methods)

### 2026-10-03 — Wave 1 · Beam · dead code, aliases, repeated rule (AUD-010/046/047/053, Beam part)
- **Deleted:** `RebarTableTagCreator.TagRebarOnElevation`, `RebarTypeCatalog.BarTypesList` + `FindHook`, `RevitDialogs.Confirm` (Beam **and** the Column copy, missed by the Column batch), `BeamSolidFaceReader.ProjectToPlane`, `BeamStack.Summary`, `StirrupZone` (Core, unused record), the `UseRealRebar` session property (bound nowhere), the orchestrator constructor taking an unused `faces`, the empty contiguity loop in `BeamContinuousStack.Validate`
- **Aliases removed:** `CreatedBeamRebar` (`MainTopBars`, `MainBottomBars`/`AdditionalBottomBars` that returned empty lists, `AdditionalTopBars`, `TotalCount`), `BeamFaces` (8), `BeamStack` (`SpanFaces`, `NormalDirection`, `StartPoint`, `BeamAxis`, `SideNormal`); 5 call sites now use `TransverseDirection` / `OriginPoint`
- **`BeamStack.EndPoint`** (the only `/ 304.8` in Beam) inlined into its one caller `DimensionCreator.ElevationSpanLine` through `RevitUnits.MmToFt`
- **Repeated rule:** `Math.Max(30 × d, 200)` (9 copies in `BeamMainBarCalculator` / `BeamAdditionalBarCalculator`) → `BeamHookLength.Default(d)` + 3 tests; `BeamSpecialBarCalculator`'s `30 × d` anchorage is a different rule and stays
- **Kept on purpose:** `BeamContinuousStack.Validate` — real checks with tests, but not called in production; wiring it in front of the run is a Wave 3 decision (not a refactor)
- **Behaviour:** none
- **Build:** Debug.R23 ✅ R26 ✅ R27 ✅ · **Tests:** Core 962/962 (+3) · **Golden run:** CHƯA TEST

### 2026-10-03 — Wave 1 · review follow-up (Column / Foundation / shared)
- **Review of fd23893..bd15b2e:** APPROVE, no High/Medium; every deleted member confirmed unused incl. TUnit + XAML; `ReadFaces` and `GetHookLength` equivalences confirmed
- **Applied:** blank lines left by deletions; `using` order in `StirrupDistributionCalculatorTests`; the `> 1002` comment now names the constant; `.editorconfig` `max_line_length` 140 → 120 to match the FM2 review trigger; the message fix of cc6d408 recorded as B-20; Column `DetailViewCreator.ResolveViewType` → `GetOrCreateViewType` (it duplicates a section type when the named one is missing — AUD-048; the Beam method only looks up and keeps its name)
- **Next:** Width/Length boundary at equality in `FoundationValidationCalculatorTests` (test-only commit); Beam format-only commit; `Application.cs`/`StartupCommand.cs` excluded from formatting (CLAUDE.md)
- **Build:** Debug.R23 ✅ R26 ✅ R27 ✅ · **Tests:** Core 962/962

### 2026-10-03 — Wave 1 · shell + MCP (AUD-045, AUD-052 partly)
- **Deleted:** `RibbonIcons.Execute` (play glyph for the template command; no button uses it; the file is linked into the MCP bridge — bridge build ✅)
- **Seed text:** `color_elements/tool.json` description lost its self-dialogue ("Undo with operate_element ResetIsolate? No — …"); takes effect for users on the next server publish (seed checksum upgrade path)
- **Not done (needs the user):** `Commands/StartupCommand.cs` — empty and unreferenced, but CLAUDE.md says it "stays put"; `ClosedXML` package — no `using ClosedXML` anywhere, removal is a `.csproj` change and the Kata session owns that file for now
- **Open in Wave 1:** Kata (AUD-050/053/054) waits for the Kata freeze; AUD-052's `AdditionalTieSpec` doc comment
- **Build:** Debug.R23 ✅ R26 ✅, McpBridge R26 ✅ · **Tests:** Mcp.Server 109/109, Core 965/965

### 2026-10-03 — Wave 1 · Beam review follow-up
- **Review of ca98561..3ae66a1:** APPROVE, no High/Medium; deletions unused repo-wide, `EndPoint` inline and the 9 hook replacements exact, removed loop had no effect
- **Applied:** `RebarTypeCatalog` hook-type collector + `HookTypes`/`CoverTypes` (no reader after `FindHook` went); `BeamContinuousStack.SupportNodes`/`SpanCount`/`SupportCount` (unused; the session has its own); `Validate` and `BeamHookLength` summaries now say what the code does; crossover case `d = 200/30` added to `BeamHookLengthTests`
- **Logged, not fixed:** B-21 (depth-step bottom bars ignore the start/end hook lengths and the stirrup allowance)
- **Process note accepted:** ca98561 was larger than the ~10-file batch size (mechanical edits); later batches keep one rule family per feature; its golden run is still owed
- **Build:** Debug.R23 ✅ R26 ✅ R27 ✅ · **Tests:** Core 966/966

### 2026-10-03 — Wave 2 · Foundation · `FoundationMeshCalculator.Calculate` (AUD-024, AUD-027 Foundation part)
- **Safety net first:** `FoundationMeshCalculatorCharacterizationTests` (ac4604b) hashes every bar (order, layer, diameter, hook, world + local points) and the statistics for 4 scenarios (axis/rotated, hooks on/off, top mat on/off, equal spacing) — captured before the change, unchanged after
- **Change:** the 246-line method with four copied layer loops → `Calculate` (validate, bounds, place each planned layer, summarise) + `PlanLayers` (layer heights and clamped hooks, in placing order) + `PlaceLayer` + `Summarise`; `BuildBarPolyline` 9 parameters / 3 bools → 5 (a `MeshLayer` record carries direction, height and a signed hook leg); magic `0.006165` → `BarKgPerMetrePerSquareMm`
- **File:** 380 → 287 lines; no public signature changed
- **Build:** Debug.R23 ✅ R26 ✅ R27 ✅ · **Tests:** Core 970/970 (4 characterization hashes identical) · **Golden run:** not needed for Core-only output proven identical; Revit side untouched

### 2026-10-03 — Wave 2 · Beam · `BeamAdditionalBarCalculator.ComputeSupportTopBars` (AUD-022)
- **Safety net first:** shared `HPRebar.Core.Tests/CharacterizationText` (reflection over every public property, numbers to 1e-6, SHA-256) + `BeamAdditionalBarCalculatorCharacterizationTests` (16d2d35): 5 stacks (single, two unequal, three, cantilever, variable depth), every branch (both layers, defaulted/explicit ratio, gap and exterior hook, an out-of-range support) — unchanged after
- **Change:** 323 lines with six near-identical layer × position blocks → `ComputeSupportTopBars` (~25 lines) + `SupportTopNode.At` (the section a support's bars are set out in: start / end / interior) + `PlaceSupportTopLayer` (one layer, bar shape by `SupportEnd`) + `ExteriorHook`
- **File:** 463 → 349 lines; public surface unchanged
- **Build:** Debug.R23 ✅ R26 ✅ R27 ✅ · **Tests:** Core 975/975 (5 hashes identical)

### 2026-10-03 — Wave 2 · review follow-up (Foundation + Beam)
- **Review of ac4604b..2617a5d:** APPROVE 8.5/10; equivalence confirmed by reading for single support, empty spans, layer 2 only, top hooks sign, `-0`, statistics order
- **Applied:** `CharacterizationText` now throws on a type outside HPRebar.Core, a public field or an object with nothing to write (it used to hash those as `{}`); `BeamAdditionalBarCalculator` private types/helpers moved below the public methods (FM5), `SupportEnd` → `SupportPosition`, `SupportTopNode` → `SupportTopSection`, layer set-out extracted (`SetOutTopLayer`, `PlaceSupportTopLayer` 87 → ~70 lines), `HostSpanIndex` set once, `ZHookFloor` documented; `FoundationMeshCalculator` constant to the top, hook guard `Math.Abs(HookRise) > 0.0` (same as the old guard even for NaN), long lines wrapped; foundation characterization moved onto the shared hasher
- **Coverage added, hashes captured from the pre-refactor code** (`git archive ac4604b` of Core + Core.Tests, scenarios copied in): single support, layer 2 only, thin footing with clamped explicit hooks; all 12 scenarios equal on old and new code
- **Logged, not fixed:** B-22 (layer 2 set out under an absent layer 1)
- **Build:** Debug.R23 ✅ R26 ✅ R27 ✅ · **Tests:** Core 978/978

### 2026-10-04 — Wave 2 · Beam · `BeamMainBarCalculator` top/bottom (AUD-023, first half)
- **Safety net first:** `BeamMainBarCalculatorCharacterizationTests` (618d4ac, 0ddfdcb): 9 scenarios hashing top and bottom bars — unspliced, spliced with/without stagger and default stock length, two-span splice over a support, cantilever unspliced/spliced, depth step, defaulted hooks on a 60 mm deep beam (negative top hook kept as before), explicit bottom start hook — unchanged after (7fee8b2 committed an empty hash by a scripting slip and failed one test; 0ddfdcb recorded it — commits are now gated on `failed: 0`)
- **Change:** `ComputeTopMainBars` 135 → 14 lines (`TopRun`, `UnsplicedTopBars`, `SplicedTopBars`); `ComputeBottomMainBars` 206 → 20 lines (`HasDepthStep`, `SteppedBottomBars`, `BottomRun`, `UnsplicedBottomBars`, `SplicedBottomBars`); shared `BarRun`, `CantileverEnds`, `StockLimit`, `EndHookLength`, `SpliceCentre`, `StaggerOffset`; private helpers below the public methods (FM5)
- **Kept apart on purpose:** top bars always carry their hook points and Hook90 angles, bottom bars only for a positive leg — merging the two would need a behaviour flag (PCC-063) or change output for negative hook legs
- **File:** 442 → 510 lines (longest method 206 → ~60); over the C2 300-line trigger, one cohesive purpose — left whole
- **Build:** Debug.R23 ✅ R26 ✅ R27 ✅ · **Tests:** Core 987/987 (9 hashes identical)

### 2026-10-04 — Wave 2 · Beam · `BeamStirrupDistributionCalculator.ComputeSpanRuns` (AUD-023, second half)
- **Safety net first:** `BeamStirrupDistributionCalculatorCharacterizationTests` (ce40154): cantilever (and one too short for any stirrup), uniform, three-zone L/4 and L/3, the short-span collapse to uniform, a 600 mm span whose midspan takes one stirrup — unchanged after (the existing stirrup tests check exception types only; messages were compared by the reviewer's probe)
- **Change:** 205 lines with the run block written five times → `ComputeSpanRuns` (~40 lines: guards, then cantilever / uniform / three-zone) + `Centre`, `EnsureWithinLimit` (same messages), `Run`, `SingleDenseRun`, `ThreeZoneRuns`, `MidspanRun`; the unused `l2` local dropped
- **File:** 312 → 248 lines
- **Build:** Debug.R23 ✅ R26 ✅ R27 ✅ · **Tests:** Core 994/994 (7 hashes identical)

### 2026-10-04 — Wave 2 · Beam review follow-up (main bars + stirrups)
- **Review of 618d4ac..8e8b5aa:** APPROVE 8.5/10; the reviewer compared copies of the old classes with the new ones on random inputs — 200 000 main-bar cases, 0 differences; 300 000 stirrup cases, 32 differences, all with `SpacingSparse = +∞` (the one-stirrup midspan position becomes `startX + 0 × ∞ = NaN` instead of the midpoint) — an input the guards should refuse, logged as B-26, not reproduced
- **Coverage added before the next change** (97d6203): right cantilever unspliced/spliced, a 12.5 m single span (bottom splice over the end column, see B-23), depth step with a right cantilever, four `ComputeNodeRun` cases
- **Applied:** `ComputeNodeRun` built from the shared `FitSpacings` + `Run` (+ `EnsureWithinLimit` now takes the parameter name — "Node stirrup"/`spacingMm` kept); `Centre` → `FitSpacings`, `SpliceCentre` → `SpliceCenter` (the file says Center everywhere), `EndHookLength` → `AnchorageHookLength` (it clashed with `BarPolyline.EndHookLength`); the seven repeated `BarPolyline` initializers → `MainBar(side, …)` + `BarSide` record (510 → 465 lines); helpers below their callers, nested types last (FM5 step-down); splice doc comments now state the index rule; B-21 reference updated
- **Logged, not fixed:** B-23 (single long span spliced over the end column), B-24 (cantilever-root splice), B-25 (spliced bottom with < 2 supports throws), B-26 (non-finite spacing / offset passes the guards)
- **Build:** Debug.R23 ✅ R26 ✅ R27 ✅ · **Tests:** Core 1002/1002 (every characterization hash identical)

### 2026-10-04 — Wave 3 · Column · tie and layout rules to Core (AUD-018 / AUD-033, Column part)
- **Entry:** Wave 2 closed for Column (no Core long methods; the add-in methods wait for fixtures); started on the user's "có" to Wave 3 Core work
- **Change:** new `HPRebar.Core/ColumnRebar/ColumnSpecRules` — `IsLayoutValid(shape, layout)` and `FirstProblem(section, layout, stirrups, ties)` — holding the layout, clearance, tie-run, Revit-limit and cross-tie rules that lived in `ColumnSpecEditor.Validate/ValidateTies`; messages word for word; the editor keeps only the "pick a bar type" check (its `RebarTypeInfo` is an add-in type) and builds the stirrup/tie specs through `ToStirrupSpec`/`ToTieSpec`, which `ToSpec` now shares
- **Tests:** 18 new `ColumnSpecRulesTests` (every rule and message, both shapes, both distribution types); the rules had no test before (add-in assembly)
- **File:** `ColumnSpecEditor` 315 → 233 lines
- **Build:** Debug.R23 ✅ R26 ✅ R27 ✅ · **Tests:** Core 1020/1020 · **Golden run:** not needed (validation only; messages identical)

### 2026-10-04 — Wave 3 · Column · one bar pipeline, element count in Core (AUD-007, AUD-018 Column part)
- **AUD-007:** new `HPRebar.Core/ColumnRebar/ColumnBarPolylines.Compute` (layout → upper splice positions → bar polylines) replaces the three hand-assembled copies in `RebarCreationService.BuildPolylines`, `BarsDivisionTabViewModel.Refresh` (schedule) and `ElevationBars.For` (preview); each caller keeps its own inputs (tie diameters as before, the preview's empty type name); the preview's `catch (ArgumentOutOfRangeException)` around the layout became an `IsLayoutValid` check — the calculator throws on exactly those conditions
- **Counts:** `ColumnElementCount.Planned/CrossTies` and `StirrupDistributionCalculator.ComputeRuns` in Core replace the service's private `RunsFor`/`CrossTieCount`
- **Behaviour:** none intended; the service now builds one segment's polylines before yielding them (was bar by bar) — only the moment an unexpected exception surfaces changes, and the transaction group rolls back either way
- **Tests:** `ColumnElementCountTests` (8), `ColumnBarPolylinesTests` (pipeline equals the three steps by hash)
- **Build:** Debug.R23 ✅ R26 ✅ R27 ✅ · **Tests:** Core 1029/1029 · **Golden run:** CHƯA TEST (touches the creation service)

### 2026-10-04 — Fix track B-23 / B-24 (+ B-25) — where long bottom bars are lapped
- **Decision (user, 2026-10-04):** lap bottom bars over the interior support nearest the middle of the bar run, never over an end support or a cantilever root; with no interior support, at a quarter of the first supported span's clear length from its start
- **Change:** `BeamMainBarCalculator.BottomSpliceCenter` replaces `Supports[Count / 2]` (at least support 1); on a tie the later support wins, which is what `Count / 2` picked for symmetric stacks, so symmetric stacks are unchanged; an asymmetric stack of 3+ spans may lap at a different interior support, the one nearer the middle (3 spans 8/4/4 m: support 1, was support 2 — longest piece 9 445 mm, was 13 445 mm)
- **Behaviour change (intended):** a single span over the stock length (B-23) now laps at L/4 — every piece fits the stock length up to ~13.8 m between column centres (12.5 m span: longest piece 10.7 m, was 13.1 m); a spliced run next to a left or right cantilever (B-24) laps in the supported span instead of at the root; a run with fewer than 2 supports (B-25) no longer throws
- **Tests:** `BeamBottomSpliceRuleTests` (5: quarter-span lap, pieces fit the stock, cantilever, tie, no supports); the three characterization hashes of exactly those cases re-recorded, every other hash unchanged
- **Build:** Debug.R23 ✅ R26 ✅ R27 ✅ · **Tests:** Core 1034/1034 · **Golden run:** CHƯA TEST

### 2026-10-04 — Wave 3 · Column review follow-up
- **Review of 7ed4d65 + 144152d:** APPROVE 8.5/10; equivalence confirmed by reading (messages and check order, layout values, the preview guard, splice sizing, planned count)
- **Applied:** `ColumnBarPolylines` documents "exactly one splice per bar" and its exceptions; argument guards on `ComputeRuns`/`Planned`/`CrossTies`; `CrossTies` without the nested ternary (doc now says "unless its leg is 0", as the code does); the elevation preview's tie runs through `ToStirrupSpec()` + `ComputeRuns` (was a hand-built spec — fourth copy); the section preview and `ElevationBars` check `ColumnSpecRules.IsLayoutValid` before the calculator instead of catching its exception
- **Tests:** one theory tying `IsLayoutValid` to the calculator for every bar count −1…9 (the previews now rely on the two agreeing); golden hashes for the pipeline under a narrower segment and at the top of the stack (replacing a test that re-ran the pipeline), a splice-count mismatch; check-order cases (layout before clearance, spacing before cross-ties) and the Revit limit at exactly 1002 vs 1003 ties; the planned-count test hard-codes 3 groups
- **Deferred:** `ColumnBarPolylines.Compute` 7 parameters (the tie diameter equals `layout.StirrupDiameter` at every caller) — its own commit
- **Build:** Debug.R23 ✅ R26 ✅ R27 ✅ · **Tests:** Core 1041/1041

### 2026-10-04 — Fix track B-23 / B-24 review follow-up
- **Review of 47a1465:** APPROVE 8/10; 24 stack shapes probed (1–5 spans, cantilevers on either or both ends, 0–2 supports, unequal spans): never an end support or a cantilever root, no index out of range
- **Applied:** supports within 1 mm of equally near count as a tie (`SpliceTieToleranceMm`) — feet-to-mm noise must not flip a symmetric beam to the earlier support; log sentence corrected (asymmetric stacks may move, see entry above); test comment corrected (supports 0/6000/11000/17000)
- **Logged, not fixed:** B-27 (one splice per run whatever its length — pieces over the stock length on 3+ spans and single spans over ~13.8 m, unreported), B-28 (a single-element cantilever stack flagged at both ends), B-29 (invented joint supports count as interior)
- **Tests:** `BeamBottomSpliceRuleTests` 5 → 7 (asymmetric 3 spans laps at support 1; 0.5 mm off a tie laps at the later support — fails without the tolerance); every characterization hash unchanged
- **Build:** Debug.R23 ✅ R26 ✅ R27 ✅ · **Tests:** Core 1043/1043 · **Golden run:** CHƯA TEST

### 2026-10-04 — Wave 3 · Foundation · plan frame to Core (AUD-018 Foundation part, AUD-029)
- **Change:** new `HPRebar.Core/FoundationRebar/Calculators/FoundationPlanFrameCalculator` — `DominantDirection` (longest straight, horizontal-enough edge, first on equal lengths, turned to +X / +Y), `Fit` (smallest rectangle along that direction, corner at the bottom level), `Compute` (both over the outline's end points); `FoundationSolidFaceReader.Read` 117 → 30 lines: reads the bottom outline into `FoundationOutlineEdge`s (mm, Revit's own edge length) and keeps the bounding-box fallback for a face without edges
- **Behaviour:** none intended; the maths now runs in millimetres instead of feet, so results equal the old ones up to floating rounding; the one exception is edge lengths a rounding step apart, which scaling to mm can make exactly equal, so the first edge now wins where the later one did (review probe: ~0.3 % of square footings, whose axis was already decided by rounding noise — logged as B-30)
- **Review:** 8/10; applied: `Fit` takes the horizontal unit part of its direction and refuses a vertical one, tests for a longer edge after shorter ones and a long steep edge before a horizontal one, reader helpers named as verbs (`ReadBottomOutline`, `ReadBoundingCorners`), lines wrapped at 120
- **Tests:** `FoundationPlanFrameCalculatorTests` (14 tests, 15 cases: axis-aligned, turned 30°, L-shape, sign rule ±X/±Y, longer edge later, square keeps the first edge, steep and curved edges ignored, bottom level, direction normalised / vertical refused, empty guards)
- **Build:** Debug.R23 ✅ R26 ✅ R27 ✅ · **Tests:** Core 1058/1058 · **Golden run:** CHƯA TEST (Foundation reader)

### 2026-10-04 — Wave 3 · Beam · support layout to Core (AUD-018 Beam part, AUD-029)
- **Change:** new `HPRebar.Core/BeamRebar/Calculators/BeamSupportLayout` — `Arrange` (merge duplicate finds, cantilever tip past a 200 mm overhang, a 300 mm stand-in column at a bare beam joint while nodes < pieces + 1, exterior/interior classes and names) and `Synthesize` (stand-in columns when nothing is found); `BeamSupportFinder.FindSupports` 158 → 67 lines: the Revit collection stays, `Measure*` return `MeasuredSupport`, `SynthesizeDefaultSupports` moved to Core; the thresholds are named constants; classification spells out `Column or ExteriorColumn` instead of relying on `Column == InteriorColumn`
- **Behaviour:** none intended; the same operations in the same order on the same millimetre values
- **Review:** 8.5/10; differential probe old vs new over 300 000 random cases (NaN, ±∞, duplicates, every type, reversed pieces): 0 differences; applied: explicit column classes, `WithStandInJoints` returns a new list (no argument mutation), named arguments in `Synthesize`, braces and joined signatures in the finder, its helpers moved below the public methods, docs for exceptions and the run end; logged B-31 (grid merge), B-32 (overlapping pieces); B-29 now points at Core
- **Tests:** `BeamSupportLayoutTests` (26 cases: classes and names, sort, merge keeps the start-most find and the first on equal centres, grid half-step split (B-31), tips at either end and at both 200 mm thresholds, stand-in joint classed interior (B-29), joint radius, enough nodes before and part-way, pieces out of order, width/depth carried, walls/girders keep their type, interior/exterior columns reclassed, guards, synthesis)
- **Build:** Debug.R23 ✅ R26 ✅ R27 ✅ · **Tests:** Core 1084/1084 · **Golden run:** CHƯA TEST (beam support detection)

### 2026-10-04 — Wave 3 · Beam · section stations and span assembly to Core (AUD-018 Beam part)
- **Change:** new `HPRebar.Core/BeamRebar/Calculators/BeamSectionStations` (`ForSpan`: mid-span for a cantilever or ≤ 1 section, a sixth + mid-span for 2, + five sixths for more; `Count` over a run) replaces `SectionViewCreator.ComputeCutStations` and `SectionViewCreator.PlannedCount` (orchestrator and view creator call Core); new `BeamSpanAssembly.Between(index, supports, BeamPieceSection)` replaces the span block of `BeamStackReader.Read` (missing supports at 0 / +4000 mm, clear length falls back to centre-to-centre, cantilever end from tip supports, default cover `DefaultCoverMm` 25)
- **Behaviour:** none intended; same expressions on the same values
- **Review:** APPROVE 9/10; differential probe old vs new over 400 000 random cases (NaN, ±∞, −0, every support type, index −1…6): 0 differences; applied: station doc says absolute run coordinates and the ≤ 1 / ≥ 3 cases, a test with a clear span not divisible by six, ≥ 4 sections pinned to the same three stations, the overlapping-support fallback pinned (logged B-33), unused using dropped; noted (not a defect today): section views, dimensions and tables walk the views by a parallel index — safe while view creation throws instead of skipping
- **Tests:** `BeamSectionStationsTests` (10 cases), `BeamSpanAssemblyTests` (9)
- **Build:** Debug.R23 ✅ R26 ✅ R27 ✅ · **Tests:** Core 1103/1103 · **Golden run:** CHƯA TEST (stack reading, section views)

### 2026-10-04 — Wave 3 · Beam · input rules to Core (AUD-018 Beam part)
- **Change:** new `HPRebar.Core/BeamRebar/Calculators/BeamSpecRules.FirstProblem(mainBars, stirrups, barTypesChosen, viewNames, spans)` holds the checks of `BeamRebarSession.Validate` with the same messages in the same order; the session builds `ToMainBarSpec()` / `ToStirrupSpec()` (extracted from `ToSpec`, which reuses them) and `ViewNamesToCreate()`, then asks Core
- **Behaviour:** none intended; `ToSpec` produces the same spec
- **Review:** 9/10, messages / order / conditions / values / spec confirmed identical; applied: `viewNames` guarded like the other arguments, `barTypesChosen` documented, tests for just-above-minimum sizes, the height rule with the larger bar and the node-spacing → view-name → span order; logged B-34 (tie estimate overflows `int` for a tiny spacing, Beam and Column); AUD-018 Beam keeps two progress-bar `PlannedCount`s open
- **Tests:** `BeamSpecRulesTests` (27 cases: every message word for word, check order, NaN/∞ cover, node spacing on/off, larger of the two bar sizes for width and height, minimum boundary, 1002/1003 stirrup limit dense and sparse, second span named, NaN spacing pinned (B-26), guards)
- **Build:** Debug.R23 ✅ R26 ✅ R27 ✅ · **Tests:** Core 1129/1129 · **Golden run:** CHƯA TEST (Beam window validation)

### 2026-10-04 — Wave 3 · Beam · planned element count to Core (AUD-018 Beam part closed)
- **Change:** new `HPRebar.Core/BeamRebar/Calculators/BeamElementCount` — `Bars` (three stirrup groups per span, every main bar, two per additional bar, four skin-bar elements on a deep enough run, hanging stirrups at each secondary beam) and `Dimensions` replace `RebarCreationService.PlannedCount` and `DimensionCreator.PlannedCount`; `BeamRebarOrchestrator.PlannedCount` still adds views, sections, dimensions and tables
- **Behaviour:** none intended (progress-bar sizing only)
- **Review:** 9/10, every term identical, no other caller of the removed methods (the TUnit `RebarCreationServiceTests` are Column's); applied: tests with skin bars switched off on a deep run, hanging stirrups switched off with secondary beams present, one deep span among shallow ones; one calling style in the tests; logged B-35 (the estimate drifts from what the creators make)
- **Tests:** `BeamElementCountTests` (12 cases)
- **Build:** Debug.R23 ✅ R26 ✅ R27 ✅ · **Tests:** Core 1141/1141 · **Golden run:** CHƯA TEST

### 2026-10-04 — Fix track B-34 — bar counts checked against Revit's limit before they become an int
- **Decision (user, 2026-10-04):** fix
- **Change:** counts are computed and compared in double and cast only after the check: `BeamSpecRules.StirrupCount`, `BeamStirrupDistributionCalculator.FitSpacings` (checks the limit itself) and `MidspanRun`, `ColumnSpecRules.TieRunProblem` and `StirrupDistributionCalculator.RequireUsableCount` (`Math.Truncate` keeps the old `(int)` truncation), `FoundationValidationCalculator` (four layers); messages print counts with `{x:0}`
- **Behaviour change (intended):** a spacing so small the count passes `int.MaxValue` is refused with the limit message (window) or `ArgumentOutOfRangeException` naming the limit (calculator) instead of being accepted and failing later with a `List` capacity error; every in-range result and message is unchanged; NaN is untouched (B-26)
- **Review:** APPROVE 8/10; differential probe old vs new (900k cases, net48 / net8 / net10): bit-identical for finite inputs; applied: NaN kept exactly as before in `MidspanRun` (`!(x > 0)` clamp) and the Column count (`RequireUsableCount` takes the intervals and adds 1 after the cast), `ParamName` asserted, the Column rule test no longer pins the `{spacing:0}` text, braces on the four Foundation checks, a comment on `Truncate`; logged B-36 (side-bar rows and cross-ties have no limit at all)
- **Tests:** 5 regression tests, each failing on the old code: Beam rules, Beam node run, Column rules, Column calculator, Foundation validation; every characterization hash unchanged
- **Build:** Debug.R23 ✅ R26 ✅ R27 ✅ · **Tests:** Core 1146/1146 · **Golden run:** CHƯA TEST

### 2026-10-04 — Fix track B-31 — duplicate supports merged by distance
- **Decision (user, 2026-10-04):** merge by distance instead of the 100 mm grid
- **Change:** `BeamSupportLayout.Merge` walks the finds in order along the run and drops a find whose centre is less than `MergeDistanceMm` (100) from the last support kept; the kept one is still the nearest the start (first found on equal centres); measuring from the kept support stops a row of close finds chaining into one
- **Behaviour change (intended):** finds 20 mm apart across a 100 mm mark (6040 / 6060) are one support, no longer an extra node and an extra span; finds 100 mm apart or more stay separate whatever grid cell they fall in
- **Review:** 8.5/10; probe old vs new over 300 000 random inputs: the new rule only ever merges more (one exact-100 mm edge of the old round-half-to-even buckets aside); applied: the boundary test asserts which support survives, a test either side of the run origin; logged B-37 (the lower centre survives whatever the element type); overlapping supports 100 mm or more apart stay with B-33; a non-finite centre is to be refused with B-26
- **Tests:** `BeamSupportLayoutTests` — the grid-split pin replaced by: 20 mm across a 100 mm mark → one support, 99.9 / 100.0 boundary, a 60 mm-step row keeps 6000 and 6120 (all three fail on the old code), finds either side of the origin
- **Build:** Debug.R23 ✅ R26 ✅ R27 ✅ · **Tests:** Core 1150/1150 · **Golden run:** CHƯA TEST

### 2026-10-04 — Fix track B-26 — NaN and infinity refused for Beam spacings, offsets and support centres
- **Decision (user, 2026-10-04):** refuse NaN and ∞
- **Change:** new `HPRebar.Core/Shared/FiniteNumber` (`IsFinite`, `IsPositive`; netstandard2.0 has no `double.IsFinite`); `BeamStirrupDistributionCalculator.ComputeSpanRuns` refuses a non-finite clear span, spacing or start offset and `ComputeNodeRun` a non-finite spacing; `BeamSpecRules` checks spacings and cover with `IsPositive` (same message), adds "Stirrup start offset must be a number." and checks node spacing with `IsPositive`; `BeamSupportLayout.Arrange` refuses a non-finite support centre
- **Behaviour change (intended):** NaN / ∞ typed into the Beam window is refused with a message instead of producing NaN stirrup positions or a `List` capacity error; every finite input gives the same result and message as before
- **Review:** APPROVE 8.5/10, finite inputs unchanged; applied: ±∞ start-offset cases, the NaN-centre comment corrected, test order; `FiniteNumber` in Core/Shared confirmed (KataExport and KataRebar write the same check by hand); logged B-38 (the elevation preview freezes Revit on a −∞ or huge negative start offset) and B-39 (other editable inputs still unchecked for NaN / ∞)
- **Tests:** 22 new cases (window rules, calculator span and node runs, support layout), each failing on the old code; the NaN pin replaced
- **Build:** Debug.R23 ✅ R26 ✅ R27 ✅ · **Tests:** Core 1171/1171 · **Golden run:** CHƯA TEST

### 2026-10-04 — Fix track B-36 — side-bar and cross-tie spacings checked, counts held to Revit's limit
- **Decision (user, 2026-10-04):** spacings must be greater than zero and counts checked against the 1002 limit, like stirrups
- **Change:** `BeamSideBarCalculator.CountRows` / `CrossTiesPerRow` (double, a spacing that is not a positive finite number falls back to 300 / 400 mm — ∞ included now); `ComputeRowCount` and `ComputeCrossTies` throw above the limit; `BeamSpecRules.FirstProblem` takes the side-bar spec and, when a span is deep enough for side bars, refuses a missing vertical or cross-tie spacing and a row or cross-tie-per-row count over the limit (window cover, stirrup and bottom bar, as the creator builds them); the session's `ToSideBarSpec()` feeds both the rules and `ToSpec`
- **Behaviour change (intended):** 0, negative, NaN or ∞ side spacings on a deep run are refused in the window instead of silently becoming 300 / 400 mm (or NaN positions for ∞); a tiny spacing is refused instead of making a million cross-ties per row or overflowing; finite in-range layouts unchanged
- **Review:** APPROVE 8/10; old vs new over ~1M inputs: 0 differences in range, the window refuses exactly what the calculator throws on; applied: one `NeedsSideBars` (user threshold, NaN-safe like the calculator, and the 700 mm row-count start) for both the spacing and the count checks, messages say "the 1002-bar limit" (separate Rebars, not one set), braces in the side-bar loops, tests at 1002 / 1003 rows against the calculator, below 700 mm and a NaN threshold; logged B-40 (rows × cross-ties per span not capped — user decision)
- **Tests:** `BeamSpecRulesTests` (+20 cases), `BeamSideBarCalculatorTests` (+8)
- **Build:** Debug.R23 ✅ R26 ✅ R27 ✅ · **Tests:** Core 1197/1197 · **Golden run:** CHƯA TEST
