# Project: HPDcePro 100% Standalone Reverse-Engineering & Truth Matrix

## Architecture
- **Ecosystem**: `HPDcePro/HPDcePro.slnx` (.NET 8.0 SDK 10.0.300, Microsoft.Testing.Platform).
- **Core Domain (`HPDcePro.Core`)**: Target `netstandard2.0;net8.0`. Pure domain structural calculations (TCVN 5574:2018, TCVN 10304:2025, TCVN 5575:2024, TCVN 2737:2023), 2D CAD drafting buffer primitives, AutoStr 24-step engine, 1D cutting stock optimizer, 13 RegApps XData codecs, Machine Learning reader (`mauhoc.db`). Zero Autodesk references.
- **UI Subsystem (`HPDcePro.UI`)**: Target `net8.0-windows`. Shared WPF MVVM library with `MahApps.Metro` 2.4.11 and Material theme tokens. Hosts all specialized engineering dialogs (`DamCong_Frm`, `MongCoc_Frm`, `MongCoc_FrmLopThepThem`, `MongCoc_FrmChonThepTuDong`, `ThangMoi_Frm`, `CocBT_Frm`, `TKT_frmMain`, `AutoStrWindow`, `Bea_ToiUuCatThepV2`), Palette controls (`DcePaletteControl`), and Ribbon builders (`DceRibbon`).
- **AutoCAD Add-In (`HPDcePro.AutoCad`)**: Target `net8.0-windows`. AutoCAD 2026 .NET add-in. Implements 311 `<CommandMethod>` endpoints, 11 interactive `DrawJig`/`EntityJig` classes, real selection prompts (`Editor.GetSelection`, `Editor.GetPoint`, `Editor.GetEntity`, `Editor.GetKeywords`), real block insertion from relative assets, 13 RegApps XData management, dockable `PaletteSet`, and CUIX integration.
- **Revit Add-In (`HPDcePro.Revit`)**: Target `net8.0-windows`. Revit 2026 add-in (`Nice3point.Revit.Api`). Implements all 23 structural commands (`CommandAddBeam`, `CommandAddColumn`, `CommandAddGrid`, `CommandAddSlab`, `CommandAddWall`, `CommandAutoCut`, `CommandAutoJoin`, `CommandAddRebarBeam`, etc.) and `AdskApplication` ribbon integration.
- **Excel Subsystem (`HPDcePro.Excel`)**: Target `net8.0`. Headless ClosedXML calculation bridge and COM UDF dispatch matching all 73 actions from `danhba_dce_excel.json`.
- **MCP Server (`HPDcePro.Mcp`)**: Target `net8.0`. Stdio Model Context Protocol server exposing 12 structural tools (`hpdcepro-mcp`).
- **Desktop Hub (`HPDcePro.Desktop`)**: Target `net8.0-windows`. Standalone engineering workstation launching `MainControlCenterWindow`.
- **Assets (`HPDcePro/assets/`)**: Self-contained engineering assets directory (17 DWGs, 132 XLSMs, `mauhoc.db` + `model.zip`, 178 System files, 8 Language dictionaries, 133 Icons, 7 CUIX menus) with 100% relative path resolution.

