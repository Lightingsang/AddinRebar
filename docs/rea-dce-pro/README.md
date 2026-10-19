# Reverse Engineering Technical Dossier: DCE Pro Structural Ecosystem
## Forensic Binary Inventory, Architecture Deconstruction & Clean-Room Specification

**Repository Target**: `HPDcePro` (`HPDcePro/HPDcePro.slnx`)  
**Analyzed Target Path**: `C:\Dce Pro` (Strictly Read-Only Baseline)  
**Investigation Methodology**: Static PE/COFF Analysis, IL Metadata Decompilation (`ilspycmd` 11.1.0), SQLite Schema Probing, AutoLISP AST Parsing, Dynamic P/Invoke Signature Extraction  
**Compliance Standard**: Clean-Room Reverse Engineering & HP Clean Code Governance (`docs/clean-code/HP_CLEAN_CODE_CORE.md`)

---

## 1. Executive Summary

This dossier presents the exhaustive, forensic reverse-engineering analysis of the legacy **DCE Pro** structural engineering software ecosystem located at `C:\Dce Pro`. DCE Pro represents a multi-decade Vietnamese structural detailing and design automation suite integrating AutoCAD, ETABS, CSI SAP2000/Safe, Microsoft Excel, and Autodesk Revit.

The software automates the full lifecycle of reinforced concrete (RC) and structural steel engineering: from structural framing plan generation and tributary load evaluation, to 3D finite-element modeling in ETABS, member sizing according to Vietnamese Standards (TCVN 5574:2018, TCVN 10304:2025, TCVN 5575:2024, TCVN 2737:2023), automated drawing production in AutoCAD, 1D cutting stock optimization for commercial rebar lengths (11,700 mm), and natural language processing (NLP) machine learning models for drawing callout interpretation.

The findings in this dossier establish the authoritative technical blueprint for **`HPDcePro`** (`HPDcePro.slnx`), a high-performance, pure C# .NET clean-room implementation that preserves complete domain fidelity and backward compatibility while eliminating all legacy unmanaged C++ binaries, background COM automation vulnerabilities, and restrictive licensing/telemetry systems.

---

## 2. Verified Binary Inventory & Evidence Catalog

Every binary assembly and database located in `C:\Dce Pro` was inspected in strictly read-only mode. Forensic hashes (SHA-256), portable executable (PE) headers, machine types, and target frameworks were verified:

| File Name & Path | Exact Size (Bytes) | Size (MB) | Machine / Subsystem | CLR (.NET) | Target Framework | SHA-256 Digest | REA Evidence ID |
|---|---|---|---|---|---|---|---|
| `C:\Dce Pro\DCE Pro.dll` | 46,747,544 | 44.58 | x86 (0x010B) | **True** | .NET Framework 4.0 | `8f7b56d1f916351fb91a8464628471499f59006ac3ba5e362b9df8dbc1225780` | `ev_c1bcd4621c3710ffb087bed3d8120354cc071ae032c61a7ad12027fdc929983e` |
| `C:\Dce Pro\DceAnalyssiss64.dll` | 346,624 | 0.33 | AMD64 (0x020B) | **False** | Native C/C++ (MSVC) | `eee3663ab1d345b16532566a0fc1199f544730a4e9e7ac6282863c1a9c75dfbd` | `ev_dce_analyssiss64_native_cdecl_69exports` |
| `C:\Dce Pro\AddinSteel\DceSteel.dll` | 21,345,280 | 20.36 | x86 (0x010B) | **True** | .NET Framework 4.0 | `d99486e9ae08c0776dc4970fe8a5584ef94d2675baba1c6d234e9e4a2a42e213` | `ev_dcesteel_managed_net40_csioapi_tcvn5575` |
| `C:\Dce Pro\AddinSteelCad\DbimSteelAutocad.dll` | 4,401,664 | 4.20 | x86 (0x010B) | **True** | .NET Framework 4.0 | `0ae34512af8106db0e8ccca42e1fbe4d1db94db5fccf3b1660d431457c7a0b79` | `ev_dbimsteelautocad_managed_net40_cad_draw` |
| `C:\Dce Pro\V.2020\DCE Excel.dll` | 11,384,216 | 10.86 | x86 (0x010B) | **True** | .NET Framework 4.0 | `6f2e820a8cd10c39fa4843ccf863699bd61b432713a531c0d2272320070374be` | `ev_dceexcel_managed_net40_udf_calc_sheets` |
| `C:\Dce Pro\AddinSetup_RVT\DBIM_RVT_26.exe` | 9,901,552 | 9.44 | x86 GUI Exe | **False** | Inno Setup 6.1.0 | `eec8a374a1b106794707117e9ba079f06e4544d9d9aad3319f9bc8e9090f8c34` | `ev_dbim_rvt_26_inno_setup_revit2026_addin` |
| `C:\Dce Pro\MayHoc\mauhoc.db` | 8,982,528 | 8.57 | SQLite 3 format | **N/A** | SQLite Database | `74eb37741c999c677df271a6c8007d2d09b704fb16c7aff884f4f11ab624e1a8` | `ev_5b5a0aff2c55fc84157b43574b7f79175f0dcda7e6d1ae96721a97d1d17a0241` |

