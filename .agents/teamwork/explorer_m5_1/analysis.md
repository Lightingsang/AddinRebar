# Architectural Analysis: AutoCAD 2026 (.NET 8) Add-In Architecture for HPKata.AutoCad

- **Milestone**: M5 — Dual-Host AutoCAD 2026 Architecture & Vertical Slice Design
- **Document ID**: `HPKATA-ARCH-M5-AUTOCAD2026-001`
- **Author**: `explorer_m5_1` (Teamwork Explorer)
- **Target Deliverable**: `HPKata/HPKata.AutoCad/HPKata.AutoCad.csproj`
- **Compliance**: `docs/clean-code/HP_CLEAN_CODE_CORE.md`, `docs/clean-code/host-appendix/autocad-civil.md`, `HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md`

---

## 1. Executive Summary

This investigation establishes the technical architecture for `HPKata.AutoCad`, the AutoCAD 2026 add-in deliverable of the **HPKata** suite. AutoCAD 2026 operates on **.NET 8** (`net8.0-windows`), introducing modern managed capabilities alongside strict execution boundaries (isolated AssemblyLoadContext, `doc.LockDocument()` requirements for out-of-band WPF interactions, and assembly unification).

The proposed architecture accomplishes four primary objectives:
1. **Clean Project Configuration**: Configures `HPKata.AutoCad.csproj` with target framework `net8.0-windows`, referencing `AutoCAD.NET 25.1.0` with runtime assets excluded, and establishing clean references to `HPKata.Core` and `HPKata.UI`.
2. **Robust Lifecycle & RegApp Management**: Implements `IExtensionApplication` (`Initialize` and `Terminate`) with automatic database initialization ensuring RegApp `"KATA"` is registered across all opened and newly created drawings via `DocumentManager` event hooks, coupled with dynamic Ribbon/Menu integration that synchronizes with AutoCAD's theme (`COLORTHEME`) and workspace (`WSCURRENT`).
3. **Safe WPF Host Integration**: Bridges `HPKata.UI` views (`SetupView`, `DetailingView`, `ScheduleView`, `CuttingStockView`) into AutoCAD via both modal (`Application.ShowModalWindow`) and modeless (`Application.ShowModelessWindow`) patterns. Includes dynamic dark/light palette synchronization via `AutocadHostTheme : IHostTheme` and `ThemeManager`, contextual reflection scopes, and strict document locking (`doc.LockDocument()`) to eliminate CAD threading deadlocks.
4. **Modular Clean Code Partitioning**: Partitions commands across 6 dedicated command entry files and 7 domain controllers, ensuring zero classes exceed 300 lines (Rule C2), zero methods exceed 50 lines (Rule M3), all AutoCAD wrappers are deterministically disposed (Rule A2), and drawing commands maintain undo integrity (Rule A15).

---

## 2. Project Configuration (`HPKata.AutoCad.csproj`)

### 2.1 Target Framework & Compiler Settings
AutoCAD 2026 (release 25.1) is built natively on .NET 8. The add-in project must target `net8.0-windows` with WPF enabled:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UseWPF>true</UseWPF>
    <RootNamespace>HPKata.AutoCad</RootNamespace>
    <AssemblyName>HPKata.AutoCad</AssemblyName>
    <Configurations>Debug;Release</Configurations>
    <Version>1.0.0</Version>
    
    <!-- Required for AssemblyDependencyResolver and isolated dynamic loading -->
    <EnableDynamicLoading>true</EnableDynamicLoading>
    <SatelliteResourceLanguages>en</SatelliteResourceLanguages>
    
    <!-- AutoCAD 2026 API Version -->
    <AutocadPackageVersion Condition="'$(AutocadPackageVersion)' == ''">[25.1.0]</AutocadPackageVersion>
    
    <!-- Bundle Deployment Paths -->
    <DeployBundle Condition="'$(DeployBundle)' == ''">true</DeployBundle>
    <BundleDir>$([System.Environment]::GetFolderPath(SpecialFolder.ApplicationData))\Autodesk\ApplicationPlugins\HPKata.bundle\</BundleDir>
  </PropertyGroup>
