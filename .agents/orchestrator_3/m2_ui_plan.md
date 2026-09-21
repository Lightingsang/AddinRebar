# Technical Specification & Implementation Plan: WPF MVVM UI & Theming (HPGeoLink)

**Milestone**: M2 (Add-In Layer & UI Feature)  
**Deliverable**: `HPAutoCad/HPAutoCad/HPGeoLink/` (View, ViewModel, Model) & `HPAutoCad/HPAutoCad/Resources/Themes/`  
**Author**: `explorer_m2_ui`  
**Target Audience**: `worker_m2` / `orchestrator_3`  
**Date**: 2026-09-20  

---

## 1. Executive Summary & Purpose

The user objective requires migrating the geodetic toolkit `HPGeo` into `HPAutoCad` as a unified feature folder `HPGeoLink` mirroring the production architecture of `HPRebar`. Milestone M1 successfully migrated pure domain algorithms, catalogs, and math into `HPAutoCad.Core` and created `HPAutoCad.TileFetch` and `HPAutoCad.Tests`.

Milestone M2 establishes the AutoCAD Add-In presentation layer (`HPAutoCad.dll` targeting `net8.0-windows` and AutoCAD 2026 / R25.1). This specification defines the complete, production-grade architecture for:
1. **WPF MVVM User Interface**: Modal dialogs (`GeoExportWindow`, `GeoImportWindow`), reusable views (`CrsSelectionView`), and the embedded satellite imagery map (`MapPanel`).
2. **ViewModel & Pure State Management**: `GeoExportViewModel` (with partial command implementation), `GeoImportViewModel`, `CrsSelectionViewModel`, decoupled via `IGeoExportShell` and `IGeoImportShell`.
3. **UI Model Layer**: Records representing UI projection items, central meridians, drawing units, preview table rows, and satellite image export arguments.
4. **Theming Architecture**: MaterialDesignThemes 5.3.2 integration, palette dictionaries (`ThemeDark.xaml`, `ThemeLight.xaml`), custom theme bridge (`MaterialThemeBridge`), and dynamic reactivity to AutoCAD's `COLORTHEME` system variable (`AutocadHostTheme`).
5. **ALC Contextual Reflection (`EnterContextualReflection`)**: Guaranteed BAML resource and pack URI resolution within the isolated `AppLoadContext`.
6. **WebView2 Resiliency**: Zero-crash Leaflet + Esri World Imagery map panel with `%LocalAppData%` isolation and automatic offline fallback.

---

## 2. Complete File Inventory & Architectural Placement

The migration reorganizes the 25 files previously in `HPGeo/HPGeo.AutoCad/UI/` into clean, standard MVVM folders under `HPAutoCad/HPAutoCad/`:
- `HPGeoLink/View/`: Windows, UserControls, Converters, and HTML helpers.
- `HPGeoLink/ViewModel/`: Observable view models and shell abstraction interfaces.
- `HPGeoLink/Model/`: UI-specific records and data transfer items.
- `Resources/Themes/`: Shared application themes, styles, and host theme monitors.

### Detailed Migration Matrix

