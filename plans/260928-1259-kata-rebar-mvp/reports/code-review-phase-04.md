# Code review — phase 04 (additional top bars over supports, rows 13-16)

## Xử lý sau review (2026-09-28)

| # | Kết quả |
|---|---|
| M1 | ✅ Cảnh báo theo ô khi ô có chữ nhưng không vẽ được (không đọc được ký hiệu / gối biên chỉ có vế ngoài / điểm cắt không ra khỏi gối) |
| M2 | ✅ Chặn khi một lớp gia cường cách thép chủ dưới < d/2 + max(25, d) + d_dưới/2 (`KataRebarLayoutResult.Blocking`) |
| M3 | ⏭ Chưa sửa: chỉ xảy ra ở gối giữa (nhiều nhịp đang bị chặn); cần user chốt vị trí ngang của 2 vế trước khi mở nhiều nhịp |
| M4 | ✅ I3 đọc như I5 theo đúng T3 đã duyệt: chỉ chữ "tâm" → tâm gối; trống/khác → mép gối |
| L2 | ✅ Kiểm khe hàng 13 dùng đường kính lớn nhất của cả thép chủ và gia cường |
| L3 | ✅ Lùi chân thép dưới kiểm thêm một lượt sau khi đã lùi |
| L4 | ✅ Bảng kiểu thép dựng lại sau khi đo dầm (giữ lựa chọn cũ theo đường kính) |
| L6 | ✅ `IsSymmetric` không phụ thuộc thứ tự nhóm |
| L1, L5, L7, L8 | ⏭ Chấp nhận / để phase nhiều nhịp |

Kiểm lại: 720/720 test; build Debug.R26/R25/R24 0 lỗi. Bản sửa review chưa chạy lại trong Revit (không đổi số đo golden; các test golden vẫn xanh).

Date 2026-09-28. Scope: `git diff 2a8e657` over `HPRebar.Core/KataRebar`, `HPRebar.Core.Tests/KataRebar`, `HPRebar/KataRebar` + untracked (`KataSupportTopBarLayout`, `KataTopLayerStack`, `KataLayerPositions`, `KataSideBars`, `KataSupportTopBarTests`). Read-only; T1-T8 not re-litigated.

## Verification done by reviewer
- `dotnet test HPRebar.Core.Tests` (scratch `--artifacts-path`): 712/715; the 3 failures are `ThemeTokenCoverageTests` (need the repo bin layout under a scratch artifacts path — environment, not this change). All Kata tests pass.
- Scratch console probe (scratchpad, references HPRebar.Core) confirmed findings M1, M2, M3, L1 numerically (numbers below).
- No build run (caller reports R24/R25/R26 pass). Core is netstandard2.0; new code uses only record/with/init/tuple-deconstruction already used in Core; no `#if` needed; Revit side touches no version-gated API → no multi-version risk found.
- File sizes: all < 300 lines (largest new: `KataSupportTopBarLayout.cs` 164). No plan/phase/rule-ID references in code comments.

## Overall
Solid. Geometry signs (outward, inner face, span side `Side(first)`), level stacking (T6), cumulative inset (T7), bottom-leg inset over every overlapping level, marks `3.{k}.{row}` + T/P, row→`Layer` mapping (`12 + Layer` in preview) are all consistent. Golden test numbers re-derived by hand (row 14 z −87, bend 87, leg 407; bottom inset 88 → x 131, leg 331) — correct. Findings are edge cases, not the live-verified path.

## High
None on the path Revit can draw today (single span, two supports).

## Medium

