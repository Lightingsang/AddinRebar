# Code review: stirrup zones that follow Kata's drawings (uncommitted, 2026-10-03)

Scope: `git diff HEAD` under HPRebar/: KataStirrupZoneLayout, KataStirrupZoneResult, KataStirrupRuns, KataSectionCuts, KataElevationSectionPainter, KataElevationCanvas.Input, 7 test files. Also checked: every `zone.Spacing` / `set.Spacing` consumer in Core + add-in.
Score: 7/10. The rule matches the DY7 golden values and the Spacing/LabelSpacing split is wired right everywhere it matters. One real geometry defect (near-duplicate hoops) and one unrelated security line in the diff.

Evidence: scratch probe (console referencing HPRebar.Core, sheet = SingleSpan with D11 / h varied) + `dotnet test HPRebar.Core.Tests --artifacts-path <scratch>` → 899/902, the 3 failures are `ThemeTokenCoverageTests` (known artifacts-path effect, not this change).

## Critical

### C1 — Unrelated line forces AI code execution on (HPRebar.McpBridge/Application.cs:115)
`settings.ExecutionEnabled = true;` in `CreateBridge()` turns on "Allow AI code execution" on every Revit start and overrides even a persisted "off". That contradicts ADR-04 / CLAUDE.md ("OFF on every Revit start and never persisted"). Note: HEAD (ac6c2d4 "Rebar kata") already changed `BridgeSettingsStore` to default `ExecutionEnabled = true` and persist it, so the guarantee is already weakened in HEAD. This line goes further.
Failure: any process running as the user that can reach pipe `hprebar-mcp-r2026` runs C# in Revit with no click, from the moment Revit starts.
Fix: keep this line out of the stirrup commit. Ask the user whether the HEAD change was meant (a dev convenience?) before it is pushed.

## High

### H1 — Middle zone puts two hoops 1–49 mm apart (KataStirrupZoneLayout.cs:160-163 + Even, :192-200)
`Even` always returns ≥ 2 stations once the length is ≥ 1 mm (`intervals = Max(1, …)`). The middle zone runs from `lastLeft + s` to `firstRight − s`, so its length is `L = gap − 2s`. When the gap is only a little over two mid spacings, L is tiny.
Seen in the probe: clear span 910, a100/a200, h 300 → middle `[850, 860]`: two hoops 10 mm apart. Revit accepts it (`SetLayoutAsNumberWithSpacing` only rejects spacing ≤ 0, RevitAPI.xml), so you get overlapping Ø8 hoops with no warning. Inner stirrups and like-hoops ties copy the same stations, so they overlap too.
Window: gap ≈ ln/2, so this hits clear spans of about 4s to 4s + 250 where `ln − 2·E ∈ (2s, 2s + ~25)`. For a200 that is 901–925 mm and 1001–1025 mm, etc.: short beams such as stair or landing beams and closely spaced columns. Before this change the code centred `ceil(gap/s − 2)` intervals and never did this.
Fix (keeps the approved rule wherever it is meaningful):
```csharp
else if (right.Count > 0 && gap >= 2.0 * sSparse - 1e-6)
{
    double inner = gap - 2.0 * sSparse;
    if (inner < 1.0 || inner >= 0.5 * sSparse)
        middle = Even(lastLeft + sSparse, firstRight - sSparse, sSparse);
    else
    {   // too short for two zone ends: spread over the whole gap, never wider than s
        int n = (int)Math.Ceiling(gap / sSparse - 1e-9) - 1;
        for (int i = 1; i <= n; i++) middle.Add(lastLeft + gap * i / (n + 1));
    }
}
```
Add a regression test: ln 910, a100/a200 → no two middle stations closer than, say, 2 × stirrup diameter.

## Medium