### Auxiliary & Secondary Host Binaries
- `C:\Dce Pro\DceAnalyssiss32.dll` (287,744 bytes, x86 PE32 Native, SHA-256: `e4c14f521123acad09326c67c269e9bd18d382c4fd2fbb59da0b9cec0d193793`): 32-bit counterpart of `DceAnalyssiss64.dll` exporting the same 69 functions.
- `C:\Dce Pro\ZwDCE Pro.dll` (46,772,120 bytes, x86 PE32, .NET 4.0, SHA-256: `c42ad424a2b3ceb087360e02d94d071efcf9fa64d7bcf7e0c6eccaccbc87df8e`): ZWCAD host adapter.
- `C:\Dce Pro\IcDCE Pro.dll` (46,933,912 bytes, x86 PE32, .NET 4.0, SHA-256: `c76ba3c87071d9450816ce9c94779602eea38bd474e2157aa7c6c3cae1cd81b1`): IntelliCAD / BricsCAD host adapter.
- `C:\Dce Pro\DceUpdate\DceUpdate.exe` (958,464 bytes, x86 PE32, .NET 4.0, SHA-256: `a422598bf5208d57dbf2eb6fb586d8dbabbf29ae4ee59301bfe10043944f4489`): Background updater and watchdog process.

---

## 3. Native vs. Managed Architecture & P/Invoke Mechanisms

### 3.1 Architectural Decomposition
The legacy system operates as a hybrid 2-tier architecture:
1. **Managed Host Tier (.NET Framework 4.0)**:
   - Primary business logic, WinForms UI, and AutoCAD ObjectARX / .NET wrapper interop reside in `DCE Pro.dll`, `DCE Excel.dll`, and `DceSteel.dll`.
   - Communication with AutoCAD is handled via `Autodesk.AutoCAD.Runtime`, `Autodesk.AutoCAD.DatabaseServices`, and `Autodesk.AutoCAD.EditorInput`.
   - Communication with Microsoft Excel is handled via COM Interop (`Microsoft.Office.Interop.Excel`) and Excel-DNA XLL add-ins.
2. **Native Computational Tier (Unmanaged Win64/Win32 C++)**:
   - High-throughput 2D geometry, point-in-polygon containment, polygon intersection, and framing topology calculations are offloaded to `DceAnalyssiss64.dll` (AMD64) or `DceAnalyssiss32.dll` (x86).

```
┌────────────────────────────────────────────────────────┐
│                   AutoCAD Process                      │
│                                                        │
│  ┌───────────────────────┐   ┌──────────────────────┐  │
│  │      DCE Pro.lsp      │   │     DCE Pro.dll      │  │
│  │ (Command Reactor/Cmd) │──>│ (.NET Framework 4.0)│  │
│  └───────────────────────┘   └──────────┬───────────┘  │
│                                         │              │
│                           Dynamic P/Invoke Binding     │
│                           LoadLibrary/GetProcAddress   │
│                                         │              │
│                                         ▼              │
│                              ┌──────────────────────┐  │
│                              │ DceAnalyssiss64.dll  │  │
│                              │  (Native C++, 69 fn) │  │
│                              └──────────────────────┘  │
└────────────────────────────────────────────────────────┘
```

