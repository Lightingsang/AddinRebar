# Milestone M2 Handoff Report: Asset Inventory & Target Layout Exploration

**Author**: explorer_m2_gen2_1  
**Date**: 2026-10-18  
**Target Milestone**: M2 — Standalone Asset & Template Migration  
**Status**: Complete (Read-Only Exploration & Execution Blueprint)  
**Parent Agent**: 1e55a9de-0d52-405d-83fa-fa58bb05a293  

---

## 1. Observation

### 1.1 Baseline Inventory: Legacy `C:\Dce Pro\` (Strictly Read-Only)
Direct PowerShell filesystem queries against `C:\Dce Pro\` yielded exact counts and file sizes across all required asset categories:

#### Category 1: DWG CAD Templates (17 files, 11,973,165 bytes / ~11.42 MB)
- **Directory 1**: `C:\Dce Pro\Cad\` (13 files):
  1. `Form 1.dwg` (376,397 bytes)
  2. `Form 1_Steel.dwg` (1,244,728 bytes)
  3. `Form 2.dwg` (400,630 bytes)
  4. `Form 2_2.dwg` (395,143 bytes)
  5. `Form 2_2_Circle3.2.dwg` (382,953 bytes)
  6. `Form 2_Circle3.2.dwg` (399,377 bytes)
  7. `Form 2_Steel.dwg` (1,592,422 bytes)
  8. `Form 3.dwg` (376,561 bytes)
  9. `Form 3_Steel.dwg` (1,579,252 bytes)
  10. `Form 4.dwg` (372,047 bytes)
  11. `Form 4_Steel.dwg` (1,428,355 bytes)
  12. `Form 5.dwg` (374,878 bytes)
  13. `Form 5_Steel.dwg` (1,423,110 bytes)
- **Directory 2**: `C:\Dce Pro\AutoStr\00.FileDungChung\` (4 files):
  14. `BlockTenBanVe.dwg` (116,934 bytes)
  15. `Chi Tiet Coc 250x250.dwg` (209,264 bytes)
  16. `GhiChuChung 5574 _ A3.dwg` (1,197,969 bytes)
  17. `Khung A3.dwg` (103,145 bytes)

#### Category 2: XLSM Engineering Workbooks (132 files, 85,434,356 bytes / ~81.48 MB)
- **Root**: `C:\Dce Pro\Excel\` (1 file):
  1. `Menu DCE_2020.xlsm` (33,915 bytes)
- **Subdirectory 1**: `C:\Dce Pro\Excel\DceSteel\` (112 files, 57,088,638 bytes):
  - PEB portal frame calculation, ASCE7 / TCVN 2737:2023 wind loads, TCVN 5575:2024 steel members, moment & shear connections (e.g., `01 TCVN 2737 2023 Loading.xlsm`, `20 TCVN 5575 Steel Beam Design - Section I.xlsm`, `211 TCVN 5575 2024 LK Momen Dam-Dam.xlsm`, `59 Thiet ke giang cap.xlsm`, etc.).
- **Subdirectory 2**: `C:\Dce Pro\Excel\File V.2020\` (19 files, 28,311,803 bytes):
  1. `01. DCE_Etabs_2020.xlsm` (97,869 bytes)
  2. `02. DCE_SeismicLoad_2020.xlsm` (1,052,620 bytes)
  3. `03. DCE_ WindLoad_2020.xlsm` (1,295,986 bytes)
  4. `04. DCE_CheckBasicDesign_2020.xlsm` (2,341,207 bytes)
  5. `05. DCE_MongDon.xlsm` (706,407 bytes)
  6. `08. DCE_ColumnWall_2020.xlsm` (1,979,034 bytes)
  7. `09. DCE_Beam_2020.xlsm` (5,327,365 bytes)
  8. `10. DCE_SctCoc_2020.xlsm` (1,519,707 bytes)
  9. `10.1 DCE_SctCoc_10304 2025.xlsm` (4,914,860 bytes)
  10. `10.1 DCE_SctCoc_10304 2025_ThemDong.xlsm` (4,941,787 bytes)
  11. `12. DCE_SLab_2020.xlsm` (1,246,642 bytes)
  12. `13. DCE_DaiCoc_2020.xlsm` (249,978 bytes)
  13. `14. DCE_LanhTo_CauThang_McDam_2020.xlsm` (101,546 bytes)
  14. `15. DCE_ThongKeThep_2020.xlsm` (379,982 bytes)
  15. `16. DCE_TienLuongThongKe.xlsm` (117,067 bytes)
  16. `17. DCE_DuToanKL.xlsm` (1,093,652 bytes)
  17. `18.DCE_Khac Phuc Loi Mat Addin.xlsm` (880,584 bytes)
  18. `20.DCE_VeCocKhoanNhoi.xlsm` (28,814 bytes)
  19. `DBIM - Tong hop lenh Autocad.xlsm` (36,696 bytes)

#### Category 3: Machine Learning Model & Database (2 files, 9,009,426 bytes / ~8.59 MB)
- `C:\Dce Pro\MayHoc\mauhoc.db` (8,982,528 bytes) — SQLite database containing training samples and rebar token annotations.
- `C:\Dce Pro\MayHoc\model.zip` (26,898 bytes) — Trained ML.NET model archive.

#### Category 4: System Configuration Files (178 files, 913,218 bytes / ~892 KB)
- `C:\Dce Pro\System\*.txt` (170 configuration files): UI dialog state, parameter defaults, and toggle states (e.g., `AdsLi.txt`, `BeamShop.txt`, `CalculateMDHa1.txt`...`zxcvmn91qw.txt`).
- Forensic analysis of non-txt files in `System/`:
  - `d3dcompiler.dat`, `dcpc.dat`, `dxgidebug.dat`, `integrity.dat`, `mfplat.dat`, `wnaspi32.dat` (6 `.dat` token files, 64–130 bytes each).
  - `dcpcore.exe` (454,040 bytes): PE executable (InternalName: `DceGuard.exe`, the legacy anti-tamper watchdog).
  - `Cache\dcpcore.bin` (454,040 bytes): Duplicate payload of `DceGuard.exe`.
  - Total in `System/`: exactly 178 files.

#### Category 5: Language Localization Dictionaries (8 files, 1,195,316 bytes / ~1.14 MB)
- **Root**: `C:\Dce Pro\Language\` (4 files):
  1. `en.txt` (88,276 bytes)
  2. `vi.txt` (85,656 bytes)
  3. `zh.txt` (85,204 bytes)
  4. `_HUONGDAN.txt` (2,248 bytes)
- **CAD**: `C:\Dce Pro\Language\Cad\` (4 files):
  5. `en.txt` (363,901 bytes)
  6. `vi.txt` (222,431 bytes)
  7. `zh.txt` (346,043 bytes)
  8. `_HUONGDAN.txt` (1,557 bytes)

#### Category 6: Visual Assets & Icons (133 files, 1,744,675 bytes / ~1.66 MB)
- `C:\Dce Pro\Icon\` (3 files: `DCE.ico`, `logo.png`, `DCE_Logo.png`)
- `C:\Dce Pro\Icon\CauThang\` (1 file: `Thang.jpg`)
- `C:\Dce Pro\Icon\IconCad\` (47 files: ribbon action icons in `.png` and `.jpg`)
- `C:\Dce Pro\Icon\IconRevit\` (13 files: Revit ribbon icons)
- `C:\Dce Pro\Icon\SlabRebar\` (13 files: slab reinforcement pattern icons)
- `C:\Dce Pro\Icon\TKT\` (56 files: rebar schedule shape icons)
- Breakdown by extension: 62 `.png`, 2 `.ico`, 69 `.jpg` = 133 files.

#### Category 7: AutoCAD CUIX Ribbon Menus (7 files, 1,768,113 bytes / ~1.69 MB)
1. `C:\Dce Pro\DBimApps.cuix` (18,771 bytes)
2. `C:\Dce Pro\MenuDCE.cuix` (291,030 bytes)
3. `MenuDCE_ACAD.cuix` (291,030 bytes)
4. `MenuDCE_BCAD.cuix` (291,030 bytes)
5. `MenuDCE_GCAD.cuix` (292,611 bytes)
6. `MenuDCE_ICAD.cuix` (292,611 bytes)
7. `MenuDCE_ZWCAD.cuix` (291,030 bytes)

#### Summary of Core Baseline:
- **Total Files**: **477 files**
- **Total Size**: **112,038,269 bytes (~106.85 MB)**

#### Companion Engineering Assets Discovered:
- `C:\Dce Pro\AutoStr\DuAnMau.autostr` (14,262 bytes) — default sample AutoStr project file.
- `C:\Dce Pro\AutoStr\00.FileDungChung\`:
  - `01.TP-THUYET MINH TKKT.docx` (212,721 bytes) — technical calculation note template.
  - `02.TP-PHU LUC THUYET MINH.docx` (58,743 bytes) — calculation appendix template.
  - `HoatTaiSan_TCVN2737_2023.json` (42,927 bytes) — TCVN 2737:2023 live load database.
- `C:\Dce Pro\AddinSteelCad\SystemSteel\` (112 `.txt` files, 1,286 bytes) — steel detailing parameters (e.g. `ToleSpace.txt`, `W1.txt`...`W8.txt`).

---

### 1.2 Current State of `HPDcePro/assets/` in Repository
- Path: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\assets\`
- **Status**: **DOES NOT EXIST** (`Exists: False`, 0 directories, 0 files).
- The repository root `HPDcePro/` currently contains only: `deploy/`, `src/`, `tests/`, `Directory.Build.props`, `HPDcePro.slnx`, `global.json`.

---

### 1.3 Audit of Hardcoded Paths in Existing Codebase
A grep search across `HPDcePro/src/` revealed exactly 3 active C# source references to `C:\Dce Pro\`:
1. `HPDcePro.Core\MachineLearning\MauHocDatabaseReader.cs:47`:
   `public const string DefaultDatabasePath = @"C:\Dce Pro\MayHoc\mauhoc.db";`
2. `HPDcePro.Desktop\Program.cs:13`:
   `public const string DefaultAutoStrSamplePath = @"C:\Dce Pro\AutoStr\DuAnMau.autostr";`
3. `HPDcePro.Mcp\Execution\Handlers\AutoStrToolHandler.cs:46`:
   `string defaultPath = @"C:\Dce Pro\AutoStr\DuAnMau.autostr";`

Additionally:
- `DuAnMau.autostr` internally contains hardcoded paths:
  ```json
  "ProjectRootPath": "C:\\Dce Pro\\AutoStr",
  "FileGhiChuChung": "C:\\Dce Pro\\AutoStr\\00.FileDungChung\\GhiChuChung 5574 _ A3.dwg",
  "FileKhungTen": "C:\\Dce Pro\\AutoStr\\00.FileDungChung\\Khung A3.dwg",
  "FileBlockTenBanVe": "C:\\Dce Pro\\AutoStr\\00.FileDungChung\\BlockTenBanVe.dwg",
  "FileChiTietCoc": "C:\\Dce Pro\\AutoStr\\00.FileDungChung\\Chi Tiet Coc 250x250.dwg",
  "ExcelGocPath": "C:\\Dce Pro\\Excel\\File V.2020",
  ```
- 17 files in `System/*.txt` contain legacy paths pointing to `C:\Dce Pro\...` (e.g., `AcSmComponents2024.txt`, `AdsLi.txt`, `Peninsula1..5.txt`).
- `HPDcePro.lsp` already uses dynamic relative path resolution `(vl-filename-directory (findfile "HPDcePro.lsp"))` with zero hardcoded `C:\Dce Pro` paths.

---

## 2. Logic Chain

1. **Standalone Requirement**: The goal of Milestone M2 is 100% standalone operation without dependency on `C:\Dce Pro\`. Therefore, all required runtime data (DWGs, Excel workbooks, ML models, configuration defaults, language dictionaries, icons, and menus) must reside within the repository under `HPDcePro/assets/`.
2. **Directory Structure Organization**:
   - The 17 DWG templates originate from two locations: 13 from `Cad/` and 4 from `AutoStr/00.FileDungChung/`. Since there are 0 filename collisions, placing all 17 in `HPDcePro/assets/cad/` allows uniform O(1) resolution by filename via `AssetPathResolver.ResolveCadTemplate(filename)`.
   - The 132 Excel workbooks naturally partition into `excel/Menu DCE_2020.xlsm`, `excel/DceSteel/*.xlsm` (112 files), and `excel/File V.2020/*.xlsm` (19 files). Maintaining this directory hierarchy matches the relative paths used in calculations.
   - Machine learning requires `mauhoc.db` and `model.zip` in `HPDcePro/assets/mayhoc/` (or alias `ml/`).
   - The 178 system files in `System/` map directly to `HPDcePro/assets/system/`.
   - The 8 language dictionaries map directly to `HPDcePro/assets/language/` (root and `Cad/`).
   - The 133 visual assets map directly to `HPDcePro/assets/icons/`.
   - The 7 CUIX menus map directly to `HPDcePro/assets/cuix/`.
   - The AutoStr project configuration `DuAnMau.autostr` and documentation templates map to `HPDcePro/assets/autostr/`.
3. **Execution Safety**:
   - `C:\Dce Pro\` must remain strictly unmodified. Robocopy executes in one-way read-only mode from source to destination.
   - Tested robocopy dry-run (`/L`) commands verified that exactly 477 core files are identified without any file omissions or unexpected path mismatches.
4. **Decoupling Engine (`AssetPathResolver`)**:
   - Hardcoded constants in `MauHocDatabaseReader.cs`, `AutoStrToolHandler.cs`, and `Program.cs` must be replaced by calls to a central `AssetPathResolver` static class in `HPDcePro.Core.Common`.
   - The resolver dynamically detects its execution context: (1) `HPDCEPRO_ASSETS_PATH` env var override, (2) recursive parent folder search starting from `AppDomain.CurrentDomain.BaseDirectory` upwards to locate `assets/`, (3) fallback to `<BaseDirectory>/assets`.

---

## 3. Caveats

1. **Legacy DceGuard Watchdog**: `C:\Dce Pro\System\dcpcore.exe` (454 KB) is a legacy watchdog binary (`DceGuard.exe`). While it accounts for 1 of the 178 files in `System/`, it is intentionally obsolete and neutralized in HPDcePro clean-room architecture. Copying it retains 100% byte parity, but HPDcePro must never invoke it.
2. **DuAnMau.autostr Internal Paths**: `DuAnMau.autostr` contains literal absolute strings `C:\\Dce Pro\\AutoStr` and `C:\\Dce Pro\\Excel\\File V.2020`. In Milestone M2 Feature 5, `AutoStrProjectSerializer` must normalize these paths upon deserialization to resolve dynamically via `AssetPathResolver`.
3. **Git Tracking & Large Files**: Total asset size is ~106.85 MB. `MayHoc/mauhoc.db` is 8.98 MB and `Cad/Form 2_Steel.dwg` is 1.59 MB. None exceed GitHub's 100 MB hard limit. All files are standard static assets that can be tracked in Git.

---

## 4. Conclusion & Target Architecture

### 4.1 Target Directory Hierarchy under `HPDcePro/assets/`

```
HPDcePro/assets/
├── cad/                     # 17 DWG templates (13 from Cad/ + 4 from AutoStr/00.FileDungChung/)
│   ├── Form 1.dwg
│   ├── Form 1_Steel.dwg
│   ├── Form 2.dwg
│   ├── Form 2_2.dwg
│   ├── Form 2_2_Circle3.2.dwg
│   ├── Form 2_Circle3.2.dwg
│   ├── Form 2_Steel.dwg
│   ├── Form 3.dwg
│   ├── Form 3_Steel.dwg
│   ├── Form 4.dwg
│   ├── Form 4_Steel.dwg
│   ├── Form 5.dwg
│   ├── Form 5_Steel.dwg
│   ├── BlockTenBanVe.dwg
│   ├── Chi Tiet Coc 250x250.dwg
│   ├── GhiChuChung 5574 _ A3.dwg
│   └── Khung A3.dwg
├── excel/                   # 132 XLSM engineering calculation workbooks
│   ├── Menu DCE_2020.xlsm
│   ├── DceSteel/            # 112 steel calculation workbooks
│   │   ├── 01 TCVN 2737 2023 Loading.xlsm
│   │   └── ... (111 more)
│   └── File V.2020/         # 19 concrete & geotech calculation workbooks
│       ├── 01. DCE_Etabs_2020.xlsm
│       ├── 09. DCE_Beam_2020.xlsm
│       ├── 10.1 DCE_SctCoc_10304 2025.xlsm
│       └── ... (16 more)
├── mayhoc/                  # 2 Machine Learning assets (~8.59 MB)
│   ├── mauhoc.db            # 8,982,528 bytes (SQLite training database)
│   └── model.zip            # 26,898 bytes (ML.NET model archive)
├── system/                  # 178 System configuration files (~892 KB)
│   ├── *.txt                # 170 parameter default files
│   ├── *.dat                # 6 legacy token files
│   ├── dcpcore.exe          # 1 legacy watchdog file
│   └── Cache/
│       └── dcpcore.bin      # 1 legacy watchdog binary
├── language/                # 8 Language dictionaries (~1.14 MB)
│   ├── en.txt
│   ├── vi.txt
│   ├── zh.txt
│   ├── _HUONGDAN.txt
│   └── Cad/
│       ├── en.txt
│       ├── vi.txt
│       ├── zh.txt
│       └── _HUONGDAN.txt
├── icons/                   # 133 Visual assets & ribbon icons (~1.66 MB)
│   ├── DCE.ico, logo.png, DCE_Logo.png (3 root)
│   ├── CauThang/            # 1 file
│   ├── IconCad/             # 47 files
│   ├── IconRevit/           # 13 files
│   ├── SlabRebar/           # 13 files
│   └── TKT/                 # 56 files
├── cuix/                    # 7 AutoCAD CUIX ribbon menus (~1.69 MB)
│   ├── DBimApps.cuix
│   ├── MenuDCE.cuix
│   ├── MenuDCE_ACAD.cuix
│   ├── MenuDCE_BCAD.cuix
│   ├── MenuDCE_GCAD.cuix
│   ├── MenuDCE_ICAD.cuix
│   └── MenuDCE_ZWCAD.cuix
├── autostr/                 # AutoStr project companion assets
│   ├── DuAnMau.autostr      # Sample project file (14,262 bytes)
│   └── 00.FileDungChung/
│       ├── 01.TP-THUYET MINH TKKT.docx
│       ├── 02.TP-PHU LUC THUYET MINH.docx
│       └── HoatTaiSan_TCVN2737_2023.json
└── systemsteel/             # 112 Steel detailing parameter txt files (1,286 bytes)
    └── *.txt
```

---

### 4.2 AssetPathResolver Interface Contract (`HPDcePro.Core.Common`)

```csharp
namespace HPDcePro.Core.Common
{
    public static class AssetPathResolver
    {
        public static string GetAssetsRoot();
        public static string ResolveCadTemplate(string filename);
        public static string ResolveExcelWorkbook(string relativePath);
        public static string ResolveSystemConfig(string filename);
        public static string ResolveLanguageDict(string relativePath);
        public static string ResolveMachineLearningDatabase();
        public static string ResolveMachineLearningModelZip();
        public static string ResolveIcon(string relativePath);
        public static string ResolveCuixMenu(string filename);
        public static string ResolveAutoStrSample();
    }
}
```

---

### 4.3 Concrete PowerShell / Robocopy Script for Worker M2

Below is the turnkey PowerShell script for Worker M2 to execute the migration with zero manual effort and zero impact on `C:\Dce Pro\`:

```powershell
# ==============================================================================
# HPDcePro Milestone M2: One-Way Engineering Asset Migration Script
# STRICTLY READ-ONLY ON SOURCE (C:\Dce Pro\) -> WRITES ONLY TO HPDcePro\assets\
# ==============================================================================
$ErrorActionPreference = "Stop"

$source = "C:\Dce Pro"
$repoRoot = "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar"
$dest = Join-Path $repoRoot "HPDcePro\assets"

Write-Host "===> Initializing Target Asset Directories..." -ForegroundColor Cyan
$subdirs = @(
    "cad",
    "excel\DceSteel",
    "excel\File V.2020",
    "mayhoc",
    "system\Cache",
    "language\Cad",
    "icons",
    "cuix",
    "autostr\00.FileDungChung",
    "systemsteel"
)
foreach ($dir in $subdirs) {
    $full = Join-Path $dest $dir
    if (-not (Test-Path $full)) {
        New-Item -ItemType Directory -Path $full -Force | Out-Null
    }
}

Write-Host "===> Copying 17 DWG Templates..." -ForegroundColor Cyan
robocopy "$source\Cad" "$dest\cad" *.dwg /R:1 /W:1 /NJH /NJS
robocopy "$source\AutoStr\00.FileDungChung" "$dest\cad" *.dwg /R:1 /W:1 /NJH /NJS

Write-Host "===> Copying 132 XLSM Workbooks..." -ForegroundColor Cyan
robocopy "$source\Excel" "$dest\excel" "Menu DCE_2020.xlsm" /R:1 /W:1 /NJH /NJS
robocopy "$source\Excel\DceSteel" "$dest\excel\DceSteel" *.xlsm /R:1 /W:1 /NJH /NJS
robocopy "$source\Excel\File V.2020" "$dest\excel\File V.2020" *.xlsm /R:1 /W:1 /NJH /NJS

Write-Host "===> Copying Machine Learning Models..." -ForegroundColor Cyan
robocopy "$source\MayHoc" "$dest\mayhoc" mauhoc.db model.zip /R:1 /W:1 /NJH /NJS

Write-Host "===> Copying 178 System Configuration Files..." -ForegroundColor Cyan
robocopy "$source\System" "$dest\system" /E /R:1 /W:1 /NJH /NJS

Write-Host "===> Copying 8 Language Localization Dictionaries..." -ForegroundColor Cyan
robocopy "$source\Language" "$dest\language" /E /R:1 /W:1 /NJH /NJS

Write-Host "===> Copying 133 Visual Assets & Icons..." -ForegroundColor Cyan
robocopy "$source\Icon" "$dest\icons" /E /R:1 /W:1 /NJH /NJS

Write-Host "===> Copying 7 AutoCAD CUIX Menus..." -ForegroundColor Cyan
robocopy "$source" "$dest\cuix" *.cuix /R:1 /W:1 /NJH /NJS

Write-Host "===> Copying AutoStr Companion Assets..." -ForegroundColor Cyan
robocopy "$source\AutoStr" "$dest\autostr" DuAnMau.autostr /R:1 /W:1 /NJH /NJS
robocopy "$source\AutoStr\00.FileDungChung" "$dest\autostr\00.FileDungChung" /E /R:1 /W:1 /NJH /NJS

Write-Host "===> Copying Steel Detailing Parameters (112 txt files)..." -ForegroundColor Cyan
robocopy "$source\AddinSteelCad\SystemSteel" "$dest\systemsteel" /E /R:1 /W:1 /NJH /NJS

Write-Host "===> Verifying Asset Migration Parity..." -ForegroundColor Green
$dwgCount = (Get-ChildItem "$dest\cad\*.dwg").Count
$xlsmCount = (Get-ChildItem "$dest\excel" -Recurse -Filter "*.xlsm").Count
$mlCount = (Get-ChildItem "$dest\mayhoc\*").Count
$sysCount = (Get-ChildItem "$dest\system" -Recurse -File).Count
$langCount = (Get-ChildItem "$dest\language" -Recurse -File).Count
$iconCount = (Get-ChildItem "$dest\icons" -Recurse -File).Count
$cuixCount = (Get-ChildItem "$dest\cuix\*.cuix").Count

Write-Host "Verification Results:" -ForegroundColor Yellow
Write-Host "  DWG Templates:  $dwgCount / 17 expected"
Write-Host "  XLSM Workbooks: $xlsmCount / 132 expected"
Write-Host "  ML Assets:      $mlCount / 2 expected"
Write-Host "  System Files:   $sysCount / 178 expected"
Write-Host "  Language Files: $langCount / 8 expected"
Write-Host "  Icons:          $iconCount / 133 expected"
Write-Host "  CUIX Menus:     $cuixCount / 7 expected"

if ($dwgCount -eq 17 -and $xlsmCount -eq 132 -and $mlCount -eq 2 -and $sysCount -eq 178 -and $langCount -eq 8 -and $iconCount -eq 133 -and $cuixCount -eq 7) {
    Write-Host "SUCCESS: 100% Asset Migration Parity Verified (477 Core Files)!" -ForegroundColor Green
} else {
    Write-Error "PARITY MISMATCH: Please inspect copy logs."
}
```

---

## 5. Verification Method

To independently verify the findings in this report, execute the following commands in PowerShell:

1. **Verify Baseline Asset Counts in `C:\Dce Pro\`**:
   ```powershell
   # DWG templates
   (Get-ChildItem -Path 'C:\Dce Pro' -Recurse -Filter '*.dwg').Count # Must return 17
   # XLSM workbooks
   (Get-ChildItem -Path 'C:\Dce Pro\Excel' -Recurse -Filter '*.xlsm').Count # Must return 132
   # ML assets
   (Get-ChildItem -Path 'C:\Dce Pro\MayHoc' -Recurse -File).Count # Must return 2
   # System files
   (Get-ChildItem -Path 'C:\Dce Pro\System' -Recurse -File).Count # Must return 178
   # Language files
   (Get-ChildItem -Path 'C:\Dce Pro\Language' -Recurse -File).Count # Must return 8
   # Icon files
   (Get-ChildItem -Path 'C:\Dce Pro\Icon' -Recurse -File).Count # Must return 133
   # CUIX menus
   (Get-ChildItem -Path 'C:\Dce Pro\*.cuix').Count # Must return 7
   ```

2. **Verify Robocopy Dry-Run**:
   Execute the robocopy dry-run commands with `/L /NJH /NJS` as demonstrated in Section 4.3; verify zero errors and matching line counts.

3. **Conditions of Invalidation**:
   This report's conclusions are invalidated if any file in `C:\Dce Pro\` is missing, if `C:\Dce Pro\` is modified, or if `HPDcePro/assets/` fails to receive the 477 core files upon worker execution.