| # | Legacy Path (`HPGeo/HPGeo.AutoCad/UI/`) | Target Path (`HPAutoCad/HPAutoCad/`) | Target Namespace | Lines | Role / Responsibility |
|---|---|---|---|---:|---|
| **View** | | | | | |
| 1 | `UI/GeoExportWindow.xaml` | `HPGeoLink/View/GeoExportWindow.xaml` | `HPAutoCad.HPGeoLink.View` | 122 | Export modal window XAML (table, map, options) |
| 2 | `UI/GeoExportWindow.xaml.cs` | `HPGeoLink/View/GeoExportWindow.xaml.cs` | `HPAutoCad.HPGeoLink.View` | 32 | Code-behind: ALC reflection, theme attach, DataContext |
| 3 | `UI/GeoImportWindow.xaml` | `HPGeoLink/View/GeoImportWindow.xaml` | `HPAutoCad.HPGeoLink.View` | 93 | Import modal window XAML (file/text source, preview) |
| 4 | `UI/GeoImportWindow.xaml.cs` | `HPGeoLink/View/GeoImportWindow.xaml.cs` | `HPAutoCad.HPGeoLink.View` | 21 | Code-behind: ALC reflection, theme attach, DataContext |
| 5 | `UI/CrsSelectionView.xaml` | `HPGeoLink/View/CrsSelectionView.xaml` | `HPAutoCad.HPGeoLink.View` | 63 | CRS Selection UserControl XAML (provinces, KTT, units) |
| 6 | `UI/CrsSelectionView.xaml.cs` | `HPGeoLink/View/CrsSelectionView.xaml.cs` | `HPAutoCad.HPGeoLink.View` | 10 | Code-behind: `InitializeComponent()` only |
| 7 | `UI/MapPanel.xaml` | `HPGeoLink/View/MapPanel.xaml` | `HPAutoCad.HPGeoLink.View` | 13 | WebView2 host UserControl XAML with fallback label |
| 8 | `UI/MapPanel.xaml.cs` | `HPGeoLink/View/MapPanel.xaml.cs` | `HPAutoCad.HPGeoLink.View` | 143 | WebView2 lifecycle, user-data folder, push data, dispose |
| 9 | `UI/MapHtml.cs` | `HPGeoLink/View/MapHtml.cs` | `HPAutoCad.HPGeoLink.View` | 67 | Leaflet 1.9.4 + Esri tiles HTML generator (SRI verified) |
| 10 | `UI/ValueConverters.cs` | `HPGeoLink/View/ValueConverters.cs` | `HPAutoCad.HPGeoLink.View` | 35 | XAML converters (InverseBool, BoolToVis, NonEmptyToVis) |
| **ViewModel** | | | | | |
| 11 | `UI/GeoExportViewModel.cs` | `HPGeoLink/ViewModel/GeoExportViewModel.cs` | `HPAutoCad.HPGeoLink.ViewModel` | 174 | Export ViewModel: state, preview rows, map JSON |
| 12 | `UI/GeoExportViewModel.Commands.cs` | `HPGeoLink/ViewModel/GeoExportViewModel.Commands.cs` | `HPAutoCad.HPGeoLink.ViewModel` | 128 | Export RelayCommands: KMZ, KML, Earth, Maps, Image |
| 13 | `UI/GeoImportViewModel.cs` | `HPGeoLink/ViewModel/GeoImportViewModel.cs` | `HPAutoCad.HPGeoLink.ViewModel` | 180 | Import ViewModel: file/pasted state, plan execution |
| 14 | `UI/CrsSelectionViewModel.cs` | `HPGeoLink/ViewModel/CrsSelectionViewModel.cs` | `HPAutoCad.HPGeoLink.ViewModel` | 143 | Shared CRS selection ViewModel (34/63 catalogs, TM) |
| 15 | `UI/IGeoExportShell.cs` | `HPGeoLink/ViewModel/IGeoExportShell.cs` | `HPAutoCad.HPGeoLink.ViewModel` | 23 | Shell interface for save dialog & application launching |
| 16 | (In `GeoImportViewModel.cs`) | `HPGeoLink/ViewModel/IGeoImportShell.cs` | `HPAutoCad.HPGeoLink.ViewModel` | 12 | Shell interface for open file dialog (`AskOpenPath`) |
| **Model** | | | | | |
| 17 | `UI/GeoExportItems.cs` | `HPGeoLink/Model/GeoExportItems.cs` | `HPAutoCad.HPGeoLink.Model` | 47 | UI records: ProvinceItem, MeridianItem, UnitItem, etc. |
| 18 | (In `GeoImportViewModel.cs`) | `HPGeoLink/Model/GeoImportItems.cs` | `HPAutoCad.HPGeoLink.Model` | 10 | UI records: `ImportPreviewRow` |
| **Themes** | | | | | |
| 19 | `UI/Theme.xaml` | `Resources/Themes/Theme.xaml` | ResourceDictionary | 105 | Base styles, tokens (Spacing, Margin), controls |
| 20 | `UI/MaterialBridge.xaml` | `Resources/Themes/MaterialBridge.xaml` | ResourceDictionary | 19 | CustomColorTheme (#0696D7/#E0641E), MD2 defaults |
| 21 | `UI/ThemeDark.xaml` | `Resources/Themes/ThemeDark.xaml` | ResourceDictionary | 21 | Palette for AutoCAD COLORTHEME 0 (dark) |
| 22 | `UI/ThemeLight.xaml` | `Resources/Themes/ThemeLight.xaml` | ResourceDictionary | 21 | Palette for AutoCAD COLORTHEME 1 (light) |
| 23 | `UI/MaterialThemeBridge.cs` | `Resources/Themes/MaterialThemeBridge.cs` | `HPAutoCad.Resources.Themes` | 111 | Runtime dynamic theme updater (swaps top-level overlay) |
| 24 | `UI/IHostTheme.cs` | `Resources/Themes/IHostTheme.cs` | `HPAutoCad.Resources.Themes` | 14 | Theme abstraction contract (`IsDark`, `Changed`) |
| 25 | `UI/HPGeoHostTheme.cs` | `Resources/Themes/AutocadHostTheme.cs` | `HPAutoCad.Resources.Themes` | 41 | AutoCAD `COLORTHEME` monitor implementation |
| 26 | `UI/ThemeInfo.cs` | `Resources/Themes/ThemeInfo.cs` | Assembly Attribute | 7 | `[assembly: ThemeInfo(None, SourceAssembly)]` |
| 27 | `UI/ThemeResources.cs` | `Resources/Themes/ThemeResources.cs` | `HPAutoCad.Resources.Themes` | 15 | Helper returning `/HPAutoCad;component/.../Theme.xaml` |

---

## 3. Namespace Migrations & Inter-Assembly Dependencies

### 3.1. Namespace Mapping Rules

All code originating from `HPGeo.AutoCad.UI.*` must be strictly mapped to the target structure:

```
HPGeo.AutoCad.UI.GeoExportWindow        → HPAutoCad.HPGeoLink.View.GeoExportWindow
HPGeo.AutoCad.UI.GeoImportWindow        → HPAutoCad.HPGeoLink.View.GeoImportWindow
HPGeo.AutoCad.UI.CrsSelectionView       → HPAutoCad.HPGeoLink.View.CrsSelectionView
HPGeo.AutoCad.UI.MapPanel               → HPAutoCad.HPGeoLink.View.MapPanel
HPGeo.AutoCad.UI.MapHtml                → HPAutoCad.HPGeoLink.View.MapHtml
HPGeo.AutoCad.UI.*Converter             → HPAutoCad.HPGeoLink.View.*Converter

HPGeo.AutoCad.UI.GeoExportViewModel     → HPAutoCad.HPGeoLink.ViewModel.GeoExportViewModel
HPGeo.AutoCad.UI.GeoImportViewModel     → HPAutoCad.HPGeoLink.ViewModel.GeoImportViewModel
HPGeo.AutoCad.UI.CrsSelectionViewModel  → HPAutoCad.HPGeoLink.ViewModel.CrsSelectionViewModel
HPGeo.AutoCad.UI.IGeoExportShell        → HPAutoCad.HPGeoLink.ViewModel.IGeoExportShell
HPGeo.AutoCad.UI.IGeoImportShell        → HPAutoCad.HPGeoLink.ViewModel.IGeoImportShell

HPGeo.AutoCad.UI.ProvinceItem           → HPAutoCad.HPGeoLink.Model.ProvinceItem
HPGeo.AutoCad.UI.MeridianItem           → HPAutoCad.HPGeoLink.Model.MeridianItem
HPGeo.AutoCad.UI.UnitItem               → HPAutoCad.HPGeoLink.Model.UnitItem
HPGeo.AutoCad.UI.PreviewRow             → HPAutoCad.HPGeoLink.Model.PreviewRow
HPGeo.AutoCad.UI.GeoImageChoice         → HPAutoCad.HPGeoLink.Model.GeoImageChoice
HPGeo.AutoCad.UI.ImportPreviewRow       → HPAutoCad.HPGeoLink.Model.ImportPreviewRow

HPGeo.AutoCad.UI.MaterialThemeBridge    → HPAutoCad.Resources.Themes.MaterialThemeBridge
HPGeo.AutoCad.UI.IHostTheme             → HPAutoCad.Resources.Themes.IHostTheme
HPGeo.AutoCad.UI.HPGeoHostTheme         → HPAutoCad.Resources.Themes.AutocadHostTheme
HPGeo.AutoCad.UI.ThemeResources         → HPAutoCad.Resources.Themes.ThemeResources
```

### 3.2. Reference to HPAutoCad.Core

The ViewModels and Models reference the pure domain types in `HPAutoCad.Core.HPGeoLink.*`:

```csharp
using HPAutoCad.Core.HPGeoLink.Catalog;     // Province, CentralMeridian, ProvinceCatalog
using HPAutoCad.Core.HPGeoLink.Conversion;  // Vn2000Converter, ConversionOptions, ConversionResult
using HPAutoCad.Core.HPGeoLink.Geometry;    // BulgeTessellator, GeometryUtils
using HPAutoCad.Core.HPGeoLink.Imagery;     // TileCoverage, GridBoundingBox, ImageryProviders
using HPAutoCad.Core.HPGeoLink.Import;      // ImportPlanner, ImportPlan, CoordinateTextParser, KmlReader
using HPAutoCad.Core.HPGeoLink.Kml;         // KmlColor, KmzWriter, KmlDocumentBuilder, KmzExportPipeline
using HPAutoCad.Core.HPGeoLink.Model;       // SurveyPoint, BoundaryPolyline, PlanePoint, GeoPoint
using HPAutoCad.Core.HPGeoLink.Projection;   // TmParameters
using HPAutoCad.Core.HPGeoLink.Settings;     // GeoSettings
using HPAutoCad.Core.HPGeoLink.Units;        // DrawingUnit, DrawingUnitFactor
```

---

## 4. WPF Views & MVVM Implementation Specification

### 4.1. `GeoExportWindow` (WPF View)

#### XAML Structure (`HPGeoLink/View/GeoExportWindow.xaml`)
- **Class**: `HPAutoCad.HPGeoLink.View.GeoExportWindow`
- **Namespaces**:
  ```xml
  xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:view="clr-namespace:HPAutoCad.HPGeoLink.View"
  xmlns:vm="clr-namespace:HPAutoCad.HPGeoLink.ViewModel"
  ```
- **Window Attributes**:
  - `Title="HPAutoCad · VN-2000 → KMZ"`
  - `Width="1040" Height="760" MinWidth="820" MinHeight="620"`
  - `WindowStartupLocation="CenterOwner" ShowInTaskbar="False"`
  - `Background="{DynamicResource Brush.Background}" Foreground="{DynamicResource Brush.Text}"`
  - `FontFamily="Segoe UI" FontSize="12" UseLayoutRounding="True"`
- **Layout Grids**:
  1. Row 0: Header with `Text.Title` ("VN-2000 → WGS84 → KMZ") and `SourceSummary`.
  2. Row 1: Embedded `<view:CrsSelectionView DataContext="{Binding Crs}"/>`.
  3. Row 2: Export output options:
     - Radio buttons for `OutputBoth`, `OutputPoints`, `OutputBoundaries`.
     - TextBoxes for `FileName` (with `SafeFileName`), `PointColor` (8-digit hex `aabbggrr`), and `LineColor`.
  4. Row 3: Preview section:
     - 2 columns separated by `GridSplitter`:
       - Column 0: `DataGrid` bound to `Preview` (`ItemsSource="{Binding Preview}"`). Columns: `#`, `E (m)`, `N (m)`, `Lat`, `Lon`, `Ghi chú`.
       - Column 2: `<view:MapPanel x:Name="MapView" Data="{Binding MapDataJson}"/>`.
     - Lower sub-row: KML preview text box bound to `KmlPreview` with visibility controlled by `{x:Static view:BoolToVisibilityConverter.Instance}`.
  5. Row 4: Status and validation:
     - `IssuesText` TextBlock with `Brush.Warning` and `{x:Static view:NonEmptyToVisibilityConverter.Instance}`.
     - `Status` TextBlock.
  6. Row 5: Action buttons (split into two horizontal bars):
     - Left bar: Satellite imagery inputs (`ImageResolutionText`, `ImageAreaRatioText`) and `Button Content="Chèn ảnh vệ tinh vào CAD"` (`Command="{Binding InsertImageCommand}"`).
     - Right bar: `Xem KML`, `Xuất KMZ` (Style: `{StaticResource Button.Primary}`), `Mở Google Earth`, `Google Maps`, `Đóng`.

#### Code-Behind (`HPGeoLink/View/GeoExportWindow.xaml.cs`)
```csharp
using System.Runtime.Loader;
using System.Windows;
using HPAutoCad.HPGeoLink.ViewModel;
using HPAutoCad.Resources.Themes;

namespace HPAutoCad.HPGeoLink.View;

public partial class GeoExportWindow : Window
{
    public GeoExportWindow(GeoExportViewModel viewModel)
    {
        using (AssemblyLoadContext.GetLoadContext(typeof(GeoExportWindow).Assembly)!.EnterContextualReflection())
        {
            Resources.MergedDictionaries.Add(ThemeResources.Styles());
            InitializeComponent();
            MaterialThemeBridge.Attach(this, AutocadHostTheme.Instance);
            MapView.DarkTheme = AutocadHostTheme.Instance.IsDark;
        }
        DataContext = viewModel;
        viewModel.CloseRequested += Close;
    }

    public bool MapEnabled
    {
        get => MapView.Visibility == Visibility.Visible;
        set => MapView.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
    }
}
```

### 4.2. `GeoImportWindow` (WPF View)

#### XAML Structure (`HPGeoLink/View/GeoImportWindow.xaml`)
- **Class**: `HPAutoCad.HPGeoLink.View.GeoImportWindow`
- **Title**: `HPAutoCad · Nhập toạ độ vào bản vẽ`
- **Layout**:
  - Source input selection:
    - RadioButton `File KML/KMZ` (`SourceIsFile`) with Browse button (`BrowseFileCommand`) and `FilePath` TextBox.
    - RadioButton `Dán toạ độ` (`SourceIsText`) with WGS84/VN-2000 mode toggle, `PairOrderIndex` ComboBox, `JoinAsPolyline`, `CloseRing` CheckBoxes, and multi-line TextBox for coordinates.
  - Coordinate system: `<view:CrsSelectionView DataContext="{Binding Crs}"/>`.
  - Preview `DataGrid` bound to `Preview` (`ImportPreviewRow` items: `#`, `Lat`, `Lon`, `E (m)`, `N (m)`, `Ghi chú`).
  - Action buttons: `Vẽ vào bản vẽ` (PrimaryButton style, `DrawCommand`, enabled when `CanDraw`), `Đóng` (`CloseCommand`).

#### Code-Behind (`HPGeoLink/View/GeoImportWindow.xaml.cs`)
```csharp
using System.Runtime.Loader;
using System.Windows;
using HPAutoCad.HPGeoLink.ViewModel;
using HPAutoCad.Resources.Themes;

namespace HPAutoCad.HPGeoLink.View;

public partial class GeoImportWindow : Window
{
    public GeoImportWindow(GeoImportViewModel viewModel)
    {
        using (AssemblyLoadContext.GetLoadContext(typeof(GeoImportWindow).Assembly)!.EnterContextualReflection())
        {
            Resources.MergedDictionaries.Add(ThemeResources.Styles());
            InitializeComponent();
            MaterialThemeBridge.Attach(this, AutocadHostTheme.Instance);
        }
        DataContext = viewModel;
        viewModel.CloseRequested += Close;
    }
}
```

### 4.3. `CrsSelectionView` (UserControl)

- **Class**: `HPAutoCad.HPGeoLink.View.CrsSelectionView`
- **DataContext**: Binds to `CrsSelectionViewModel`.
- **Elements**:
  - Catalog selection RadioButtons: 34 provinces (post-2025) vs 63 provinces (legacy).
  - Province ComboBox: `ItemsSource="{Binding Provinces}"`, `SelectedItem="{Binding SelectedProvince}"`, `IsEditable="True"`, `IsTextSearchEnabled="True"`.
  - Central Meridian ComboBox: `ItemsSource="{Binding Meridians}"`, `SelectedItem="{Binding SelectedMeridian}"`.
  - Manual Central Meridian TextBox: `CentralMeridianText` with `MeridianNote`.
  - Advanced Expander: `k0Text`, `falseEastingText`, `falseNorthingText`, and `Units` ComboBox (`SelectedUnit`).
- **Code-Behind**: Pure `InitializeComponent()`.

### 4.4. ViewModels

#### 1. `CrsSelectionViewModel` (`HPGeoLink/ViewModel/CrsSelectionViewModel.cs`)
- Subclasses `CommunityToolkit.Mvvm.ComponentModel.ObservableObject`.
- Properties:
  - `bool useCurrentCatalog` (defaults to true).
  - `ProvinceItem? selectedProvince`, `MeridianItem? selectedMeridian`.
  - `string centralMeridianText`, `string k0Text`, `string falseEastingText`, `string falseNorthingText`.
  - `UnitItem? selectedUnit`, `string meridianNote`.
  - `TmParameters CurrentTm` (derived from parsed text fields).
  - `double MetersPerUnit` (derived from `SelectedUnit`).
- Methods:
  - `Apply(...)`: Hydrates settings loaded from CAD NOD Xrecord (`HPGEO`) or user settings JSON.
  - Rebuilds province and meridian collections without infinite event recursion using `_suspend` guard.
  - Event `public event Action? Changed;` notified on any parameter modification.

#### 2. `GeoExportViewModel` (`HPGeoLink/ViewModel/GeoExportViewModel.cs` & `Commands.cs`)
- Manages export state:
  - Points (`IReadOnlyList<SurveyPoint>`) and boundaries (`IReadOnlyList<BoundaryPolyline>`).
  - Child ViewModel: `public CrsSelectionViewModel Crs { get; }`.
  - Injected dependency: `IGeoExportShell _shell`.
  - Collections: `ObservableCollection<PreviewRow> Preview`.
  - Properties: `FileName`, `PointColor`, `LineColor`, `OutputBoth`, `OutputPoints`, `OutputBoundaries`, `CanExport`, `Status`, `IssuesText`, `MapDataJson`, `KmlPreview`.
- Commands:
  - `[RelayCommand] void ToggleKmlPreview()`
  - `[RelayCommand] void ExportKmz()`
  - `[RelayCommand] void OpenGoogleEarth()`
  - `[RelayCommand] void OpenGoogleMaps()`
  - `[RelayCommand] void InsertImage()`: Sets `ImageChoice = new GeoImageChoice(...)` and triggers `CloseRequested`.
  - `[RelayCommand] void Close()`
- Event: `public event Action? CloseRequested;`

#### 3. `GeoImportViewModel` (`HPGeoLink/ViewModel/GeoImportViewModel.cs`)
- Manages import state:
  - Child ViewModel: `public CrsSelectionViewModel Crs { get; }`.
  - Injected dependency: `IGeoImportShell _shell`.
  - Input modes: `SourceIsFile` vs `SourceIsText` (`PastedText`, `PastedIsWgs84`, `PairOrderIndex`, `JoinAsPolyline`, `CloseRing`).
  - Output property: `ImportPlan? Result`.
  - Preview: `ObservableCollection<ImportPreviewRow> Preview`.
- Commands:
  - `[RelayCommand] void BrowseFile()`: Invokes `_shell.AskOpenPath()`.
  - `[RelayCommand] void Draw()`: Assigns `Result = CurrentPlan` and invokes `CloseRequested`.
  - `[RelayCommand] void Close()`

#### 4. Shell Interfaces (`IGeoExportShell.cs`, `IGeoImportShell.cs`)
```csharp
namespace HPAutoCad.HPGeoLink.ViewModel;

public interface IGeoExportShell
{
    string? AskSavePath(string? initialDirectory, string suggestedFileName);
    void OpenPath(string path);
    string OpenInGoogleEarth(string kmzPath);
    void OpenUrl(string url);
}

public interface IGeoImportShell
{
    string? AskOpenPath();
}
```

### 4.5. Model Records (`HPGeoLink/Model/GeoExportItems.cs` & `GeoImportItems.cs`)

```csharp
namespace HPAutoCad.HPGeoLink.Model;

public sealed record ProvinceItem(Province Province)
{
    public string Name => Province.Name;
    public string Hint => Province.HasSingleMeridian
        ? CentralMeridian.Format(Province.CentralMeridians[0])
        : string.Join(" · ", Province.CentralMeridians.Select(CentralMeridian.Format)) + " (chọn KTT)";
    public override string ToString() => Name;
}

public sealed record MeridianItem(double Degrees, string FormerProvince)
{
    public string Label => FormerProvince.Length == 0
        ? CentralMeridian.Format(Degrees)
        : $"{CentralMeridian.Format(Degrees)} — {FormerProvince}";
    public override string ToString() => Label;
}

public sealed record UnitItem(DrawingUnit Unit, bool FromDrawing)
{
    public string Label => DrawingUnitFactor.Label(Unit) + (FromDrawing ? " — INSUNITS" : "");
    public override string ToString() => Label;
}

public sealed record PreviewRow(string Label, string Easting, string Northing, string Lat, string Lon, string Note);

public sealed record ImportPreviewRow(string Label, string Lat, string Lon, string Easting, string Northing, string Note);

public sealed record GeoImageChoice(
    TmParameters Tm,
    DrawingUnit Unit,
    double MetersPerUnit,
    double ResolutionMPerPx,
    double AreaRatio,
    double MarginM,
    GridBoundingBox ExtentDrawingUnits,
    int PointCount,
    int BoundaryCount);
```

---

## 5. WebView2 MapPanel & Leaflet Satellite Integration

### 5.1. The Component Architecture
`MapPanel` hosts a real-time web map rendering satellite imagery under the survey boundaries.
- **XAML (`HPGeoLink/View/MapPanel.xaml`)**:
  ```xml
  <UserControl x:Class="HPAutoCad.HPGeoLink.View.MapPanel"
               xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
               xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
               xmlns:wv2="clr-namespace:Microsoft.Web.WebView2.Wpf;assembly=Microsoft.Web.WebView2.Wpf">
      <Grid>
          <Border BorderBrush="{DynamicResource Brush.Border}" BorderThickness="1" Background="{DynamicResource Brush.Input}">
              <wv2:WebView2 x:Name="Browser" Visibility="Collapsed"/>
          </Border>
          <TextBlock x:Name="Fallback" Margin="12" TextWrapping="Wrap" VerticalAlignment="Center" HorizontalAlignment="Center" TextAlignment="Center"
                     Foreground="{DynamicResource Brush.TextMuted}" Text="Đang tải bản đồ…"/>
      </Grid>
  </UserControl>
  ```

### 5.2. Runtime Resilience & Lifecycle Management

1. **User Data Folder Isolation**:
   AutoCAD runs from `C:\Program Files\Autodesk\AutoCAD 2026\`, which is protected and read-only for standard users. Initializing WebView2 with default options will crash AutoCAD with an access denied error.
   `MapPanel` strictly directs user data to a local AppData directory:
   ```csharp
   private static readonly string UserDataFolder =
       Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HPAutoCad", "webview2");
   ```
2. **Crash-Proof Startup**:
   If the WebView2 Runtime is missing (`WebView2RuntimeNotFoundException`), an old version is present, or a firewall blocks the socket:
   - The exception is caught and logged via `HPGeoLog.Error`.
   - The `Browser` element is kept `Visibility.Collapsed`.
   - The `Fallback` label is updated: `"Bản đồ không khả dụng (...). Dùng nút Mở Google Earth / Google Maps."`.
   - **Crucial Rule**: WebView2 failure must NEVER crash the modal dialog or AutoCAD!
3. **Subresource Integrity (SRI) & Leaflet 1.9.4**:
   `MapHtml.cs` embeds Leaflet from unpkg with SHA-384 cryptographic integrity hashes.
   - Tiles are served from `https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}`.
   - Custom markers for points (yellow circles `#FFD400`) and boundaries (red polygons `#FF3030`).
   - Dynamic boundary fitting: `map.fitBounds(bounds, { padding: [24, 24], maxZoom: 19 })`.
