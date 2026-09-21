# Handoff Report — Autodesk Robot Structural Analysis Professional 2026 COM API (RobotOM)

**Author**: Explorer 2 (RobotOM API Researcher)  
**Recipient**: Project Orchestrator (`orchestrator_7`, conversation ID: `b32c5a58-8b71-46dd-ba9a-5c9e4b6709de`)  
**Timestamp**: 2026-09-21T13:30:00Z  
**Type**: Hard Handoff (Investigation Complete)

---

## 1. Observation

1. **Installation Environment & Interop Binary**:
   - Robot 2026 is installed at `C:\Program Files\Autodesk\Robot Structural Analysis Professional 2026\Exe\`.
   - File `Interop.RobotOM.dll`: Length `1,892,360` bytes (~1.89 MB), Assembly `Interop.RobotOM, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null`, ImageRuntimeVersion `v4.0.30319`, 3,078 exported types.
   - Primary executable is `robot.exe` (x64).
   - Official documentation and samples found in `C:\Program Files\Autodesk\Robot Structural Analysis Professional 2026\SDK\`:
     - `Robot API.chm`, `Robot API.pdf`, `Getting Started Guide Robot API.pdf`.
     - Official C# sample project: `Samples\RobotSDKSample_Steel\RobotSDKSample\Robot.cs`.
     - Full tutorial HTML in `Tutorial\std\tut\`.

2. **COM Identifiers in Windows Registry**:
   - `HKCR\Robot.Application\CLSID` -> `{F7870790-CDE5-11D1-8FF1-00A02447BAAE}`
   - `HKCR\CLSID\{F7870790-CDE5-11D1-8FF1-00A02447BAAE}\LocalServer32` -> `C:\Program Files\Autodesk\Robot Structural Analysis Professional 2026\Exe\robot.exe`
   - `IRobotApplication` Interface GUID: `{F787078F-CDE5-11D1-8FF1-00A02447BAAE}`
   - `IRobotStructure` Interface GUID: `{F5E4E9F0-B604-11D2-984C-0080C86BE4DF}`

3. **Live Headless COM Instantiation**:
   - Executed live PowerShell script instantiating `New-Object -ComObject "Robot.Application"`.
   - Reported: `Visible = 0`, `Version = 26`, `ProgramVersion = "39.0.1.11984"`.
   - Supports non-interactive automated execution via `robot.Interactive = 0` and `robot.Visible = 0`.

4. **Units Architecture**:
   - Internal calculation engine (`iapp.Project.CalcEngine`) and results (`iapp.Project.Structure.Results`) strictly use **base SI units**: Coordinates and lengths in meters (`m`), Forces in Newtons (`N`), Moments in Newton-meters (`N·m`), Displacements in meters (`m`), Stresses in Pascals (`N/m²`).
   - Verified via `Tutorial\ie\tut\tut_102.html`: `"UNITS. All returned results are values in the SI system."`
   - Presentation units are controlled via `iapp.Project.Preferences.Units` (`IRobotUnitMngr`). Setting `unitMngr.UseMetricAsDefault = true; unitMngr.Refresh();` configures Force to `kN`, StructureDim to `m`, Moment to `kN*m`, and Stress to `MN/m²` (MPa).

5. **Structural Objects & FEA Calculation**:
   - Executed end-to-end FEA calculation live in Robot 2026 via COM:
     - Created 2D Frame project (`I_PT_FRAME_2D = 1`).
     - Created 2 nodes: Node 1 (0, 0), Node 2 (6, 0).
     - Created Bar 1 connecting Node 1 and Node 2.
     - Assigned boundary labels: Node 1 `"Fixed"`, Node 2 `"Pinned"`.
     - Assigned cross-section `"W 16x40"`.
     - Created Load Case 1: Dead load (`I_LRT_DEAD = 7`, `I_DRV_ENTIRE_STRUCTURE = 15`).
     - Created Load Case 2: Live load (`I_LRT_BAR_UNIFORM = 5`, `I_BURV_PZ = 2`, value `-10000 N/m` applied to Bar 1).
     - Executed `Calculate()`: Returned `-1` (`Results.Available = -1`).
     - Reaction verification: Node 1 $F_Z = 37,500\text{ N}$ ($37.5\text{ kN}$), $M_Y = -45,000\text{ N}\cdot\text{m}$ ($-45\text{ kN}\cdot\text{m}$); Node 2 $F_Z = 22,500\text{ N}$ ($22.5\text{ kN}$).
     - Midspan moment: $M_Y = 22,500\text{ N}\cdot\text{m}$ ($+22.5\text{ kN}\cdot\text{m}$ relative to beam chord, matching exact hand mechanics).

6. **RTD Snapshot Capability**:
   - Executed `robot.Project.SaveAs(".../test_robot_model.rtd")`.
   - Successfully generated a valid, readable 671,744 byte `.rtd` binary file.

---

## 2. Logic Chain

1. **Host-Neutral Architecture Compliance**:
   - Following `McpShared` conventions established for Revit, AutoCAD, Navisworks, ETABS, Civil 3D, SAP2000, Power BI, and Excel:
   - Robot Structural Analysis Professional is an out-of-process COM host, matching the exact architecture of `HPSap2000` and `HPEtabs`.
   - Therefore, `HPRobot.McpBridge` should be implemented as a standalone WPF application (.NET 8.0-windows) running beside `robot.exe`, holding the COM attachment, listening on Named Pipe `hprobot-mcp-2026`, with MaterialDesignThemes 5.3.2 UI.

2. **Assembly Referencing & Roslyn Runtime Scripting**:
   - Observation 1 & 2 showed `Interop.RobotOM.dll` is an external vendor assembly of 1.89 MB.
   - If `<EmbedInteropTypes>true</EmbedInteropTypes>` were used, Roslyn dynamic script compilation would fail because `typeof(IRobotApplication).Assembly` would resolve to the host bridge assembly instead of the complete interop metadata library.
   - Therefore, `<Reference Include="Interop.RobotOM"><HintPath>$(RobotInstallDir)Interop.RobotOM.dll</HintPath><Private>false</Private><EmbedInteropTypes>false</EmbedInteropTypes></Reference>` must be used, with an assembly resolver (`RobotAssemblyResolver`) dynamically resolving it at runtime from `$(RobotInstallDir)`.

3. **Units Handling & Script Standardization**:
   - Observation 4 proved that Robot's numerical engine computes and extracts internal forces in Newtons (`N`) and moments in `N·m`.
   - However, structural engineers and MCP clients expect standard Metric units: meters (`m`), kilonewtons (`kN`), kilonewton-meters (`kN·m`), and megapascals (`MPa`).
   - Therefore, `RobotUnitsPolicy` must enforce `unitMngr.UseMetricAsDefault = true` during execution, restore previous user settings in `finally`, and seed tools must scale raw force results by $10^{-3}$ to present clean `kN` and `kN·m` to MCP clients.

4. **3-Tier Safety & Snapshot Recovery**:
   - Observation 5 & 6 showed that structural mutations (`Create`, `SetLabel`, `SetValue`) and calculations (`Calculate`) immediately alter the model state.
   - Therefore, Roslyn AST analyzer `RobotTierAnalyzer` must classify scripts into `Read` (queries), `Write` (geometry/loads mutations), and `Delete/Heavy` (element deletion or `Calculate()`).
   - `Write` and `Delete/Heavy` operations must trigger `RobotSnapshotManager` to commit the model via `SaveAs` and preserve an `.rtd` backup in `.hprobot_snapshots/` before script execution.

---

## 3. Caveats

- **Active Model Requirement**: RobotOM methods on `Project.Structure` throw COM exceptions if no project is active (`robot.Project.IsActive == 0`). The bridge must check `IsActive != 0` before dispatching structural scripts.
- **Elevation/Admin Mismatch**: If Robot is launched as Administrator while the Bridge runs as standard user (or vice versa), Windows COM ROT (Running Object Table) will isolate the instances, causing `GetActiveObject` to fail. The bridge UI must document and detect this condition.
- **Asynchronous FEA Dialogs**: In GUI mode, large meshing or non-linear iterations can display progress dialogs unless `robot.Interactive = 0` is set.
- **Section Databases**: Available section labels depend on the user's active databases (e.g. AISC, Eurocode, French, etc.). New projects without loaded sections require loading via `IRobotBarSectionData.LoadFromDBase()`.

---

## 4. Conclusion

1. **Technical Feasibility**: 100% confirmed. The Autodesk Robot 2026 COM API (`RobotOM.dll`) provides comprehensive, robust, and lightning-fast out-of-process automation for geometry, properties, loads, analysis, and results extraction.
2. **Deliverables Delivered**:
   - Comprehensive technical analysis written to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_survey_robot_2\analysis.md`.
   - Complete object model, enum definitions, script execution globals, 3-tier safety rules, units policy, and 12 embedded seed tool specs documented.
