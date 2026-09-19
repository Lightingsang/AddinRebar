# HPGeo — VN-2000 → WGS84 → KMZ for AutoCAD 2026

Converts survey points (POINT) and boundaries (LWPOLYLINE) drawn in VN-2000 / TM-3 — central meridian per
province — to WGS84 and writes a KMZ that opens in Google Earth at the right place. The engine is a port of
the reference HTML tool `X:\02-TOOL\04-Other\ToolVN2000ConvertGoogleEarth (TheoDiaDanhHanhChinhMoi).html`
(Snyder TM inverse + 7-parameter Helmert in the coordinate-frame convention; 34 + 63 province table) with the
reverse direction added and pinned against proj4.

## Layout

| Project | TFM | What |
|---|---|---|
| `HPGeo.Core` | net8.0 | Projection (TM forward/inverse, Helmert 7, geocentric), province catalogue (embedded JSON), converter + validation, KML/KMZ writer and reader, pasted-text parser, import planner, script-argument parser, settings record. No AutoCAD. |
| `HPGeo.AutoCad` | net8.0-windows | The add-in proper: drawing readers/writer, the five command bodies, the two WPF dialogs (CommunityToolkit.Mvvm) with the shared CRS block, the WebView2 map panel, per-drawing (Xrecord) and per-user (JSON) settings. Loaded into its own `AssemblyLoadContext` from `Contents\App\`. |
| `HPGeo.AutoCad.Loader` | net8.0-windows | What AutoCAD loads: `IExtensionApplication`, the commands, the `VN2000` ribbon panel on the shared `HPAutoCad` tab, the load context, the bundle deploy target. Zero NuGet dependencies on purpose — a second Serilog/Mvvm in AutoCAD's default context is a `FileLoadException`. |
| `HPGeo.Tests` | net10.0-windows | xUnit v3: golden fixtures (forward = the oracle's own JS, reverse = proj4), catalogue, converter, KML, view model. |
| `tools/` | node | `extract-province-data.js` (oracle → `HPGeo.Core/Data/vn2000-provinces.json`), `gen-golden-*.js` (fixtures), `acceptance.ps1` (unattended AutoCAD run), `dialog-check.ps1` (dialog screenshots). |

## Commands

- `HPGEO` — pick POINT/LWPOLYLINE (pick-first set counts; Enter = all of model space), then the dialog: catalogue (34 / 63 provinces), province with type-ahead, central meridian with the former-province note, manual KTT, k0/FE/FN/unit under *Nâng cao*, output (points + boundaries / points / boundaries), colours (aabbggrr), preview table, **satellite map** (Leaflet over Esri World Imagery in a WebView2; needs internet — the Google Earth / Maps buttons are the fail-safe), KML preview, *Xuất KMZ & mở Google Earth* (writes the KMZ and opens it at once in the desktop Google Earth — `googleearth.exe` from the Google Earth Pro / Google Earth install folders, else the `.kmz` association; the status line says which, or that none was found), *Mở Google Earth*, *Google Maps*, and **Chèn ảnh vệ tinh vào CAD** (HPGEOIMAGE from the dialog: resolution and **vùng ×** boxes beside it — the image covers that many times the selection's bounding-box area, default 10; the dialog closes and the same pipeline as `-HPGEOIMAGE` runs on the command line over the extent of everything selected — points and boundaries — with the dialog's zone and unit, zooming out when the view would overflow the 4096 px cap). Ribbon: panel `VN2000` ▸ `KMZ` on the **shared `HPAutoCad` tab** (never a tab of its own — the rule for every HP AutoCAD add-in; whichever bundle loads first creates the tab, the others join it).
- `-HPGEOKMZ cm=105.75 out=C:\out\site.kmz [type=both|points|boundaries] [unit=m|mm] [layer=<wildcard>] [k0= fe= fn= name= pcolor= lcolor=]` — no dialog; every POINT/LWPOLYLINE of model space. For scripts and the acceptance harness.
- `HPGEOIMPORT` — the reverse direction: a KML/KMZ (Google Earth "Save Place As…" or a file this tool wrote) or pasted text (`lat, lon` lines, or VN-2000 `E,N` / cadastral `X,Y` with the reference tool's order rules) becomes POINT + LWPOLYLINE entities on the layer `HPGEO-IMPORT`, written in one transaction (one `U` removes them).
- `-HPGEOIMPORT file=C:\in\site.kmz cm=105.75 [unit=m|mm] [k0= fe= fn=]` — no-dialog import.
- `-HPGEOIMAGE [cm=105.75] [handle=<hex>|layer=<wildcard>] [res=0.3|zoom=19] [area=10|margin=30] [unit=m|mm] [out=<file.png>] [provider=esri] [k0= fe= fn=]` — satellite imagery under a land boundary: the closed LWPOLYLINEs named (one handle, a layer wildcard, or all of model space) give the extent; by default the image covers **10 × the boundary's bounding-box area** (`area=`, the plot in its surroundings — the margin is the root of (W+2m)(H+2m) = r·W·H), or `margin=` metres on every side; with no explicit `res=`/`zoom=` a view too wide for the caps is taken at the finest zoom that fits (`RESOLUTION_REDUCED` warning) instead of refused; Web Mercator tiles (Esri World Imagery, cached in `%LocalAppData%\HPGeo\tiles`, ≤ 1024 tiles / 4096 px a side) are fetched, **warped onto the VN-2000 grid** through a 64-px control grid with a Catmull-Rom bicubic kernel (bilinear softened every edge) (the exact VN-2000 → WGS84 → Mercator chain, not a rotated affine placement — the affine residual is reported and `IMAGE_DISTORTION` warns above 0.5 m), stamped with the attribution and written as `<dwg>_hpgeo_<label>.png` + `.pgw` beside the DWG (an unsaved drawing: `%LocalAppData%\HPGeo\images`), then inserted as one north-up `RasterImage` on the layer `HPGEO-IMAGE` at the bottom of the draw order. Zone and unit are resolved value by value: argument → the drawing's record → the user's settings → the TM-3 defaults (so a drawing stored with k0 0.9996 is never projected with 0.9999), unit: `unit=` → the record → INSUNITS; what was used is printed and written back. Escape cancels the download; a deadline of 60–300 s (3 s per tile) ends a black-holed connection. One `U` removes the image, its definition, the layer and the settings record. Refusals: `NO_BOUNDARY`, `NO_CENTRAL_MERIDIAN`, `UNKNOWN_UNIT`, `LAYER_LOCKED`, `OUTPUT_NOT_WRITABLE`, `OUTSIDE_VIETNAM`, `TOO_MANY_TILES`, `TILE_FETCH_FAILED` (first failing URL + reason + count — a captive-portal page is "not an image", never cached), `CANCELLED`; nothing is inserted and no PNG/PGW pair is left behind. Esri's terms for saving tiles into a drawing: [chưa xác minh] — the user's call; the attribution is always burnt in and logged.
- `HPGEOINFO` — diagnostics: version, product, INSUNITS and factor, the settings stored in the drawing and the user's last ones, what the drawing holds, the imagery state (RasterImage count on `HPGEO-IMAGE`, provider, resolution, margin, tile-cache size), a trial conversion of the first point, the log folder.

Settings precedence when a dialog opens: the drawing's own record (named-object dictionary `HPGEO`, written after every export/import) → the user's last (`%AppData%\HPGeo\settings.json`) → the first-run defaults (34-province catalogue, TP. Hồ Chí Minh, 105°45′).

## Rules that are not optional

- In the drawing **X = Easting, Y = Northing**. Cadastral paperwork writes the same pair as X = Northing, Y = Easting; the tool never swaps drawing geometry — the plausibility check only *diagnoses* ("có vẻ X/Y bị đảo").
- VN-2000 values are metres. INSUNITS decides the factor; INSUNITS 0 means the user must choose — nothing is assumed.
- No central meridian → no conversion. A result outside Viet Nam (lat 7.5–24, lon 101.5–110.5) is an error naming the E/N and KTT the tool understood.
- EPSG codes are not assigned: the reference material contradicts itself on them and the engine does not need them.
- The KML this tool writes carries a hidden exact `<Point>` (9 decimals) inside every survey marker and 9-decimal rings, so a re-import lands within a millimetre; the reference tool printed 7.

## Build, deploy, verify

```bash
cd HPGeo
dotnet build HPGeo.slnx -c Debug            # deploys %AppData%\Autodesk\ApplicationPlugins\HPGeo.bundle\ (AutoCAD closed)
dotnet build HPGeo.slnx -c Debug -p:DeployBundle=false   # while AutoCAD is open
dotnet test HPGeo.Tests -p:DeployBundle=false             # 161 tests (3 of them live, behind HPGEO_LIVE_TILES=1: the 4-tile spike, the acceptance prefetch, the helper exe)
cd tools && npm install && npm run all       # regenerate the province JSON and the golden fixtures from the oracle
powershell -File tools/acceptance.ps1        # starts its own AutoCAD, 52 checks (export, import round trip, arcs + mirrored polyline, -HPGEOIMAGE insert through the helper process with an emptied cache / U / a wide view zoomed out to fit / relative re-insert + reopen / 3 refusals / screenshot, stored zone), evidence in output/acceptance/
powershell -STA -File tools/dialog-check.ps1 # opens HPGEO (both COLORTHEMEs; the light run presses "Chèn ảnh vệ tinh vào CAD" through UIA and the log must show the RasterImage) + HPGEOIMPORT, screenshots + WebView2 gate, evidence in output/acceptance/
```

Logs: `%LocalAppData%\HPGeo\logs\` (`loader.log`, `hpgeo-YYYYMMDD.log`). Removing the bundle folder uninstalls.

**Sharpness ceiling:** Esri World Imagery in Viet Nam stops at zoom 19 (0.29 m/px at 11° N — zoom 20 answers a "Map data not yet available" tile). Google Earth shows Google's own, finer imagery there; a Google tile source is out of scope. The add-in therefore keeps zoom 19 across the whole ×10 view (the 4096 px cap), resamples bicubically, and raises the drawing's IMAGEQUALITY from Draft to High when it finds it so — that is the sharpest a static raster from this source gets.

**Network from inside AutoCAD:** tiles are downloaded by the helper process `Contents\App\TileFetch\HPGeo.TileFetch.exe` (net8 console over `HPGeo.Core`; the add-in writes a small request file — provider id + `z/x/y` list, never a URL —, the helper fills the shared cache `%LocalAppData%\HPGeo\tiles` and reports `progress` / `fail` / `done` lines on stdout; Escape kills it). This is what makes the feature work on the dev machine, where the Windows Firewall rule `Autocad2026` (outbound Block for `acad.exe`) makes every in-process download fail with `WSAEACCES`; the in-process `TileFetcher` remains the fallback when the helper is missing or cannot start (`HELPER_UNAVAILABLE` warning). `HPGEOINFO` prints which path is active.