4. **Clean Teardown**:
   On `Unloaded`, `Browser.CoreWebView2.WebMessageReceived` is unhooked and `Browser.Dispose()` is called within a try-catch block to swallow late COM disposal race conditions.

---

## 6. Theming Architecture & AutoCAD COLORTHEME Integration

### 6.1. Design Philosophy
AutoCAD provides a dark/light mode via the `COLORTHEME` system variable (0 = Dark, 1 = Light).
HPAutoCad themes are implemented using **MaterialDesignThemes 5.3.2**, following the proven repository pattern from `HPRebar` and `HPAutoCad.McpBridge`:
- **HP Palette Dominance**: MaterialDesign styles follow the HP palette tokens, never the inverse.
- **Top-Level Overlay Swapping**: `MaterialThemeBridge` replaces an overlay dictionary at index 0 of `window.Resources.MergedDictionaries`. Mutating nested dictionaries fails to trigger repaint on visible WPF controls in AutoCAD.
- **Independence from Application.Current**: Add-ins in AutoCAD have no control over `Application.Current.Resources`. All theming operates strictly at the `Window.Resources` level.

### 6.2. Theming Files in `Resources/Themes/`

#### 1. `ThemeInfo.cs`
```csharp
using System.Windows;

[assembly: ThemeInfo(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly)]
```
*Purpose*: Because MaterialDesignThemes is ILRepacked into `HPAutoCad.dll`, `Themes/Generic.xaml` becomes `HPAutoCad.dll`'s generic dictionary. WPF requires this assembly attribute to look up default control styles (`PackIcon`, `Card`, `Expander`).

