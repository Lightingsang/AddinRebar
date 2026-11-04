# Code review — phase 4 Kata Column Rebar (Revit side + merged button), 2026-11-03

Scope: `KataColumnRebarCommand`, `View/Web/ColumnExport/*` (bridge, session, window, contracts), `KataColumnRebarCreator`,
`KataDamComReader` diff, `Application.cs` (KataColumn lines only), `HPRebar.csproj` gates, `RibbonIcons` glyph,
frontend `src/kataColumn/*`. Shared Kata services read where the creator depends on them. Read-only review.

Checks run: `dotnet build HPRebar/HPRebar.csproj -c Debug.R26 -p:DeployAddin=false -p:SkipFrontendBuild=true` ✅
(0 warnings in reviewed files); `npx vitest run src/kataColumn --pool=threads` ✅ 19/19 (fork pool hit
ERR_IPC_CHANNEL_CLOSED once — env, not code).

## Verified OK (do not re-open)
- `KataDamComReader.Read()` = `Read(SheetName, RangeAddress)` with the same constants → beam path byte-identical.
- Gates: creator + command `Compile Remove` for RevitVersion != 2026; `View\Web\**` already removed; ribbon lines
  inside `#if REVIT2026` / `#if KATA_ONLY && REVIT2026`; no leftover `KataColumnExport*` symbol anywhere.
- Group atomicity: `KataTransactionRunner` rolls the inner transaction back before rethrow, commit status checked,
  `Assimilate` status checked, `group.RollBack()` only while started. Unfiltered exceptions still roll back
  (TransactionGroup dispose). Workshared ownership failure reaches the page as Revit's text (live report).
- `Plan()` clears `_planned` first → any re-entrant/early Generate fails safe (BAD_REQUEST), never on a half plan.
- Write enforces active FullName == probed FullName; Excel COM never inside a Revit transaction (R12).
- Modal ShowDialog → handlers on Revit's thread (R1 by design, ADR-0014).

## Findings