## Feature Inventory
| # | Feature | Description | Milestone | Source |
|---|---------|-------------|-----------|--------|
| 1 | Tooling & Artifact Verification | Verify `rea@6.6.0`, `ilspycmd`, extract `DBIM_RVT_26.exe` to scratch dir | M1 | Survey 1, R1 |
| 2 | Master Truth Matrix Generation | Generate `docs/rea-dce-pro/TRUTH_MATRIX.md` mapping all 311 CAD commands, 73 Excel actions, 49 Steel functions, 29 MBKC functions, 69 native C++ exports, 23 Revit commands, 11 Jigs, 12 UI forms, and 24 AutoStr steps | M1 | Survey 1, 3, R1 |
| 3 | Mocks & Obfuscation Classification | Label existing sample mocks as `ĐANG LÀ MOCK` and uninspectable bodies as `CHƯA GIẢI MÃ ĐƯỢC` with token/RVA | M1 | Survey 1, 2, R1 |
| 4 | Static Asset Directory Population | Copy 17 DWGs, 132 XLSMs, `mauhoc.db` + `model.zip`, 178 System files, 8 Lang dicts, 133 Icons, 7 CUIX into `HPDcePro/assets/` | M2 | Survey 3, R2 |
| 5 | Relative Path Normalization | Update LISP, config txt files, and C# source to resolve relative to `HPDcePro/assets/` with 0 references to `C:\Dce Pro` | M2 | Survey 3, R2 |
| 6 | 11 CAD Interactive Jigs Implementation | Implement all 11 `DrawJig`/`EntityJig` classes (`VeMBKC_JIG`, `JigSlabRebar`, `TagLeader`, `TagChiaThep`, `ijgRaithepchamtron`, `ijgVeDaiVong`, `JigVeDaiC`, `VePolylineBatKy`, `ijgVeThepVachCT`, `JigVeVachHam`, `DimNhanhMBKC`) in `HPDcePro.AutoCad/Jigs/` | M3 | Survey 1, 2, R3 |
| 7 | Specialized UI Forms & Palettes | Implement 12 specialized dialogs (`DamCong_Frm`, `MongCoc_Frm`, `MongCoc_FrmLopThepThem`, `MongCoc_FrmChonThepTuDong`, `ThangMoi_Frm`, `CocBT_Frm`, `TKT_frmMain`, `AutoStrWindow`, `Bea_ToiUuCatThepV2`), `DcePalette`, and `DceRibbon` in `HPDcePro.UI` | M3 | Survey 1, 2, R3 |
| 8 | AutoCAD Selection & Command Realization | Replace all `CalculateSample*` and dummy drawing methods with interactive `Editor.GetSelection`, `Editor.GetPoint`, `Editor.GetEntity`, and Jigs across all 311 commands | M3 | Survey 2, 3, R3 |
| 9 | 13 RegApps XData & AutoStr Pipeline Realization | Implement real 13 RegApps XData read/write and connect 24 AutoStr steps to authentic domain calculations and CAD buffers | M3 | Survey 2, 3, R3 |
| 10 | 23 Revit Structural Commands Migration | Implement all 23 Revit commands (`CommandAddBeam`, `CommandAddColumn`, `CommandAddGrid`, `CommandAddSlab`, `CommandAddWall`, `CommandAutoCut`, `CommandAutoJoin`, `CommandAddRebarBeam`, etc.) and `AdskApplication` in `HPDcePro.Revit` | M4 | Survey 1, 3, R3 |
| 11 | 73 Excel Ribbon Actions & UDFs Migration | Implement all 73 Excel ribbon dispatch actions and ClosedXML UDF bridges in `HPDcePro.Excel` | M4 | Survey 3, R3 |
| 12 | Single-Node Sequential Build Verification | Compile `HPDcePro/HPDcePro.slnx` with `-c Release` and `-c Debug` via `--disable-build-servers -m:1` with 0 warnings, 0 errors | M5 | AC, R4 |
| 13 | 100% Test Pass & Adapter Verification | Execute all unit and adapter test suites with 100% green tests | M5 | AC, R4 |
| 14 | Clean Code Governance Compliance | Verify <=300 lines/file, <=50 lines/method, 0 dead code, 0 unannotated catch across all files in `HPDcePro/` | M5 | AC, R4 |
| 15 | Forensic Integrity Audit | Independent Forensic Auditor verification with binary veto against cheating, dummy returns, or mock fabrication | M5 | Hard Constraint |

## Milestones
| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| M1 | Comprehensive Binary Analysis, Package Extraction & Master Truth Matrix | Features 1, 2, 3: Extract `DBIM_RVT_26.exe`, catalog assemblies via `rea` & `ilspycmd`, generate `docs/rea-dce-pro/TRUTH_MATRIX.md` | Survey done | DONE |
| M2 | Standalone Asset & Template Migration | Features 4, 5: Copy and normalize all 17 DWGs, 132 XLSMs, `mauhoc.db` + `model.zip`, 178 System files, 8 Lang dicts, 133 Icons into `HPDcePro/assets/`; eliminate all hardcoded `C:\Dce Pro` paths | M1 | PLANNED |
| M3 | 1:1 AutoCAD Functional, UI & Interactive Jig Migration | Features 6, 7, 8, 9: Implement 11 Jigs, 12 specialized UI forms/palettes/ribbons, real CAD prompts/selections, 13 RegApps XData, and 24 AutoStr steps | M2 | PLANNED |
| M4 | 1:1 Revit & Excel Functional Migration | Features 10, 11: Implement 23 Revit commands + `AdskApplication` in `HPDcePro.Revit`; implement 73 Excel actions and UDF bridge in `HPDcePro.Excel` | M2 | PLANNED |
| M5 | Final Build, 3-Layer Parity & Forensic Integrity Audit | Features 12, 13, 14, 15: Single-node Release/Debug build, 100% test pass, clean code audit, and forensic integrity audit | M1, M2, M3, M4 | PLANNED |