#### 2. `IHostTheme.cs` & `AutocadHostTheme.cs`
- `IHostTheme`: Defines `bool IsDark { get; }` and `event Action? Changed;`.
- `AutocadHostTheme`: Singleton listening to `Autodesk.AutoCAD.ApplicationServices.Core.Application.SystemVariableChanged`. When `args.Name == "COLORTHEME"`, fires `Changed`.
```csharp
using HPAutoCad.HPGeoLink.Service;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPAutoCad.Resources.Themes;

public sealed class AutocadHostTheme : IHostTheme
{
    public static AutocadHostTheme Instance { get; } = new();

    private AutocadHostTheme()
    {
        AcadApp.SystemVariableChanged += (_, args) =>
        {
            if (string.Equals(args.Name, "COLORTHEME", StringComparison.OrdinalIgnoreCase)) Changed?.Invoke();
        };
    }

    public bool IsDark
    {
        get
        {
            try
            {
                return Convert.ToInt32(AcadApp.GetSystemVariable("COLORTHEME")) == 0;
            }
            catch (Exception exception)
            {
                HPGeoLog.Warning("COLORTHEME unreadable; assuming dark: " + exception.Message);
                return true;
            }
        }
    }

    public event Action? Changed;
}
```

#### 3. Palettes (`ThemeDark.xaml` & `ThemeLight.xaml`)
- Standard HP color tokens:
  - `Brush.Background` (`#2B2B2B` Dark / `#F3F3F3` Light)
  - `Brush.Surface` (`#353535` Dark / `#E8E8E8` Light)
  - `Brush.Input` (`#1F1F1F` Dark / `#FFFFFF` Light)
  - `Brush.Border` (`#4A4A4A` Dark / `#BDBDBD` Light)
  - `Brush.Text` (`#E6E6E6` Dark / `#1E1E1E` Light)
  - `Brush.TextMuted` (`#A0A0A0` Dark / `#5F5F5F` Light)
  - `Brush.Accent` (`#0696D7`)
  - `Brush.AccentText` (`#FFFFFF`)
  - `Brush.Danger`, `Brush.Warning`, `Brush.Success`
  - Toolkit bridge tokens: `Brush.Foreground.Primary`, `Brush.Foreground.Secondary`, `Brush.SurfaceElevated`.