### 3.2 Dynamic Runtime P/Invoke Binding
Unlike standard .NET applications that use compile-time `[DllImport("DceAnalyssiss64.dll")]` declarations, DCE Pro utilizes dynamic runtime binding inside `ClassLibrary1.NativeInterop`:
- Evaluates pointer width (`IntPtr.Size == 8`) to choose between `DceAnalyssiss64.dll` and `DceAnalyssiss32.dll`.
- Invokes Win32 kernel methods via P/Invoke:
  ```csharp
  [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
  private static extern IntPtr LoadLibrary(string lpFileName);

  [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
  private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
  ```
- Converts unmanaged function pointers to managed delegates using `Marshal.GetDelegateForFunctionPointer` and caches them in a `ConcurrentDictionary<string, Delegate>`.
- Calls native exports using standard C calling convention (`CallingConvention.Cdecl`).

### 3.3 Forensic Deconstruction of `ClassLibrary1.DAnalysisss`
During reverse engineering, special scrutiny was directed toward `ClassLibrary1.DAnalysisss`, ostensibly named to resemble finite element or geometric analysis:
```csharp
public class DAnalysisss
{
    public bool normalizedVectorr();
    public void CreateNormalOrigin(ref ResponseModel Checksever);
    public bool Plansever(string CheckseverPlan, string DCELicenseKey);
}
```
**Forensic Verdict**: This class is a **camouflaged anti-reverse-engineering decoy**. Despite the geometric naming conventions (`normalizedVectorr`, `CreateNormalOrigin`), the implementation contains **zero mathematical or structural code**. Its entire internal routine consists of serializing license payloads, computing machine hashes, and dispatching HTTP REST calls to remote authorization servers (`CheckseverPlan`, `DCELicenseKey`).

**Clean-Room Decision**: `ClassLibrary1.DAnalysisss` is completely excluded from `HPDcePro`. The genuine 2D geometry functions wrapped by `NativeInterop` are ported to pure, cross-platform C# in `HPDcePro.Core.Geometry`.

---

## 4. Summary of Legacy DRM, Telemetry, and Updater Systems to Exclude

To ensure `HPDcePro` is a permanent, clean-room, privacy-respecting, and fully offline-capable engineering solution, all legacy licensing, telemetry, and auto-update mechanisms have been forensic-mapped for **strict omission**:

```
                       LEGACY DRM LAYER (C:\Dce Pro)
                                  │
         ┌────────────────────────┼────────────────────────┐
         ▼                        ▼                        ▼
  Remote Account API       Node-Lock & WMI          Tamper & Watchdog
  - DbimTaiKhoanApi        - DceSerinumber          - DceGuard
  - LicenseCheckAPI        - DceFingerprint         - DceUpdate.exe
  - LicenseMoi             - WMI MAC/CPU/Disk       - CheckAssemblyIntegrity
         │                        │                        │
         └────────────────────────┼────────────────────────┘
                                  ▼
                 TOTAL OMISSION IN HPDCEPRO (100% CLEAN)
```

### 4.1 Remote Account & License Verification APIs
- **`DbimTaiKhoanApi`**: Manages remote HTTPS REST authentication, seat leases, password resets, and license token validation (`GoiApiTaiKhoan`, `LayLicenseKhaDung`, `KiemTraDangNhap`).
- **`LicenseMoi` & `LicenseCheckAPI`**: Data transfer objects (DTOs) transmitting hardware identifiers, active license keys, customer IDs, device expiration dates, and VIP status indicators.
- **Exclusion Requirement**: No network connections, REST calls, or seat tokens exist in `HPDcePro`.

### 4.2 Hardware Fingerprinting & WMI Harvesting
- **`DceSerinumber`**: Queries the Windows Management Instrumentation (WMI) subsystem using `System.Management`:
  - `Win32_Processor.ProcessorId`
  - `Win32_BaseBoard.SerialNumber`
  - `Win32_DiskDrive.Signature` / SerialNumber
  - Network Adapter MAC addresses via `NetworkInterface.GetAllNetworkInterfaces()`
  - Computes MD5 and custom rolling checksums (`Serinumber()`, `Serinumber2()`, `Serinumber3()`).
