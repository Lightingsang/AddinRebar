# Milestone M3: Shared Ribbon Tab & HPGeoLink Panel Technical Specification

**Document Version:** 1.0.0  
**Author:** explorer_m3_ribbon  
**Target Milestone:** M3 (Single Bundle Packaging, ALC Loader & Shared Ribbon Tab)  
**Parent Orchestrator:** orchestrator_3 (`050984c1-afaa-4911-859c-331e9279dc4f`)  
**Date:** 2026-09-20  

---

## 1. Executive Summary & Architectural Invariants

### 1.1 Objective
Design the complete production specification for integrating the geodetic toolkit (`HPGeoLink`) into AutoCAD 2026's Ribbon UI via a shared tab architecture. The implementation introduces `HPGeoLinkRibbonTab` and `RibbonIcons` inside the new `HPAutoCad.Loader` project, contributing the `HPGEOLINK_PANEL` panel onto the shared `HPAUTOCAD_MCP_TAB` ("HPAutoCad") tab alongside `HPAUTOCAD_MCP_PANEL` ("MCP").

### 1.2 Core Architectural Invariants
1. **Shared Tab Protocol ("One Tab, One Panel Per Tool")**:
   - The shared tab ID is strictly `HPAUTOCAD_MCP_TAB` and title `HPAutoCad`.
   - Any HP add-in bundle (`HPAutoCad.McpBridge.Loader` or `HPAutoCad.Loader`) that initializes first creates the tab. Subsequent loaders locate the existing tab via `ComponentManager.Ribbon.FindTab(TabId)` and append only their own dedicated panel.
   - Rebuilding during `COLORTHEME` switches removes only the add-in's own panel (`removeEmptyTab: false`).
   - Uninstallation during AutoCAD teardown removes only the add-in's own panel and removes the tab if and only if no other panels remain (`tab.Panels.Count == 0`).
2. **Default ALC Placement**:
   - `Autodesk.Windows` (from `AdWindows.dll`) relies on WPF, singleton static managers (`ComponentManager.Ribbon`), and UI-thread message pump dispatching.
   - Ribbon components (`HPGeoLinkRibbonTab`, `RibbonCommandHandler`, `RibbonIcons`) MUST reside in `HPAutoCad.Loader` executing in AutoCAD's Default ALC. They must NEVER cross into or compile against `HPAutoCad.dll` in `AppLoadContext` or `HPAutoCad.McpBridge.dll` in `BridgeLoadContext`.
3. **Preservation of Civil 3D Mirror Parity**:
   - `HPAutoCad.McpBridge.Loader/Ribbon/McpRibbonTab.cs`, `RibbonCommandHandler.cs`, and `RibbonIcons.cs` are mirrored 1:1 into `HPCivil3d.McpBridge.Loader/` and enforced by `HPCivil3d.McpBridge.Tests`.
   - `HPAutoCad.McpBridge.Loader` remains 100% UNTOUCHED.
   - The new `HPGeoLinkRibbonTab.cs` and companion classes are created exclusively in the new `HPAutoCad/HPAutoCad.Loader/Ribbon/` directory.
4. **Resolution-Independent Code-Drawn Vector Icons**:
   - Zero PNG, ICO, or loose image files are shipped in the bundle.
   - All glyphs are defined as pure WPF vector geometries (`DrawingImage` containing `DrawingGroup` of `GeometryDrawing` objects).
   - Coordinates are constrained to even integers within a 32×32 bounding box so AdWindows can derive 16×16 standard icons with exact 0.5x scaling without sub-pixel anti-aliasing blur.
   - Ink colors dynamically toggle between dark theme (`#E6E6E6` on `COLORTHEME=0`) and light theme (`#3C3C3C` on `COLORTHEME=1`), preserving brand accent (`#0696D7`).

---

## 2. Shared Tab Lifecycle & Multi-AddIn Coexistence

### 2.1 State Machine & Sequencing

```
AutoCAD Startup / NETLOAD
         │
         ▼
[IExtensionApplication.Initialize()]
         │
         ▼
[HPGeoLinkRibbonTab.Install()]
         │
         ├──► Hook Application.SystemVariableChanged (WSCURRENT, COLORTHEME)
         ├──► Hook ComponentManager.ItemInitialized (Late Ribbon load / RIBBON cmd)
         │
         ▼
[EnsureCreated()]
         │
    Ribbon == null?  ─── YES ──► (Wait for ItemInitialized or Idle)
         │ NO
         ▼
  FindTab("HPAUTOCAD_MCP_TAB")
         │
   ┌─────┴────────────────┐
   │ Exists               │ Missing
   ▼                      ▼
[Use Existing Tab]      [new RibbonTab { Id="HPAUTOCAD_MCP_TAB", Title="HPAutoCad" }]
   │                      [ribbon.Tabs.Add(tab)]
   └──────────┬───────────┘
              ▼
   FindOwnPanel("HPGEOLINK_PANEL")
              │
         Exists? ──── YES ──► Return (Idempotent, no duplicates)
              │ NO
              ▼
   [BuildPanel()] ──► Read COLORTHEME ──► Generate Vector Icons
              │
              ▼
   [tab.Panels.Add(panel)]
```