#### 4. `MaterialBridge.xaml`
Bootstrap linking the MaterialDesign2 defaults:
```xml
<ResourceDictionary
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:md="http://materialdesigninxaml.net/winfx/xaml/themes">

    <ResourceDictionary.MergedDictionaries>
        <md:CustomColorTheme BaseTheme="Dark" PrimaryColor="#0696D7" SecondaryColor="#E0641E"/>
        <ResourceDictionary Source="pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesign2.Defaults.xaml"/>
    </ResourceDictionary.MergedDictionaries>

    <FontFamily x:Key="MaterialDesignFont">Segoe UI</FontFamily>
</ResourceDictionary>
```

#### 5. `Theme.xaml`
Merges `/HPAutoCad;component/Resources/Themes/MaterialBridge.xaml`, declares layout tokens (`Spacing.XS` = 4, `Spacing.S` = 8, `Spacing.M` = 16, `Margin.Row`, `Margin.Section`), and control styles:
- `TextBlock` (default, `Text.Muted`, `Text.Title`, `Text.Section`).
- `Button` (default outlined, `Button.Primary` raised).
- `RadioButton` based on `MaterialDesignRadioButton`.
- `Expander` based on `MaterialDesignExpander` with compact padding (`md:ExpanderAssist.HorizontalHeaderPadding="0,6"`).
- `DataGrid` based on `MaterialDesignDataGrid` with custom `DataGrid.ColumnHeader` and `DataGrid.Cell` styling.