```

### 2.2 Package References & Dependency Isolation
The package reference to `AutoCAD.NET` must enforce `ExcludeAssets="runtime" PrivateAssets="all"`:
- **Critical Justification**: `AutoCAD.NET` brings `AcCoreMgd.dll`, `AcDbMgd.dll`, and `AcMgd.dll`. These managed assemblies are hosted directly in memory by `acad.exe`. If copied to the build output or load folder, the CLR runtime faces type duplication and invalid cast exceptions between the host's copy and the plugin's local copy.
- **WPF & UI Packages**: Packages `CommunityToolkit.Mvvm`, `MaterialDesignThemes`, and `MaterialDesignColors` match the exact versions defined in `HPKata.UI` (5.2.1 / 8.4.0) to prevent BAML type resolution collisions.

```xml
  <ItemGroup>
    <!-- AutoCAD 2026 Managed API - Compile-time only, never copy to output -->
    <PackageReference Include="AutoCAD.NET" Version="$(AutocadPackageVersion)" ExcludeAssets="runtime" PrivateAssets="all" />
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.0" />
    <PackageReference Include="MaterialDesignThemes" Version="5.2.1" />
    <PackageReference Include="MaterialDesignColors" Version="5.2.1" />
  </ItemGroup>

  <ItemGroup>
    <!-- Domain Core (Pure C#) and Shared WPF MVVM Library -->
    <ProjectReference Include="..\HPKata.Core\HPKata.Core.csproj" />
    <ProjectReference Include="..\HPKata.UI\HPKata.UI.csproj" />
  </ItemGroup>
```

### 2.3 Application Bundle Packaging (`PackageContents.xml`)
AutoCAD automatically discovers and registers plugins placed in `%AppData%\Autodesk\ApplicationPlugins\`.
The bundle manifest `PackageContents.xml` registers `HPKata.AutoCad.dll` and companion AutoLISP commands `GoiLenh_HPKata.lsp` across AutoCAD 2025 and 2026:

```xml
<?xml version="1.0" encoding="utf-8"?>
<ApplicationPackage 
    SchemaVersion="1.0" 
    AutodeskProduct="AutoCAD" 
    ProductType="Application" 
    Name="HPKata Rebar Suite" 
    AppVersion="1.0.0" 
    Author="HP Team" 
    ProductCode="{B7A5D2E1-4321-4F98-A1B2-C3D4E5F60001}" 
    UpgradeCode="{C8B6E3F2-5432-5A09-B2C3-D4E5F6A70002}">
  
  <CompanyDetails Name="HP Team" Url="https://github.com/hp-team" />
  
  <Components Description="AutoCAD 2025-2026 .NET 8 Components">
    <RuntimeRequirements OS="Win64" Platform="AutoCAD*" SeriesMin="R25.0" SeriesMax="R25.1" />
    <ComponentEntry 
        AppName="HPKata" 
        Version="1.0.0" 
        ModuleName="./Contents/HPKata.AutoCad.dll" 
        AppDescription="HPKata Structural Rebar Detailing, Linking & Scheduling" 
        LoadOnAutoCADStartup="True">
      <Commands GroupName="HPKATA_CMDS">
        <!-- Setup & Rebar Creation -->
        <Command Local="setupkata" Global="setupkata" />
        <Command Local="setupthep" Global="setupthep" />
        <Command Local="ve_thep" Global="ve_thep" />
        <Command Local="vt" Global="vt" />
        <Command Local="taothep" Global="taothep" />
        <Command Local="sua_thep" Global="sua_thep" />
        <Command Local="sua" Global="sua" />
        <!-- Linking & Tags -->
        <Command Local="tao_lien_ket" Global="tao_lien_ket" />
        <Command Local="link" Global="link" />
        <Command Local="update_thep" Global="update_thep" />
        <Command Local="ud" Global="ud" />
        <Command Local="xem_lien_ket" Global="xem_lien_ket" />
        <Command Local="tim" Global="tim" />
        <!-- Schedule & Optimization -->
        <Command Local="thong_ke_thep" Global="thong_ke_thep" />
        <Command Local="tkt" Global="tkt" />
        <Command Local="to_hop_thep" Global="to_hop_thep" />
        <Command Local="tohopthep" Global="tohopthep" />
      </Commands>
    </ComponentEntry>
    
    <!-- AutoLISP Companion Loader -->
    <ComponentEntry 
        AppName="HPKataLisp" 
        Version="1.0.0" 
        ModuleName="./Contents/GoiLenh_HPKata.lsp" 
        AppDescription="HPKata AutoLISP 100% Command Alias Parity" 
        LoadOnAutoCADStartup="True" />
  </Components>
</ApplicationPackage>
```

### 2.4 MSBuild Automated Deployment with File-Lock Protection
When compiling in `Debug`, the project copies outputs directly to the bundle location. A file lock probe avoids crashing `dotnet build` when AutoCAD is running:

```xml
  <Target Name="DeployBundle" AfterTargets="Build" Condition="'$(DeployBundle)' == 'true' And '$(Configuration)' == 'Debug' And '$(DesignTimeBuild)' != 'true'">
    <PropertyGroup>
      <_TargetDll>$(BundleDir)Contents\HPKata.AutoCad.dll</_TargetDll>
    </PropertyGroup>
    <Delete Files="$(_TargetDll)" ContinueOnError="true" />
    <PropertyGroup>
      <_IsLocked Condition="Exists('$(_TargetDll)')">true</_IsLocked>
    </PropertyGroup>
    <Warning Condition="'$(_IsLocked)' == 'true'" Text="AutoCAD is running and locking $(_TargetDll). Skipped bundle overwrite; bin output is updated." />
    
    <ItemGroup Condition="'$(_IsLocked)' != 'true'">
      <_BundleFiles Include="$(TargetDir)**\*" />
      <_Manifest Include="PackageContents.xml" />
      <_LispFile Include="GoiLenh_HPKata.lsp" />
    </ItemGroup>
    <Copy SourceFiles="@(_Manifest)" DestinationFolder="$(BundleDir)" Condition="'$(_IsLocked)' != 'true'" SkipUnchangedFiles="true" />
    <Copy SourceFiles="@(_LispFile)" DestinationFolder="$(BundleDir)Contents\" Condition="'$(_IsLocked)' != 'true'" SkipUnchangedFiles="true" />
    <Copy SourceFiles="@(_BundleFiles)" DestinationFiles="@(_BundleFiles->'$(BundleDir)Contents\%(RecursiveDir)%(Filename)%(Extension)')" Condition="'$(_IsLocked)' != 'true'" SkipUnchangedFiles="true" />
  </Target>
</Project>
```

---

## 3. Lifecycle Management & Database Initialization

### 3.1 `IExtensionApplication` Implementation
AutoCAD requires an entry class implementing `Autodesk.AutoCAD.Runtime.IExtensionApplication`, registered via the assembly attribute:

```csharp
[assembly: ExtensionApplication(typeof(HPKata.AutoCad.Application.HPKataExtensionApplication))]
```

The lifecycle contract encapsulates:
1. `Initialize()`: Executed once when AutoCAD loads `HPKata.AutoCad.dll`.
2. `Terminate()`: Executed when AutoCAD unloads the extension or shuts down.

```csharp
namespace HPKata.AutoCad.Application;

public sealed class HPKataExtensionApplication : IExtensionApplication
{
    public static HPKataExtensionApplication Instance { get; private set; } = null!;
    public RegAppManager RegApp { get; private set; } = null!;
    public CadDocumentEvents DocEvents { get; private set; } = null!;

    public void Initialize()
    {
        Instance = this;
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        var ed = doc?.Editor;
        ed?.WriteMessage("\n[HPKata] Initializing HPKata Rebar Suite (AutoCAD 2026 .NET 8)...\n");

        // 1. Initialize RegApp Manager & Document Event Listeners
        RegApp = new RegAppManager();
        DocEvents = new CadDocumentEvents(RegApp);
        DocEvents.Register();

        // 2. Ensure RegApp "KATA" on current active document
        if (doc != null)
        {
            RegApp.EnsureRegistered(doc.Database);
        }

        // 3. Initialize Host Theme Watcher
        _ = AutocadHostTheme.Instance;

        // 4. Initialize Ribbon UI
        HPKataRibbonTab.Install();

        ed?.WriteMessage("[HPKata] Loaded successfully. Type 'setupkata' or 'vt' to start.\n");
    }

    public void Terminate()
    {
        // 1. Teardown Ribbon UI
        HPKataRibbonTab.Uninstall();

        // 2. Unregister Document Events
        DocEvents.Unregister();

        // 3. Clear Highlights and Active Transient Overlays
        CadHighlightService.ClearAllHighlights();

        // 4. Close any open modeless dialogs
        CadDialogService.CloseAllModeless();
    }
}
```

### 3.2 Dynamic RegApp `"KATA"` Registration Across All Documents
KataPro attaches all dynamic linking, shape data, and bar attributes via AutoCAD XData records stamped with `RegApp = "KATA"`.
- If an entity attempt to attach XData before `"KATA"` exists in the database's `RegAppTable`, AutoCAD throws `ErrorStatus.BadDxfHeader` or `ErrorStatus.RegappIdNotFound`.
- Drawings can be created at runtime (`DocumentCreated`) or opened dynamically (`DocumentActivated`). Therefore, `RegAppManager` must guarantee registration across the active drawing and every newly created or switched document.

```csharp
namespace HPKata.AutoCad.Application;

public sealed class RegAppManager
{
    public const string KataRegAppName = "KATA";

    public bool EnsureRegistered(Database db)
    {
        if (db == null) return false;

        using var tr = db.TransactionManager.StartTransaction();
        var regTable = (RegAppTable)tr.GetObject(db.RegAppTableId, OpenMode.ForRead);
        if (regTable.Has(KataRegAppName))
        {
            return true;
        }

        regTable.UpgradeOpen();
        using var regRecord = new RegAppTableRecord { Name = KataRegAppName };
        regTable.Add(regRecord);
        tr.AddNewlyCreatedDBObject(regRecord, true);
        tr.Commit();
        return true;
    }
}
```

The document event listener ensures no DWG database misses registration:

```csharp
namespace HPKata.AutoCad.Application;

public sealed class CadDocumentEvents
{
    private readonly RegAppManager _regApp;
    private bool _isRegistered;

    public CadDocumentEvents(RegAppManager regApp) => _regApp = regApp;

    public void Register()
    {
        if (_isRegistered) return;
        _isRegistered = true;
        AcadApp.DocumentManager.DocumentCreated += OnDocumentCreated;
        AcadApp.DocumentManager.DocumentActivated += OnDocumentActivated;
    }

    public void Unregister()
    {
        if (!_isRegistered) return;
        _isRegistered = false;
        AcadApp.DocumentManager.DocumentCreated -= OnDocumentCreated;
        AcadApp.DocumentManager.DocumentActivated -= OnDocumentActivated;
    }

    private void OnDocumentCreated(object? sender, DocumentCollectionEventArgs e)
    {
        if (e.Document?.Database != null)
        {
            _regApp.EnsureRegistered(e.Document.Database);
        }
    }

    private void OnDocumentActivated(object? sender, DocumentCollectionEventArgs e)
    {
        if (e.Document?.Database != null)
        {
            _regApp.EnsureRegistered(e.Document.Database);
        }
    }
}
```

### 3.3 Dynamic Ribbon & Menu Integration
Following the repository's AutoCAD Ribbon standard (`Rule A14`):
1. Creates a dedicated Ribbon Tab: `HPKATA_TAB` (`"HPKata"`).
2. Four cohesive panels:
   - **Panel 1: Cấu hình (Setup)**: `setupkata` (Setup master), `setupthep` (Rebar parameters).
   - **Panel 2: Vẽ & Sửa Thép (Draw & Edit)**: `ve_thep` (`vt`), `taothep`, `sua_thep` (`sua`), `sua_moc_dai` (`smd`), `neo_thep` (`neo`), `cat_thep` (`cut`), `dimthep`.
   - **Panel 3: Liên Kết (Link & Track)**: `tao_lien_ket` (`link`), `copy_tao_lien_ket` (`cl`), `xoa_lien_ket` (`dl`), `xem_lien_ket` (`tim`), `update_thep` (`ud`), `ghi_thep` (`gt`), `check_lap`.
   - **Panel 4: Thống Kê & Cắt Thép (Schedule & 1D-CSP)**: `thong_ke_thep` (`tkt`), `sua_thong_ke` (`suatk`), `tong_hop_thep` (`ththep`), `thong_ke_coupler` (`tkco`), `to_hop_thep` (`tohopthep`), `toexcel`, `in_tem_thep` (`itt`).
3. Handles `WSCURRENT` (workspace changes) and `COLORTHEME` (dark/light switching) via `Application.SystemVariableChanged` and defers UI rebuilds to `Application.Idle` to prevent UI thread lockups.

---

## 4. WPF Host Integration & Dialog Hosting

### 4.1 Host Theme Synchronization (`AutocadHostTheme`)
AutoCAD toggles themes via the system variable `"COLORTHEME"` (0 = Dark, 1 = Light).
`AutocadHostTheme` implements `HPKata.UI.Common.IHostTheme`:

```csharp
namespace HPKata.AutoCad.Hosting;

public sealed class AutocadHostTheme : IHostTheme
{
    public static AutocadHostTheme Instance { get; } = new();

    private AutocadHostTheme()
    {
        AcadApp.SystemVariableChanged += (_, args) =>
        {
            if (string.Equals(args.Name, "COLORTHEME", StringComparison.OrdinalIgnoreCase))
            {
                Changed?.Invoke();
            }
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
            catch
            {
                return true; // Default to Dark if unreadable
            }
        }
    }

    public event Action? Changed;
}
```

When any dialog is shown, `ThemeManager.Attach(window, AutocadHostTheme.Instance)` attaches the appropriate `ThemeDark.xaml` / `ThemeLight.xaml` palette and subscribes to theme transitions.

### 4.2 AssemblyLoadContext Contextual Reflection
WPF BAML resources require the correct AssemblyLoadContext to resolve type converters and styles. Any dialog instantiation must be scoped within `EnterContextualReflection()`:

```csharp
using (AssemblyLoadContext.GetLoadContext(typeof(SetupView).Assembly)?.EnterContextualReflection())
{
    var view = new SetupView(viewModel);
    // show view...
}
```

### 4.3 Modal vs. Modeless Dialog Execution Matrix

| View | Purpose | Recommended Mode | Rationale |
|---|---|---|---|
| `SetupView` | Configure 86 parameters, layers, hooks, covers | **Modal** | Changes apply to active drawing environment before drawing operations begin. |
| `DetailingView` | Select shape, diameter, length, spacing | **Modal** | User configures rebar parameters then immediately places it into CAD. |
| `ScheduleView` | Audit schedule table, filter components, review tonnage | **Modeless** | User needs to zoom, pan, and inspect drawing while viewing schedule table. |
| `CuttingStockView` | Solve 1D-CSP, view cutting patterns, waste % | **Modeless** | User inspects cut lengths and bar marks against CAD drawing elements. |

### 4.4 Centralized Dialog Service (`CadDialogService`)
`CadDialogService` centralizes window creation, theme attachment, modal/modeless hosting, and instance tracking:

```csharp
namespace HPKata.AutoCad.Hosting;

public static class CadDialogService
{
    private static ScheduleView? _activeScheduleView;
    private static CuttingStockView? _activeCuttingStockView;

    public static bool? ShowModal(Window window)
    {
        using (AssemblyLoadContext.GetLoadContext(window.GetType().Assembly)?.EnterContextualReflection())
        {
            ThemeManager.Attach(window, AutocadHostTheme.Instance);
            var acadHwnd = AcadApp.MainWindow.Handle;
            return AcadApp.ShowModalWindow(acadHwnd, window);
        }
    }

    public static void ShowModelessSchedule(Func<ScheduleView> factory)
    {
        if (_activeScheduleView != null && _activeScheduleView.IsLoaded)
        {
            BringToFront(_activeScheduleView);
            return;
        }

        using (AssemblyLoadContext.GetLoadContext(typeof(ScheduleView).Assembly)?.EnterContextualReflection())
        {
            var window = factory();
            _activeScheduleView = window;
            ThemeManager.Attach(window, AutocadHostTheme.Instance);
            window.Closed += (_, _) => _activeScheduleView = null;
            AcadApp.ShowModelessWindow(AcadApp.MainWindow.Handle, window, false);
        }
    }

    public static void ShowModelessCuttingStock(Func<CuttingStockView> factory)
    {
        if (_activeCuttingStockView != null && _activeCuttingStockView.IsLoaded)
        {
            BringToFront(_activeCuttingStockView);
            return;
        }

        using (AssemblyLoadContext.GetLoadContext(typeof(CuttingStockView).Assembly)?.EnterContextualReflection())
        {
            var window = factory();
            _activeCuttingStockView = window;
            ThemeManager.Attach(window, AutocadHostTheme.Instance);
            window.Closed += (_, _) => _activeCuttingStockView = null;
            AcadApp.ShowModelessWindow(AcadApp.MainWindow.Handle, window, false);
        }
    }

    public static void CloseAllModeless()
    {
        _activeScheduleView?.Close();
        _activeCuttingStockView?.Close();
    }

    private static void BringToFront(Window window)
    {
        if (window.WindowState == WindowState.Minimized)
            window.WindowState = WindowState.Normal;
        window.Activate();
        window.Focus();
    }
}
```

### 4.5 Modeless CAD Interactions & `doc.LockDocument()` Discipline
When a modeless window triggers CAD interactions (e.g., picking rebar on canvas, updating schedule block attributes):
1. **Window Hiding During Prompts**:
   `window.Hide()` -> Prompt user on CAD command line -> `window.Show()` -> `window.Activate()`.
2. **Mandatory Document Locking**:
   Because WPF event handlers run on the WPF Dispatcher thread, accessing the AutoCAD document or database without `doc.LockDocument()` immediately causes an `eDocumentSwitchDisabled` fatal exception.

```csharp
public static void ExecuteCadAction(Action<Document, Transaction> action)
{
    var doc = AcadApp.DocumentManager.MdiActiveDocument;
    if (doc == null) return;

    using (doc.LockDocument())
    using (var tr = doc.Database.TransactionManager.StartTransaction())
    {
        action(doc, tr);
        tr.Commit();
    }
}
```

---

## 5. Clean Code Architecture & Modular Command Partitioning

### 5.1 Adherence to HP Clean Code Governance
- **Rule C1 & C2**: Files must not exceed 300 lines. ViewModels $\le 250$ lines.
- **Rule M1 & M3**: Methods must not exceed 50 lines.
- **Rule A2**: Deterministic disposal of all AutoCAD wrappers (`Transaction`, `DocumentLock`, `DBObject`).
- **Rule A15**: Drawing-writing commands never use `CommandFlags.NoUndoMarker`.
- **Rule S5 (DIP)**: Thin command entry points delegating to modular domain controllers.

### 5.2 Layered Directory Structure for `HPKata.AutoCad`

```
HPKata.AutoCad/
├── HPKata.AutoCad.csproj            # net8.0-windows, AutoCAD.NET 25.1.0, bundle deployment
├── PackageContents.xml             # AutoCAD ApplicationPlugins manifest
├── GoiLenh_HPKata.lsp              # AutoLISP companion alias definitions
│
├── Application/                    # Lifecycle & Document Hooks
│   ├── HPKataExtensionApplication.cs  # IExtensionApplication entry point (<80 lines)
│   ├── RegAppManager.cs               # "KATA" RegApp registration (<50 lines)
│   └── CadDocumentEvents.cs           # DocumentManager Created/Activated events (<60 lines)
│
├── Ribbon/                         # Ribbon UI Tab & Panels
│   ├── HPKataRibbonTab.cs             # Ribbon tab, workspace & theme listeners (<150 lines)
│   ├── HPKataRibbonPanels.cs          # Panel definitions: Setup, Draw, Link, Sched (<200 lines)
│   └── RibbonCommandHandler.cs        # Ribbon button command executor (<45 lines)
│
├── Hosting/                        # WPF Integration
│   ├── AutocadHostTheme.cs            # COLORTHEME -> IHostTheme (<45 lines)
│   ├── CadDialogService.cs            # Modal/Modeless window coordinator (<110 lines)
│   └── CadTransactionContext.cs       # Document locking & transaction wrapper (<70 lines)
│
├── Commands/                       # AutoCAD [CommandMethod] Entry Points (<120 lines each)
│   ├── SetupCommands.cs               # setupkata, setupthep
│   ├── RebarCommands.cs               # ve_thep (vt), sua_thep (sua), taothep, nhan, smd, hooks, cut
│   ├── LinkCommands.cs                # link, cl, dl, tim, xoa, ud, check_lap
│   ├── ScheduleCommands.cs            # tkt, suatk, sstk, ththep, tkco, toexcel, toexcelqh
│   ├── CuttingStockCommands.cs        # tohopthep, loadkho, upkho
│   └── TagCommands.cs                 # gt, stt0, stt1, timthep, itt
│
├── Controllers/                    # Business & Interaction Orchestration (<220 lines each)
│   ├── SetupController.cs             # Manages SetupView + KataConfig sync
│   ├── RebarDrawController.cs         # Prompts points, builds polyline, calls DetailingView
│   ├── RebarEditController.cs         # Selects rebar, loads XData, updates geometry/XData
│   ├── RebarLinkController.cs         # Handles handle-linking, highlight, and tag sync
│   ├── ScheduleController.cs          # Scans drawing, aggregates bars, shows ScheduleView
│   ├── CuttingStockController.cs      # Executes 1D-CSP solver, shows CuttingStockView
│   └── TagController.cs               # Inserts tag blocks, assigns marks, QR code tags
│
└── Services/                       # AutoCAD Database & Entity Services (<220 lines each)
    ├── CadSelectionService.cs         # Pick-first, prompt selection, type filters
    ├── CadPolylineService.cs          # 2D Polyline geometry, bend fillets, width
    ├── CadXDataService.cs             # Reads/writes "KATA" XData records via HPKata.Core
    ├── CadBlockService.cs             # Reads/writes rebar tag blocks and attributes
    └── CadHighlightService.cs         # Transient graphics and highlight management
```

### 5.3 Detailed Class & Method Design

#### 1. `SetupCommands.cs` & `SetupController.cs`
- `[CommandMethod("setupkata")]` / `[CommandMethod("setupkata1")]`: Invokes `SetupController.RunMasterSetup()`.
- `[CommandMethod("setupthep")]`: Invokes `SetupController.RunRebarSetup()`.
- `SetupController` loads active drawing configuration via `KataConfigParser`, opens `SetupView` modally via `CadDialogService.ShowModal`, and persists modified settings to drawing named dictionaries. Lines: ~85.

#### 2. `RebarCommands.cs`, `RebarDrawController.cs`, `RebarEditController.cs`
- `[CommandMethod("ve_thep")]` / `[CommandMethod("vt")]`: Invokes `RebarDrawController.DrawRebar()`.
  Prompts user for geometry or displays `DetailingView` modally; builds 2D lightweight polyline with proper layer (`kata_thep chu`), thickness, and bend fillets via `CadPolylineService`.
- `[CommandMethod("sua_thep")]` / `[CommandMethod("sua")]`: Invokes `RebarEditController.EditRebar()`.
  Prompts user to select rebar; reads XData via `CadXDataService`; populates `DetailingViewModel`; shows `DetailingView`; on commit, updates polyline dimensions, recalculates lengths, and syncs linked tag blocks.
- `[CommandMethod("taothep")]`: Converts existing AutoCAD lines/polylines to Kata smart rebar.
- `[CommandMethod("sua_moc_dai")]` / `[CommandMethod("smd")]`: Toggles stirrup hooks ($90^\circ, 135^\circ, 180^\circ$).
- `[CommandMethod("cat_thep")]` / `[CommandMethod("cut")]`: Splits bar with $30d$ lap splice.
- Lines: `RebarCommands.cs` ~110 lines; `RebarDrawController.cs` ~190 lines; `RebarEditController.cs` ~180 lines.

#### 3. `LinkCommands.cs` & `RebarLinkController.cs`
- `[CommandMethod("tao_lien_ket")]` / `[CommandMethod("link")]`: Invokes `RebarLinkController.CreateLink()`.
  Prompts user to select rebar polyline and tag block (`thep`); extracts handles; writes mutual handle cross-references into XData `KATA_THEP` and `KATA_TAG`.
- `[CommandMethod("update_thep")]` / `[CommandMethod("ud")]`: Reads polyline geometry changes and updates tag attributes (`CD1T`, `SL`).
- `[CommandMethod("xem_lien_ket")]` / `[CommandMethod("tim")]`: Highlights both polyline and linked tag block using `CadHighlightService`.
- `[CommandMethod("xoa_highlight")]` / `[CommandMethod("xoa")]`: Clears highlights.
- `[CommandMethod("copy_tao_lien_ket")]` / `[CommandMethod("cl")]`: Clones rebar and tag, generating fresh handle links.
- Lines: `LinkCommands.cs` ~95 lines; `RebarLinkController.cs` ~210 lines.

#### 4. `ScheduleCommands.cs` & `ScheduleController.cs`
- `[CommandMethod("thong_ke_thep")]` / `[CommandMethod("tkt")]`: Invokes `ScheduleController.ShowSchedule()`.
  Scans active drawing for linked rebar tags; maps them to `RebarItem` models in `HPKata.Core`; passes items to `ScheduleViewModel`; launches `ScheduleView` modelessly via `CadDialogService.ShowModelessSchedule`.
- `[CommandMethod("tong_hop_thep")]` / `[CommandMethod("ththep")]`: Generates summary table grouped by diameter and TCVN bins.
- `[CommandMethod("thong_ke_coupler")]` / `[CommandMethod("tkco")]`: Summarizes mechanical couplers.
- `[CommandMethod("toexcel")]`: Exports schedule table to Excel via `ClosedXmlExporter`.
- Lines: `ScheduleCommands.cs` ~105 lines; `ScheduleController.cs` ~220 lines.

#### 5. `CuttingStockCommands.cs` & `CuttingStockController.cs`
- `[CommandMethod("to_hop_thep")]` / `[CommandMethod("tohopthep")]`: Invokes `CuttingStockController.OptimizeCutting()`.
  Scans drawing or accepts items from `ScheduleViewModel`; launches `CuttingStockViewModel`; runs `CuttingStockEngine` with standard 11,700 mm stock bars; displays `CuttingStockView` modelessly.
- `[CommandMethod("loadkho")]` / `[CommandMethod("upkho")]`: Loads and commits warehouse offcuts.
- Lines: `CuttingStockCommands.cs` ~75 lines; `CuttingStockController.cs` ~185 lines.

#### 6. `TagCommands.cs` & `TagController.cs`
- `[CommandMethod("ghi_thep")]` / `[CommandMethod("gt")]`: Inserts tag block with leader and automatically links to rebar.
- `[CommandMethod("stt0")]` / `[CommandMethod("stt1")]`: Numbers sequential bar marks.
- `[CommandMethod("in_tem_thep")]` / `[CommandMethod("itt")]`: Generates QR code tags for fabrication bundles via `QrTagGenerator`.
- Lines: `TagCommands.cs` ~80 lines; `TagController.cs` ~170 lines.

---

## 6. Verification and Risk Analysis

### 6.1 Verification Matrix
| Requirement | Architecture Mechanism | Verification Target |
|---|---|---|
| .NET 8 / AutoCAD 2026 Target | `TargetFramework=net8.0-windows`, `AutoCAD.NET 25.1.0` | `dotnet build` succeeds without runtime DLL collisions |
| RegApp `"KATA"` Availability | `RegAppManager` hooked to `DocumentCreated` & `DocumentActivated` | XData read/write succeeds on any opened/new drawing |
| Theme Synchronization | `AutocadHostTheme` monitoring `"COLORTHEME"` sysvar | WPF dialog dynamically flips palettes on CAD theme toggle |
| Thread Safety & No Deadlocks | `doc.LockDocument()` in all modeless CAD interactions | Zero `eDocumentSwitchDisabled` crashes when interacting from UI |
| Clean Code Compliance | Controller separation, class $\le 300$, method $\le 50$ | AST walker / code review checklist passes with zero errors |

### 6.2 Risk Mitigation
1. **Ribbon Null Reference on Startup**:
   AutoCAD may fire `IExtensionApplication.Initialize()` before the Ribbon UI (`ComponentManager.Ribbon`) is fully instantiated.
   *Mitigation*: Subscribing to `ComponentManager.ItemInitialized` ensures the Ribbon tab is attached the moment the UI framework becomes available.
2. **Modeless Window Disposal Crash**:
   If a modeless window references an AutoCAD `Document` that is closed while the window remains open, subsequent operations fail.
   *Mitigation*: `CadDocumentEvents` listens to `DocumentDestroyed` and automatically closes or re-binds dependent modeless dialogs.
3. **WPF BAML Context Resolution in Custom ALC**:
   AutoCAD plugin loading via ALC can cause WPF to fail finding assembly resources.
   *Mitigation*: Wrapping all window instantiations in `AssemblyLoadContext.GetLoadContext(viewType.Assembly)?.EnterContextualReflection()`.

---
*End of Architectural Analysis.*
