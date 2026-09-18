# Code review — HPGeo (VN-2000 ↔ WGS84 ↔ KMZ add-in for AutoCAD 2026)

Date 2026-09-18 · scope `HPGeo/` (Core, AutoCad, Loader, Tests; 5 228 LOC C#, 4 windows/controls XAML) · findings only, no file edited.
Baseline: `dotnet test HPGeo.Tests -p:DeployBundle=false` → 81/81 pass (README still says 65). AutoCAD not run.
Method: every file in the brief read; hypotheses on the parser, tessellator, KML reader, reverse transform and argument parser verified by a scratch console against `HPGeo.Core` (numbers below are measured, not estimated); AutoCAD API claims checked against the repo's own verified reader (`HPAutoCad.Aec/Cad/EntityShapeReader.cs`).

## What is solid (so it is not re-litigated)

- Projection: `Helmert7.Inverse` adjugate re-derived by hand — correct; 236 reverse golden cases span lat 8.1–21.7, lon 101.1–110.4, all 17 meridians; the height iteration (`Vn2000Wgs84Transform.cs:45`) converges to < 1e-8 m in 2–3 steps at the four corners of the country (probe). NaN / pole / `k0=1e-300` inputs give NaN, never throw, and the envelope check turns them into `OUTSIDE_VIETNAM`.
- WPF: every `IsChecked` / `SelectedItem` / `SelectedIndex` / `Text` binding (23 checked) targets a settable property; `SourceIsText` has a setter on purpose; `MapPanel.Data` is a OneWay DP; DataGrids are read-only through the implicit style (`Theme.xaml:105`). No TwoWay-to-get-only binding remains. Implicit styles are merged into the window's own `Resources`, not `Application.Current`, so nothing leaks into AutoCAD's UI.
- Loader/ALC: prefix guard for `Ac`/`Ad`/`Autodesk.` + `AssemblyDependencyResolver`, `ExcludeAssets=runtime` on `AutoCAD.NET`, contextual reflection around every pack-URI load, ribbon events unsubscribed in `Terminate`, `DynamicInvoke` unwrapped. No path loads an Autodesk assembly twice.
- AutoCAD API: Xrecord created with `SetAt` before `AddNewlyCreatedDBObject`; existing Xrecord opened ForWrite before `Data =`; `LayerTable.UpgradeOpen()` only when adding; locked/frozen import layer refused before anything is opened for write; erased ids skipped; every command catches `System.Exception`.
- XML: `XDocument` defaults keep `XmlResolver = null` (XXE → empty, probe) and cap entity expansion at 10 M chars.

## Findings (ranked)

### High

**H1 — Arc flattening is 8 chords per arc whatever the radius: metres of error on real boundaries.**
`HPGeo.Core/Geometry/BulgeTessellator.cs:12,53` (`SegmentsPerArc = 8`, used by `DrawingReader.cs:84`).
Measured max chord deviation: r 5 m / 90° → 0.024 m (the header comment promises "< 5 mm"); r 50 m → 0.24 m; r 200 m → 0.96 m; r 500 m / 90° → 2.41 m; r 300 m / 270° → 12.9 m. Interior vertices are exactly on the arc (1e-14), so only the chord count is wrong.
Scenario: a parcel fronting a road curve (R 200–500 m is routine) exports to a KMZ whose boundary sits 1–2.5 m inside the kerb in Google Earth; nothing warns. This is the tool's core promise.
Fix: pick the count from a sagitta tolerance — `n = max(1, ceil(|θ| / (2·acos(1 − tol/r))))`, tol ≈ 0.01 m in drawing units (needs the unit factor, or sample after scaling) — exactly what `HPAutoCad.Aec` does (`EntityShapeReader.cs:227-230`, `ArcSteps(radius, sweep)`); or in the reader use `Polyline.GetArcSegmentAt(i).GetSamplePoints(...)`. Add a test that asserts deviation ≤ tol for r ∈ {5, 50, 500} and θ ∈ {90°, 180°, 270°}, plus negative bulge and a closed ring whose last segment is an arc.

**H2 — `HPGEO` and `-HPGEOKMZ` modify the drawing while registered `NoUndoMarker` and documented "never modifies the drawing".**
`HPGeo.AutoCad.Loader/HPGeoCommands.cs:12-19` (flags), `Commands/HPGeoDialogCommand.cs:49` and `Commands/HPGeoKmzScriptCommand.cs:44-49` (`DocumentSettingsStore.Write` → Xrecord in the NOD), doc comments at `HPGeoKmzScriptCommand.cs:19` and `HPGeoCommands.cs:21`.
`NoUndoMarker` is for commands that do not touch the database: with no marker, the NOD change is appended to the previous command's undo group.
Scenario A: user draws the boundary (PLINE), runs HPGEO and exports, presses U once to drop a stray point → the PLINE disappears together with the settings record.
Scenario B: a batch script `-HPGEOKMZ cm=… out=… ` then `_.CLOSE` stalls on "Save changes?" (or the read-only-drawing variant); `tools/acceptance.ps1:103` only passes because it does `_.SAVEAS` first. Users' own scripts will not.
Fix: remove `NoUndoMarker` from the two commands (they do write), or make the drawing-side store an explicit opt-in / separate `HPGEOSETZONE`; correct both comments. Keep `HPGEOINFO` as is (read-only).

### Medium

**M1 — `DrawingReader` reads polyline vertices in OCS (`GetPoint2dAt`), not WCS.**
`HPGeo.AutoCad/Cad/DrawingReader.cs:73-74`. `Polyline.GetPoint2dAt` returns the vertex in the polyline's OCS; `GetPoint3dAt` returns WCS. A mirrored LWPOLYLINE (normal −Z) or one imported with a tilted normal yields X negated / garbage, and the bulge sign is OCS-relative too.
Scenario: a boundary copied from a mirrored sheet → E ≈ −600 000 → `OUTSIDE_VIETNAM` naming a nonsense E while the POINTs of the same drawing convert fine; the user blames the meridian. With an oblique normal the error is silent.
Fix: `GetPoint3dAt(i)` (+ `GetArcSegmentAt` for arcs), as `HPAutoCad.Aec/Cad/EntityShapeReader.cs:222` already does; optionally refuse `pl.Normal` not parallel to Z with a clear issue.

**M2 — Pasted text with a leading numeric index is misread; the documented format `1;10.77;106.70` does not work.**
`HPGeo.Core/Import/CoordinateTextParser.cs:51,66-70` (regex takes the first two numbers), doc comment `:39-41`.
Probe: `1;10.77;106.70` → (1, 10.77) "ngoài Việt Nam"; `1 600125.887 1231608.428` and the tab-separated Excel form `1\t600125.887\t1231608.428` → grid (1, 600125.887) "cặp mơ hồ" → `OUTSIDE_VIETNAM`. Every "STT E N" table pasted from Excel fails; no test covers it (tests use `M1`, `POINT`, or no label).
Fix: when a line holds ≥ 3 numbers and the first is an integer without a decimal point (or equals the running row number), treat it as the label; add the four probe lines as `[Theory]` cases. Also note in the UI that the decimal separator is the dot (`588940,2481 1251366,2704` becomes four integers today — a reference-tool decision, but worth one line of help text).

**M3 — `KmlReader` drops every coordinate tuple written as `lon, lat, alt` (space after the comma).**
`HPGeo.Core/Import/KmlReader.cs:97-100`: whitespace split first, then `,` split → `"106.6684,"` has an empty second part → skipped. Probe: a Point and a LineString in that form → 0 features → "File KML không có Placemark nào có toạ độ" although Google Earth opens the same file.
Fix: collapse `,\s+` → `,` (or regex `(-?\d+(\.\d+)?),\s*(-?\d+(\.\d+)?)`) before splitting; add a malformed-input test set (spaces, CRLF inside `<coordinates>`, inner-only polygon — today read as the outer ring, `KmlReader.cs:82-83`).

**M4 — `DocumentSettingsStore.Read` is not lenient: a foreign or newer `output` value makes every HPGeo command fail on that drawing.**
`HPGeo.AutoCad/Cad/DocumentSettingsStore.cs:52` calls `KmlExportOptions.ParseOutput`, which throws `ArgumentException`; `Read` is the first thing `HPGEO`, `HPGEOIMPORT`, `HPGEOINFO` and `-HPGEOKMZ` do.
Scenario: version N+1 stores `output=lines`; a colleague on version N gets "Kiểu xuất 'lines' không hợp lệ" from all four commands until the Xrecord is deleted by hand. Same shape for any other tool using the key `HPGEO`.
Fix: `try { … } catch (ArgumentException) { Output = Both }`, and treat a non-string/odd pairing as "no settings", as the class comment already promises.

**M5 — No size cap when reading a KML/KMZ into memory inside acad.exe.**
`KmlReader.cs:33-39` inflates the whole `.kml` entry / file into an `XDocument`. A 5 MB KMZ that inflates to gigabytes (deliberately, or a huge Google Earth export) exhausts AutoCAD's memory with the user's unsaved drawings open.
Fix: refuse `entry.Length` / `FileInfo.Length` above e.g. 64 MB with an issue; optionally stream with `XmlReader` and stop after N placemarks.

**M6 — Map page: third-party script without SRI, and the site's location leaves the machine as soon as the dialog opens.**
`HPGeo.AutoCad/UI/MapHtml.cs:19-20` loads Leaflet from unpkg with no `integrity`/`crossorigin`; `:32` fetches Esri tiles for the converted extent; `MapPanel.xaml.cs:58` sets a UA naming the add-in and version. Host surface is limited to string messages (no `AddHostObjectToScript`), so a compromised CDN cannot reach AutoCAD, but it can run arbitrary script in the panel and phone home; and a surveyor previewing a confidential plot has already disclosed its bounding box to Esri before deciding to export.
Fix: pin `integrity=` hashes (or ship leaflet.js locally through `SetVirtualHostNameToFolderMapping`), and add an "offline / no map" toggle persisted in settings.json.

**M7 — Escape at the selection prompt means "convert all of model space".**
`Commands/HPGeoDialogCommand.cs:74-77`: `PromptStatus.Cancel` falls through to `ModelSpaceIds`. On a 100 k-entity survey base map the user cannot abort HPGEO once started; it reads every entity and opens the dialog.
Fix: `if (picked.Status == PromptStatus.Cancel) return Array.Empty<ObjectId>()` and end the command; keep Enter/empty = all.

### Low

**L1 — `MapPanel.OnLoaded` (async void) outlives the modal window.** `MapPanel.xaml.cs:47-71,124-136`: closing the dialog while `CreateAsync`/`EnsureCoreWebView2Async` is pending disposes the browser first; the continuation then throws, logs a false "WebView2 could not start" ERR and pokes the disposed control. Guard with an `_unloaded` flag. Also `PushData` ignores `""` (`:106`), so the map keeps the previous points when the conversion becomes invalid.

**L2 — `-HPGEOIMPORT` rebuilds its argument line by re-quoting values.** `Commands/HPGeoImportScriptCommand.cs:32`: `kv.Key != "file"` is case-sensitive on a case-insensitive dictionary (`FILE=` → "Tham số 'FILE' không hợp lệ"); a value containing `"` breaks the tokeniser. Give `ExportArguments` an overload that takes the dictionary.

**L3 — Output path hygiene.** `KmzWriter.SafeFileName` (`KmzWriter.cs:43-50`) leaves reserved names and trailing dots (`CON` → `CON.kmz` = the console device; `..` → `...kmz`); `-HPGEOKMZ out=` accepts relative traversal / UNC and overwrites silently (`FileMode.Create`, `KmzWriter.cs:29`). Acceptable for a script command; document it and reject device names in `SafeFileName`.

**L4 — XML-illegal characters are not stripped from names.** `KmlDocumentBuilder.cs:146` uses `SecurityElement.Escape`; a label/document name with U+0001 produces a KMZ Google Earth refuses (probe: `XmlException`). Latent today (labels are numeric), one `Regex.Replace(@"[\x00-\x08\x0B\x0C\x0E-\x1F]", "")` away.

**L5 — Test gaps that matter.** `ExportArguments` (the whole script contract: tokeniser, quoting, unknown keys, unit words, `.kmz` suffixing) and `WildcardPattern` (`#`, `~`, comma lists, 200 ms timeout path) have zero tests; `BulgeTessellator` has one semicircle (no radius/deviation, bulge > 1, negative, closed ring ending in an arc, NaN bulge); the parser has no numeric-index / tab case; `KmlReader` has no malformed inputs; nothing pins that HPGEO/-HPGEOKMZ leave DBMOD in a defined state (acceptance could check `DBMOD` after `-HPGEOKMZ`). README count is stale (65 → 81).

**L6 — Doc/contract drift to fix with the code:** `HPGeoKmzScriptCommand.cs:19` "never modifies the drawing" (H2), `BulgeTessellator.cs:8` "< 5 mm" (H1), `CoordinateTextParser.cs:39` sample formats (M2), `HPGeoCommands.cs:21` "these two write to the drawing" (H2).

## Recommended order

1. H1 + M1 together in `DrawingReader`/`BulgeTessellator` (same code path; copy the AEC reader's approach) with the deviation test.
2. H2 (flags + comments) — one-line fix, real user-data consequence.
3. M2, M3, M4 — each a small parser/read fix plus tests.
4. M5, M6, M7 — robustness/privacy, each < 20 lines.
5. Lows in any order; L5 tests alongside the fixes above.

**Status:** DONE_WITH_CONCERNS

**Summary:** The engine, the ALC loader and the WPF bindings are in good shape — the maths is pinned, no binding can crash AutoCAD, nothing loads an Autodesk assembly twice. The concerns are in the drawing-facing edges: arcs are flattened with a fixed chord count that puts real boundaries metres off (H1), the two export commands write to the DWG under `NoUndoMarker` and against their own documentation (H2), polylines are read in OCS (M1), and the two import paths reject inputs the docs and Google Earth accept (M2, M3). None of the 81 tests would catch any of them.