#### 6. `MaterialThemeBridge.cs`
- Dynamically resolves assembly name (`HPAutoCad`).
- `Attach(Window window, IHostTheme host)`: Hooks `host.Changed`, marshals to `window.Dispatcher`, and unhooks on `window.Closed`.
- `Apply(Window window, bool dark)`: Wraps execution in `EnterContextualReflection()`, creates `Theme.Create(dark ? BaseTheme.Dark : BaseTheme.Light, ...)`, and replaces `OverlayMarker` in `window.Resources.MergedDictionaries`.

---

## 7. ALC Contextual Reflection Pattern (`EnterContextualReflection`)

### 7.1. The Root Cause in .NET 8 Isolated Load Contexts
In AutoCAD 2026 (.NET 8), `HPAutoCad.dll` is loaded into a dedicated, isolated `AssemblyLoadContext` (`AppLoadContext`) to prevent DLL version conflicts with other plugins.
When a WPF Window executes:
1. `InitializeComponent()` loads BAML streams and encounters pack URIs (`pack://application:,,,/HPAutoCad;component/...`) or references to custom controls and converters in XML namespaces.
2. WPF resolves the assembly by calling `Assembly.Load("HPAutoCad")`.
3. In .NET 8, `Assembly.Load` evaluates the **Default Load Context** (`AssemblyLoadContext.Default`).
4. Because `HPAutoCad.dll` is in `AppLoadContext` and NOT in the Default ALC, `Assembly.Load` throws `FileNotFoundException` or `XamlParseException: Could not load file or assembly 'HPAutoCad'`.