## Interface Contracts

### 1. Interactive CAD Jigs (`HPDcePro.AutoCad.Jigs`)
```csharp
namespace HPDcePro.AutoCad.Jigs;

public abstract class DceDrawJigBase : Autodesk.AutoCAD.EditorInput.DrawJig
{
    public abstract PromptStatus StartJig(Editor editor);
}

public sealed class VeMBKCJig : DceDrawJigBase { ... }
public sealed class JigSlabRebar : DceDrawJigBase { ... }
public sealed class TagLeaderJig : DceDrawJigBase { ... }
public sealed class TagChiaThepJig : DceDrawJigBase { ... }
public sealed class JigRaiThepChamTron : DceDrawJigBase { ... }
public sealed class JigVeDaiVong : DceDrawJigBase { ... }
public sealed class JigVeDaiC : DceDrawJigBase { ... }
public sealed class VePolylineBatKyJig : DceDrawJigBase { ... }
public sealed class JigVeThepVachCt : DceDrawJigBase { ... }
public sealed class JigVeVachHam : DceDrawJigBase { ... }
public sealed class DimNhanhMbkcJig : DceDrawJigBase { ... }
```

### 2. Specialized UI Forms (`HPDcePro.UI.Views`)
```csharp
namespace HPDcePro.UI.Views;

// Inherit MahApps.Metro.Controls.MetroWindow
public partial class DamCongWindow : MetroWindow { ... }
public partial class MongCocWindow : MetroWindow { ... }
public partial class MongCocLopThepThemWindow : MetroWindow { ... }
public partial class MongCocChonThepTuDongWindow : MetroWindow { ... }
public partial class ThangMoiWindow : MetroWindow { ... }
public partial class CocBtWindow : MetroWindow { ... }
public partial class MasterRebarScheduleDashboardWindow : MetroWindow { ... } // TKT_frmMain
public partial class AutoStrExecutionWindow : MetroWindow { ... } // AutoStrWindow live progress
public partial class CuttingOptimizationAdvancedWindow : MetroWindow { ... } // Bea_ToiUuCatThepV2
```

### 3. Asset Resolver Contract (`HPDcePro.Core.Common.AssetPathResolver`)
```csharp
namespace HPDcePro.Core.Common;

public static class AssetPathResolver
{
    public static string GetAssetsRoot();
    public static string ResolveCadTemplate(string filename);
    public static string ResolveExcelWorkbook(string relativePath);
    public static string ResolveSystemConfig(string filename);
    public static string ResolveLanguageDict(string relativePath);
    public static string ResolveMachineLearningDatabase();
    public static string ResolveMachineLearningModelZip();
}
```

## Code Layout
- `HPDcePro/src/HPDcePro.Core/`: Pure calculations, AutoStr steps, ML reader, XData codecs, AssetPathResolver.
- `HPDcePro/src/HPDcePro.UI/`: Specialized WPF dialogs, palettes, themes, and ViewModels.
- `HPDcePro/src/HPDcePro.AutoCad/`: 311 commands, 11 Jigs, prompt readers, block inserters, ribbon setup.
- `HPDcePro/src/HPDcePro.Revit/`: 23 structural commands, AdskApplication, geometry extractors, rebar generators.
- `HPDcePro/src/HPDcePro.Excel/`: 73 Ribbon dispatch handlers, ClosedXML reports, TCVN UDF bridge.
- `HPDcePro/src/HPDcePro.Mcp/`: Stdio MCP server tool handlers.
- `HPDcePro/src/HPDcePro.Desktop/`: Standalone workstation runner.
- `HPDcePro/assets/`: 17 DWGs, 132 XLSMs, `mauhoc.db`, `model.zip`, 178 System files, 8 Lang dicts, 133 Icons, 7 CUIX.
- `docs/rea-dce-pro/TRUTH_MATRIX.md`: Master function-by-function compatibility matrix.

## Clean Code Governance
- **Rule C2**: Max 300 lines per `.cs` file.
- **Rule M3**: Max 50 lines per method.
- **Rule Q-B1**: 0 dead or commented-out code.
- **Rule Q-B2**: 0 unannotated empty catch blocks.
- **TreatWarningsAsErrors**: Build must succeed with 0 warnings, 0 errors.
- **Sequential Build**: `--disable-build-servers -m:1`.
- **Integrity**: Forensic Auditor veto is non-negotiable. Zero hardcoded test outputs or mock fabrication.