- **`DceFingerprint`**: Harvests full system profiles (`BatDauThuThap`, `LayJson`) into JSON payloads transmitted to remote servers.
- **Exclusion Requirement**: Zero WMI querying, zero hardware telemetry, and zero device-locked licensing in `HPDcePro`.

### 4.3 Background Watchdog & Auto-Updater Persistence
- **`DceUpdate.exe`**: Executable that downloads binary patches and uses `RestartManagerHelper` to release locked CAD DLLs.
- **`DceGuard`**: Implements persistent background watchdog monitoring (`CaiDatDceGuardAsync`, `DamBaoDceGuardNgamAsync`, `KichHoatLaiDceGuardAsync`) to prevent modification or debugging of installed assemblies.
- **Exclusion Requirement**: Zero background services, watchdog processes, or forced updater executables in `HPDcePro`.

### 4.4 Binary Anti-Tamper Checksums
- `NativeInterop.CheckIntegrity`: Reads cryptographic hashes from external verification files (`wnaspi32.dat`, `zxcvmn91qw.txt`) and asserts MD5 match before allowing execution.
- Native exports `CheckAssemblyIntegrity` and `GenerateIntegrityFile` in `DceAnalyssiss64.dll`.
- **Exclusion Requirement**: Removed entirely; `HPDcePro` relies on standard .NET assembly verification and open source unit test assertions.

---

## 5. Architectural Dossier Index

The detailed technical specifications for each subsystem of the reverse-engineered suite are organized into four companion technical documents:

1. **[`01-domain-architecture.md`](01-domain-architecture.md)**:
   - RC Beam Detailing & Optimization (`DceDamMatCatMoi`, `DceShopBeam`, `DuAnVeDam`).
   - Column & Shear Wall Detailing (`DceColumn`, `DceWall`).
   - Slab Detailing (`DceSlab`).
   - Foundation & Pile Capacity (`DceMongCocMoi`, TCVN 10304:2025).
   - Stairs & Lintel Detailing (`DceThangMoi`).
   - Rebar Schedule Aggregation & 1D Cutting Stock Optimization (11,700 mm).
   - The 13 CAD XData Schemas (`RebarXdataDCE`, `DceRebarXDataTKT`, `Links_XDataObject`, etc.).
   - Pure C# 2D Geometry Engine replacing all 69 native functions in `DceAnalyssiss64.dll`.

2. **[`02-autostr-and-mayhoc.md`](02-autostr-and-mayhoc.md)**:
   - The AutoStr 24-Step Automated Structural Engineering Workflow (`AutoStrStep01` to `AutoStrStep24`).
   - Complete `.autostr` JSON Schema and project serialization model (`AutoStrConfig`).
   - Machine Learning Rebar Tokenizer (`mauhoc.db` SQLite schema with 45,020 samples).
   - NLP feature extraction flags and ML.NET token classification pipeline.

3. **[`03-steel-and-excel.md`](03-steel-and-excel.md)**:
   - Structural Steel Design modules (`DceSteel.dll`, `DbimSteelAutocad.dll`, TCVN 5575:2024, TCVN 2737:2023).
   - Excel calculation ecosystem (>85 `.xlsm` workbooks).
   - Decompiled Excel-DNA XLL Custom User Defined Functions (UDFs).
   - ClosedXML headless schedule and calculation report replacement engine.

4. **[`04-command-mapping-and-roadmap.md`](04-command-mapping-and-roadmap.md)**:
   - Complete 69-command mapping table from `DCE Pro.lsp` to `HPDcePro.AutoCad`.
   - 6 Pure AutoLISP utilities (`GenC`, `rdt`, `tencaukien`, `t2m`, `CHIAVIEWPORT`, `DCENAP`).
   - Clean, production-ready `HPDcePro.lsp` loader script.
   - Staged 6-phase implementation roadmap for `HPDcePro`.
