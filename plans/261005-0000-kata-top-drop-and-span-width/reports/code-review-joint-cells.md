# Code review — joint cells (row 24 "*", rows 17/18 at supports), uncommitted on 70c28ec

Scope: `KataDamSheetParser.ParseSupport` (rows 24/17/18), `KataZeroWidthSupports` (BottomRows, `BarsThroughTheJoint`), tests `KataJointCellsTests` (new, 4) + B01 fixture K24. ~70 changed LOC.
Tests: `dotnet test HPRebar.Core.Tests` → 1339/1339 pass. Probes: scratch console on HPRebar.Core with the B01 sheet + cell mutations (results below).

Score: 7/10. Main path (B01 I17 → 2Ø20 L2 19200..23500, K24 honoured) correct; no duplication of notes; console ends and un-joinable zero-width supports report rows 17/18. Four real gaps.

## Critical
None.

## High
None.

## Medium

M1 — rows 17/18 lost silently at a support whose width is unreadable (`abc`, `0.0`, `-400`, `0x800`, empty).
- [KataDamSheetParser.cs:222](HPRebar/HPRebar.Core/KataRebar/Parsers/KataDamSheetParser.cs#L222) notes only when `width > 0`; [KataZeroWidthSupports.cs:68](HPRebar/HPRebar.Core/KataRebar/Parsers/KataZeroWidthSupports.cs#L68) `IsZeroWidth` excludes `WidthUnreadable`. Neither side reports.
- Probe: G11 = "abc" (or "0.0") + G17 = "2f20" → plan blocked (G11 unreadable) but G17 in neither Skipped nor Blocking. Breaks "every filled cell drawn or reported".
- Same rule written twice, inverted, in two files (PCC-230) — they disagree exactly here.
- Fix: in `ParseSupport` note rows 17/18 unless the support is a readable zero width: `bool zeroWidth = width == 0.0 && row11?.Trim() == "0"; if (!zeroWidth) { Note(…17…); Note(…18…); }` — or expose `KataSupportRebarSpec.IsZeroWidth` and use it in both places. Test: unreadable width + row 17 → Skipped contains it.

M2 — joint bars change the joined span's OWN row 18 bars.
- [KataSpanBottomBarLayout.cs:60](HPRebar/HPRebar.Core/KataRebar/Calculators/KataSpanBottomBarLayout.cs#L60): row 18 under a filled row 17 is extended by G1. Giving I17 to the span makes H18 look "under row 17".
- Probe: H18 = "2f16" → 19200..23500 without I17, 18700..24000 with I17 (+500 each end). Not Kata's rule for H18 (H17 empty), and no note says so.
- Fix: carry a flag on the span (e.g. `BottomExtraLayer2FromJoint`) or keep joint bars in their own list and skip the G1 extension when layer 2 came from the joint; or, simplest, when the span has its own row 18, report the joint row 17 instead of drawing it (extend `hasOwn` to "has any own bottom extra"). Test: H18 own + I17 → H18 extents unchanged.

M3 — "bars through the joint" may not pass under the joint.
- Cut is min(H3·L, L/6) from the joined span's faces regardless of where the joint is ([KataZeroWidthSupports.cs:114-118](HPRebar/HPRebar.Core/KataRebar/Parsers/KataZeroWidthSupports.cs#L114-L118)).
- Probe: H11 = 500, J11 = 6000 → joint at 18600, bars still 19200..23500; note claims "qua nút".
- Fix: joint offset is known (`left.Length` before the merge); if offset < L/6 or > 5L/6 of the final joined length (cut ≤ L/6, so inside that band is always covered) report instead of drawing, or add a warning. Needs the final length → check after the loop, or in the layout where `st.SpanStart` is known. Test with a short left span.

M4 — rows 17/18 "0" / "-" at a support with width reported as unsupported.
- `Note` ([KataDamSheetParser.cs:383](HPRebar/HPRebar.Core/KataRebar/Parsers/KataDamSheetParser.cs#L383)) skips only empty; `ReportCells` / `BarsThroughTheJoint` also skip "0" and "-". Probe: E17 "0", E18 "-" → two "chưa hỗ trợ, không vẽ" lines (B01 itself writes J17 "-").
- Fix: one `IsBlankCell(text)` (empty / "0" / "-") used by `Note`, `ReportCells`, `BarsThroughTheJoint` (PCC-055, PCC-232). Check row 23/24 callers accept that (a "0" there is also "none").

## Low

L1 — drawn/honoured cells land in `Skipped`, shown as "[Bỏ qua] …" ([KataRebarPreviewBuilder.cs:13](HPRebar/HPRebar/KataRebar/ViewModel/KataRebarPreviewBuilder.cs#L13)); `KataRebarPlan.Skipped` doc says "does not draw yet" ([KataRebarPlan.cs:21](HPRebar/HPRebar.Core/KataRebar/Models/KataRebarPlan.cs#L21)). Precedent exists (inner-stirrup Outcome), but I17 is drawn. Fix: route notes with an Outcome to `Warnings` / an info list, or update the doc + prefix.
L2 — Outcome text bakes project sample data: "(B01 I17: 850 / 1300 từ mặt gối)" shows for every project's joint. Move the B01 numbers to the XML comment; keep "điểm cắt Kata tại nút chưa rõ". Also "Meaning — Outcome — …" renders a double em dash (cosmetic).
L3 — two consecutive joints: I17 note says "nhịp H và J gộp" while the final span is H..L and the cut uses 12 700 (probe: 20200..28700, still under I). Note is stale, harmless; K17 correctly reported (hasOwn).
L4 — row-24 "*" check bypasses `Note` with an inline `notes.Add`; `"**"` / `"* "` variants fall to "chưa hỗ trợ" (fine), but the literal `"*"` and the row numbers 17/18/24 are magic in two files (PCC-055) — `KataSpanBottomBarLayout.Layer1Row/Layer2Row` already exist, reuse them.
L5 — FM2: unbraced single-line `if`s in new lines (KataZeroWidthSupports.cs:40-41, 102; KataDamSheetParser.cs:216-220) — matches file style, but the checklist asks for braces on new lines. `row == 17 ? … : …` decided three times in `BarsThroughTheJoint`: a two-row table `(row, layer, has, with)` reads better (M1 one level of abstraction).
L6 — tests: names not `Method_Scenario_ExpectedResult` (PCC-277; file convention is prose, acceptable). Missing cases: I18 alone, I17+I18, unjoined interior zero width (blocked, 17/18 reported), console end O17, two consecutive joints, M1/M2/M3 probes.

## Edge cases checked (probe results)
| Case | Result |
|---|---|
| B01 baseline | 2Ø20 L2 19200..23500, I17 + K24 notes with Outcome ✅ |
| H17 own | I17 reported, not drawn ✅ (test) |
| I18 only | L1 19200..23500, note ✅ |
| I17 + I18 | both drawn, I18 extended by G1 (row 18 under row 17 — consistent) ✅ |
| I17 garbage "abc" | reported "chưa hỗ trợ" ✅ |
| O17 at console end | reported ✅ |
| I24 "*" at joined support | honoured note ✅ |
| Two consecutive joints | I17 drawn, K17 reported, L17 reported ✅ (L3) |
| Note duplication | none: width>0 → parser, zero width → merge only ✅ |
| G11 unreadable + G17 | silent ⚠️ M1 |
| H18 own + I17 | H18 extended +500 ⚠️ M2 |
| H=500 / J=6000 | bars miss the joint ⚠️ M3 |
| E17 "0", E18 "-" | noise notes ⚠️ M4 |

KataScopeFilter: every DetailingNote → Skipped with `Outcome ?? NotSupported` ✅; unjoined interior zero width also blocks via `ReportRunEnds` ✅.

## Recommended order
1. M1 (contract, 3 lines) + test. 2. M4 shared blank predicate. 3. M2 (decide: report when own bars exist, simplest). 4. M3 warning/report. 5. L1/L2 wording.

**Status:** DONE_WITH_CONCERNS
**Summary:** Main joint path correct, 1339/1339 pass; 4 Medium gaps (silent rows at unreadable width, side effect on own row 18, joint not covered by bars, "0"/"-" noise).