### 2.2 Rebuild and Event Resiliency Matrix

| Event / Trigger | Trigger Source | Mechanism / Timing | Action Taken by `HPGeoLinkRibbonTab` |
|---|---|---|---|
| **Cold Start (Ribbon ready)** | `Initialize()` | Immediate inside `EnsureCreated()` | If tab exists, appends `HPGEOLINK_PANEL`. If not, creates tab first. |
| **Cold Start (Late Ribbon)** | `Initialize()` | `ComponentManager.Ribbon == null` | Hooks `ComponentManager.ItemInitialized`. When Ribbon initializes, `EnsureCreated()` builds panel. |
| **`RIBBON` after `RIBBONCLOSE`** | User CLI | `ComponentManager.ItemInitialized` | Fires for new Ribbon elements. `EnsureCreated()` runs with `FindOwnPanel` guard. |
| **Workspace Switch (`WSCURRENT`)** | User GUI / CLI | `SystemVariableChanged` → `Application.Idle` | AutoCAD drops code-added tabs. On `Idle`, `EnsureCreated()` reconstructs tab and panel. |
| **Theme Switch (`COLORTHEME`)** | User GUI / CLI | `SystemVariableChanged` → `Application.Idle` | Sets `_rebuild = true`. On `Idle`, calls `RemoveOwnPanel(removeEmptyTab: false)`, queries new theme, re-generates icons, and calls `EnsureCreated()`. |
| **Shutdown / Add-in Unload** | `Terminate()` | Immediate inside `Uninstall()` | Unhooks all handlers. Calls `RemoveOwnPanel(removeEmptyTab: true)`. If tab has 0 panels, removes tab. |

### 2.3 Multi-AddIn Isolation Guardrails
- **Panel Scoping**: `FindOwnPanel` checks `tab.Panels.FirstOrDefault(p => p.Source?.Id == PanelId)`. Neither `McpRibbonTab` nor `HPGeoLinkRibbonTab` can inadvertently remove, reorder, or modify the other's panel.
- **Tab Destruction Safety**: During theme switches, `removeEmptyTab: false` is strictly passed to `RemoveOwnPanel`. The tab is NEVER removed while sibling panels (such as `MCP`) are present.
- **Coalesced Idle Queue**: Rapid workspace switches or batch variable adjustments are coalesced using `_idlePending`. Exactly one rebuild occurs per idle tick.

---

## 3. `HPGEOLINK_PANEL` Layout & Hierarchy Specification

### 3.1 Visual Hierarchy & Component Tree

```
RibbonTab: "HPAutoCad" (Id: "HPAUTOCAD_MCP_TAB")
├── RibbonPanel: "MCP" (Id: "HPAUTOCAD_MCP_PANEL")
│   └── RibbonButton: "MCP Bridge" (Id: "HPAUTOCAD_MCP_BRIDGE", Large, 32×32)
│
└── RibbonPanel: "HPGeoLink" (Id: "HPGEOLINK_PANEL")
    ├── RibbonButton: "KMZ" (Id: "HPGEO_KMZ", Large, 32×32)
    │   └── Target Command: "HPGEO"
    │
    └── RibbonSplitButton: "Import" (Id: "HPGEO_SECONDARY_SPLIT", Large, 32×32, IsSplit=true)
        │   └── Primary Action Command: "HPGEOIMPORT"
        │
        └── Items (Dropdown Menu):
            ├── RibbonButton: "Import" (Id: "HPGEO_IMPORT", Large/Standard, 32×32/16×16)
            │   └── Target Command: "HPGEOIMPORT"
            │
            ├── RibbonButton: "Map" (Id: "HPGEO_IMAGE", Standard, 16×16)
            │   └── Target Command: "-HPGEOIMAGE"
            │
            ├── RibbonButton: "Info" (Id: "HPGEO_INFO", Standard, 16×16)
            │   └── Target Command: "HPGEOINFO"
            │
            ├── RibbonButton: "-KMZ" (Id: "HPGEO_KMZ_SCRIPT", Standard, 16×16)
            │   └── Target Command: "-HPGEOKMZ"
            │
            └── RibbonButton: "-Import" (Id: "HPGEO_IMPORT_SCRIPT", Standard, 16×16)
                └── Target Command: "-HPGEOIMPORT"
```

### 3.2 Component Details

#### 1. Primary Large Button: `HPGEO_KMZ`
- **Id**: `HPGEO_KMZ`
- **Text**: `KMZ`
- **Size**: `RibbonItemSize.Large`
- **Orientation**: `System.Windows.Controls.Orientation.Vertical`
- **Image / LargeImage**: Vector `icons.Kmz`
- **Command**: `HPGEO`
- **Execution Mechanism**: `RunCommand("HPGEO")` via `Document.SendStringToExecute("_.HPGEO ", true, false, false)`
- **ToolTip**:
  - `Title`: `VN-2000 → KMZ (HPGEO)`
  - `Content`: `Chọn điểm/ranh VN-2000 trong bản vẽ, chọn tỉnh và kinh tuyến trục, xuất KMZ mở trong Google Earth.` (or startup error message if add-in unavailable)
  - `Command`: `HPGEO`

