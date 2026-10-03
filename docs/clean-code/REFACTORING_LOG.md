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