3. **Ready for Next Phases**:
   - Phase 0: McpShared Robot host profile & contracts integration.
   - Phase 1: `HPRobot.McpBridge` standalone WPF app.
   - Phase 2: `HPRobot.Mcp.Server` stdio server with 24 tools.
   - Phase 3: Automated test suites & live verify harness.

---

## 5. Verification Method

To independently verify these findings on the development machine:

1. **Verify Binary & Metadata**:
   ```powershell
   powershell -NoProfile -Command "$asm = [System.Reflection.Assembly]::LoadFile('C:\Program Files\Autodesk\Robot Structural Analysis Professional 2026\Exe\Interop.RobotOM.dll'); Write-Host 'Types:' $asm.GetTypes().Count"
   ```
   *Expected result*: `Types: 3078`

2. **Verify COM Instantiation & Version**:
   ```powershell
   powershell -NoProfile -Command "$r = New-Object -ComObject 'Robot.Application'; Write-Host 'Ver:' $r.Version 'ProgVer:' $r.ProgramVersion; $r.Quit(1)"
   ```
   *Expected result*: `Ver: 26 ProgVer: 39.0.1.11984`

3. **Verify End-to-End FEA Pipeline**:
   Run the live test script created during investigation:
   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_survey_robot_2\test_live_fea_pipeline.ps1"
   ```
   *Expected result*: Exits with code 0, prints reactions `FZ1=37500 N, MY1=-45000 N*m, FZ2=22500 N`, and confirms `FEA Pipeline test completed successfully!`.