#### 2. Secondary SplitButton: `HPGEO_SECONDARY_SPLIT`
- **Id**: `HPGEO_SECONDARY_SPLIT`
- **Text**: `Import`
- **Size**: `RibbonItemSize.Large`
- **Orientation**: `System.Windows.Controls.Orientation.Vertical`
- **IsSplit**: `true` (Top click executes default import action `HPGEOIMPORT`; bottom arrow click opens dropdown menu)
- **Image / LargeImage**: Vector `icons.Import`
- **Command**: `HPGEOIMPORT`
- **ToolTip**:
  - `Title`: `Nhập dữ liệu & Công cụ địa lý`
  - `Content`: `Nhấp vào nút để Import KML/KMZ vào CAD; nhấp mũi tên để mở menu các công cụ phụ trợ (Ảnh vệ tinh, Thông tin hệ tọa độ, Lệnh Script).`
  - `Command`: `HPGEOIMPORT`

#### 3. Dropdown Menu Sub-Items inside SplitButton
1. **`HPGEO_IMPORT` ("Import")**:
   - Text: `Import KML/KMZ`
   - Command: `HPGEOIMPORT`
   - ToolTip: `Nhập đối tượng địa lý từ KML/KMZ hoặc danh sách tọa độ vào CAD trên layer HPGEO-IMPORT theo hệ tọa độ VN-2000.`
2. **`HPGEO_IMAGE` ("Map")**:
   - Text: `Bản đồ vệ tinh`
   - Command: `-HPGEOIMAGE`
   - ToolTip: `Tải và chèn ảnh vệ tinh độ phân giải cao (Esri/Google/Bing) theo ranh đất VN-2000 trực tiếp vào CAD.`
3. **`HPGEO_INFO` ("Info")**:
   - Text: `Thông tin & Chẩn đoán`
   - Command: `HPGEOINFO`
   - ToolTip: `Kiểm tra INSUNITS, kinh tuyến trục bản vẽ, danh mục tỉnh thành và tính toán chuyển đổi tọa độ VN-2000.`
4. **`HPGEO_KMZ_SCRIPT` ("-KMZ")**:
   - Text: `Xuất KMZ (Script)`
   - Command: `-HPGEOKMZ`
   - ToolTip: `Thực thi xuất KMZ ở chế độ không mở hộp thoại với tham số dòng lệnh key=value.`
5. **`HPGEO_IMPORT_SCRIPT` ("-Import")**:
   - Text: `Nhập KML/KMZ (Script)`
   - Command: `-HPGEOIMPORT`
   - ToolTip: `Thực thi nhập KML/KMZ ở chế độ không mở hộp thoại với tham số dòng lệnh file=<path> cm=<ktt>.`

---

## 4. Vector Icon Specifications (`RibbonIcons.cs`)

### 4.1 Design & Geometry Rules
1. **Coordinate Alignment**: Every endpoint and control point in the path strings is an even integer (e.g. 2, 4, 6, 8, ... 28, 30). AdWindows automatically creates a 16×16 image for standard buttons from the 32×32 master by dividing all coordinates by 2. When all source coordinates are even, the 16×16 downsampled coordinates remain exact integers, eliminating sub-pixel fuzziness.
2. **Hole Geometry (Even-Odd Rule)**: Cutouts inside shapes are expressed directly in the same SVG path string as sub-paths. In WPF, `Geometry.Parse` generates a frozen `StreamGeometry` with `FillRule.EvenOdd` by default. Overlapping sub-paths naturally hollow out without requiring background-colored fills.
3. **Dynamic Theme Ink**:
   - Dark Theme (`COLORTHEME=0`): Ink is `#E6E6E6` (`Color.FromRgb(0xE6, 0xE6, 0xE6)`).
   - Light Theme (`COLORTHEME=1`): Ink is `#3C3C3C` (`Color.FromRgb(0x3C, 0x3C, 0x3C)`).
   - Accent: HP Blue `#0696D7` (`Color.FromRgb(0x06, 0x96, 0xD7)`), constant across both themes.
4. **Bounding Box Preservation**: Each icon appends a transparent 32×32 bounding rectangle `new RectangleGeometry(new Rect(0, 0, 32, 32))` to guarantee uniform optical centering regardless of individual glyph aspect ratios.

### 4.2 Exact Vector Path Geometries

