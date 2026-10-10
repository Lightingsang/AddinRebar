# Colour by search set — live verify (2026-10-10)

Contract (grill-me, confirmed): 44 colour sets of sheet `ColorSearchSet(DSC)`, names + RGB from the sheet, element rules in `HPNavis/tools/bim-coordinator/color-set-mapping.json` (user decisions: Fire Protection Wet → FireSprinkler; cable trays by model EC/EF = ELV, EE = LV; SH soil, TP + TNT waste, TN(BM) kitchen, TNM rain, TH vent, CNL / CN-L cold water; HVAC by standard Revit classification, unverified), permanent colours, reset of HP elements only.

API (apidocs 2027 = installed 2026, reflected): `AppearanceOverrides` is read-only (`SavedViewpoint.GetAppearanceOverrides()` → `MaterialOverride{Item, Color, Transparency}`); no API writes overrides into a viewpoint. Painting = `DocumentModels.OverridePermanentColor`; read-back = `ModelGeometry.PermanentColor`; reset = `ResetPermanentMaterials(items)`.

Model: copy `HPNavis/output/live-verify/bim/THCSLT-HPC-ZZ-ZZ-CM-ZZ-0001-colors.nwd` (= original, SHA256 B27F…).

## Findings that changed code
| Live fact | Change |
|---|---|
| Element > System Type is a NamedConstant element reference (`PipingSystemType "TNM", #714123`) — wildcard/contains on it match nothing (preview showed 6 plumbing sets empty) | match the System Type tab's `Name` (`HasPropertyByDisplayName("System Type","Name")`, `PropertyKey.ByDisplayName`) — 8/8 systems = expected counts; System Name `"<abbr> *"` also 8/8 but is an instance name |
| enumerating a saved set's `ModelItemCollection` ≈ 1.3 ms/element (HP_S_All 36k = 46 s; first paint 62 s, a combined run timed out at 120 s); `Count`, `Take(25)`, `Contains`, `OverridePermanentColor(collection)` ≈ 0 ms | painter never enumerates: paints collections whole in sheet order (later wins natively), samples 25/set, expected colour = last painting set whose collection `Contains` the element |

## Results
| Check | Result |
|---|---|
| preview | 33 active sets (11 pending not built), counts: plumbing pipes 979 + 1 218 + 220 + 749 + 264 + 2 273 = 5 703 + fire 269 = 5 972 = all pipes; EP fittings 4 481; ELV 42 = EC 41 + EF 1; LV 92 |
| sets dryRun / apply | 33 created → rolled back / 33 created |
| sets re-run | 33 unchanged |
| paint dryRun | 13 982 painted, read-back 450/450, rolled back (410/450 samples back to model colour; 40 coincide: model already red fire / green cold water) |
| paint apply | 13 982 elements, sync + paint 26 s, read-back 450/450, 0 sampled elements taken by a later set |
| reset (dryRun) | 13 982 reset, control wall with a user magenta stays magenta |
| tests | BIMCoordinator 108, Server 76, Bridge 177 |
| published exe | tools/list 32 |

## Open
- 11 pending sets need project System Type abbreviations (raw water, hot-water return, kitchen/smoke/outside/pressure air, condenser, condensate, refrigerant).
- 13 HVAC/Dry/Drencher/HotWater sets unverified (no HVAC in THCSLT).
- `System Type` tab by display name assumes an English Navisworks UI.
- Production THCSLT still unsaved (no search sets, no colours) — apply + Save is the user's step.
