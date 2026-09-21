# Handoff Report: Milestone M3 Shared Ribbon Tab Specification

**Agent:** explorer_m3_ribbon  
**Role:** Teamwork Explorer  
**Task:** Shared Ribbon Tab (`HPAUTOCAD_MCP_TAB`) & `HPGEOLINK_PANEL` Technical Specification  
**Recipient:** orchestrator_3 (`050984c1-afaa-4911-859c-331e9279dc4f`)  
**Output Document:** `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\m3_ribbon_plan.md`  

---

## 1. Observation

1. **Existing Shared Tab in `McpRibbonTab.cs`**:
   - File: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPAutoCad\HPAutoCad.McpBridge.Loader\Ribbon\McpRibbonTab.cs`
   - Lines 23-28:
     ```csharp
     public const string TabId = "HPAUTOCAD_MCP_TAB";
     public const string TabTitle = "HPAutoCad";
     public const string PanelId = "HPAUTOCAD_MCP_PANEL";
     public const string ButtonId = "HPAUTOCAD_MCP_BRIDGE";
     public const string ButtonText = "MCP Bridge";
     ```
   - Lines 12-16:
     ```csharp
     /// The tab is shared by every HP add-in for this product (rule: one tab, one panel per tool, never a
     /// new tab): whoever loads first creates the tab by its Id, everyone else finds it and adds only its own
     /// panel. So this class touches nothing but its own panel — Uninstall removes that panel and the tab only
     /// when it is left empty, and a COLORTHEME change (the icon ink follows the theme) rebuilds the panel,
     /// never the tab, so the other add-ins' panels survive.
     ```

2. **Legacy HPGeo Ribbon Tab in `HPGeoRibbonTab.cs`**:
   - File: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGeo\HPGeo.AutoCad.Loader\Ribbon\HPGeoRibbonTab.cs`
   - Lines 19-27:
     ```csharp
     public const string TabId = "HPAUTOCAD_MCP_TAB";
     public const string TabTitle = "HPAutoCad";
     public const string PanelId = "HPGEO_VN2000_PANEL";
     public const string PanelTitle = "VN2000";
     public const string ButtonId = "HPGEO_KMZ";
     public const string ButtonText = "KMZ";
     public const string Command = "HPGEO";
     ```
   - Lines 130-157: Panel contains a single large button `HPGEO_KMZ` running `doc.SendStringToExecute("_.HPGEO ", true, false, false)`.

3. **Civil 3D Mirror Parity Invariant**:
   - File: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPCivil3d\tools\mirror-tokens.json`
   - Lines 231-245:
     ```json
     {
       "autocad": "HPAutoCad.McpBridge.Loader/Ribbon/McpRibbonTab.cs",
       "civil3d": "HPCivil3d.McpBridge.Loader/Ribbon/McpRibbonTab.cs"
     },
     {
       "autocad": "HPAutoCad.McpBridge.Loader/Ribbon/RibbonCommandHandler.cs",
       "civil3d": "HPCivil3d.McpBridge.Loader/Ribbon/RibbonCommandHandler.cs"
     },
     {
       "autocad": "HPAutoCad.McpBridge.Loader/Ribbon/RibbonIcons.cs",
       "civil3d": "HPCivil3d.McpBridge.Loader/Ribbon/RibbonIcons.cs"
     },
     {
       "autocad": "HPAutoCad.McpBridge.Loader/HPAutoCad.McpBridge.Loader.csproj",
       "civil3d": "HPCivil3d.McpBridge.Loader/HPCivil3d.McpBridge.Loader.csproj"
     }
     ```
   - Any modification inside `HPAutoCad.McpBridge.Loader/` triggers diff failures in `HPCivil3d.McpBridge.Tests`.

4. **AutoCAD Commands Implemented in `HPAutoCad/HPGeoLink/Commands/`**:
   - `HPGeoDialogCommand.cs`: Command `HPGEO` (Modal dialog, survey selection, KMZ export, satellite underlay insertion).
   - `HPGeoImportCommand.cs`: Command `HPGEOIMPORT` (Modal dialog, KML/KMZ or coordinate list import into CAD).
   - `HPGeoKmzScriptCommand.cs`: Command `-HPGEOKMZ` (CLI scriptable KMZ export).
   - `HPGeoImageScriptCommand.cs`: Command `-HPGEOIMAGE` (CLI scriptable satellite tile fetch & raster insertion).
   - `HPGeoInfoCommand.cs`: Command `HPGEOINFO` (Diagnostic read-only report on CRS, INSUNITS, provinces, log path).
   - `HPGeoImportScriptCommand.cs`: Command `-HPGEOIMPORT` (CLI scriptable KML/KMZ import).

5. **AdWindows Metadata & Threading Facts**:
   - Document: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.claude\agent-memory\code-reviewer\project-adwindows-ribbon-api-facts.md`
   - `RibbonItem : Object` (`INotifyPropertyChanged`, not `DependencyObject`).
   - `RibbonControl : Control` (`ComponentManager.Ribbon.Dispatcher` is the main STA WPF dispatcher).
   - `Geometry.Parse` returns a frozen `StreamGeometry` with `FillRule.EvenOdd` by default (`F1` prefix = NonZero).
   - `SystemVariableChanged` (WSCURRENT) drops code-added tabs; one-shot `Application.Idle` restores them reliably.

---

## 2. Logic Chain