### 7.2. The Proven Solution: `EnterContextualReflection`
.NET Core provides `AssemblyLoadContext.EnterContextualReflection()`, which sets `AssemblyLoadContext.CurrentContextualReflectionContext` on the thread.
When `EnterContextualReflection()` is active:
- Any call to `Assembly.Load` made by WPF BAML parsers automatically resolves types against the isolated `AssemblyLoadContext`!
- The Window constructor must wrap resource dictionary merging, `InitializeComponent()`, and theme attachment in this scope:

```csharp
using (AssemblyLoadContext.GetLoadContext(typeof(GeoExportWindow).Assembly)!.EnterContextualReflection())
{
    Resources.MergedDictionaries.Add(ThemeResources.Styles());
    InitializeComponent();
    MaterialThemeBridge.Attach(this, AutocadHostTheme.Instance);
    MapView.DarkTheme = AutocadHostTheme.Instance.IsDark;
}
```

- Likewise, inside `MaterialThemeBridge.Apply`:
```csharp
#if NETCOREAPP
using var reflectionScope = AssemblyLoadContext.GetLoadContext(typeof(MaterialThemeBridge).Assembly)?.EnterContextualReflection();
#endif
```
This ensures that when AutoCAD changes `COLORTHEME` while a dialog is open, the background dispatcher thread loading `ThemeDark.xaml` / `ThemeLight.xaml` pack URIs continues to resolve within the ALC without throwing.

---

## 8. Concrete Step-by-Step Implementation Instructions for Worker

The worker implementing M2 UI should execute the following phases in exact order:

### Phase 1: Directory Setup
Create the directory structure inside `HPAutoCad/HPAutoCad/`:
```bash
HPAutoCad/HPAutoCad/
├── HPGeoLink/
│   ├── Model/
│   ├── Service/
│   ├── View/
│   └── ViewModel/
└── Resources/
    └── Themes/
```

### Phase 2: Model & Shell Interfaces
1. Create `HPAutoCad/HPAutoCad/HPGeoLink/Model/GeoExportItems.cs`:
   - Namespace: `HPAutoCad.HPGeoLink.Model`.
   - Records: `ProvinceItem`, `MeridianItem`, `UnitItem`, `PreviewRow`, `GeoImageChoice`.
2. Create `HPAutoCad/HPAutoCad/HPGeoLink/Model/GeoImportItems.cs`:
   - Namespace: `HPAutoCad.HPGeoLink.Model`.
   - Record: `ImportPreviewRow`.
3. Create `HPAutoCad/HPAutoCad/HPGeoLink/ViewModel/IGeoExportShell.cs`:
   - Methods: `AskSavePath`, `OpenPath`, `OpenInGoogleEarth`, `OpenUrl`.
4. Create `HPAutoCad/HPAutoCad/HPGeoLink/ViewModel/IGeoImportShell.cs`:
   - Method: `AskOpenPath`.

### Phase 3: Theming Layer (`Resources/Themes/`)
1. Create `Resources/Themes/ThemeInfo.cs`:
   - `[assembly: ThemeInfo(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly)]`.
