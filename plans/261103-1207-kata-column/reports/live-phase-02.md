# Live — Kata Column Export, 2026-11-03

Setup: user's Revit 31328 untouched (deployed DLL renamed `HPRebar.dll.locked-20261103142411`, Debug.R26 deployed).
Test Revit 18032 on scratch copy `kata-col-es0001.rvt` of `XDMTHCSTH-HPC-LH_HC-ZZ-M3-ES-0001_sangtq6ANLE.rvt`
(workshared, opened read-only), stack C_350x500 → C_300x450 ids 10075038; 9784601; 10141454; 10141800; 10141944;
10142052 (found read-only through the user's MCP, `transaction none`), selected by Manage › Select by ID.
Excel active = scratch copy `kata-col-live.xlsm`; dedicated acad 29844 for VC.

| Check | Result |
|---|---|
| Ribbon button + icon on HPRebar tab | ✅ |
| Window opens on the preselection; page via CDP (`HPREBAR_KATA_DEBUG_PORT=9335`) | ✅ [live-es0001-page.png](live-es0001-page.png) |
| Storeys MÓNG 2.150 / TẦNG 1 3.650 / 2–4 3.600 / KỸ THUẬT 3.050, b×h 350×500 then 300×450 centred, K 350/600/600/600/600/550 | ✅ |
| C1 200 (beam width), C2 kept (no floor at the cap head), C5 −3.05, B9 0.8 (cap top −2.250 = column base) | ✅ |
| Write → COM read-back: C1–C5, B9, rows 10–15 A/B/C/D/K; E, I, L of the sample kept; rows 16+ A..D/K empty | ✅ |
| VC (`VE_COT`) on that sheet: levels −3.050 −2.250 −0.100 +3.550 +7.150 +10.750 +14.350, level names, sections 350×500 / 300×450 | ✅ [variants/live-es0001-vc.json](variants/live-es0001-vc.json) |
| Close test Revit | ⚠️ "Editable Elements" (relinquish/keep) appeared for the workshared copy and vanished; process stopped instead — never relinquish from a copy (it shares the user's local identity) |

Found + fixed: sketch was a hairline (one scale for 19 m × 0.35 m) → own horizontal scale.
Open: C3 = type name when Mark is empty (this project keeps names in `STR_ElementName`, e.g. C24) — user to decide.
