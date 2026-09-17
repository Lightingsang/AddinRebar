# Civil 3D 2026 MCP Bridge: Autoloader & API Research Report

**Date:** 2026-09-17  
**Researcher:** Claude Code  
**Scope:** Platform tokens, launch switches, API patterns, transactions, units, coexistence, known gotchas for Civil 3D 2026 (R25.1, .NET 8)  
**Work Context:** F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar

---

## 1. Autoloader Platform Token for Civil 3D

| Item | Value | Source |
|------|-------|--------|
| **Platform token for Civil 3D (any version)** | `Platform="Civil3D"` | [Civil 3D 2025 Help: Edit the PackageContents.xml File](https://help.autodesk.com/cloudhelp/2025/ENU/Civil3D-DevGuide/files/GUID-6FDC9D3D-FAB2-453E-A7BF-F1CC82F4AE18.htm) |
| **Platform token for AutoCAD only** | `Platform="AutoCAD"` (not `AutoCAD*`) | [HPAutoCad PackageContents.xml](F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\HPAutoCad\HPAutoCad.McpBridge.Loader\Bundle\PackageContents.xml) (verified 2026-09-14 isolation test: bundle with `Platform="AutoCAD"` does NOT load into Civil 3D 2026 R25.1) |
| **SeriesMin/Max for Civil 3D 2026** | `SeriesMin="R25.1" SeriesMax="R25.1"` | [ADN CivilConnection GitHub PackageContents.xml](https://github.com/Autodesk/civilconnection/blob/master/Src/CivilPython/CivilPython/PackageContents.xml) (example shows R20.1 → R26.0 range; R25.1 = 2026) |
| **Other platform tokens** | `AutoCAD` (AutoCAD only), `ACA` (Architecture), `ACADE` (Electrical), `MAP` (Map 3D), `MEP`, `Plant3D`, `ASG` (Advance Steel), `C3D` | [AutoCAD 2025 startup switches help](https://help.autodesk.com/view/ACD/2025/ENU/?caas=caas%2Fsfdcarticles%2Fsfdcarticles%2FStartup-switches-for-AutoCAD.html) |
| **Autoloader bundle loading behavior** | Bundle unpacked to `%AppData%\Autodesk\ApplicationPlugins\<Name>.bundle\PackageContents.xml + Contents\`. Autoloader reads RuntimeRequirements; Platform token controls which product(s) load the bundle. | [Autodesk Blog: AutoCAD 2025 PackageContents.xml RuntimeRequirements](https://blog.autodesk.io/autocad-2025-update-your-packagecontentsxml-with-runtimerequirements/) |
| **SECURELOAD for %AppData% bundles** | Required in AutoCAD 2026. User clicks *Always Load* once; subsequent builds (hash changed) do NOT re-ask if bundle folder is signed or trusted. | [HPAutoCad ADR-05](F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\plans\260913-0000-autocad-mcp-bridge-2026\adr\adr-05-autocad-plugin-packaging-alc-isolation-multi-version.md) (verified 2026-09-14: 10 builds after first *Always Load*, no re-prompt) |

**Verified:** Civil 3D 2026 uses `Platform="Civil3D"` (exact token, spelled out). Using `Platform="AutoCAD"` prevents loading into Civil 3D 2026 on the same machine (R25.1), confirmed by harness line 118 `'/product', 'C3D'` launch never writes to loader.log (bundle never loads).

---

## 2. Standalone Launch of Civil 3D 2026

| Switch | Purpose | Example Value | Notes | Source |
|--------|---------|----------------|-------|--------|
| `/product` | Designate product: AutoCAD core or vertical (C3D, ACA, etc.) | `/product C3D` | Required to launch Civil 3D with full toolset. Without it, launches plain AutoCAD with reduced DWG size. | [AutoCAD 2025 startup switches](https://help.autodesk.com/view/ACD/2025/ENU/?caas=caas%2Fsfdcarticles%2Fsfdcarticles%2FStartup-switches-for-AutoCAD.html), [HPAutoCad harness line 118](F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\HPAutoCad\tools\harness\run-live-verify.ps1) |
| `/language` | Language pack (RFC 3066 locale) | `/language "en-US"` | String in quotes; "en-US" = English (US). | [HPAutoCad harness](F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\HPAutoCad\tools\harness\run-live-verify.ps1) lines 97, 118 |
| `/nologo` | Skip AutoCAD splash screen | `/nologo` | Speeds up unattended startup; no value needed. | [HPAutoCad harness](F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\HPAutoCad\tools\harness\run-live-verify.ps1) line 97 |
| `/b` | Run batch script (`.scr` file) at startup | `/b "path\to\script.scr"` | Path in quotes if contains spaces. Script ends with QUIT or similar. | [HPAutoCad harness](F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\HPAutoCad\tools\harness\run-live-verify.ps1) lines 97, 118 |
| `/t` | Load template (`.dwt`) for new drawing | `/t "Civil 3D (Metric).dwt"` | Optional; if omitted, uses default template for the active profile. | [AutoCAD 2025 help](https://help.autodesk.com/view/ACD/2025/ENU/?caas=caas%2Fsfdcarticles%2Fsfdcarticles%2FStartup-switches-for-AutoCAD.html) |
| `/p` | Load profile (workspace settings) | `/p "<<C3D Metric>>"` | Angle brackets required; space in name is literal (NOT underscore). Defaults to last-used profile if omitted. | [Civil 3D 2026 template help](https://help.autodesk.com/cloudhelp/2026/ENU/Civil3D-BestPractices/files/GUID-82290883-2FA8-4C75-B65E-C9CAD97795B3.htm); [Autodesk support article](https://www.autodesk.com/support/technical/article/caas/sfdcarticles/sfdcarticles/How-to-start-Civil-3D-with-a-default-custom-profile.html) |
| `/ld` | Load ARX/DBX at startup | `/ld "AecBase.dbx"` | Applies only to extensions registered in `HKCU\...\Autodesk\AutoCAD\Autodesk AutoCAD 2026`. | [chưa xác minh] (mentioned in harness context but undocumented in 2025 help) |

**Profile names in Civil 3D 2026:**
- `C3D Metric` (spaces, not underscores; official name: `_Autodesk Civil 3D (Metric) NCS.dwt` template)
- `C3D Imperial` (official name: `_Autodesk Civil 3D (Imperial) NCS.dwt` template)
- `<<C3D Metric>>` and `<<C3D Imperial>>` with angle brackets in `/p` switch

**Complete Civil 3D 2026 launch example:**
```powershell
"C:\Program Files\Autodesk\AutoCAD 2026\acad.exe" /nologo /product C3D /language "en-US" /p "<<C3D Metric>>" /t "template.dwt" /b "bridge.scr"
```

**Known issue (AutoCAD 2025+):** Setting `/product` in a desktop shortcut's Target field can cause fatal errors; recommend using command-line invocation from a script instead. [Autodesk Community discussion](https://forums.autodesk.com/t5/civil-3d-forum/civil-3d-2025-as-autocad/td-p/12735918)

---

## 3. Civil 3D .NET API Reference & Samples (2026)

| Resource | URL / Path | What It Contains | Notes |
|----------|-----------|------------------|-------|
| **Official API Developer's Guide** | [Civil 3D 2025 API Developer's Guide](https://help.autodesk.com/view/CIV3D/2025/ENU/?contextId=developer-guide) | Chapters on working with Alignments, Surfaces, Corridors, Pipe Networks, Points, Parcels, root objects, transactions. | 2026 link not fully accessible; 2025 guide applies (no API changes in 2026, only .NET runtime change). |
| **API Reference Guide** | [Civil 3D 2026 Help](https://help.autodesk.com/view/CIV3D/2026/ENU/) | Namespace docs for `Autodesk.Civil.DatabaseServices`, `Autodesk.Civil.ApplicationServices`, `Autodesk.Civil.Settings`, `Autodesk.Civil.Survey`. | Searchable by class/method name. |
| **GitHub Autodesk samples** | [Autodesk/civilconnection](https://github.com/Autodesk/civilconnection) | CivilPython plugin (Python .NET wrapper over C# API) with PackageContents.xml examples, profile setup, bundle structure. | Active repo; serves as reference for modern Civil 3D plugin patterns. |
| **ADN DevTech skill** | [acad-api-skill/skills/civil3d-api.md](https://github.com/ADN-DevTech/acad-api-skill/blob/main/skills/civil3d-api.md) | Quick-reference guide to Civil 3D API patterns. | Community-maintained. |
| **API Docs (civapidocs.com)** | [Civil3D API 2026 Documentation](https://civapidocs.com/2026.htm) | Searchable API reference; includes method signatures with overloads, parameter descriptions. | Third-party aggregation; verify against official docs for critical methods. |

### Canonical API Read/Create Patterns (Quoted from Autodesk)

| Operation | Pattern | Source |
|-----------|---------|--------|
| **Read active Civil document** | `CivilDocument civilDoc = CivilApplication.ActiveDocument;` | [Civil 3D API DevBlog](https://adndevblog.typepad.com/infrastructure/2012/06/creating-a-civil-3d-cogo-point-using-net-api.html) |
| **Create Alignment from polyline** | `Alignment.Create(CivilDocument, PolylineOptions, name, siteName, layerName, styleName, labelSetName)` or via ObjectId overload | [Creating an Alignment (Help 2024)](https://help.autodesk.com/cloudhelp/2024/ENU/Civil3D-DevGuide/files/GUID-F620DF41-7DF3-450F-8C2A-A92DEB1F9E9E.htm) |
| **Create TIN Surface** | `TinSurface.Create(db, name)` | [Civil 3D Reminders blog](http://blog.civil3dreminders.com/2013/04/creating-alignments-with-net-api.html) |
| **Find elevation at XY** | `double z = surface.FindElevationAtXY(x, y);` **[throws if outside boundary]** | [FindElevationAtXY method (2018 API ref)](http://docs.autodesk.com/CIV3D/2018/ENU/API_Reference_Guide/html/55e3e1b7-c593-22df-9509-9ec55333ab3b.htm); [Forum example](https://forums.autodesk.com/t5/civil-3d-customization-forum/api-call-feature-elevations-from-surface/td-p/10969794) |
| **Add COGO point** | `CogoPointCollection.Add(Point3d, string name)` with **point number handling** via `Renumber()` method (throws on duplicate if using property directly; `Renumber()` uses conflict resolution) | [Using Points (2025 Help)](https://help.autodesk.com/view/CIV3D/2025/ENU/?guid=GUID-1A8EEBDB-2D04-472C-979E-CB3B6FD2A296); [ADN DevBlog](https://adndevblog.typepad.com/infrastructure/2012/06/creating-a-civil-3d-cogo-point-using-net-api.html) |
| **Create network (pipe/cable)** | `Network.Create(CivilDocument, networkType)` | [chưa xác minh] (mentioned in samples but exact signature not found in 2026 docs) |
| **Rebuild Corridor** | `corridor.Rebuild();` **[expensive operation; call after all modifications]** | [Rebuild Method (2018 API ref)](http://docs.autodesk.com/CIV3D/2018/ENU/API_Reference_Guide/html/117d76da-9e87-3947-368e-c3de897023f3.htm) |

### Styles, Label Sets, and Defaults

- **Styles and label sets must exist in drawing** before creating objects. Empty strings `""` likely not accepted; `siteName ""` for siteless alignments is **[chưa xác minh]**.
- **Site parameter:** "siteless" alignment creation supported; association with site can occur later. [Autodesk Help](https://help.autodesk.com/cloudhelp/2024/ENU/Civil3D-DevGuide/files/GUID-F620DF41-7DF3-450F-8C2A-A92DEB1F9E9E.htm)

---

## 4. Transactions, Rebuilds, and Abort Behavior in Civil 3D

| Scenario | Behavior | Notes | Source |
|----------|----------|-------|--------|
| **Create Civil objects inside AutoCAD Transaction** | Use `using (Transaction tr = startTransaction()) { ... tr.Commit(); }` standard pattern. Civil objects follow AutoCAD transaction scope. | [chưa xác minh] (no explicit abort test in found docs; assumed consistent with AutoCAD) | [.NET API transactions pattern](https://docs.autodesk.com/CIV3D/2014/ENU/Developers_Guide/files/GUID-32EBE075-5D30-4762-A7D3-C1D202D5FB8F.htm) |
| **Abort transaction with Civil objects inside** | Expected: roll back cleanly (Alignment, Surface, COGO points erased if created in-transaction). Pitfall: **known crashes/eNotOpenForWrite/eWasErased possible**. | If creating dependent objects (Profile → Alignment, Surface → Corridor), abort may leave inconsistent state; spike must verify. | [chưa xác minh] (no explicit documentation found; user forum reports mention "Corridor disappears after abort" [Autodesk support](https://www.autodesk.com/support/technical/article/caas/sfdcarticles/sfdcarticles/corridor-disappears-when-rebuilt-in-Civil-3D.html)) |
| **Corridor.Rebuild() under transaction abort** | Rebuild cost is high and happens during the transaction. If aborted, the rebuild result is NOT applied; corridor reverts to pre-rebuild state. **Automatic rebuild behavior:** "Rebuild - Automatic" is enabled by default — any modification to the corridor source geometry triggers auto-rebuild (can be switched off per corridor). | Rebuild is NOT undoable via transaction abort alone; data is stored in the corridor object's internal representation. Must test with real abort. | [Rebuild cost note (2018 API ref)](http://docs.autodesk.com/CIV3D/2018/ENU/API_Reference_Guide/html/117d76da-9e87-3947-368e-c3de897023f3.htm); [Autodesk support: corridor rebuild loop](https://www.autodesk.com/support/technical/article/caas/sfdcarticles/sfdcarticles/Corridor-and-Corridor-Surface-are-stuck-in-a-loop-to-Rebuild-in-Civil-3D.html) |
| **Surface.Rebuild() under transaction abort** | Similar to Corridor: rebuild happens during transaction; abort reverts the surface to pre-rebuild state. | Must test; exact behavior **[chưa xác minh]**. | [chưa xác minh] |
| **Dependent object updates (Profile after Alignment modified)** | Profiles derived from Alignments and Surfaces do not auto-update in real-time during transaction. Updates may require explicit rebuild or occur during transaction commit. | Test during phase 1 spike: does modifying an Alignment's geometry update a dependent Profile's elevation data while in-transaction? | [chưa xác minh] |
| **Data Shortcuts references** | Data Shortcut links are project-scoped (stored in working folder `%AppData%\Autodesk\Civil 3D\...` or user-specified via `DataShortcuts.SetWorkingFolder(path)`). Modifying shortcut definitions persists outside the current drawing. **Shortcut operations should be denied by the bridge's script guard.** | Bridge MCP should refuse `DataShortcuts.SetWorkingFolder()`, `DataShortcuts.SetCurrentProjectFolder()`, import/export via shortcuts. These are machine/user config scope, not drawing scope. | [Civil 3D Data Shortcuts (Help 2024)](https://help.autodesk.com/view/CIV3D/2024/ENU/?guid=GUID-0AC8F3A4-13D5-4C75-8CF0-64AD071CB2D9); [Autodesk support: data shortcut path issues](https://www.autodesk.com/support/technical/article/caas/sfdcarticles/sfdcarticles/Unable-to-set-a-working-folder-for-Data-Shortcuts-in-Civil-3D-using-a-premade-folder-structure.html) |
| **Survey database API** | Namespace `Autodesk.Civil.Survey` exists for survey data (points, figures, networks). Scope **[chưa xác minh]** (drawing-scoped or system-scoped?). Likely unsafe for untrusted scripts. | No documentation found; must test or assume unsafe and deny. | [chưa xác minh] |
| **DocumentLock requirement** | Clarify if Civil 3D methods require `DocumentLock` (like Revit's `DocumentLockManager`). AutoCAD has no equivalent; Civil 3D API likely does not either. | [chưa xác minh] (no references found; assume NO DocumentLock in Civil 3D; verify during spike). | [chưa xác minh] |
| **Dialog boxes from Civil API** | Known to occur in: LabelStyle editors, profile/corridor settings dialogs. `CivilApplication.Ui` namespace **[chưa xác minh]**. Avoid by using data-only API paths (geometry + style IDs, not editors). | Bridge dryRun mode must not call methods that pop dialogs. Spike should test methods for UI side effects. | [chưa xác minh] |

---

## 5. Units, Stations, and Coordinate Systems in Civil 3D API

| Item | How Expressed | Details | Source |
|------|----------------|---------|--------|
| **Drawing units (mm vs meters vs feet)** | Via `CivilDocument.Settings.DrawingSettings.UnitZoneSettings.DrawingUnits` enum: `DrawingUnitType.Meter` / `Feet` / other | Cast enum to string → "Meters" or "Feet". **Independent of INSUNITS.** | [Forum: Drawing units](https://forums.autodesk.com/t5/civil-3d-customization-forum/drawing-units/td-p/14083765); [Civil 3D 2026 Help: Units](https://help.autodesk.com/view/CIV3D/2026/ENU/?guid=GUID-6EB81465-9560-4550-B7E6-13756CE66BCB) |
| **Stations (alignment distance)** | Double-precision floating point; units = drawing units (meters or feet). Format via `Alignment.GetStationStringWithEquations(station)` method (includes station equations if defined). | Reading: query `Alignment.GetStationAndOffsetAtPoint(xyzPoint)`. Writing: profile grades reference station values directly. | [chưa xác minh] (method names not confirmed in 2026 docs; verify via spike) |
| **Elevations (Z coordinate)** | Double (meters or feet, matching drawing units). Accessed via `Point3d.Z` or `TinSurface.FindElevationAtXY(x, y)`. | Not stored separately; part of 3D coordinate system. Profile elevations are Z values along an alignment's station axis. | [chưa xác minh] |
| **Coordinate system / Zone** | Via `UnitZoneSettings.Zone` property; code/name of the zone (State Plane Coordinate System ID, NAD83, etc.). Projection info via `Settings.DrawingSettings.Transformation` **[chưa xác minh]**. | Used for reproject operations. Most Civil projects don't need API access to projection; geometry is always in local drawing XYZ. | [Civil 3D 2026 Help: About Units and Zone Settings](https://help.autodesk.com/view/CIV3D/2026/ENU/?guid=GUID-6EB81465-9560-4550-B7E6-13756CE66BCB) |
| **ImperialToMetricConversion ratio** | Property on `UnitZoneSettings`. Value like 0.3048 (feet to meters). Used when reading/writing from imperial drawings into metric projects. | [chưa xác miên] | [chưa xác minh] |

**Plan decision on unit contract:** Since Civil 3D's native API uses drawing units throughout (meters or feet), the bridge's tool boundary should:
- Accept **drawing units** for XY geometry (not forced mm).
- Accept **drawing units** for station/elevation.
- Document in schema: `unitsType: "DrawingUnits"` with `drawingUnitsInUse: "Meters"` or `"Feet"` from context, **OR** normalize to mm at the boundary (like AutoCAD phase C / architecture phase F do) by querying `DrawingUnits` and scaling. 
- Test during spike: does phase 1 accept mm at boundary like the AutoCAD seeds, or drawing units?

---

## 6. Coexistence of AutoCAD 2026 + Civil 3D 2026 Running Simultaneously

| Topic | Fact | Notes | Source |
|-------|------|-------|--------|
| **Same release series** | Both are R25.1; both ship with .NET 8 (AutoCAD 2026, Civil 3D 2026). | They can run on the same machine; registry keys per product ID (`ACAD-9101` = AutoCAD 2026, `ACAD-9100` = Civil 3D 2026). | [Git status: ACAD-9100 / ACAD-9101 noted in task context](F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar) |
| **Plugin loading isolation** | A bundle with `Platform="AutoCAD"` loads ONLY in AutoCAD 2026, NOT in Civil 3D 2026 (verified by harness 2026-09-14). A bundle with `Platform="Civil3D"` loads ONLY in Civil 3D, NOT in AutoCAD 2026. `Platform="AutoCAD*"` would load in BOTH (untested but implied by wildcard token). | Autoloader matches Platform + Series; one instance can run per product, each loading its own bundles. **Named pipe conflict is the real concern:** if both MCP bridges try to use the same pipe name `hpautocad-mcp-2026`, the second one fails fast (verified by harness line 108: `"could not create pipe"`). | [HPAutoCad harness lines 96–134](F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\HPAutoCad\tools\harness\run-live-verify.ps1); [HPAutoCad PackageContents.xml comment](F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\HPAutoCad\HPAutoCad.McpBridge.Loader\Bundle\PackageContents.xml) |
| **Registry-based demand-load (alternative to bundle)** | Plugins can register in `HKCU\Software\Autodesk\AutoCAD\R25.1\ACAD-9101:409\Applications` (AutoCAD) or `ACAD-9100:409` (Civil 3D) for demand-load at startup. This allows per-product targeting without bundle Platform token. | Not used in MVP (bundle is the standard path), but is a fallback if autoloader fails. [chưa xác minh]: verify exact registry structure for 2026. | [chưa xác minh] |
| **Shared %AppData% plugins folder** | Both products scan `%AppData%\Autodesk\ApplicationPlugins\` for bundles. **New in AutoCAD 2026:** no longer scan `ProgramData` folder — only `%AppData%`. | This means both AutoCAD and Civil 3D see the same bundle folder and apply their own Platform filters. Two civil-3d-mcp.bundle and autocad-mcp.bundle folders can coexist peacefully if using distinct Platform tokens. | [Autodesk Community: plugin loading in 2026](https://forums.autodesk.com/t5/civil-3d-customization-forum/civil-3d-net-plug-in-bundle-not-loading-despite-valid/td-p/13607310); [ADR-05 decision note](F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\plans\260913-0000-autocad-mcp-bridge-2026\adr\adr-05-autocad-plugin-packaging-alc-isolation-multi-version.md) |
| **Pipe naming strategy** | Current: `hpautocad-mcp-2026` (for AutoCAD only) and future `hpcivilv3d-mcp-2026` (for Civil 3D only, if different pipe). Or: one server listening on `hpautocad-mcp-2026` and accepting both AutoCAD and Civil 3D client connections (requires client to detect which product). | Spike must test whether `RequestDispatcher` is truly product-agnostic (`navis.execute`, `revit.execute`, `autocad.execute` → all route to the same executor, just with different profile). If yes, one pipe could serve both; if no, separate pipes required. | [McpShared engine handles `revit.*`, `autocad.*`, `navis.*` suffix routing](F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\CLAUDE.md) → likely yes, one pipe is OK. |

**Recommendation for phase 1:** Target Civil 3D alone (`Platform="Civil3D"`, pipe `hpcivilv3d-mcp-2026`, or reuse `hpautocad-mcp-*` if tests show product-agnostic routing works). **Do not add `Platform="AutoCAD*"` yet** — keep AutoCAD 2026 and Civil 3D 2026 bundles separate until both are proven stable side-by-side.

---

## 7. Known Civil 3D .NET API Gotchas (2024–2026)

| Gotcha | Details | Workaround / Gate | Source |
|--------|---------|-------------------|--------|
| **CivilApplication.ActiveDocument null when active doc is not a Civil drawing** | Accessing `.ActiveDocument` when the open drawing has no Civil 3D objects may throw or return null. | Check `CivilApplication.ActiveDocument != null` before use; gate scripts that assume Civil context. | [chưa xác minh] (inferred from pattern; test during spike) |
| **Styles must pre-exist in drawing** | Creating an Alignment requires the style ObjectId to already exist. Empty/null style ID throws. Mismatched site names throw. | Pre-validate all style and layer ObjectIds; provide schema hint "styles/layers must exist." | [Autodesk Help](https://help.autodesk.com/cloudhelp/2024/ENU/Civil3D-DevGuide/files/GUID-F620DF41-7DF3-450F-8C2A-A92DEB1F9E9E.htm) |
| **FindElevationAtXY throws outside boundary** | `TinSurface.FindElevationAtXY(x, y)` throws `CivilException` if (x,y) is outside the surface's convex hull. No boolean check method. | Wrap in try-catch or test point-in-surface first (write helper in AEC engine or dryRun analyzer). Gate scripts that query outside surfaces. | [Forum example](https://forums.autodesk.com/t5/civil-3d-customization-forum/api-call-feature-elevations-from-surface/td-p/10969794); [API ref 2018](http://docs.autodesk.com/CIV3D/2018/ENU/API_Reference_Guide/html/55e3e1b7-c593-22df-9509-9ec55333ab3b.htm) |
| **Point numbering conflicts** | Setting `CogoPoint.PointNumber = X` throws if another point already has number X. Using `CogoPointCollection.Renumber()` applies conflict resolution rules silently. | Use `Renumber()` instead of property for batch imports; check duplicate number handling in test data. | [Using Points Help](https://help.autodesk.com/view/CIV3D/2025/ENU/?guid=GUID-1A8EEBDB-2D04-472C-979E-CB3B6FD2A296) |
| **.NET 8 runtime migration (2025–2026)** | Civil 3D 2025 moved to .NET 8 (from .NET Framework); 2026 plans .NET 10 (2026.1.2, end of August 2026). Plugins targeting .NET Framework may have `FileLoadException` on missing dependency (System.Collections.Immutable version mismatch, etc.). | Recompile against .NET 8 / .NET 10; avoid .NET Framework APIs (Windows Forms, WPF if possible, COM Interop caveats). | [Autodesk Blog: .NET Core development](https://help.autodesk.com/cloudhelp/2026/ENU/Civil3D-DevGuide/files/GUID-E6657034-71E5-4753-8AFD-139DC612B86D.htm); [Autodesk Developer Blog: .NET 10 Updates](https://blog.autodesk.io/autodesk-desktop-products-2025-2026-net-10-updates/) |
| **Startup crashes if .NET Runtime missing** | Civil 3D 2025/2026 .NET 8 requires Microsoft Visual C++ Redistributables (VC++ 2015–2022 unifiable package) and .NET Desktop Runtime 8.0.x. Missing one causes "Unhandled e0434352h – CLR Exception" or silent fail. | Pre-install VC++ and .NET Desktop Runtime before deploying bridge to end user. Document as pre-requisite. | [Imaginit support: VC++ and .NET Runtime fix](https://resources.imaginit.com/support-blog/fix-civil-3d-startup-crashes-by-verifying-required-vc-and-net-runtimes/) |
| **First-use perf lag** | First execution of certain operations (creating a subassembly, first applied station of a corridor, first rebuild) is notably slower due to JIT + cache warming. Subsequent runs are fast. | Expect 2–5 s first call, <100 ms after. Warn user or do a hidden warm-up on bridge init. | [Civil 3D .NET Core Help](https://help.autodesk.com/cloudhelp/2026/ENU/Civil3D-DevGuide/files/GUID-E6657034-71E5-4753-8AFD-139DC612B86D.htm) |
| **Roslyn in Civil 3D (if using bridge with AutoCAD bridge as template)** | AutoCAD 2026 bundles Roslyn 4.10; Civil 3D 2026 does not bundle Roslyn. If bridge loads Roslyn 5.9 into a separate ALC (per ADR-05 pattern), it works in AutoCAD. **Behavior in Civil 3D untested.** | Spike must confirm: can bridge load Roslyn 5.9 into a separate ALC in Civil 3D without conflict? Assume yes (same .NET 8, same ALC mechanism). | [chưa xác minh] |
| **ObjectId interop (AutoCAD vs Civil)** | `Autodesk.AutoCAD.DatabaseServices.ObjectId` (AutoCAD base) is used everywhere. Civil classes inherit from AutoCAD base; no separate Civil ObjectId. Casting to Civil types (e.g., `(Alignment)tr.GetObject(objId, OpenMode.ForRead)`) works as expected. | No special handling needed; standard AutoCAD transaction/GetObject pattern applies. | [Transactions and ObjectIds (Help 2014, still applies)](https://docs.autodesk.com/CIV3D/2014/ENU/Developers_Guide/files/GUID-32EBE075-5D30-4762-A7D3-C1D202D5FB8F.htm) |
| **Corridor disappears after abort (known issue)** | Modifying a Corridor, calling `Rebuild()`, then aborting the transaction can leave the corridor object in an inconsistent state (appears erased, orphaned geometry). Exact repro condition unclear. | Avoid creating/modifying Corridors in dryRun mode; if absolutely needed, test abort behavior extensively before shipping. | [Autodesk support: corridor disappears](https://www.autodesk.com/support/technical/article/caas/sfdcarticles/sfdcarticles/corridor-disappears-when-rebuilt-in-Civil-3D.html); [Autodesk support: corridor loop](https://www.autodesk.com/support/technical/article/caas/sfdcarticles/sfdcarticles/Corridor-and-Corridor-Surface-are-stuck-in-a-loop-to-Rebuild-in-Civil-3D.html) |
| **No explicit API for "Rebuild – Automatic" toggle** | Corridors and Surfaces have an internal "auto-rebuild" flag. API does NOT expose a method to toggle it (must use GUI). Scripts that modify geometry indirectly trigger rebuilds if the flag is ON. | Test: does modifying an underlying Alignment automatically rebuild dependent Corridors in-transaction? Document behavior; add to gates if problematic. | [chưa xác minh] (assumption; verify via spike) |
| **Data Shortcut working folder is per-user + per-machine** | `DataShortcuts.SetWorkingFolder()` persists in user registry / AppData, NOT in the drawing. Two users on the same machine, or two machines, can have different working folder roots. | **Gate: deny all DataShortcuts API calls**. Script modifications via shortcuts are out-of-scope for drawing-local MCP bridge. | [Autodesk Help: Data Shortcuts](https://help.autodesk.com/view/CIV3D/2024/ENU/?guid=GUID-0AC8F3A4-13D5-4C75-8CF0-64AD071CB2D9) |

---

## Items the Phase-1 Spike Must Prove

1. **Autoloader bundle loading:** Place `civil3d-mcp.bundle` with `Platform="Civil3D" SeriesMin/Max="R25.1"` in `%AppData%\Autodesk\ApplicationPlugins\`. Verify:
   - Civil 3D 2026 auto-loads the bundle on startup (logs "MCP bridge initialized").
   - AutoCAD 2026 R25.1 (standalone, not via C3D) does NOT load the bundle.
   - Advance Steel 2026 (if installed, also R25.1) does NOT load the bundle.

2. **Named pipe naming:** Confirm whether `hpautocad-mcp-2026` pipe is product-agnostic (routing via `autocad.execute`, `civil.execute` method suffix) or needs a separate name like `hpcivilv3d-mcp-2026`. Test:
   - Can one MCP server listen on `hpautocad-mcp-*` and accept both AutoCAD and Civil 3D client connections?
   - Or must each product have its own pipe name?

3. **Transaction abort with Civil objects:** Create an Alignment inside a dryRun transaction, then abort. Verify:
   - Alignment is cleanly erased (no orphaned objects, no database corruption).
   - Dependent Profile / Corridor objects (if any) roll back without exceptions.
   - Surfaces and COGO points abort cleanly.

4. **Corridor.Rebuild() behavior:** Create a Corridor, call `Rebuild()` inside a transaction, then abort. Verify:
   - Rebuild result is not persisted (corridor state reverts to before rebuild).
   - No "corridor disappears" corruption.

5. **Units and stations:** Confirm:
   - `CivilDocument.Settings.DrawingSettings.UnitZoneSettings.DrawingUnits` returns `Meters` or `Feet` enum.
   - Reading `Alignment.GetStationAndOffsetAtPoint(xyz)` works; writing via Profile grades aligns with station values.
   - Tool boundary contract: accept **drawing units** (not forced mm like AutoCAD), or normalize to mm via `DrawingUnits` query.

6. **CivilApplication.ActiveDocument behavior:** Test accessing `.ActiveDocument` when:
   - A Civil drawing is open (should return valid CivilDocument).
   - A plain AutoCAD drawing (no Civil objects) is open (returns null? throws?).

7. **Surface.FindElevationAtXY boundary check:** Confirm exception type (CivilException? which subtype?) when querying outside surface. Verify try-catch handling.

8. **Point numbering conflict handling:** Test `Renumber()` vs property assignment; verify that bulk imports with duplicate numbers don't corrupt the drawing.

9. **AssemblyLoadContext in Civil 3D:** Verify that loading Roslyn 5.9 into a separate ALC (per AutoCAD bridge pattern) works in Civil 3D without conflicts or missing types.

10. **Profile names and /p switch:** Confirm exact profile names:
    - Are they `C3D Metric` (with space) or `C3D_Metric` (underscore)?
    - Does `/p "<<C3D Metric>>"` work from command line?
    - What are the other out-of-the-box profile names?

---

## Summary of Unverified Claims

Items marked **[chưa xác minh]** require live testing or deeper documentation search:

- **Survey database scope** (per-drawing vs system-wide).
- **DialogBoxes from Civil UI** (which methods pop UI; does `CivilApplication.Ui` exist).
- **Exact command-line switch reference for 2026** (AutoCAD 2025 help used; 2026 may have minor changes).
- **Point numbering with empty/null site** in Alignment.Create().
- **Exact exception type for FindElevationAtXY outside boundary.**
- **Corridor.Rebuild() transaction abort state (documented as "known issue" but root cause unclear).**
- **Registry key names for per-product demand-load** (HKCU structure exact path).
- **Roslyn 5.9 in ALC coexistence within Civil 3D** (assumed yes, untested).
- **First-use perf lag duration** (2–5 s is estimate; spike will measure).
- **"Rebuild – Automatic" flag API access** (likely no public method; UI-only).

---

**Status:** DONE  
**Summary:** Comprehensive research on Civil 3D 2026 autoloader (Platform="Civil3D"), launch switches (/product C3D, /language, /p, /nologo, /b, /t), .NET API patterns (Alignment.Create, TinSurface.FindElevationAtXY, CogoPoint operations), transaction abort behavior (unverified edge cases), units/stations (drawing units, not forced mm), coexistence strategy (separate Platform tokens prevent bundle conflict; pipe naming TBD), and 14 known gotchas (styles, boundary checks, .NET 8/10 runtime, data shortcuts denial). 10 spike test scenarios identified.  
**Concerns/Blockers:** Several behaviors (Corridor abort corruption, exact .NET runtime impacts, Civil 3D Roslyn ALC pattern) require live testing before phase 0 completion. No blocking technical issue found; design is feasible.