### H1 — Generate warnings never shown (silent degradation)
[ColumnRebarView.tsx:42-46](../../../HPRebar/HPBimToolkit.Frontend/src/kataColumn/ColumnRebarView.tsx#L42) toasts only
`created/deleted`; `result.warnings` dropped (mock always `[]`, no test). Lost: "no 135° hook type → hoops drawn
without hooks" ([KataColumnRebarCreator.cs:58-61](../../../HPRebar/HPRebar/KataExport/Service/KataColumnRebarCreator.cs#L58)),
"created cover type …", "N non-Kata bars on host may shift" (cover change), numbering refusals, every Revit warning
the runner deleted (bar outside host). Failure path drops them too. User ships hookless ties unaware. Rule C5/Q
(errors visible). Fix: render `warnings` as a `Messages tone="note"` block after every generate (success or not) +
vitest with a non-empty warning list.

### M1 — Step 2 reads the ACTIVE workbook with no identity check
[KataColumnRebarSession.cs:204](../../../HPRebar/HPRebar/KataExport/View/Web/ColumnExport/KataColumnRebarSession.cs#L204)
→ `KataDamComReader.Read` (ActiveWorkbook, returns Name only). Step 1 writes by FullName
([KataColumnWebBridge.cs:152-158](../../../HPRebar/HPRebar/KataExport/View/Web/ColumnExport/KataColumnWebBridge.cs#L152)),
step 2 does not compare. Scenario: write C24 into scratch `…\copy\Kata.xlsm`; Excel activates `C:\kata_pro\Kata.xlsm`
(holds C7, same storey count) → Reload plans C7's bars on C24's faces; page shows "Kata.xlsm" for both (Name only).
Undoable, but wrong rebar. Fix: add FullName to `KataDamReadResult` (additive), in `Plan()` refuse/flag when
`_probedWorkbook` is set and differs; show FullName (tooltip) in step 2 footer; warn when sheet C3 ≠ name last written.

### M2 — Generate creates the last plan, not the sheet as it is now
[KataColumnRebarSession.cs:234-243](../../../HPRebar/HPRebar/KataExport/View/Web/ColumnExport/KataColumnRebarSession.cs#L234).
User edits E/I/L in Excel while on step 2, clicks "Tạo thép" without "Đọc lại" → old bars created, sheet says
otherwise, success toast. Fix: Generate re-reads + replans; if the summary differs from the one the page shows
(fingerprint returned by reload, echoed by generate) refuse with the fresh state; else create.

### M3 — Page busy state split → generate on an unseen plan, close mid-run
[ColumnApp.tsx:108-121](../../../HPRebar/HPBimToolkit.Frontend/src/kataColumn/ColumnApp.tsx#L108) sets `mode='rebar'`
then reloads with the parent `busy`; [ColumnRebarView.tsx:26-28](../../../HPRebar/HPBimToolkit.Frontend/src/kataColumn/ColumnRebarView.tsx#L26)
has its own `busy`, so "Tạo thép" is enabled showing the `column.load` summary while reload runs; host handles
messages serially → creation uses the re-read plan the user never saw. During generate, step tabs and Close stay
enabled: Close is queued, window closes right after creation, toast/warnings lost; tab switch unmounts the view.
(STA COM pumping during `Plan()` can also re-enter the dispatcher — page guard is the real defence.) Fix: one `busy`
in `ColumnApp` passed down; disable tabs, Close, Reload, Generate while any request is pending.

### M4 — Hook side of round hoops decided by float noise; tie/round/free-form paths unverified live
`KataRebarCurveFactory.IsRight` uses chord `(end−start)` as facing
([KataRebarCurveFactory.cs:43-44,65](../../../HPRebar/HPRebar/KataRebar/Service/KataRebarCurveFactory.cs#L65)); for
the half-circle arcs of [KataColumnRebarCreator.cs:233-241](../../../HPRebar/HPRebar/KataExport/Service/KataColumnRebarCreator.cs#L233)
the chord ⟂ tangent, centre lies on the chord line → dot ≈ 0 → Left/Right by rounding (mirrored frame flips it) →
hook may point out of the concrete. Live run (`live-phase-04.md`) = rectangular, 0 ties: closed ties, C crossties,
round hoops (two arcs + 135° hooks), `CreateFreeForm` main bars and the no-hook fallback are CHƯA TEST. Fix: facing =
`curve.ComputeDerivatives(0/1, true).BasisX` (tangent) in the creator's call or factory; live-run a 2-layer column
with I3 ties, a round storey and a two-direction crank before phase 5.

### L1 — Host timeouts are inert
Handlers are synchronous (`Task.FromResult(...)`), so `WaitAsync(timeout)` in
[CommandDispatcher.cs:56](../../../HPRebar/HPWebHost.Core/Protocol/CommandDispatcher.cs#L56) starts after the work.
`GenerateTimeout` comment ([KataColumnWebBridge.cs:86-87](../../../HPRebar/HPRebar/KataExport/View/Web/ColumnExport/KataColumnWebBridge.cs#L86))
misleads; only the page's 330 s bounds it → a > 330 s run shows TIMEOUT while Revit commits later (rerun is
idempotent, so low). Fix: correct the comment; page text "may still be running".

### L2 — Cryptic / raw failures
- `TieCurves` `.Min/.Max` on an empty slot match → "Sequence contains no elements"
  ([KataColumnRebarCreator.cs:176-180](../../../HPRebar/HPRebar/KataExport/Service/KataColumnRebarCreator.cs#L176)); guard + named error.
- Catch filter ([:84](../../../HPRebar/HPRebar/KataExport/Service/KataColumnRebarCreator.cs#L84)) lets
  KeyNotFound/NullReference escape → INTERNAL raw .NET text, no `Log.Error` with the column name. Catch `Exception`
  at this boundary (rollback + log + structured error).
- Mixed language: "There is no valid plan…", "The model has no rebar bar type of Ø…" in a VI page.

### L3 — Beam wording/rules leak into column runs
`KataHostCoverService` warns "Đổi lớp bảo vệ **dầm**… (J9)"; `KataRebarNumberAssigner` says "Dầm không tên (ô **B3**
trống)" and leaves bars in the model-wide default partition when sheet C3 is empty
([KataColumnRebarCreator.cs:72,117](../../../HPRebar/HPRebar/KataExport/Service/KataColumnRebarCreator.cs#L72)). Fix: host
noun/cell label parameter, or refuse Generate when C3 is empty.

### L4 — `PlaneNormal` absolute tolerance
`|dir × d| > 1e-4` ft = 0.03 mm perpendicular ([:271](../../../HPRebar/HPRebar/KataExport/Service/KataColumnRebarCreator.cs#L271)):
sub-mm face jitter sends a planar bar to free-form (no shape family, worse schedule/numbering). Use ~0.5 mm.

### L5 — Placement / small
- `KataColumnRebarSession` (Revit API: bar-type resolution, creator orchestration) sits in `View/Web/ColumnExport/`;
  feature convention puts services in `Service/` (CLAUDE.md feature folder, C1).
- Icon: bars and hoops in one Steel path, even-odd → six 2×2 holes at crossings
  ([RibbonIcons.cs KataColumnRebar](../../../HPRebar/HPRebar/Resources/Icons/RibbonIcons.cs)); split hoops into their own part.
  Arrow points away from the column (column → sheet) while the comment says "bars coming from the sheet".
- `BarTypes` keyed by Ø only: a hoop Ø equal to a main Ø takes the main (deformed) type — intended per comment, say so
  in the summary when it happens.

## Table

| # | Sev | File:line | Rule |
|---|---|---|---|
| H1 | High | ColumnRebarView.tsx:42 | C5, error visibility |
| M1 | Medium | KataColumnRebarSession.cs:204 | R12 adapter identity, safety note of plan |
| M2 | Medium | KataColumnRebarSession.cs:234 | K1 (one source of truth = sheet) |
| M3 | Medium | ColumnApp.tsx:108 / ColumnRebarView.tsx:26 | page state race |
| M4 | Medium | KataRebarCurveFactory.cs:65 + creator:233 | Revit API correctness, P8 (untested paths) |
| L1 | Low | KataColumnWebBridge.cs:86 | comment truth |
| L2 | Low | KataColumnRebarCreator.cs:84,176 | error boundary |
| L3 | Low | creator:72,117 (shared services) | user-facing text |
| L4 | Low | KataColumnRebarCreator.cs:271 | tolerance |
| L5 | Low | session placement, icon, bar types | feature folder, C1 |

Score: **6.5/10** — Revit transaction/rollback design and the beam-path generalisation are sound; the gaps are on the
user-visibility side (warnings, workbook identity, stale plan, page busy) and live coverage of ties/round/free-form.

**Status:** DONE_WITH_CONCERNS

## Fix round (2026-11-03)

| # | Fix | Status |
|---|---|---|
| H1 | `ColumnRebarView` keeps `result.warnings` on the page (`rebar-warnings`), vitest | ✅ — live: surfaced cover-type creation + refused Rebar Numbers (see live report) |
| M1 | `KataDamReadResult.WorkbookFullName` (init, additive); session refuses a plan read from another workbook than step 1 wrote; empty C3 refused; other C3 noted | ✅ build + Core; live CHƯA TEST (one workbook only) |
| M2 | `Generate` re-reads + re-plans, refuses when the summary JSON differs from the one shown; page always reloads after Generate | ✅ live: edit I15 → refused + page re-read, next click created |
| M3 | one `busy` + `run` in `ColumnApp`, passed to step 2; tabs, Close, Reload, Generate disabled while pending | ✅ vitest |
| M4 | `KataRebarCurveFactory`: hook side from `ComputeDerivatives(0/1).BasisX` (tangent), lines unchanged | ✅ build; round/tie paths live CHƯA TEST |
| L1 | inert `GenerateTimeout` removed, comment says handlers are synchronous | ✅ |
| L2 | `TieCurves` guard with named message; error boundary `catch (Exception) when not OOM`; messages VI | ✅ |
| L3 | beam wording in shared cover/numbering services ("Dầm C24", J9) | ❌ deferred — shared with beams, needs a host-noun parameter |
| L4 | plane tolerance 0.5 mm | ✅ |
| L5 | session moved to `KataExport/Service/` (own result types, `Compile Remove` non-2026); icon hoops own part, comment | ✅ |

Checks: build Debug.R26 ✅, Core 1779/1779 ✅, vitest kataColumn 21/21 ✅, tsc + eslint ✅.

New defect found by H1 (logged, not fixed): Revit refuses some Kata Rebar Numbers — sheet all Ø20: numbers 9, 10
(top storey main bars) kept Revit's 13/14; with Ø22 on the top storey: 11, 12. Present since the first live run (warnings
were hidden). Cause not proven (candidates: Kata groups bars Revit sees as different — top anchorage bends / crank —
or a number held by another bar during assignment). Needs Revit-side inspection of the two groups.

### Root cause of the refused numbers (live 2026-11-03 17:20–17:30, diagnostics since removed)
- Revit numbers before the swap (sheet all Ø20): Kata 9 = Revit 9 (3 face bars, 2786.6 mm) + Revit 12 (2 corner bars,
  2797.7 mm); Kata 10 = Revit 10 (2 corner, 1997.7) + Revit 11 (3 face, 1986.6). Same `Rebar Shape 8`.
- Centrelines right after `CreateFromCurves` are identical for all ten top bars (L597.1 A5.8 L115.9 A5.8 L… A55.0 L265.0).
  After `Regenerate` Revit stretches the last leg (F5·d top anchorage) of the **corner bars** — leg in a diagonal plane —
  from 265.0 to 276.1 mm; two face bars also move 2 mm. Revit then counts them as different bars.
- Kata's key (segment lengths rounded to 5 mm) is right about the design; the difference is Revit re-fitting the
  shape on a diagonal plane. Fix options: corner anchorage legs along one face direction (b or h) instead of the
  diagonal; or number corner bars apart. `KataRebarNumberAssigner` now logs each Revit number's shape, length and ids
  when a number is refused.

### Fix (user decision 2026-11-03: corner legs along b)
- `KataColumnBarPlanner`: per-bar side direction (`SideOf`): face bars outward; corner bars along their crank into the
  storey above when it has one (a two-face step cranks diagonally), else along b. Lap insets, starter feet, cut legs and
  roof legs use it, so every bar lies in one vertical plane. First try (feet/legs along b only) left corner bars of a
  two-face step bent both ways → free-form `M_00`, numbered apart (Kata 4/6 merged by Revit) — fixed by `SideOf`.
- Tests: `Plan_CornerFeetAndLegs_RunAlongB_NeverDiagonal`, `Plan_Steps_EveryBarLiesInOneVerticalPlane` (4 cases); Core 1784/1784.
- Live (copy 14, detached): Revit 12 numbers = Kata 12, 0 changed, 0 warnings; rerun 94/94, no warnings.

### Live of untested branches (see live-phase-04.md)
- Round hoops: two hooked arcs are refused by Revit → fixed with `KataColumnRoundHoopFactory` (lapped-circle shape).
- Open: Revit shortens one inner closed tie of TẦNG 1 by 6.5 mm on regenerate (other storeys untouched, same plan) →
  Kata 21 split into two Revit numbers. Cause not proven (suspect host geometry of that storey). Logged, not fixed.
