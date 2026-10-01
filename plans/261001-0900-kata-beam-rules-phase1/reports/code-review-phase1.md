# Code review — Kata beam rules phase 1 (2026-10-01)

Reviewer: code-reviewer agent (read-only, numbers probed against built Core). Verdict: DONE_WITH_CONCERNS → fixed below.

| # | Sev | Finding | Fix | Status |
|---|---|---|---|---|
| C1 | Critical | `HPRebar/HPRebar.McpBridge/Application.cs:115` `settings.ExecutionEnabled = true;` (uncommitted, not this task) forces Revit AI code execution on at every start; commit ac6c2d4 also defaults/persists `ExecutionEnabled` in `McpShared/.../BridgeSettingsStore.cs:52-56` | Not touched (other session's file). Must not be committed; user decision on ac6c2d4 | 👤 user |
| H1 | High | Dense zones meeting (Ln ≤ 4h): leftovers from both faces leave a gap up to sDense + sEnd (298 mm at a150/a200) | `KataStirrupZoneLayout.ThreeZones`: gap > dense spacing → evenly spaced filler zone "Giữa nhịp (đai dày)" (≤ min(sDense, sEnd)). Also pre-existing: gap in (sSparse, 2·min] got no middle → now any gap > sSparse gets one | ✅ fixed, `Stirrup_gaps_never_exceed_the_spacing_of_their_zone` (7 cases), 12-span stress asserts gaps |
| M1 | Medium | Stagger ignored the run-through end of the weaker side of `a;b` cells (380 mm instead of 500) | `TopRow.LeftThrough/RightThrough` from `KataSupportTopBarLayout.RunThrough`; stagger references furthest end of inner row; outer run-through exempt (anchorage) | ✅ fixed, `The_weaker_side_running_through_*` |
| M2 | Medium | Outer row held at opposite face could end behind inner row; warning said 0 mm; warning also fired when raw cut already past the face | Signed message ("ngắn hơn hàng 14 100 mm"), only moves outward, wording "nhịp không đủ chỗ cắt lệch"; extended reach rounded up to cut step 50 (L2) | ✅ fixed, `An_outer_row_that_ends_short_*` |
| M3 | Medium | NaN/∞ through dialog → crash (`capacity`) / NaN geometry; bound mismatch 0.45 vs < 0.5; store cached unsanitised | `KataSettingsJson.Sanitize` public, used by JSON read, rule builder, `KataSettingsStore.Save`; dialog rejects non-finite; one bound < 0.5 | ✅ fixed, `Settings_that_are_not_finite_*` |
| L1 | Low | `KataDamSheetParser.cs` 300 lines | `ParsePositive` moved to `KataBarNotationParser` (296 lines) | ✅ |
| L2 | Low | G1 comma swap "1,5" → 1.5 mm; off-grid extended cuts | Comma swap removed (invariant only); extended reach rounded | ✅ |
| L3 | Low | G1 warning on every beam of a workbook with `-300;11700` | Kept: user should know G1 is not used | ➖ by design |
| L4 | Low | Ln < 2·offset: first stirrup may sit inside support | Pre-existing, out of scope | ➖ |
| L5 | Low | Unreadable row 20 reported as "trống" | Pre-existing wording, out of scope | ➖ |

Not changed (existing behaviour, noted in docs §3): an interior support's row cut can stop inside the neighbouring column when that span is short; only beam-end columns clamp.

After fixes: Core tests 816/816; `dotnet build HPRebar/HPRebar.csproj -c Debug.R26|R25|R24 -p:DeployAddin=false` 0 errors.