**M1 — Cell content that yields no bar is dropped silently** — [KataSupportTopBarLayout.cs:40-42](HPRebar/HPRebar.Core/KataRebar/Calculators/KataSupportTopBarLayout.cs#L40-L42), [KataTopLayerStack.cs:37-38](HPRebar/HPRebar.Core/KataRebar/Calculators/KataTopLayerStack.cs#L37-L38)
- Scenario: C14 `2f18;0` (bars only on the outer side of the first support) → `At(spanSide:+1)` makes no level → `continue`. Probe: 0 bars, 0 warnings. Same for unparseable text (`2f18(L=2000)` → `ParseSingleBar` null) and for `Add`'s `end.X - start.X < 1` guard (reach from centre smaller than half the column).
- Before this change the scope filter listed rows 13-16 as skipped; now nothing reports them, so the user believes the cell was drawn.
- Fix: in `Build`, when the raw cell (`KataSideBars.Text`) is non-empty but the span side is empty / no bar was added, warn `"{addr} '{text}': vế phía ngoài gối biên — không vẽ"`; for text that parsed to nothing, warn at parse time (keep the cell text; compare token count vs item count).

**M2 — Stacked levels are never checked against the bottom bars** — [KataTopLayerStack.cs:34-42](HPRebar/HPRebar.Core/KataRebar/Calculators/KataTopLayerStack.cs#L34-L42)
- Scenario: beam 300×260, rows 13-16 = 2Ø25 / 2Ø28 / 2Ø28 / 2Ø28. Probe: row 16 centre z = −209.5, bottom bars z = −217 (Ø20) → centres 7.5 mm apart (need ≥ 49): bars intersect; row-16 leg = 7.5 mm (< 10d). Only anchorage-shortfall warnings appear. A deeper stack goes below the bottom cover (legRoom < 0 → straight bar outside the section).
- Fix: after stacking, if `z − d/2 − LayerGap(d, dBot) < zBottom + dBot/2` → blocking error (or at least a warning naming the row), and skip levels below.

**M3 — T8 left/right bars coincide over the interior support** — [KataSupportTopBarLayout.cs:56-61](HPRebar/HPRebar.Core/KataRebar/Calculators/KataSupportTopBarLayout.cs#L56-L61), positions from [KataSupportTopBarLayout.cs:116-118](HPRebar/HPRebar.Core/KataRebar/Calculators/KataSupportTopBarLayout.cs#L116-L118)
- Scenario (the unit-test sheet itself): `2f20;2f16` at support 1. Probe: 3.2.1T Ø20 at y ±53.5, z −43, x 4900→6757; 3.2.1P Ø16 at y ±53.5, z −43, x 6443→8300 → the two bar pairs occupy the same line over 314 mm. Edge bars of rows 14-16 always coincide (both sides start at the stirrup corners). `CheckSpacing` runs per side, so the union is never checked. The test only asserts X.
- Latent: multi-span is blocked by the scope filter, so Revit cannot draw it yet. Becomes High when multi-span unlocks.
- Fix options (the placement across Y is not covered by T8, so the user decides): place the P bars in the gaps the T bars leave (one `BetweenMainBars` over count L+R, split by side), or shift one side by one bar diameter in Y. Add a Y assertion to `An_interior_support_with_different_sides_draws_one_bar_per_side`.

**M4 — I3 origin parsing contradicts T3 for blank/other text** — [KataDamSheetParser.cs:49-52](HPRebar/HPRebar.Core/KataRebar/Parsers/KataDamSheetParser.cs#L49-L52) (existing code, now driving geometry)
- T3: "chứa 'tâm' → tâm gối, còn lại → mép gối". I5 follows it; I3 does the reverse ("contains mép → face, else centre"), so blank I3 or "L từ trục" → centre. Rows 14-16 then reach half a column width less than T3 says. Golden test sets I3 explicitly, so it does not catch this; `KataBeamRebarSpec.CutoffOriginLayer2` default is also `FromColumnCenter`.
- Fix: parse I3 like I5 (`IndexOf("tâm") >= 0 ? Centre : Face`) and align the model default; add a blank-I3 test. If the centre default is intended for Kata's blank template, confirm with the user and write it into T3.

## Low

**L1 — Reach not validated; bars overlap each other or run into the far support**
- [KataSupportTopBarLayout.cs:45-47](HPRebar/HPRebar.Core/KataRebar/Calculators/KataSupportTopBarLayout.cs#L45-L47), [Cuts:83-94](HPRebar/HPRebar.Core/KataRebar/Calculators/KataSupportTopBarLayout.cs#L83-L94). H3 = 1.2 (typo for 0.12, or "L/4" written 4) on a 6000 span: probe gives 3.1.2 x 87→6757 and 3.2.2 x 43→6713 at the same (y, z) → the two supports' bars overlap along 6.6 m; the clamped straight end at 6757 / 43 lands on the main bars' leg (and ignores the level inset), no warning. Same overlap starts from ratio ≈ 0.55 from centre. Interior supports clamped into support 0 behave the same (stress test pins x = 46 = main-leg station).
- Fix: warn when `ratio > 0.5` or when support k's right cut passes support k+1's left cut; clamp to `far face − a − level.Inset` and warn instead of silently clamping.

**L2 — Spacing check ignores the main-bar diameter on row 13** — [KataSupportTopBarLayout.cs:119](HPRebar/HPRebar.Core/KataRebar/Calculators/KataSupportTopBarLayout.cs#L119), [KataLayerPositions.cs:36-47](HPRebar/HPRebar.Core/KataRebar/Calculators/KataLayerPositions.cs#L36-L47). Main Ø25 + extra Ø16 in a gap: clear computed with 16 for both bars → under-reports by (25−16)/2. Pass per-bar diameters (or `level.Diameter`) to `CheckSpacing`.

**L3 — Bottom-leg inset decided on the inset-0 leg** — [KataMainBarLayout.cs:87-99](HPRebar/HPRebar.Core/KataRebar/Calculators/KataMainBarLayout.cs#L87-L99). `LegsOverlap` tests each level against the bottom leg computed with inset 0; the final (inset) leg is longer and can then overlap a deeper level that was not counted. Also a straight lower level whose end falls outboard of the moved bottom leg is never considered. Narrow window, but a two-pass (recompute until inset stops growing) is cheap.

**L4 — Bar-type table built from the pre-pick plan only** — [KataRebarViewModel.cs:124](HPRebar/HPRebar/KataRebar/ViewModel/KataRebarViewModel.cs#L124), [KataRebarTypeResolver.cs:122-123](HPRebar/HPRebar/KataRebar/Service/KataRebarTypeResolver.cs#L122-L123). Diameters come from `plan.Layout.ExtraTopBars`, whose existence depends on geometry (support width > 0, length ≥ 1 mm); `ApplyMatch` does not rebuild the table. A bar that appears only with measured geometry → handler fails "Thiếu RebarBarType cho Øxx" with no row to fix. Fix: take the extra diameters from `spec.Supports[].TopExtraSides` (geometry-free), or rebuild mappings in `ApplyMatch` via `KeepChoices`.

**L5 — Two sources of truth for rows 13-16** — [KataSupportRebarSpec.cs:45-64](HPRebar/HPRebar.Core/KataRebar/Models/KataSupportRebarSpec.cs#L45-L64). `TopExtraLayerN` (flattened, both sides merged) and `TopExtraSides`; `Sides()` prefers the latter, so a `with { TopExtraLayer1 = … }` is ignored silently. Only remaining non-parser use of `TopExtraLayerN` is the fallback in [KataTopLayerStack.cs:56](HPRebar/HPRebar.Core/KataRebar/Calculators/KataTopLayerStack.cs#L56). Document "TopExtraSides wins" on the property or derive `TopExtraLayerN` from sides.

**L6 — `IsSymmetric` is order-sensitive** — [KataSideBars.cs:18-19](HPRebar/HPRebar.Core/KataRebar/Models/KataSideBars.cs#L18-L19). `2f20+1f18;1f18+2f20` → treated as different sides → two overlapping bars (M3). Compare as multisets (group by diameter, sum counts).

**L7 — `mainBars = longitudinal − extraTop`** — [KataRebarOrchestrator.cs:43-45](HPRebar/HPRebar/KataRebar/Service/KataRebarOrchestrator.cs#L43-L45). Correct today (creation is all-or-throw), but breaks silently if creation ever skips a bar. Return both counts from `CreateLongitudinalBars`.

**L8 — Row-13 extra larger than main shares the main centre depth** — [KataTopLayerStack.cs:31-33](HPRebar/HPRebar.Core/KataRebar/Calculators/KataTopLayerStack.cs#L31-L33). `TopBarCentreDepth` is sized for the main diameter; Ø25 extra over Ø16 main loses (25−16)/2 = 4.5 mm cover. Per T1 by design — a warning would do.

## Edge cases checked, no issue
- Span side at end supports: `Build` (`spanSide`), `At`, `TopEndsAt` (`Side(first)`) and `Anchor` all agree (first support → Right/outward −1, last → Left/+1).
- Empty rows take no room (compacted stack) while marks keep the row number — consistent in layout, preview (`12 + Layer`) and log.
- Row-13 extras bend at the main-bar station at other Y → no leg clash; lower levels end `s` inboard, clear = max(25, d).
- Revit side: extras stamped with the host tag → deleted on re-run; diameters included in handler check and mapping; no new transaction/threading paths.

## Recommended actions
1. M1 warnings for dropped/unparsed row 13-16 content (cheap, user-visible).
2. M2 stack-vs-bottom-bar check (blocking error).
3. M4 align I3 parsing with T3 (or get user confirmation on the centre default).
4. M3 before multi-span unlocks: decide Y placement of T/P bars; add Y assertion.
5. L1 reach validation + warning; L4 geometry-free mapping.

**Status:** DONE_WITH_CONCERNS
**Summary:** No High on the drawable single-span path; 4 Medium (silent drops, stack can pass the bottom bars, T8 bars coincide, I3 parse contradicts T3), 8 Low.
**Concerns/Blockers:** M4 may need a user decision if the centre default for blank I3 is intended.