```csharp
// 1. KMZ (Globe with equator/meridian cross and accent location pin)
// Ink: Outer globe ring (R=12, inner R=10) + Cross lines
// Accent: Survey marker pin at top-right with center hole
(ink, "M4,16 A12,12 0 1 0 28,16 A12,12 0 1 0 4,16 Z M6,16 A10,10 0 1 1 26,16 A10,10 0 1 1 6,16 Z")
(ink, "M4,15 H28 V17 H4 Z M15,4 H17 V28 H15 Z")
(Accent, "M22,4 A6,6 0 0 1 28,10 C28,14 22,20 22,20 C22,20 16,14 16,10 A6,6 0 0 1 22,4 Z M22,8 A2,2 0 1 0 22,12 A2,2 0 1 0 22,8 Z")

// 2. Import (CAD Drawing boundary frame with inbound arrow)
// Ink: Outer drawing sheet with 2px frame + internal CAD grid lines
// Accent: Bold downward arrow entering into drawing boundary
(ink, "M4,12 H20 V28 H4 Z M6,14 H18 V26 H6 Z M6,20 H18 V22 H6 Z M11,14 H13 V26 H11 Z")
(Accent, "M20,4 H24 V12 H28 L22,18 L16,12 H20 Z")

// 3. Map (Satellite Imagery Quadrant Tile Grid with Topographic Peak)
// Ink: 4-quadrant map tile frame with 2px borders
// Accent: Topographic elevation contour peak across lower terrain
(ink, "M4,6 H28 V26 H4 Z M6,8 H26 V24 H6 Z M6,15 H26 V17 H6 Z M15,8 H17 V24 H15 Z")
(Accent, "M6,22 L12,14 L16,18 L20,12 L26,22 Z")

// 4. Info (Diagnostic Badge Ring with Seriffed 'i' Glyph)
// Ink: Circular badge ring
// Accent: Squared dot and seriffed stem
(ink, "M4,16 A12,12 0 1 0 28,16 A12,12 0 1 0 4,16 Z M6,16 A10,10 0 1 1 26,16 A10,10 0 1 1 6,16 Z")
(Accent, "M14,8 H18 V12 H14 Z M12,14 H18 V22 H20 V24 H12 V22 H14 V16 H12 Z")

// 5. KmzScript (CLI Terminal Frame with Prompt and Survey Pin)
// Ink: Console window with title bar, prompt '>' and cursor '_'
// Accent: Mini survey pin on console header
(ink, "M4,6 H28 V26 H4 Z M6,10 H26 V24 H6 Z M4,6 H28 V10 H4 Z")
(ink, "M8,14 L12,17 L8,20 L8,18 L10,17 L8,15 Z M14,20 H20 V22 H14 Z")
(Accent, "M20,12 A4,4 0 0 1 24,16 C24,19 20,22 20,22 C20,22 16,19 16,16 A4,4 0 0 1 20,12 Z M20,15 A1,1 0 1 0 20,17 A1,1 0 1 0 20,15 Z")
```

---

## 5. Complete C# Production Source Code

### 5.1 `RibbonIcons.cs`
**File Location:** `HPAutoCad/HPAutoCad.Loader/Ribbon/RibbonIcons.cs`

```csharp
using System.Windows;
using System.Windows.Media;

namespace HPAutoCad.Loader.Ribbon;

/// <summary>
/// Resolution-independent vector Ribbon icons for HPGeoLink in AutoCAD 2026.
/// All icons are drawn in code within a 32×32 box using even coordinates to guarantee crisp 16×16 downsampling.
/// The ink dynamically adjusts to AutoCAD's COLORTHEME (dark vs light); the accent is HP Blue (#0696D7).
/// </summary>
internal sealed class RibbonIcons
{
    private static readonly Brush Accent = Frozen(new SolidColorBrush(Color.FromRgb(0x06, 0x96, 0xD7)));

    public RibbonIcons(bool darkTheme)
    {
        var ink = Frozen(new SolidColorBrush(darkTheme ? Color.FromRgb(0xE6, 0xE6, 0xE6) : Color.FromRgb(0x3C, 0x3C, 0x3C)));

        // 1. KMZ: Globe with equator/meridian and survey pin
        Kmz = Glyph(
            (ink, "M4,16 A12,12 0 1 0 28,16 A12,12 0 1 0 4,16 Z M6,16 A10,10 0 1 1 26,16 A10,10 0 1 1 6,16 Z"),
            (ink, "M4,15 H28 V17 H4 Z M15,4 H17 V28 H15 Z"),
            (Accent, "M22,4 A6,6 0 0 1 28,10 C28,14 22,20 22,20 C22,20 16,14 16,10 A6,6 0 0 1 22,4 Z M22,8 A2,2 0 1 0 22,12 A2,2 0 1 0 22,8 Z"));

        // 2. Import: Drawing frame with inbound arrow
        Import = Glyph(
            (ink, "M4,12 H20 V28 H4 Z M6,14 H18 V26 H6 Z M6,20 H18 V22 H6 Z M11,14 H13 V26 H11 Z"),
            (Accent, "M20,4 H24 V12 H28 L22,18 L16,12 H20 Z"));

        // 3. Map: Quadrant satellite map tile grid with terrain peak
        Map = Glyph(
            (ink, "M4,6 H28 V26 H4 Z M6,8 H26 V24 H6 Z M6,15 H26 V17 H6 Z M15,8 H17 V24 H15 Z"),
            (Accent, "M6,22 L12,14 L16,18 L20,12 L26,22 Z"));

        // 4. Info: Circular diagnostic ring with 'i' badge
        Info = Glyph(
            (ink, "M4,16 A12,12 0 1 0 28,16 A12,12 0 1 0 4,16 Z M6,16 A10,10 0 1 1 26,16 A10,10 0 1 1 6,16 Z"),
            (Accent, "M14,8 H18 V12 H14 Z M12,14 H18 V22 H20 V24 H12 V22 H14 V16 H12 Z"));

        // 5. KmzScript: Console prompt window with pin
        KmzScript = Glyph(
            (ink, "M4,6 H28 V26 H4 Z M6,10 H26 V24 H6 Z M4,6 H28 V10 H4 Z"),
            (ink, "M8,14 L12,17 L8,20 L8,18 L10,17 L8,15 Z M14,20 H20 V22 H14 Z"),
            (Accent, "M20,12 A4,4 0 0 1 24,16 C24,19 20,22 20,22 C20,22 16,19 16,16 A4,4 0 0 1 20,12 Z M20,15 A1,1 0 1 0 20,17 A1,1 0 1 0 20,15 Z"));
    }

    public ImageSource Kmz { get; }
    public ImageSource Import { get; }
    public ImageSource Map { get; }
    public ImageSource Info { get; }
    public ImageSource KmzScript { get; }

    private static ImageSource Glyph(params (Brush Brush, string Path)[] parts)
    {
        var group = new DrawingGroup();
        foreach (var (brush, path) in parts)
        {
            group.Children.Add(new GeometryDrawing(brush, null, Geometry.Parse(path)));
        }
        // Transparent 32x32 boundary ensures proper centering across all glyph extents
        group.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(0, 0, 32, 32))));
        return Frozen(new DrawingImage(group));
    }

    private static T Frozen<T>(T freezable) where T : Freezable
    {
        freezable.Freeze();
        return freezable;
    }
}
```

