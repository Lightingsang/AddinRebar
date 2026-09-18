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
| `HPGeo.AutoCad.Loader` | net8.0-windows | What AutoCAD loads: `IExtensionApplication`, the commands, the ribbon tab `HPGeo ▸ VN2000 ▸ KMZ`, the load context, the bundle deploy target. Zero NuGet dependencies on purpose — a second Serilog/Mvvm in AutoCAD's default context is a `FileLoadException`. |
| `HPGeo.Tests` | net10.0-windows | xUnit v3: golden fixtures (forward = the oracle's own JS, reverse = proj4), catalogue, converter, KML, view model. |
| `tools/` | node | `extract-province-data.js` (oracle → `HPGeo.Core/Data/vn2000-provinces.json`), `gen-golden-*.js` (fixtures), `acceptance.ps1` (unattended AutoCAD run), `dialog-check.ps1` (dialog screenshots). |

## Commands

- `HPGEO` — pick POINT/LWPOLYLINE (pick-first set counts; Enter = all of model space), then the dialog: catalogue (34 / 63 provinces), province with type-ahead, central meridian with the former-province note, manual KTT, k0/FE/FN/unit under *Nâng cao*, output (points + boundaries / points / boundaries), colours (aabbggrr), preview table, **satellite map** (Leaflet over Esri World Imagery in a WebView2; needs internet — the Google Earth / Maps buttons are the fail-safe), KML preview, *Xuất KMZ…*, *Mở Google Earth*, *Google Maps*. Ribbon: `HPGeo ▸ VN2000 ▸ KMZ`.
- `-HPGEOKMZ cm=105.75 out=C:\out\site.kmz [type=both|points|boundaries] [unit=m|mm] [layer=<wildcard>] [k0= fe= fn= name= pcolor= lcolor=]` — no dialog; every POINT/LWPOLYLINE of model space. For scripts and the acceptance harness.
- `HPGEOIMPORT` — the reverse direction: a KML/KMZ (Google Earth "Save Place As…" or a file this tool wrote) or pasted text (`lat, lon` lines, or VN-2000 `E,N` / cadastral `X,Y` with the reference tool's order rules) becomes POINT + LWPOLYLINE entities on the layer `HPGEO-IMPORT`, written in one transaction (one `U` removes them).
- `-HPGEOIMPORT file=C:\in\site.kmz cm=105.75 [unit=m|mm] [k0= fe= fn=]` — no-dialog import.
- `HPGEOINFO` — diagnostics: version, product, INSUNITS and factor, the settings stored in the drawing and the user's last ones, what the drawing holds, a trial conversion of the first point, the log folder.

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
dotnet test HPGeo.Tests -p:DeployBundle=false             # 111 tests
cd tools && npm install && npm run all       # regenerate the province JSON and the golden fixtures from the oracle
powershell -File tools/acceptance.ps1        # starts its own AutoCAD, 32 checks (export, import round trip, arcs + mirrored polyline, stored zone), evidence in output/acceptance/
powershell -STA -File tools/dialog-check.ps1 # opens HPGEO (both COLORTHEMEs) + HPGEOIMPORT, screenshots + WebView2 gate, evidence in output/acceptance/
```

Logs: `%LocalAppData%\HPGeo\logs\` (`loader.log`, `hpgeo-YYYYMMDD.log`). Removing the bundle folder uninstalls.