### M1 — Very short spans: 4 hoops at 50 mm, and a comment that says the zones meet (KataStirrupZoneLayout.cs:116,129-130; KataStirrupRuns.cs:14)
Rounding up to 50 makes E = 100 for ln 201–400. That is below ln/2, so the zones do not meet and `Even(50→100)` gives two hoops 50 apart at each face. Probe: ln 250 → `[450,500] [550,600]`, ln 300 → `[450,500] [600,650]`. Both are tagged "a100" (runs merge on LabelSpacing). HEAD gave ln 250 two hoops (50 and 200). The updated KataStirrupRuns doc says "T2-DY14's 250 mm span: its two dense zones meet", but with E = 100 < 125 they do not. No test pins the stirrup stations of the DY14 250 span; only its tags are pinned.
Fix: either confirm against T2-DY14.dwg and pin the stations in a test, or treat `E − offset < s/2` as one station like `Grid` does. Correct the comment either way.

### M2 — The preview grid shows the placed spacing, not the sheet's (HPRebar/KataRebar/View/KataRebarView.xaml:261)
"Bước đai" binds `Spacing` with `a{0:0}`, so a sheet a100 zone shows "a97" while the canvas, tags and section footer say a100. Bind `LabelSpacing`. If the placed value is useful, show it in its own column (e.g. "a100 (96.4)").

## Low

- L1 KataRebarLog.cs:46 logs `zone.Spacing` unformatted (`96.42857142857143`). Use `{Spacing:0.##}` and also log LabelSpacing.
- L2 Filler zone (zones meet, leftover gap): `NominalSpacing = middleSpacing = gap/(fill+1)`, so its tag reads e.g. "a88". This was already the case before. Kata would probably label it with the dense spacing. Consider `NominalSpacing = Math.Min(sDense, sEnd)` for FillerZone; note this would then merge with the neighbouring runs in KataStirrupRuns, which is likely what Kata draws.
- L3 Near-meeting spans (seismic 2h, e.g. ln 2420 with E 1200): `right[0]` is dropped when it is < s/2 from the last left hoop, leaving ≈ 116 mm between the dense zones under an a100 label. This was already the case before and is cosmetic.
- L4 KataRebarPreviewBuilder.cs:25 prints `SupportSpacing` for the right support and ignores `EndSupportSpacing` (row 22). This was already the case before.
- L5 Shift + left click with no drag no longer selects a column (`_panning` blocks `click`). This is acceptable; mention it in the tooltip or help if any.

## Spacing vs LabelSpacing consumers (verified)

| Consumer | Uses | Correct? |
|---|---|---|
| KataStirrupSetCreator.cs:108 `SetLayoutAsNumberWithSpacing(zone.Count, zone.Spacing)` | placed | yes. (n−1)·Spacing = last − first exactly; Revit takes any spacing > 0 |
| KataInnerStirrupLayout.cs:82 → KataBarSet.Spacing → KataBarSetCreator.cs:79 | placed | yes. Stations = hoop + ds; CheckLayout compares array length |
| KataTieStations.cs:45 → KataSideBarLayout.cs:122 / KataLayerSpacerTieLayout.cs:127 | placed | yes. A filtered subset of evenly spaced stations stays evenly spaced |
| KataSectionCuts.cs:87, :94-95 (zone/set windows) | placed | yes (geometry) |
| KataSectionCuts.cs:105 content key, KataStirrupRuns.cs:26-29, KataBarTagBuilder.cs:93, section footer | label | yes |
| KataRebarView.xaml:261 preview | placed | no, see M2 |
| KataRebarLog.cs:46 | placed | yes, but unformatted (L1) |
| KataBarNumbering hoop key | shape only | not affected |

Edge cases checked: zone shorter than offset (`Max(endZone, offset)` → one station; Grid clamps at 0); EndSupportSpacing (right zone nominal = sEnd, tooClose uses min); cantilever (unchanged, placed = nominal exactly); Fixed-Number sets (longitudinal only, KataRebarCreationService.cs:156, not affected); dense/middle boundary (middle starts at lastLeft + s, so no duplicate); gap between 1 and 2 mid spacings (one hoop at the midpoint, transitions in (s/2, s)); count = 1 zones (Spacing = nominal).

## Unresolved (user)
1. C1: was the change in HEAD to persist `ExecutionEnabled` with a default of true intentional? The new uncommitted line forces it on.
2. M1: what does T2-DY14 draw in its 250 mm span: 2 hoops (HEAD) or 4 at 50 mm (now)?