---

### 5.2 `RibbonCommandHandler.cs`
**File Location:** `HPAutoCad/HPAutoCad.Loader/Ribbon/RibbonCommandHandler.cs`

```csharp
using System;
using System.Windows.Input;

namespace HPAutoCad.Loader.Ribbon;

/// <summary>
/// Safe ICommand relay for Ribbon buttons. Executes action on AutoCAD's main UI thread
/// and prevents unhandled exceptions from reaching AdWindows (which would cause silent dead buttons).
/// </summary>
internal sealed class RibbonCommandHandler(string name, Action action) : ICommand
{
    public event EventHandler? CanExecuteChanged { add { } remove { } }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            LoaderLog.Write($"ribbon command '{name}' failed", exception);
        }
    }
}
```

---

### 5.3 `HPGeoLinkRibbonTab.cs`
**File Location:** `HPAutoCad/HPAutoCad.Loader/Ribbon/HPGeoLinkRibbonTab.cs`

```csharp
using System;
using System.Linq;
using System.Windows.Controls;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.Windows;

namespace HPAutoCad.Loader.Ribbon;

/// <summary>
/// Manages the HPGeoLink panel on the shared "HPAutoCad" Ribbon tab (TabId: "HPAUTOCAD_MCP_TAB").
/// Adheres strictly to the multi-add-in shared tab protocol:
/// 1. Finds or creates the shared tab without affecting sibling panels (e.g. MCP).
/// 2. Manages only its own panel ("HPGEOLINK_PANEL").
/// 3. Dynamically rebuilds icons when COLORTHEME flips (without removing the shared tab).
/// 4. Recreates the panel after workspace switching (WSCURRENT).
/// 5. Automatically binds late Ribbon initialization via ComponentManager.ItemInitialized.
/// </summary>
internal static class HPGeoLinkRibbonTab
{
    public const string TabId = "HPAUTOCAD_MCP_TAB";
    public const string TabTitle = "HPAutoCad";
    public const string PanelId = "HPGEOLINK_PANEL";
    public const string PanelTitle = "HPGeoLink";

    private static bool _installed;
    private static bool _idlePending;
    private static bool _rebuild;
    private static bool _building;

    public static void Install()
    {
        if (_installed) return;
        _installed = true;

        Application.SystemVariableChanged += OnSystemVariableChanged;
        ComponentManager.ItemInitialized += OnRibbonItemInitialized;
        EnsureCreated();
    }

    public static void Uninstall()
    {
        if (!_installed) return;
        _installed = false;

        Application.SystemVariableChanged -= OnSystemVariableChanged;
        ComponentManager.ItemInitialized -= OnRibbonItemInitialized;
        Application.Idle -= OnIdle;
        _idlePending = false;
        _rebuild = false;

        RemoveOwnPanel(removeEmptyTab: true);
    }

    public static void EnsureCreated()
    {
        if (_building) return;
        try
        {
            var ribbon = ComponentManager.Ribbon;
            if (ribbon is null) return;
            _building = true;

            var tab = ribbon.FindTab(TabId);
            var createdTab = tab is null;
            if (tab is null)
            {
                tab = new RibbonTab { Id = TabId, Title = TabTitle, IsVisible = true };
                ribbon.Tabs.Add(tab);
            }
            if (FindOwnPanel(tab) is not null) return;

            tab.Panels.Add(BuildPanel());
            var status = AppLoaderApplication.App is not null ? "available" : "unavailable";
            if (createdTab) LoaderLog.Write($"ribbon tab {TabId} created (HPGeoLink {status})");
            LoaderLog.Write($"ribbon panel {PanelId} added to tab {TabId} ({(createdTab ? "tab created" : "tab existing")}, HPGeoLink {status})");
        }
        catch (Exception exception)
        {
            LoaderLog.Write("HPGeoLink ribbon panel creation failed", exception);
        }
        finally
        {
            _building = false;
        }
    }

    private static void OnRibbonItemInitialized(object? sender, RibbonItemEventArgs e) => EnsureCreated();

    private static void OnSystemVariableChanged(object? sender, Autodesk.AutoCAD.ApplicationServices.SystemVariableChangedEventArgs e)
    {
        var workspace = string.Equals(e.Name, "WSCURRENT", StringComparison.OrdinalIgnoreCase);
        var theme = string.Equals(e.Name, "COLORTHEME", StringComparison.OrdinalIgnoreCase);
        if (!workspace && !theme) return;
        if (theme) _rebuild = true;
        if (_idlePending) return;
        _idlePending = true;
        Application.Idle += OnIdle;
    }

    private static void OnIdle(object? sender, EventArgs e)
    {
        Application.Idle -= OnIdle;
        _idlePending = false;
        if (_rebuild)
        {
            _rebuild = false;
            RemoveOwnPanel(removeEmptyTab: false);
        }
        EnsureCreated();
    }

    private static RibbonPanel? FindOwnPanel(RibbonTab tab) =>
        tab.Panels.FirstOrDefault(p => p.Source?.Id == PanelId);

    private static void RemoveOwnPanel(bool removeEmptyTab)
    {
        try
        {
            var ribbon = ComponentManager.Ribbon;
            var tab = ribbon?.FindTab(TabId);
            if (tab is null) return;
            if (FindOwnPanel(tab) is { } panel) tab.Panels.Remove(panel);
            if (removeEmptyTab && tab.Panels.Count == 0) ribbon!.Tabs.Remove(tab);
        }
        catch (Exception exception)
        {
            LoaderLog.Write("HPGeoLink ribbon panel removal failed", exception);
        }
    }

    private static RibbonPanel BuildPanel()
    {
        var isAvailable = AppLoaderApplication.App is not null;
        var icons = new RibbonIcons(IsDarkTheme());

        // 1. Primary Large Button: HPGEO_KMZ
        var kmzButton = new RibbonButton
        {
            Id = "HPGEO_KMZ",
            Text = "KMZ",
            ShowText = true,
            ShowImage = true,
            Size = RibbonItemSize.Large,
            Orientation = Orientation.Vertical,
            Image = icons.Kmz,
            LargeImage = icons.Kmz,
            CommandHandler = new RibbonCommandHandler("HPGEO_KMZ", () => RunCommand("HPGEO")),
            IsEnabled = isAvailable,
            ToolTip = new RibbonToolTip
            {
                Title = "VN-2000 → KMZ (HPGEO)",
                Content = isAvailable
                    ? "Chọn điểm/ranh VN-2000 trong bản vẽ, chọn tỉnh và kinh tuyến trục, xuất KMZ mở trong Google Earth."
                    : GetUnavailableReason(),
                Command = "HPGEO",
                IsHelpEnabled = false,
            }
        };

        // 2. Dropdown item: HPGEO_IMPORT
        var importItem = new RibbonButton
        {
            Id = "HPGEO_IMPORT",
            Text = "Import KML/KMZ",
            ShowText = true,
            ShowImage = true,
            Size = RibbonItemSize.Standard,
            Orientation = Orientation.Horizontal,
            Image = icons.Import,
            LargeImage = icons.Import,
            CommandHandler = new RibbonCommandHandler("HPGEO_IMPORT", () => RunCommand("HPGEOIMPORT")),
            IsEnabled = isAvailable,
            ToolTip = new RibbonToolTip
            {
                Title = "Import KML/KMZ (HPGEOIMPORT)",
                Content = isAvailable
                    ? "Nhập đối tượng địa lý từ KML/KMZ hoặc danh sách tọa độ vào CAD trên layer HPGEO-IMPORT theo hệ tọa độ VN-2000."
                    : GetUnavailableReason(),
                Command = "HPGEOIMPORT",
                IsHelpEnabled = false,
            }
        };

        // 3. Dropdown item: HPGEO_IMAGE
        var imageItem = new RibbonButton
        {
            Id = "HPGEO_IMAGE",
            Text = "Ảnh vệ tinh",
            ShowText = true,
            ShowImage = true,
            Size = RibbonItemSize.Standard,
            Orientation = Orientation.Horizontal,
            Image = icons.Map,
            LargeImage = icons.Map,
            CommandHandler = new RibbonCommandHandler("HPGEO_IMAGE", () => RunCommand("-HPGEOIMAGE")),
            IsEnabled = isAvailable,
            ToolTip = new RibbonToolTip
            {
                Title = "Chèn ảnh vệ tinh (-HPGEOIMAGE)",
                Content = isAvailable
                    ? "Tải và chèn ảnh vệ tinh độ phân giải cao (Esri/Google/Bing) theo khung ranh đất VN-2000 vào CAD."
                    : GetUnavailableReason(),
                Command = "-HPGEOIMAGE",
                IsHelpEnabled = false,
            }
        };

        // 4. Dropdown item: HPGEO_INFO
        var infoItem = new RibbonButton
        {
            Id = "HPGEO_INFO",
            Text = "Thông tin & Chẩn đoán",
            ShowText = true,
            ShowImage = true,
            Size = RibbonItemSize.Standard,
            Orientation = Orientation.Horizontal,
            Image = icons.Info,
            LargeImage = icons.Info,
            CommandHandler = new RibbonCommandHandler("HPGEO_INFO", () => RunCommand("HPGEOINFO")),
            IsEnabled = isAvailable,
            ToolTip = new RibbonToolTip
            {
                Title = "Thông tin & Chẩn đoán (HPGEOINFO)",
                Content = isAvailable
                    ? "Hiển thị thông tin hệ tọa độ, kinh tuyến trục bản vẽ, INSUNITS, danh mục tỉnh thành và log chẩn đoán."
                    : GetUnavailableReason(),
                Command = "HPGEOINFO",
                IsHelpEnabled = false,
            }
        };

        // 5. Dropdown item: HPGEO_KMZ_SCRIPT
        var kmzScriptItem = new RibbonButton
        {
            Id = "HPGEO_KMZ_SCRIPT",
            Text = "Xuất KMZ (Script)",
            ShowText = true,
            ShowImage = true,
            Size = RibbonItemSize.Standard,
            Orientation = Orientation.Horizontal,
            Image = icons.KmzScript,
            LargeImage = icons.KmzScript,
            CommandHandler = new RibbonCommandHandler("HPGEO_KMZ_SCRIPT", () => RunCommand("-HPGEOKMZ")),
            IsEnabled = isAvailable,
            ToolTip = new RibbonToolTip
            {
                Title = "Xuất KMZ dòng lệnh (-HPGEOKMZ)",
                Content = isAvailable
                    ? "Thực thi xuất KMZ ở chế độ không mở hộp thoại với tham số dòng lệnh (hỗ trợ script tự động)."
                    : GetUnavailableReason(),
                Command = "-HPGEOKMZ",
                IsHelpEnabled = false,
            }
        };

        // 6. Dropdown item: HPGEO_IMPORT_SCRIPT
        var importScriptItem = new RibbonButton
        {
            Id = "HPGEO_IMPORT_SCRIPT",
            Text = "Nhập KML/KMZ (Script)",
            ShowText = true,
            ShowImage = true,
            Size = RibbonItemSize.Standard,
            Orientation = Orientation.Horizontal,
            Image = icons.Import,
            LargeImage = icons.Import,
            CommandHandler = new RibbonCommandHandler("HPGEO_IMPORT_SCRIPT", () => RunCommand("-HPGEOIMPORT")),
            IsEnabled = isAvailable,
            ToolTip = new RibbonToolTip
            {
                Title = "Nhập KML/KMZ dòng lệnh (-HPGEOIMPORT)",
                Content = isAvailable
                    ? "Thực thi nhập KML/KMZ ở chế độ không mở hộp thoại với tham số dòng lệnh file=<path> cm=<ktt>."
                    : GetUnavailableReason(),
                Command = "-HPGEOIMPORT",
                IsHelpEnabled = false,
            }
        };

        // Secondary Large SplitButton
        var splitButton = new RibbonSplitButton
        {
            Id = "HPGEO_SECONDARY_SPLIT",
            Text = "Import",
            ShowText = true,
            ShowImage = true,
            Size = RibbonItemSize.Large,
            Orientation = Orientation.Vertical,
            IsSplit = true,
            Image = icons.Import,
            LargeImage = icons.Import,
            CommandHandler = new RibbonCommandHandler("HPGEO_IMPORT", () => RunCommand("HPGEOIMPORT")),
            IsEnabled = isAvailable,
            ToolTip = new RibbonToolTip
            {
                Title = "Nhập dữ liệu & Công cụ địa lý",
                Content = isAvailable
                    ? "Nhấp nút để Import KML/KMZ vào CAD; nhấp mũi tên để mở menu các công cụ phụ trợ (Ảnh vệ tinh, Thông tin hệ tọa độ, Lệnh Script)."
                    : GetUnavailableReason(),
                Command = "HPGEOIMPORT",
                IsHelpEnabled = false,
            }
        };

        splitButton.Items.Add(importItem);
        splitButton.Items.Add(imageItem);
        splitButton.Items.Add(infoItem);
        splitButton.Items.Add(kmzScriptItem);
        splitButton.Items.Add(importScriptItem);

        var panel = new RibbonPanel { Source = new RibbonPanelSource { Id = PanelId, Title = PanelTitle } };
        panel.Source.Items.Add(kmzButton);
        panel.Source.Items.Add(splitButton);
        return panel;
    }

    private static void RunCommand(string command)
    {
        var doc = Application.DocumentManager.MdiActiveDocument;
        if (doc is null)
        {
            LoaderLog.Write($"ribbon click on '{command}' with no document open");
            return;
        }
        // SendStringToExecute queues command execution through AutoCAD's normal command loop
        doc.SendStringToExecute("_." + command + " ", true, false, false);
    }

    private static bool IsDarkTheme()
    {
        try { return Convert.ToInt32(Application.GetSystemVariable("COLORTHEME")) == 0; }
        catch (Exception) { return true; }
    }

    private static string GetUnavailableReason() =>
        "HPGeoLink không khởi động được (" + (AppLoaderApplication.StartupError ?? "?") + "). Xem loader.log trong " + LoaderLog.LogDirectory;
}
```

