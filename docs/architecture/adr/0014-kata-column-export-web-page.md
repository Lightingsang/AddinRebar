# 0014 — Kata Column Rebar: Revit column stacks into Kata sheet "Cot" and back as rebar, a web page on Revit 2026

- **Status:** Accepted (2026-11-03, by user — plan `plans/261103-1207-kata-column/`)
- **Tags:** [REVIT] [PROJECT]

## Context
Kata Export writes beam runs into Kata's sheet "Dam"; Kata Pro's own CAD command draws them and Kata Rebar builds the
bars in Revit. The user asked for the same chain for columns: Revit geometry → sheet "Cot" → Kata Pro's `VC` draws the
CAD drawing, and later the sheet → 3D column rebar in Revit. On 2026-11-03 the user decided:
- **CAD:** Kata Pro draws; HPRebar writes the sheet only.
- **UI:** a web page like Kata Export, **Revit 2026 only**; walls (sheet "Vach") later. First planned as two buttons
  (`Kata Column Export`, `Kata Column Rebar`); merged the same day by the user into **one button `Kata Column Rebar`**
  that, like Kata Export, writes the geometry to Excel, reads the bars back and creates real rebar in Revit.
- The reverse-engineered `SPEC_VeCot_Kata.md` is not the cell contract: the real `Kata.xlsm` and Kata's own drawings
  are (`plans/261103-1207-kata-column/reports/kata-column-cell-contract.md`, 19 + 4 drawing variants).

## Decision
1. **Rules in Core:** `HPRebar.Core/KataExport/ColumnStack/` (folder not `Column`: that name shadows the beam tests'
   `Column(...)` helper). `KataColumnSheetBuilder` turns a stack (storeys, plan sections in the lowest column's frame,
   beam depth at each head, pile cap, slab, crossing beam) into the cells A/B/C/D/K of rows 10.. and C1–C5, B9.
   `KataColumnStepRule` holds Kata's placement rule: negative b/h = the storey keeps the left face of the one below,
   positive = centred, D = 0 = round (C = diameter); any other offset is written as the nearer case with a warning.
   Bars, hoops, ties, splice positions (E–J, L–V, H4, H5, F4, F5) and the option boxes are user input and never written.
2. **Host:** `KataColumnRebarCommand` measures the picked columns on Revit's API thread (`KataColumnStackReader`),
   then opens the **modal** `KataExport/View/Web/ColumnExport/KataColumnWebWindow` on the Kata Export page's virtual
   host and profile (start page `column.html`). One bridge `KataColumnWebBridge`: commands
   `column.load|probe|write|reload|generate|close`, event `column.theme`. Step 1 (`probe`/`write`) reaches Excel through
   COM on the window's UI thread; step 2 (`reload`/`generate`, `KataColumnRebarSession`) reads sheet Cot back, plans
   the bars and creates them — on Revit's thread, because the modal window runs inside the command. `KataWorkbookTarget` + `KataExcelRange` are
   the COM plumbing shared with the beam writer (taken out of `KataExcelWriter`, behaviour unchanged). Rows below the
   stack are cleared A..V (Kata reads storeys until A is empty).
3. **Page:** `HPBimToolkit.Frontend/column.html` + `src/kataColumn/`, two steps. 1: storey table, header cells, a
   sketch along b (Kata's elevation direction), warnings, editable C3/C4, Write. 2 (re-reads Excel when opened): the
   plan per storey, the sheet's errors, Create; same kit, tokens and `hp.kata.*` choices as
   ADR-0012/0013. Fixtures `src/kataColumn/__fixtures__/` are written by `HPREBAR_UPDATE_KATA_FIXTURES=1`.
4. **Multi-version:** command and button only when `RevitVersion` is 2026 (csproj `Compile Remove` + `#if REVIT2026`).

## Consequences
- \+ The placement and cell rules have tests against Kata's own sample and drawings.
- \+ The beam and column writers share one COM attachment and busy handling.
- − Modal: re-picking means closing the window and clicking again.
- − Kata's elevation shows b only; a step in h is written but Kata draws it in the section alone.
- − `embed-frontend.targets` now watches `column.html` as its last page.

## Alternatives
- **A column mode inside the Kata Export window.** Rejected by the user: the beam window keeps its own flow.
- **Drawing the CAD sheet from HPRebar (HPAutoCad).** Rejected by the user for now: Kata Pro already draws it.

## Rules
ADR-0008 P5 exception as extended by ADR-0010…0013. DEPENDENCY_RULES D6: no new package.