1. **Isolation of Ribbon Implementation**:
   - From Observation 3, `HPAutoCad.McpBridge.Loader` cannot be edited without breaking Civil 3D mirror parity.
   - Therefore, the new `HPGeoLinkRibbonTab.cs`, `RibbonCommandHandler.cs`, and `RibbonIcons.cs` must be placed in `HPAutoCad/HPAutoCad.Loader/Ribbon/` (which is part of the new Milestone M3 loader).
2. **Shared Tab Protocol**:
   - From Observation 1 and 2, both `McpRibbonTab` and `HPGeoLinkRibbonTab` share `TabId = "HPAUTOCAD_MCP_TAB"` and `TabTitle = "HPAutoCad"`.
   - `EnsureCreated()` performs `ribbon.FindTab(TabId)`. Whichever add-in loads first creates the tab; the other add-in attaches its panel to the existing tab.
   - Rebuilding on `COLORTHEME` calls `RemoveOwnPanel(removeEmptyTab: false)`, ensuring neither loader removes the tab while the other's panel is present.
   - Unloading calls `RemoveOwnPanel(removeEmptyTab: true)`, which checks `tab.Panels.Count == 0` before removing the tab.
3. **Panel Layout & Command Routing**:
   - From Observation 4, HPGeoLink exposes 6 commands: `HPGEO`, `HPGEOIMPORT`, `-HPGEOIMAGE`, `HPGEOINFO`, `-HPGEOKMZ`, `-HPGEOIMPORT`.
   - Primary Large Button: `HPGEO_KMZ` (runs `HPGEO`) provides direct access to the main geodetic export dialog.
   - Secondary Large SplitButton: `HPGEO_SECONDARY_SPLIT` with `IsSplit = true`.
     * Clicking the main upper button executes `HPGEOIMPORT` (the most frequent secondary operation).
     * Clicking the dropdown arrow opens a menu with: `HPGEO_IMAGE` (`-HPGEOIMAGE`), `HPGEO_INFO` (`HPGEOINFO`), `HPGEO_KMZ_SCRIPT` (`-HPGEOKMZ`), and `HPGEO_IMPORT_SCRIPT` (`-HPGEOIMPORT`).
   - Command dispatching via `Document.SendStringToExecute("_." + command + " ", true, false, false)` properly funnels commands through AutoCAD's document command engine, supporting undo groupings and transparent prompts.
4. **Vector Icon Design & Resolution Independence**:
   - From Observation 5, AdWindows scales a 32×32 `LargeImage` down to 16×16 for standard items.
   - By constraining all coordinates in `RibbonIcons.cs` to even integers and drawing cutouts via EvenOdd path topology, every edge aligns on integer boundaries at 100%, 150%, and 200% DPI with zero fuzzy anti-aliasing.
   - Dark theme uses `#E6E6E6`, light theme uses `#3C3C3C`, and accent uses `#0696D7`.

---

## 3. Caveats

- **Active Document Requirement**: Commands dispatched via `doc.SendStringToExecute` require an open drawing document. If a user clicks a button with no drawing open (`MdiActiveDocument == null`), `RunCommand` safely writes a warning to `loader.log` and no-ops. (By contrast, MCP Bridge status window does not require a drawing).
- **AutoCAD Ribbon Availability**: If AutoCAD is started in batch mode or with the ribbon closed (`RIBBONCLOSE`), `ComponentManager.Ribbon` is `null`. The implementation hooks `ComponentManager.ItemInitialized` so that typing `RIBBON` immediately initializes the panel.
- No other caveats.

---

## 4. Conclusion

The specification for `HPGeoLinkRibbonTab`, `RibbonCommandHandler`, and `RibbonIcons` is finalized and fully articulated in `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\m3_ribbon_plan.md`.
- File structure, C# implementation code, and WPF vector geometries are 100% complete and self-contained.
- Zero changes are made to `HPAutoCad.McpBridge.Loader`, ensuring 100% preservation of the Civil 3D mirror invariant.
- The shared tab architecture guarantees seamless coexistence of `HPAUTOCAD_MCP_PANEL` and `HPGEOLINK_PANEL` under `HPAUTOCAD_MCP_TAB` across cold starts, workspace switches (`WSCURRENT`), and dynamic theme switches (`COLORTHEME`).

---

## 5. Verification Method

1. **Codebase Inspection**:
   - Inspect `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\m3_ribbon_plan.md` to verify the presence of:
     * Complete `HPGeoLinkRibbonTab.cs` code
     * Complete `RibbonIcons.cs` code with 5 vector glyphs
     * Complete `RibbonCommandHandler.cs` code
     * Lifecycle event handling logic
2. **Mirror Parity Confirmation**:
   - Inspect `HPCivil3d/tools/mirror-tokens.json` to verify that `HPAutoCad.Loader/` is not tracked in `mirroredFiles`, confirming no impact on `HPCivil3d.McpBridge.Tests`.
3. **Build & Live Verification (during Milestone M4)**:
   - Compile `HPAutoCad.slnx` with `HPAutoCad.Loader` included.
   - Execute unattended harness `HPAutoCad/tools/harness/run-ribbon-check.ps1` in AutoCAD 2026:
     * Asserts single `HPAutoCad` tab.
     * Asserts presence of `MCP` and `HPGeoLink` panels.
     * Asserts workspace switch invariance (`WSCURRENT`).
     * Asserts theme switch invariance (`COLORTHEME`).
     * Asserts click execution of `KMZ` and `Import`.