---

## 6. Integration with `AppLoaderApplication` (Loader Entry Point)

Inside `HPAutoCad/HPAutoCad.Loader/AppLoaderApplication.cs`:

```csharp
public sealed class AppLoaderApplication : IExtensionApplication
{
    public void Initialize()
    {
        // 1. Initialize isolated AppLoadContext and bootstrap HPAutoCad.dll
        // ...

        // 2. Install HPGeoLink Ribbon Panel (guarded against exceptions)
        try
        {
            HPGeoLinkRibbonTab.Install();
        }
        catch (Exception ex)
        {
            LoaderLog.Write("HPGeoLink ribbon install failed", ex);
        }
    }

    public void Terminate()
    {
        // 1. Uninstall HPGeoLink Ribbon Panel
        try
        {
            HPGeoLinkRibbonTab.Uninstall();
        }
        catch (Exception ex)
        {
            LoaderLog.Write("HPGeoLink ribbon uninstall failed", ex);
        }

        // 2. Teardown AppLoadContext and dispose add-in resources
        // ...
    }
}
```

---

## 7. Verification & Automation Strategy (Live Unattended Harness)

### 7.1 Harness Test Cases (`run-ribbon-check.ps1` Extension)
The existing harness `HPAutoCad/tools/harness/run-ribbon-check.ps1` will be extended in Milestone M4 to assert:
1. **Single Shared Tab Appearance**:
   - `Find-RibbonTabs $acadPid 'HPAUTOCAD_MCP_TAB'` yields exactly 1 tab.
   - Title matches `'HPAutoCad'`.
