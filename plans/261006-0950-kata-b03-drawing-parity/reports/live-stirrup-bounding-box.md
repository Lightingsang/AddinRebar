# Live — outer stirrups hidden in cross sections, 2026-10-06

Symptom (user, Dam kata test.rvt, Detail 3 at x = 1634 across B03 span 1): U/C ties and bars drawn, outer hoops missing.

Cause (read-only on the user's model): all 10 B03 outer stirrup sets had a bounding box mirrored behind the first bar (e.g. 9820507 bars 400..2950, box -2155..415). Revit picks what to draw in a view by the bounding box, so the set was not in Detail 3 (collector by view: absent, view geometry 0) though a bar at 1750 lies in the 181 mm view depth. Inner U/C sets and B01 hoops: box correct. `KataStirrupSetCreator` laid every set out with barsOnNormalSide = true, found it growing towards -X and laid it out again with false; the box stayed from the first layout.

Fix: [KataStirrupSetCreator.cs](../../../HPRebar/HPRebar/KataRebar/Service/KataStirrupSetCreator.cs) — side from `accessor.Normal · AxisX`, one layout; growth still checked; `CheckBoundingBox`: box must hold first and last bar (±30 mm) or the set rolls back to single bars (logged).

| Check (copy b01-test, pid 16396, KataB03 J9 30/25) | Result |
|---|---|
| Generate B03 | 10 stirrup sets, 0 single stirrups |
| Every shape-driven set in the model, box X vs bars | 108/108 within 30 mm |
| Temp section like Detail 3 (dryRun, rolled back) | outer set 9820595 in view, geometry 1 (was absent / 0) |
| Build Debug.R26, Core tests | OK, 1594/1594 |

Not run: B01 regenerated with the new code (needs KataB1 active) — B01 sets grew +X on the first layout, so the chosen side is the same (inference).
User's own model keeps the bad boxes until B03 is generated again there.
