# Research: `Formax01/VinaCAD_Express-Tools` (develop) — what it is, what carries over

Analysed 2026-09-19 by reading the GitHub tree + source over the API (no clone kept). Read-only; nothing from the repo is in this
worktree.

## Facts

| Item | Finding |
|---|---|
| Host | **VinaCAD 2026** (ODA Teigha-based CAD). Namespaces `Prima.VinaCAD.ApplicationServices` / `Prima.VinaCAD.EditorInput` / `Teigha.DatabaseServices` / `Teigha.Geometry` / `Teigha.Runtime`; references `Lib/VinaCad/2026/TD_Mgd_Net8_26.12_17.dll`, `VinaCADCoreMgd_Net8.dll`; `PackageContents.xml` `Platform="VinaCAD" SeriesMin="R26.7" SeriesMax="R26.12"` |
| Stack | .NET 8 `net8.0-windows`, WPF, 7 projects: `Tools.VinaCad.App` (37 `[CommandMethod]`s in one `Commands.cs`, `.cuix` menu — the ribbon loader is commented out), `Tools.VinaCad.Action` (one class per command, prompt loops), `Tools.VinaCad.Helper` (`DimHelper` 1 460 lines, `DrawWallHelper` 1 303, `BlockHelper` 1 084, `BlockTemplateLoader`, `TextHelper`, `WallObjectQueryHelper`), `Tools.VinaCad.Modeling` (static mutable settings + DTOs), `Tools.View` / `Tools.ViewModel` / `Tools.Model` (WPF), `Resources` (`TemplateBlocks.dwg`, PNG lookup tables, `Sample.xlsx`) |
| Closed deps | `PrMVVMCore.dll`, `PrLogTrackingSystem.dll` (Prima's own), log4net, Newtonsoft.Json — shipped as binaries in `Lib/` |
| License | **None.** README is the GitLab template pointing at `gitlab2.tgl-cloud.com/skyace-group/prima-solutions/go_global/toolsvinacad` → company product code, all rights reserved by default |
| Activity | 67 commits, 2 people (dev + reviewer), last 2026-09-18; feature branches per command; no tests; Vietnamese identifiers |
| Design | Interactive commands: `ed.GetSelection` / `GetString` / `GetDistance` loops, `MessageBox`, `ShowModalWindow`, `SendStringToExecute("XLDM ")` to chain commands; settings are `static` properties (`XLTKCTSetting`, `XLDCDSetting`, `NumberingDrawingsSetting`, `XLDatTenCocSetting`) set by a dialog command and read by the work command; block/dimstyle names hard-coded in `StringDefinition` and imported from `TemplateBlocks.dwg` on demand (`BlockTemplateLoader.LoadDimStylesFromFile`) |

## Why it cannot be installed into HPAutoCad

1. Different API binary — Teigha.NET mirrors AutoCAD.NET class-for-class (`Database`, `Transaction`, `BlockReference`, `RotatedDimension`, `Editor`, `PromptSelectionResult`, `TypedValue`/`DxfCode`, `SelectionFilter`), so a *mechanical* port compiles, but the built DLL loads only in VinaCAD.
2. Every command is a prompt loop; the HPAutoCad bridge guard denies `ed.Get*` and `SendStringToExecute`, and seeds take `args`, never prompts.
3. Data dependency on the vendor template (`TKT_TD`, `TKT`, `TKH`, `TKS_B*`, `DVC_B`, `cocMet`, dimstyle `1-1-50-CongDon`).
4. No license — copying code, the DWG or the PNGs is not allowed; ideas are.
5. Quality: static globals, 2 000-line actions, duplicated natural-sort in two DTOs, no tests → not a base to inherit.

## Command inventory → disposition

| Commands | What they do (as read) | Disposition |
|---|---|---|
| `KTD` duplicate dims · `DCD` cumulative ("cộng dồn") dim check · `XLDCD` its settings | `DimHelper`: collect `RotatedDimension`/`AlignedDimension` from a selection; group by direction (horizontal / vertical / other angle) → by dim-line station (`DimLinePoint` projected, tolerance `LechPhuong` 10) → sort along the direction → split into chains where the gap between consecutive extension points exceeds `LechChanDim` 0.9; flag overlaps (start before previous end beyond tolerance) and **text overrides** (`|measurement × DIMLFAC − parsed text| > 1`); duplicates = same dim line (truncated coordinate), sharing one extension point with the other end inside the partner. For a clean chain it creates **running dimensions**: a `"0"` `RotatedDimension` at the base point, then one per segment from the base to the cumulative station, on the same dim line, colour 2, `Dimexe` 5, dimstyle from the template. Wrong dims are put in the implied selection + zoomed | **Port as phase 1** (`audit_dimension_chains`, `create_running_dimensions`). HPAutoCad has no dimension audit at all (grep of `HPAutoCad.Aec` for chain/override: none) |
| `XLDM` / `XLSDM` settings · `DSHBV` number drawings · `VBDM` draw the drawing-list table | Title-block INSERTs by block name → sorted top→bottom-then-left→right or left→right-then-top→bottom with a row tolerance → attribute `DRAWINGNO` set to `prefix + running number` (also an A/B/…/AA alphabet variant); the list table reads `DRAWINGNO`, `DRAWINGTITLE1/2`, `SCALE`, `DATE` | **Port as phase 2** over `BlockService` + `MemberTagging` band ordering + the `Table` writer |
| `TDC` pile coordinate table · `VC` draw piles from a list · `DTC` name piles · `XLDTC` settings | Piles = INSERTs on a layer containing `coc`/`cọc`/`pile` or a block named so (fallback heuristics: has attributes, or contains a circle/polyline); nearest TEXT within a distance is the mark; four reading orders; naming `prefix + number` with a text height/style; table of mark / X / Y; `VC` inserts the pile block at each listed coordinate | **Port as phase 3** |
| `WW` draw wall · `EW` erase + auto-heal · `WWT` change thickness · `WWO` offset wall · `TW` trim/fix | Double-line walls from picked points: two offset lines + optional caps, each line tagged in XData (regapp, segment id, side L/R, thickness, stored centreline) so later commands find the pair; junction cleanup (miter L, T, X: intersections of face lines, trim/extend, remove pieces inside another wall's footprint, `IsPointInPolygon`), heal gaps ≤ 600, parallel test cos 0.5°, thickness ≤ 5 000; `WallObjectQueryHelper` selects only LINEs on the wall layer in a crossing window | **Port as phase 4** (create / repair / thickness); `WWO` folded into create (centreline from an existing face + offset). Biggest effort — the two source files are 4 000 lines |
| `XLTKCT` settings · `TKCT` / `TKCK` schedule blocks · `QS` / `QD` edit schedule rows · `QF` totals | Attributed blocks per bar mark (`DK` diameter, `SLTB` count, segment lengths, `DAI` bar length, `CD` total, `DT`), lap per diameter class (≤10 / 10–16 / >16), hook (normal / seismic), stock length 11.7 m; totals split ≤10 / 10–18 / >18 + shaped steel; natural sort of marks | **Phase 5, optional** — worthless without an attributed block convention; if kept, the mapping is an argument |
| `FDT` find text · `RenameText` · `RenameLayer` · `BB` make block · `BBE` erase blocks · `IPT` import template | Selection + rename / erase helpers | Not ported — covered by `query_entities`, `update_entities_batch`, `manage_blocks_attributes` |
| `BT` / `NEO` / `NOI` / `BTCNN` / `DTT` | Show a PNG lookup table (concrete grades, anchorage / lap tables) | Not a tool |
| `ABOUTEXPRESS`, `Sample` | — | — |

## Algorithm notes kept for the port (paraphrased, not code)

- **Chain grouping is two-level:** first cluster by the dim line's station along the chain normal (running average within a line
  tolerance), then sort by the start station and cut the chain wherever the next start is farther than the gap tolerance from the
  previous end. Overlap is *not* a cut — it stays in the chain and is reported. Aligned dims use the vector between the extension points as
  the direction and a ±0.03 rad angle equality.
- **Displayed value vs measured:** `measurement × DIMLFAC` compared with the numeric text; a non-numeric override is an override too.
- **Running dims:** a zero-length dimension with text `"0"` at the base, then dimensions from the base to each cumulative station using
  `"<>"` text (so they self-measure), dim line point = the source's, display properties copied from the source.
- **Duplicate dims:** same dim line + one shared extension point + the other extension point inside the partner's span.
- **Reading orders:** rows by a band tolerance then columns, or columns then rows; `MemberTagging` already does the band clustering.
- **Wall pairing without tags:** two parallel LINEs (cos ≥ cos 0.5°) whose perpendicular distance ≤ max thickness and whose projections
  overlap; a free end is capped by a perpendicular LINE of the thickness; a T-junction is closed by trimming the crossing face; a gap ≤ the
  heal distance is closed by extending both faces to their intersection.