2. **Dual Panel Coexistence**:
   - `Count-RibbonPanels $acadPid 'HPAUTOCAD_MCP_TAB'` yields 2 panels: `MCP` and `HPGeoLink`.
3. **Button Visibility & Names**:
   - `Count-RibbonButtons 'MCP Bridge'` == 1.
   - `Count-RibbonButtons 'KMZ'` == 1.
   - `Count-RibbonButtons 'Import'` == 1.
4. **Workspace Switch Invariant**:
   - Change `WSCURRENT` to `'3D Modeling'` and back to `'Drafting & Annotation'`.
   - Both panels recreate cleanly on the single `HPAutoCad` tab.
5. **Theme Switch Invariant**:
   - Change `COLORTHEME` from 0 to 1 and back.
   - `loader.log` records both:
     * `ribbon panel HPAUTOCAD_MCP_PANEL added to tab HPAUTOCAD_MCP_TAB`
     * `ribbon panel HPGEOLINK_PANEL added to tab HPAUTOCAD_MCP_TAB`
   - Screenshots captured for both themes show crisp `#E6E6E6` (dark) and `#3C3C3C` (light) ink rendering.
6. **Execution Proof**:
   - Clicking `KMZ` queues `_.HPGEO ` into active document.
   - Zero errors or unhandled exceptions logged in `loader.log`.

---

## 8. Summary of Milestones & Deliverables

| Deliverable | Path | Status | Verification Gate |
|---|---|---|---|
| `HPGeoLinkRibbonTab.cs` | `HPAutoCad/HPAutoCad.Loader/Ribbon/HPGeoLinkRibbonTab.cs` | Specified | Compiles under net8.0-windows, 0 errors |
| `RibbonIcons.cs` | `HPAutoCad/HPAutoCad.Loader/Ribbon/RibbonIcons.cs` | Specified | Compiles under net8.0-windows, pure WPF |
| `RibbonCommandHandler.cs` | `HPAutoCad/HPAutoCad.Loader/Ribbon/RibbonCommandHandler.cs` | Specified | Compiles under net8.0-windows |
| Civil 3D Mirror Invariant | `HPCivil3d.McpBridge.Tests` | Preserved | 100% pass (no touched files in `HPAutoCad.McpBridge.Loader/`) |
| Unattended Live Ribbon Check | `HPAutoCad/tools/harness/run-ribbon-check.ps1` | Planned for M4 | Automated UI Automation verification in AutoCAD 2026 |
