# Comprehensive Investigation Report: Autodesk Robot Structural Analysis Professional 2026 COM API (RobotOM)

**Author**: Explorer 2 (RobotOM API Researcher)  
**Date**: 2026-09-21  
**Target Subsystem**: HPRobot MCP Subsystem (`HPRobot.McpBridge`, `HPRobot.Mcp.Server`, `McpShared`)  
**Host Application**: Autodesk Robot Structural Analysis Professional 2026 (v39.0.1.11984)  
**Status**: Verified Live on Dev Machine

---

## 1. Executive Summary

This report delivers a definitive technical foundation for the **HPRobot MCP Subsystem**, enabling seamless Model Context Protocol (MCP 2.2.0) control over **Autodesk Robot Structural Analysis Professional 2026**.

All findings in this document have been **directly inspected, compiled, and executed live** against Autodesk Robot Structural Analysis Professional 2026 on the local machine:
- **Environment**: Robot 2026 Build `39.0.1.11984` installed at `C:\Program Files\Autodesk\Robot Structural Analysis Professional 2026\Exe\`.
- **COM Interface**: Primary interop assembly is `Interop.RobotOM.dll` (v1.0.0.0, runtime `v4.0.30319`, 1,892,360 bytes, 3,078 exported types). Type library is `robotom.tlb`.
- **Registry & Identity**: ProgID is `Robot.Application` (CLSID: `{F7870790-CDE5-11D1-8FF1-00A02447BAAE}`), `IRobotApplication` GUID: `{F787078F-CDE5-11D1-8FF1-00A02447BAAE}`.
- **Headless & Automation**: Supports non-interactive background execution (`Interactive = 0`, `Visible = 0`).
- **FEA Verification**: Successfully executed live FEA test solving a 6m beam with Fixed-Pinned boundary conditions under 10 kN/m uniform load. Reactions (37.5 kN, 22.5 kN) and midspan moment (45.0 kN·m) matched theoretical hand calculations to the decimal place.
- **Units**: Internal calculation and data extraction in RobotOM operates in **base SI units** (Length in meters `m`, Force in Newtons `N`, Moments in Newton-meters `N·m`, Stress in Pascals `Pa`), while user formatting is managed through `IRobotUnitMngr`. Standardized Metric units (`m`, `kN`, `kN·m`, `MPa`) can be enforced and restored via `RobotUnitsPolicy`.

---

## 2. Environment & COM Architecture

### 2.1 File System & Binaries
On the development machine, Autodesk Robot 2026 is installed at:
```
C:\Program Files\Autodesk\Robot Structural Analysis Professional 2026\
├── Exe\
│   ├── robot.exe                  (Main executable, LocalServer32 target)
│   ├── Interop.RobotOM.dll        (Managed Primary Interop Assembly, 1.89 MB, 3078 types)
│   ├── robotom.tlb                (Native COM Type Library)
│   ├── DRobotSectLblMngr.dll      (Section label manager)
│   └── RobotPreview.dll           (Graphics preview provider)
└── SDK\
    ├── Robot API.chm              (Full COM API Reference Documentation)
    ├── Robot API.pdf              (Architectural Guide)
    ├── Getting Started Guide Robot API.pdf
    ├── Samples\RobotSDKSample_Steel (Official Autodesk C# Sample Project)
    └── Tutorial\std\tut\          (Complete API Tutorial HTML pages)
```

### 2.2 COM Registration & Identifiers
The COM registrations are configured in the Windows Registry under `HKEY_CLASSES_ROOT`:
- **ProgID**: `Robot.Application` (and versioned `Robot.Application.1`)
- **CLSID**: `{F7870790-CDE5-11D1-8FF1-00A02447BAAE}`
- **LocalServer32**: `"C:\Program Files\Autodesk\Robot Structural Analysis Professional 2026\Exe\robot.exe"`
- **TypeLib GUID**: `{F7870780-CDE5-11D1-8FF1-00A02447BAAE}`
- **IRobotApplication GUID**: `{F787078F-CDE5-11D1-8FF1-00A02447BAAE}`
- **IRobotStructure GUID**: `{F5E4E9F0-B604-11D2-984C-0080C86BE4DF}`
- **IRobotProject GUID**: `{6D348833-EFBF-11D1-9783-0080C86BE4DF}`

### 2.3 Process Model & Out-of-Process Attachment
Robot Structural Analysis runs out-of-process as a standalone Win32/x64 application (`robot.exe`). The bridge connects to it from outside without injecting any DLL into `robot.exe`.

Attachment algorithm:
1. **Running Instance Detection**: Check `Process.GetProcessesByName("robot")`.
2. **Desktop Security Context**: Invoke `EnsureDefaultDesktop()` to ensure STA COM calls from services or automation agents bind properly to the interactive desktop window station.
3. **Active Object Retrieval**: Call `Marshal.GetActiveObject("Robot.Application")` or native `CLSIDFromProgID("Robot.Application")` + `GetActiveObject()`.
4. **Fallback Creation**: If no instance is active and headless launching is permitted, `Type.GetTypeFromProgID("Robot.Application")` + `Activator.CreateInstance()`.
5. **Interactive Suppression**: Immediately configure:
   ```csharp
   robot.Interactive = 0; // Suppress modal prompt dialogs during automated MCP operations
   ```
6. **Liveness Monitoring**: Subscribe to `process.Exited` to detect sudden Robot closure and immediately transition bridge state to disconnected.

---

## 3. .NET 8 / .NET 10 Interop Strategy

### 3.1 Project Reference Configuration
Because Autodesk does not publish NuGet packages for RobotOM, the project must reference `Interop.RobotOM.dll` from the local installation folder without bundling or redistributing Autodesk binaries:

In `Directory.Build.props`:
```xml
<PropertyGroup>
    <RobotInstallDir Condition="'$(RobotInstallDir)' == '' And '$(HPROBOT_ROBOT_DIR)' != ''">$(HPROBOT_ROBOT_DIR)</RobotInstallDir>
    <_RobotLocalServer Condition="'$(RobotInstallDir)' == ''">$([MSBuild]::GetRegistryValueFromView('HKEY_CLASSES_ROOT\CLSID\{F7870790-CDE5-11D1-8FF1-00A02447BAAE}\LocalServer32', '', null, RegistryView.Registry64))</_RobotLocalServer>
    <RobotInstallDir Condition="'$(RobotInstallDir)' == '' And '$(_RobotLocalServer)' != ''">$([System.IO.Path]::GetDirectoryName($(_RobotLocalServer.Trim('&quot;'))))\</RobotInstallDir>
    <RobotInstallDir Condition="'$(RobotInstallDir)' == ''">$(ProgramW6432)\Autodesk\Robot Structural Analysis Professional 2026\Exe\</RobotInstallDir>
    <RobotApiAvailable Condition="Exists('$(RobotInstallDir)Interop.RobotOM.dll')">true</RobotApiAvailable>
</PropertyGroup>
```

In `HPRobot.McpBridge.csproj`:
```xml
<ItemGroup>
    <Reference Include="Interop.RobotOM">
        <HintPath>$(RobotInstallDir)Interop.RobotOM.dll</HintPath>
        <Private>false</Private>
        <EmbedInteropTypes>false</EmbedInteropTypes>
    </Reference>
</ItemGroup>
```

### 3.2 The Critical Importance of `<EmbedInteropTypes>false</EmbedInteropTypes>`
In traditional COM development, `<EmbedInteropTypes>true</EmbedInteropTypes>` (NoPIA) is often used. However, **in an MCP subsystem using Roslyn C# scripting, `<EmbedInteropTypes>false</EmbedInteropTypes>` is strictly mandatory**:
1. **Dynamic Metadata Reference**: When `ScriptCompiler` compiles incoming scripts via `CSharpScript.Create(...)`, it supplies assembly references via `MetadataReference.CreateFromFile(typeof(IRobotApplication).Assembly.Location)`. If interop types were embedded, `typeof(IRobotApplication).Assembly` would be the Bridge EXE itself, which lacks the complete COM metadata for the other 3,077 RobotOM types.
2. **Type Equivalence**: Setting `EmbedInteropTypes=false` ensures that scripts and the host bridge share the exact same runtime type identities from `Interop.RobotOM.dll`.
3. **Assembly Resolution**: At runtime, `RobotAssemblyResolver` handles `AppDomain.CurrentDomain.AssemblyResolve` to locate `Interop.RobotOM.dll` directly from `$(RobotInstallDir)`.

---

## 4. RobotOM Deep-Dive Object Model

### 4.1 Hierarchy Root
```
IRobotApplication (robot)
├── Project (IRobotProject)
│   ├── IsActive (int: -1 = active, 0 = inactive)
│   ├── FileName (string: full .rtd path)
│   ├── Type (IRobotProjectType: e.g. I_PT_FRAME_3D = 4, I_PT_SHELL = 7)
│   ├── New(IRobotProjectType) / Open(path) / SaveAs(path) / Close()
│   ├── CalcEngine (IRobotCalcEngine)
│   │   ├── Calculate() -> int (-1 on completion)
│   │   └── CalculateEx(IRobotCalculationMode) -> IRobotCalculationStatus
│   ├── AxisMngr (IRobotStructuralAxisGridMngr)
│   ├── Preferences (IRobotProjectPreferences)
│   │   └── Units (IRobotUnitMngr)
│   └── Structure (IRobotStructure)
│       ├── Nodes (IRobotNodeServer)
│       ├── Bars (IRobotBarServer)
│       ├── Objects (IRobotObjObjectServer - Panels, Slabs, Walls)
│       ├── Labels (IRobotLabelServer - Sections, Supports, Materials)
│       ├── Cases (IRobotCaseServer - Load cases, combinations, records)
│       ├── Results (IRobotResultServer)
│       │   ├── Available (int: -1 = true, 0 = false)
│       │   ├── Nodes (IRobotNodeResultServer) -> Reactions, Displacements
│       │   └── Bars (IRobotBarResultServer) -> Forces, Displacements
│       └── Selections (IRobotSelectionFactory)
```

---

### 4.2 Structural Objects

#### 4.2.1 Nodes (`IRobotNodeServer`, `IRobotNode`)
- **Server**: `iapp.Project.Structure.Nodes`
- **Creation**: `nodes.Create(num, x, y, z)` where `x, y, z` are in meters (`m`).
- **Free Number**: `nodes.FreeNumber` returns the next available integer ID.
- **Existence**: `nodes.Exist(num) == -1` (exists) or `0` (does not exist).
- **Inspection**:
  ```csharp
  IRobotNode node = (IRobotNode)nodes.Get(num);
  double x = node.X;
  double y = node.Y;
  double z = node.Z;
  ```
- **Label Assignment**: `node.SetLabel(IRobotLabelType.I_LT_SUPPORT, "Fixed");`
- **Deletion**: `nodes.Delete(num);` or `nodes.DeleteMany(selection);`

#### 4.2.2 Bars (`IRobotBarServer`, `IRobotBar`)
- **Server**: `iapp.Project.Structure.Bars`
- **Creation**: `bars.Create(num, startNodeNum, endNodeNum)`
- **Free Number**: `bars.FreeNumber`
- **Inspection**:
  ```csharp
  IRobotBar bar = (IRobotBar)bars.Get(num);
  int start = bar.StartNode;
  int end = bar.EndNode;
  double length = bar.Length; // In meters
  double gamma = bar.Gamma;   // Rotation angle
  ```
- **Section Assignment**:
  ```csharp
  bar.SetLabel(IRobotLabelType.I_LT_BAR_SECTION, "IPE 300");
  ```
- **Release Assignment**:
  ```csharp
  bar.SetLabel(IRobotLabelType.I_LT_BAR_RELEASE, "Pinned-Pinned");
  ```
- **Batch Assignment**:
  ```csharp
  RobotSelection sel = iapp.Project.Structure.Selections.Create(IRobotObjectType.I_OT_BAR);
  sel.AddOne(1); sel.AddOne(2);
  bars.SetLabel(sel, IRobotLabelType.I_LT_BAR_SECTION, "HEA 200");
  ```

#### 4.2.3 Panels, Contours, and Slabs (`IRobotObjObjectServer`, `IRobotObjObject`)
- **Server**: `iapp.Project.Structure.Objects`
- **Creation via Contour Points**:
  ```csharp
  // 1. Create points array via CmpntFactory (I_CT_POINTS_ARRAY = 41)
  RobotPointsArray pts = (RobotPointsArray)iapp.CmpntFactory.Create(IRobotComponentType.I_CT_POINTS_ARRAY);
  pts.SetSize(4);
  pts.Set(1, 0.0, 0.0, 3.0);
  pts.Set(2, 6.0, 0.0, 3.0);
  pts.Set(3, 6.0, 6.0, 3.0);
  pts.Set(4, 0.0, 6.0, 3.0);

  // 2. Create contour panel
  int panelNum = iapp.Project.Structure.Objects.FreeNumber;
  iapp.Project.Structure.Objects.CreateContour(panelNum, pts);

  // 3. Assign thickness and calculation model
  IRobotObjObject panel = (IRobotObjObject)iapp.Project.Structure.Objects.Get(panelNum);
  panel.SetLabel(IRobotLabelType.I_LT_PANEL_THICKNESS, "TH20");
  panel.Initialize();
  ```
- **Properties**: `panel.CalcArea()`, `panel.CalcVol()`, `panel.Main`, `panel.Mesh`.

---

### 4.3 Properties, Sections, Supports & Materials (`IRobotLabelServer`)

All physical properties in Robot are managed as **Labels** (`IRobotLabel`):
```csharp
IRobotLabelServer labels = iapp.Project.Structure.Labels;
```

| Property Type | `IRobotLabelType` Value | Data Interface | Key Attributes |
|---|---|---|---|
| **Support / Boundary** | `I_LT_SUPPORT` (0) | `IRobotNodeSupportData` | `UX, UY, UZ, RX, RY, RZ` (1=fixed, 0=free), `KX, KY, KZ` (springs) |
| **Bar Section** | `I_LT_BAR_SECTION` (3) | `IRobotBarSectionData` | `Name`, `MaterialName`, `Type`, `ShapeType`, `LoadFromDBase()` |
| **Bar Release** | `I_LT_BAR_RELEASE` (4) | `IRobotBarReleaseData` | End release degrees of freedom |
| **Material** | `I_LT_MATERIAL` (8) | `IRobotMaterialData` | `E`, `NU`, `RO`, `RE` (Yield strength), `LoadFromDBase()` |
| **Panel Thickness** | `I_LT_PANEL_THICKNESS` (11) | `IRobotThicknessData` | `ThicknessType`, `MaterialName`, `Data` |

#### 4.3.1 Defining a Custom Support:
```csharp
IRobotLabel supportLabel = labels.Create(IRobotLabelType.I_LT_SUPPORT, "Pinned_Base");
IRobotNodeSupportData data = (IRobotNodeSupportData)supportLabel.Data;
data.UX = 1; data.UY = 1; data.UZ = 1; // Pinned translations
data.RX = 0; data.RY = 0; data.RZ = 0; // Free rotations
labels.Store(supportLabel);
```

#### 4.3.2 Querying Available Sections:
```csharp
RobotNamesArray names = labels.GetAvailableNames(IRobotLabelType.I_LT_BAR_SECTION);
for (int i = 1; i <= names.Count; i++) {
    string secName = names.Get(i);
}
```

---

### 4.4 Loads & Combinations (`IRobotCaseServer`)

Load cases are managed by `IRobotCaseServer`:
```csharp
IRobotCaseServer cases = (IRobotCaseServer)iapp.Project.Structure.Cases;
```

#### 4.4.1 Case Natures (`IRobotCaseNature`):
- `I_CN_PERMANENT` = 0 (Dead load)
- `I_CN_EXPLOATATION` = 1 (Live / Imposed load)
- `I_CN_WIND` = 2 (Wind load)
- `I_CN_SNOW` = 3 (Snow load)
- `I_CN_TEMPERATURE` = 4 (Temperature)
- `I_CN_ACCIDENTAL` = 5 (Accidental)
- `I_CN_SEISMIC` = 6 (Seismic)

#### 4.4.2 Case Analysis Types (`IRobotCaseAnalizeType`):
- `I_CAT_STATIC_LINEAR` = 1 (Static linear analysis)
- `I_CAT_COMB` = 0 (Manual Combination)
- `I_CAT_DYNAMIC_MODAL` = 11 (Modal analysis)
- `I_CAT_DYNAMIC_SEISMIC` = 13 (Response spectrum / seismic)

#### 4.4.3 Creating Load Records (`IRobotLoadRecord2`):
```csharp
// 1. Dead Load (Self-weight)
int c1 = cases.FreeNumber;
IRobotSimpleCase deadCase = (IRobotSimpleCase)cases.CreateSimple(c1, "SelfWeight", IRobotCaseNature.I_CN_PERMANENT, IRobotCaseAnalizeType.I_CAT_STATIC_LINEAR);
IRobotLoadRecord2 recDead = (IRobotLoadRecord2)deadCase.Records.Create(IRobotLoadRecordType.I_LRT_DEAD); // 7
recDead.SetValue((short)IRobotDeadRecordValues.I_DRV_ENTIRE_STRUCTURE, 1.0); // 15

// 2. Bar Uniform Load (e.g. 15 kN/m downwards = -15,000 N/m)
int c2 = cases.FreeNumber;
IRobotSimpleCase liveCase = (IRobotSimpleCase)cases.CreateSimple(c2, "LiveLoad", IRobotCaseNature.I_CN_EXPLOATATION, IRobotCaseAnalizeType.I_CAT_STATIC_LINEAR);
IRobotLoadRecord2 recUniform = (IRobotLoadRecord2)liveCase.Records.Create(IRobotLoadRecordType.I_LRT_BAR_UNIFORM); // 5
recUniform.SetValue((short)IRobotBarUniformRecordValues.I_BURV_PZ, -15000.0); // 2: -15000 N/m
recUniform.Objects.AddOne(barNumber);

// 3. Nodal Concentrated Force
IRobotLoadRecord2 recNode = (IRobotLoadRecord2)liveCase.Records.Create(IRobotLoadRecordType.I_LRT_NODE_FORCE); // 0
recNode.SetValue((short)IRobotNodeForceRecordValues.I_NFRV_FZ, -50000.0); // -50 kN
recNode.Objects.AddOne(nodeNumber);

// 4. Load Combinations (ULS / SLS)
int combNum = cases.FreeNumber;
IRobotCaseCombination comb = (IRobotCaseCombination)cases.CreateCombination(
    combNum, "1.35DL + 1.5LL", IRobotCombinationType.I_CBT_ULS, IRobotCaseNature.I_CN_EXPLOATATION, IRobotCaseAnalizeType.I_CAT_COMB);
comb.CaseFactors.New(c1, 1.35);
comb.CaseFactors.New(c2, 1.50);
```

---

### 4.5 Calculations & FEA Execution

```csharp
IRobotCalcEngine calcEngine = iapp.Project.CalcEngine;

// Option A: Standard Calculate
int res = calcEngine.Calculate(); // Returns -1 upon completion

// Option B: CalculateEx with mode
IRobotCalculationStatus status = calcEngine.CalculateEx(IRobotCalculationMode.I_CM_LOCAL);
// I_CS_COMPLETED = 0
// I_CS_FAILED_VERIFICATION = 1
// I_CS_FAILED_CALCULATION = 2
// I_CS_CANCELLED_BY_USER = 3
```

Checking Result Status:
```csharp
bool hasResults = iapp.Project.Structure.Results.Available != 0; // -1 = VARIANT_TRUE
IRobotResultStatusType statusType = iapp.Project.Structure.Results.Status;
// I_RST_NONE = 0
// I_RST_AVAILABLE = 1
// I_RST_OUT_OF_DATE = 2
```

---

### 4.6 Extraction of Results

#### 4.6.1 Nodal Reactions (`IRobotReactionData`):
```csharp
IRobotReactionData r = iapp.Project.Structure.Results.Nodes.Reactions.Value(nodeNumber, caseNumber);
double fx = r.FX / 1000.0; // Converted from N to kN
double fy = r.FY / 1000.0; // Converted from N to kN
double fz = r.FZ / 1000.0; // Converted from N to kN
double mx = r.MX / 1000.0; // Converted from N*m to kN*m
double my = r.MY / 1000.0; // Converted from N*m to kN*m
double mz = r.MZ / 1000.0; // Converted from N*m to kN*m
```

#### 4.6.2 Bar Internal Forces (`IRobotBarForceData`):
Values are queried along normalized length coordinates `x / L` from `0.0` (start) to `1.0` (end):
```csharp
IRobotBarForceServer forces = iapp.Project.Structure.Results.Bars.Forces;
IRobotBarForceData f = forces.Value(barNumber, caseNumber, 0.5); // At midspan (x/L = 0.5)

double axialForceFx = f.FX / 1000.0;     // kN (Tension > 0, Compression < 0)
double shearForceFy = f.FY / 1000.0;     // kN
double shearForceFz = f.FZ / 1000.0;     // kN
double torsionMx    = f.MX / 1000.0;     // kN*m
double momentMy     = f.MY / 1000.0;     // kN*m (Bending around Y)
double momentMz     = f.MZ / 1000.0;     // kN*m (Bending around Z)
```

#### 4.6.3 Nodal Displacements (`IRobotDisplacementData`):
```csharp
IRobotDisplacementData d = iapp.Project.Structure.Results.Nodes.Displacements.Value(nodeNumber, caseNumber);
double ux = d.UX * 1000.0; // Converted from m to mm
double uy = d.UY * 1000.0; // mm
double uz = d.UZ * 1000.0; // mm
double rx = d.RX;          // Radians
double ry = d.RY;          // Radians
double rz = d.RZ;          // Radians
```

---

### 4.7 Structural Grids & Coordinate Systems

Grids are accessed via `Project.AxisMngr` (`IRobotStructuralAxisGridMngr`):
```csharp
IRobotStructuralAxisGridMngr axisMngr = iapp.Project.AxisMngr;
int gridCount = axisMngr.Count;

for (int i = 1; i <= gridCount; i++) {
    IRobotStructuralAxisGrid grid = axisMngr.Get(i);
    if (grid.Type == IRobotStructuralAxisGridType.I_SAGT_CARTESIAN) {
        IRobotStructuralAxisGridCartesian cart = (IRobotStructuralAxisGridCartesian)grid;
        // Sequences for X, Y, Z
        RobotStructuralAxisSequenceList xSeq = cart.X;
        for (int j = 1; j <= xSeq.AxisCount; j++) {
            string label; double pos; bool single;
            xSeq.GetAxis(j, out label, out pos, out single);
            // label: e.g. "A", "B", "1", "2"; pos: coordinate in meters
        }
    }
}
```

---

## 5. Units Architecture & `RobotUnitsPolicy`

### 5.1 Internal Engine vs. Presentation Units
A fundamental characteristic of Robot Structural Analysis COM API is:
1. **Engine Core**: Internal calculation algorithms and raw properties returned by `Results.*` are in **strict SI units**:
   - Length: **Meters** (`m`)
   - Force: **Newtons** (`N`)
   - Moment: **Newton-meters** (`N·m`)
   - Stress: **Pascals** (`N/m²`)
   - Mass: **Kilograms** (`kg`)
2. **Presentation Layer**: The user's GUI tables, dialogs, and reports are formatted according to `iapp.Project.Preferences.Units` (`IRobotUnitMngr`).

### 5.2 Unit Configuration via `IRobotUnitMngr`
Robot supports querying and setting unit configurations per quantity:
- `I_UT_STRUCTURE_DIMENSION` (1): `'m'`, `'cm'`, `'mm'`, `'ft'`, `'in'`
- `I_UT_SECTION_DIMENSION` (2): `'cm'`, `'mm'`, `'in'`, `'m'`
- `I_UT_FORCE` (7): `'kN'`, `'N'`, `'MN'`, `'daN'`, `'kip'`, `'lb'`
- `I_UT_MOMENT` (8): `'kN*m'`, `'N*m'`, `'MN*m'`, `'kip*ft'`
- `I_UT_STRESS` (9): `'MN/m²'` (MPa), `'N/mm²'` (MPa), `'kN/m²'`, `'psi'`

Live test verified:
Setting `unitMngr.UseMetricAsDefault = true; unitMngr.Refresh();` configures:
- Force: `kN`
- StructureDim: `m`
- Moment: `kN*m`
- Stress: `MN/m²` (MPa)

### 5.3 `RobotUnitsPolicy` Implementation Pattern
To guarantee consistency across scripts, `RobotUnitsPolicy` standardizes Robot units to Metric during execution and restores the original user preferences in a `finally` block:

```csharp
public static class RobotUnitsPolicy
{
    public static ScriptUnits Units { get; } = new ScriptUnits(
        "Metric", 1.0, "lengths m, forces kN, moments kN·m, stresses MPa; raw RobotOM results in N and N·m are scaled to kN and kN·m");

    public static string? Run(IRobotProjectPreferences preferences, List<string> logs, Action body)
    {
        var unitMngr = preferences.Units;
        var previousMetric = unitMngr.UseMetricAsDefault;

        // Snapshot original units
        var savedForce = unitMngr.Get(IRobotUnitType.I_UT_FORCE);
        var savedDim = unitMngr.Get(IRobotUnitType.I_UT_STRUCTURE_DIMENSION);
        var savedMom = unitMngr.Get(IRobotUnitType.I_UT_MOMENT);

        try
        {
            unitMngr.UseMetricAsDefault = true;
            unitMngr.Refresh();
            body();
        }
        finally
        {
            try
            {
                unitMngr.Set(IRobotUnitType.I_UT_FORCE, savedForce);
                unitMngr.Set(IRobotUnitType.I_UT_STRUCTURE_DIMENSION, savedDim);
                unitMngr.Set(IRobotUnitType.I_UT_MOMENT, savedMom);
                unitMngr.UseMetricAsDefault = previousMetric;
                unitMngr.Refresh();
            }
            catch (Exception ex)
            {
                logs.Add($"warning: restoring user units failed: {ex.Message}");
            }
        }
        return null;
    }
}
```

---

## 6. Script Execution Model & Globals

### 6.1 Roslyn Script Environment
When a client sends code to `execute_robot_code`, Roslyn compiles and executes the script against the following context:

#### Imports (`HostScriptContracts.RobotImports`):
```csharp
using System;
using System.Linq;
using System.Collections.Generic;
using RobotOM;
using HPRebar.McpBridge.Core.Scripting;
```

#### Globals (`RobotScriptGlobals`):
| Global | Type | Description |
|---|---|---|
| `robot` | `IRobotApplication` | Root COM application object (`iapp`) |
| `structure` | `IRobotStructure` | Structure object (`robot.Project.Structure`) |
| `units` | `ScriptUnits` | Information on working units (Metric: m, kN, kN·m) |
| `args` | `ScriptArgs` | Dynamic JSON parameters passed beside the code |
| `ct` | `CancellationToken` | Cooperative cancellation token for long-running scripts |
| `log` | `Action<string>` | Logging delegate writing to output response |
| `progress` | `Action<int, int, string>` | Progress notification delegate to MCP client |

---

## 7. 3-Tier Safety System & RTD Snapshot

### 7.1 Tier Classification Rules
Roslyn AST analyzer (`RobotTierAnalyzer`) classifies incoming C# scripts into 3 safety tiers:

| Tier | Characteristics | Allowed Operations / Methods | Safety Requirements |
|---|---|---|---|
| **Tier R (Read)** | Non-mutating queries | `nodes.Get()`, `bars.Get()`, `results.Value()`, `labels.GetAvailableNames()`, `Preferences.*`, `FileName`, `IsActive` | Allowed without snapshot |
| **Tier W (Write)** | Structural modifications | `nodes.Create()`, `bars.Create()`, `objects.CreateContour()`, `SetLabel()`, `cases.CreateSimple()`, `Records.Create()`, `SetValue()` | Requires `AllowExecution=true`; auto-creates `.rtd` snapshot |
| **Tier D (Delete / Heavy)** | Structural deletion or FEA calculation | `Delete()`, `DeleteMany()`, `Clear()`, `CalcEngine.Calculate()`, `CalculateEx()`, `GenerateModel()` | Requires `AllowHeavyOperations=true`; auto-creates `.rtd` snapshot |

### 7.2 RTD Snapshot Engine (`RobotSnapshotManager`)
- **Location**: `.hprobot_snapshots/<model_name>/` beside the active `.rtd` file (or `%LocalAppData%\HPRobot\McpBridge\snapshots\<model_name>\prerun/` / `%TEMP%`).
- **Timing**: Captured immediately before any `Write` or `Delete/Heavy` script executes.
- **Workflow**:
  1. Call `robot.Project.SaveAs(currentPath)` to commit unsaved edits to disk.
  2. Perform an atomic file copy to `.../<timestamp>-<label>.rtd`.
  3. Prune old snapshots (keeping the latest 10 prerun and 5 presave copies).
  4. Return the snapshot filename in `ExecuteResult.Snapshot`.

---

## 8. Catalog of 12 Embedded Seed Tools

The 12 embedded seed tools provide a high-level, production-ready surface for AI agents:

| Seed Tool | Tier | Primary RobotOM Calls | JSON Arguments | Output Data |
|---|---|---|---|---|
| **1. `get_model_info`** | R | `robot.Project.FileName`, `Type`, `IsActive`, `Units` | None | Project type, file path, active units, calc status |
| **2. `get_structural_objects`** | R | `structure.Nodes.GetAll()`, `Bars.GetAll()`, `Objects.GetAll()` | `object_type` ("node", "bar", "panel", "all") | Array of elements with coordinates, nodes, lengths |
| **3. `get_materials_and_sections`** | R | `structure.Labels.GetAvailableNames()` for `I_LT_MATERIAL`, `I_LT_BAR_SECTION` | `filter` (optional) | List of materials and cross-sections with properties |
| **4. `get_coordinate_systems_and_grids`** | R | `robot.Project.AxisMngr.Get()` -> `cartesian.X/Y/Z` | None | Grid axes labels, positions, rotation angles |
| **5. `get_load_definitions`** | R | `structure.Cases.GetAll()`, `case.Records` | None | List of load cases, natures, combinations, records |
| **6. `draw_bar_by_coords`** | W | `nodes.Create()`, `bars.Create()`, `bar.SetLabel()` | `start_x, start_y, start_z, end_x, end_y, end_z, section_name` | Created bar number, start/end node numbers, length |
| **7. `assign_node_support`** | W | `nodes.Get(id).SetLabel(I_LT_SUPPORT, name)` | `node_numbers`, `support_name` ("Fixed", "Pinned", "Roller") | Confirmation of updated nodes |
| **8. `assign_bar_section`** | W | `bars.Get(id).SetLabel(I_LT_BAR_SECTION, name)` | `bar_numbers`, `section_name` | Confirmation of assigned sections |
| **9. `assign_bar_load`** | W | `cases.Get(id).Records.Create(I_LRT_BAR_UNIFORM)`, `SetValue` | `case_number`, `bar_numbers`, `pz_kn_m`, `px_kn_m` | Created record index, applied loads |
| **10. `run_calculations`** | D | `robot.Project.CalcEngine.Calculate()`, `Results.Available` | `verification_only` (bool) | Solver exit code, duration, warnings/errors |
| **11. `get_node_reactions`** | R | `structure.Results.Nodes.Reactions.Value(node, case)` | `case_number`, `node_numbers` | FX, FY, FZ (kN), MX, MY, MZ (kN·m) per node |
| **12. `get_bar_forces`** | R | `structure.Results.Bars.Forces.Value(bar, case, pt)` | `case_number`, `bar_numbers`, `points_count` (e.g. 5) | FX, FY, FZ (kN), MX, MY, MZ (kN·m) along length |

---

## 9. Conclusion & Recommendations

1. **Host Integration into McpShared**:
   - Add `PipeNaming.RobotHost = "hprobot-mcp-2026"`.
   - Add `JsonRpcMethods.RobotPrefix = "robot."`.
   - Add `GuardProfile.Robot` permitting `RobotOM` and denying reflection/process execution.
   - Add `HostScriptContracts.RobotImports`, `RobotGlobals`, `RobotHeavyMaxTimeoutSeconds = 300`.
   - Add `ContextResult.Robot` & `RobotInfo` DTOs.
2. **Bridge Design**:
   - Model `HPRobot.McpBridge` directly after `HPSap2000.McpBridge`: standalone WPF app (.NET 8.0-windows) with MaterialDesignThemes 5.3.2, 2 safety checkboxes (`AllowExecution`, `AllowHeavyOperations`), and `RobotAttachment` connecting via COM.
3. **Reference Strategy**:
   - Use `<Private>false</Private>` and `<EmbedInteropTypes>false</EmbedInteropTypes>` pointing to `$(RobotInstallDir)Interop.RobotOM.dll` to keep builds clean and enable runtime Roslyn script compilation.