2. Create `Resources/Themes/IHostTheme.cs`:
   - Namespace `HPAutoCad.Resources.Themes`.
3. Create `Resources/Themes/AutocadHostTheme.cs`:
   - Implements `IHostTheme`, monitors `COLORTHEME` via `AcadApp.SystemVariableChanged`.
4. Create `Resources/Themes/ThemeDark.xaml` & `Resources/Themes/ThemeLight.xaml`:
   - Palette definitions with `Brush.*` and `Brush.Foreground.*` keys.
5. Create `Resources/Themes/MaterialBridge.xaml`:
   - `CustomColorTheme` and `MaterialDesign2.Defaults.xaml`.
6. Create `Resources/Themes/Theme.xaml`:
   - Merges `/HPAutoCad;component/Resources/Themes/MaterialBridge.xaml`.
   - Defines spacing tokens and control styles for TextBlock, Button, RadioButton, Expander, DataGrid.
7. Create `Resources/Themes/MaterialThemeBridge.cs`:
   - `PaletteFolder = "Resources/Themes"`.
   - Top-level dictionary swap with `EnterContextualReflection()`.
8. Create `Resources/Themes/ThemeResources.cs`:
   - Returns `/HPAutoCad;component/Resources/Themes/Theme.xaml`.

### Phase 4: ViewModels (`HPGeoLink/ViewModel/`)
1. Create `HPGeoLink/ViewModel/CrsSelectionViewModel.cs`:
   - Subclasses `ObservableObject`.
   - Catalog management (34 current / 63 legacy) and TM calculation.
2. Create `HPGeoLink/ViewModel/GeoExportViewModel.cs`:
   - Pure state coordination, `Preview` population, `BuildMapData` JSON creation.
3. Create `HPGeoLink/ViewModel/GeoExportViewModel.Commands.cs`:
   - RelayCommands: `ToggleKmlPreview`, `ExportKmz`, `OpenGoogleEarth`, `OpenGoogleMaps`, `InsertImage`, `Close`.
4. Create `HPGeoLink/ViewModel/GeoImportViewModel.cs`:
   - File & text import planner execution, `Preview` population, `Draw`, `BrowseFile`, `Close`.

### Phase 5: Converters, MapHtml & MapPanel
1. Create `HPGeoLink/View/ValueConverters.cs`:
   - `InverseBoolConverter`, `BoolToVisibilityConverter`, `NonEmptyToVisibilityConverter`.
2. Create `HPGeoLink/View/MapHtml.cs`:
   - Leaflet 1.9.4 HTML generator with SRI hashes and message bridge.
3. Create `HPGeoLink/View/MapPanel.xaml` & `MapPanel.xaml.cs`:
   - UserControl with `Microsoft.Web.WebView2.Wpf.WebView2`.
   - User-data folder `%LocalAppData%\HPAutoCad\webview2`.
   - Fail-safe fallback TextBlock and safe disposal on `Unloaded`.

### Phase 6: UserControls & Windows
1. Create `HPGeoLink/View/CrsSelectionView.xaml` & `CrsSelectionView.xaml.cs`:
   - XML namespace mapping: `xmlns:view="clr-namespace:HPAutoCad.HPGeoLink.View"`.
2. Create `HPGeoLink/View/GeoExportWindow.xaml` & `GeoExportWindow.xaml.cs`:
   - XML namespace mapping: `xmlns:view="clr-namespace:HPAutoCad.HPGeoLink.View"`, `xmlns:vm="clr-namespace:HPAutoCad.HPGeoLink.ViewModel"`.
   - Constructor wrapped in `using (...EnterContextualReflection())`.
3. Create `HPGeoLink/View/GeoImportWindow.xaml` & `GeoImportWindow.xaml.cs`:
   - XML namespace mapping and ALC contextual reflection wrapper.

### Phase 7: Logging & Supporting Service
1. Create `HPGeoLink/Service/HPGeoLog.cs`:
   - Logging to `%LocalAppData%\HPGeo\logs\hpgeo-YYYYMMDD.log` for acceptance harness compatibility.
2. Create `HPGeoLink/Service/GoogleEarthLauncher.cs`:
   - Detection of `googleearth.exe` and process launch helper.

---

## 9. Verification Matrix & Acceptance Criteria

| Check | Item | Method | Expected Result |
|---|---|---|---|
| **V1** | Static Compilation | `dotnet build HPAutoCad/HPAutoCad.csproj` | 0 errors, 0 warnings. BAML compilation passes without missing resource keys. |
| **V2** | ALC Isolation | Inspect BAML pack URIs | All component URIs reference `/HPAutoCad;component/...`. |
| **V3** | Repack Integrity | Check ILRepack on `HPAutoCad.dll` | `MaterialDesignThemes.Wpf.dll` merged into `HPAutoCad.dll`. No loose toolkit DLLs in output. |
| **V4** | ThemeInfo Attribute | Reflection audit | `ThemeInfoAttribute` present on `HPAutoCad.dll` with `SourceAssembly` generic dictionary. |
| **V5** | Unattended Dialog Harness | `tools/harness/run-bridge-unattended.ps1` | `HPGEO` and `HPGEOIMPORT` dialogs open off-screen, render UI controls, and close cleanly without freezing AutoCAD. |
| **V6** | Dynamic Theming Flip | Flip `COLORTHEME` 0 ↔ 1 live | Dialog background, text, and MaterialDesign controls update colors on the fly without XAML parsing errors. |
| **V7** | Offline Resilience | Disconnect network in VM / sandbox | `MapPanel` displays graceful fallback message; export and import commands function at 100%. |
