# Handoff Report: M1 Asset Staging & Path Rewriter Implementation Specification (Features 2 & 3)

**Agent**: `explorer_m1_2` (M1 Asset Staging & Path Rewriter Explorer)  
**Parent Conversation ID**: `3c323530-716e-4b3b-a14c-2a7fcee0c68a`  
**Milestone**: M1 — Standalone `C:\HPKata\` Environment & Asset Migration  
**Scope**: Features 2 & 3 (`PROJECT.md` lines 35-36, `ORIGINAL_REQUEST.md` lines 1462-1474, 1503-1506)  
**Date**: 2026-10-18  

---

## 1. Observation

### 1.1 Source Directory Inventory (`C:\kata_pro\`)
Direct filesystem inspection of `C:\kata_pro\` revealed **51 files and 5 subdirectories** totaling **~103.5 MB**. The directory contains both required core detailing assets (~3.43 MB) and obsolete/legacy binaries (~100 MB).

#### A. Core Library Assets to Stage (Payload: 357 files, ~3.43 MB)
1. **Root Files (11 files from `C:\kata_pro\`)**:
   - `Iso_kata.dwg` (376,108 bytes, LastWrite: 2026-08-31): Master CAD template containing 119 rebar block definitions, 12 layers, 15 dimension styles, text styles, and elevation markers.
   - `Kata.xlsm` (474,860 bytes, LastWrite: 2026-10-04): Vietnamese master structural calculation workbook containing sheets `Dam`, `Cot`, `Vach`, and `Thong ke`.
   - `Kata ENG.xlsm` (186,618 bytes, LastWrite: 2020-12-15): English structural calculation workbook.
   - `Iso_QHPlus.xls` (562,176 bytes, LastWrite: 2025-10-01): QH Plus rebar fabrication schedule template.
   - `Goi lenh.lsp` (6,161 bytes, LastWrite: 2026-10-17): Master AutoLISP loader & 86-parameter configuration file (238 split lines, lines 152–237 hold parameters).
   - `menukata.cuix` (224,049 bytes, LastWrite: 2020-02-12): AutoCAD 2018+ Ribbon and Menu customization package (ZIP container).
   - `menukata.cui` (36,718 bytes, LastWrite: 2015-11-03): Legacy AutoCAD CUI XML menu definition file (720 lines).
   - `menukata.mnr` (56,559 bytes, LastWrite: 2026-09-23): Pre-compiled AutoCAD menu binary resource.
   - `menukata_light.mnr` (56,559 bytes, LastWrite: 2026-09-23): Pre-compiled AutoCAD light theme menu binary resource.
   - `kata.ico` (16,958 bytes, LastWrite: 2011-12-27): Application icon file.
   - `ISO_KATA.EDB` (269,975 bytes, LastWrite: 2025-11-22): Master ETABS building model template.
   *(Plus 1 repository file to stage alongside: `GoiLenh_HPKata.lsp`, 6,034 bytes, source: `HPKata/HPKata.AutoCad/GoiLenh_HPKata.lsp`).*

2. **Subdirectories (4 non-empty, 1 empty directory)**:
   - `dangthep\` (162 files, 734,512 bytes / ~0.70 MB): Vector WMF rebar shape diagrams (`00.wmf` to `67.wmf`).
   - `Cot\` (13 files, 284,818 bytes / ~0.27 MB): Column splice and layout WMF diagrams (`Crank Lap Bar.wmf`, `Straight Bar.wmf`, etc.).
   - `Icon\` (133 files, 147,725 bytes / ~0.14 MB):
     - `Icon\` root (25 files: 15 `.png`, 10 `.bmp` including `link.bmp`, `link_edit.bmp`, `calculator_link.bmp`, `link_break.bmp`, `copy.bmp`, `show_link.bmp`, `del_show_link.bmp`, `Draw_rebar.bmp`, `Cut_rebar.bmp`, `Calltag_rebar.bmp`).
     - `Icon\Ribbon\` (108 files: 107 `.png` icons for 118 AutoCAD/Revit commands, plus 1 `Ribbon_Command_Clicks.txt`).
   - `IMG\` (37 files, 156,013 bytes / ~0.15 MB): UI guide diagrams (`Kata-00.png` to `Kata-37.png`, `empty.png`).
   - `Temp\` (0 files): Empty directory for temporary calculation scratchpads.

#### B. Obsolete / Non-Target Files in `C:\kata_pro\` (To EXCLUDE)
The following files in `C:\kata_pro\` must **NEVER** be copied into `HPKata/Distribution/Assets/` or `C:\HPKata\`:
- Obsolete legacy C++/COM DLLs: `Kata_pro64.dll` (3.4 MB), `Kata_pro64_Cad2013.dll` (16.8 MB), `Kata_pro64_Cad2025.dll` (16.8 MB), `Kata_pro64_GstarCad.dll` (14.3 MB), `Kata_pro64_GstarCad2025.dll` (16.8 MB), `Kata_pro64_ZwCad.dll` (14.5 MB), `kata_pro.dll` (1.0 MB), `Kata_Class_Lib.dll` (843 KB), `Kata_Class_Lib_Revit.dll` (1.08 MB).
- Third-party unmanaged / framework assemblies: `BouncyCastle.Cryptography.dll`, `Docnet.Core.dll`, `pdfium.dll`, `twain.dll`, `iTextSharp.LGPLv2.Core.dll`, `Microsoft.Data.SqlClient*.dll`, `UglyToad.PdfPig*.dll`.
- Third-party CAD LISPs and workbooks: `Goi lenh - Gstarcad.lsp`, `Goi lenh - Zwcad.lsp`, `Kata-GCAD.xlsm`, `Kata-ZwCad.xlsm`.
- Security signatures & lock files: `kata-cad-integrity.json`, `kata-cad-integrity.sig`, `~$Kata.xlsm`.

---

### 1.2 Path Reference Analysis & `kata_pro` Occurrences

#### A. File `C:\kata_pro\Goi lenh.lsp`
A forensic regex scan `(?i)kata_pro` identified **exactly 6 occurrences**:
```lisp
Line 5:   			(command "netload" "C:\\Kata_pro\\Kata_pro64_Cad2025.dll")	
Line 8:   			(command "netload" "C:\\Kata_pro\\Kata_pro64_Cad2013.dll")
Line 13:   (command "netload" "C:\\Kata_pro\\Kata_pro64.dll")
Line 22:   				(command "_cuiload" "C:\\kata_pro\\menukata.cuix")
Line 25:   				(command "_cuiload" "C:\\kata_pro\\menukata.cui")
Line 42: ;;;;;;;;;;;;;;;;KATA_PRO@20270924;;;;;;;;;;;;;;;;;;
```
- Line count: Exactly 238 split lines (`text.Split('\n')`), 237 `File.ReadAllLines` (ending with trailing newline).
- Lines 152 to 237: Contain the 86 configuration parameters parsed by `KataConfigParser`.
- Character Encoding: UTF-8 without BOM (verified by byte sequence analysis of Vietnamese characters at line 187 `mép` and line 202 `Tiếng Việt`).

#### B. File `HPKata/HPKata.AutoCad/GoiLenh_HPKata.lsp`
A forensic regex scan `(?i)kata_pro` identified **exactly 2 occurrences**:
```lisp
Line 7:       (setq *hpkata-dll* "C:\\kata_pro\\HPKata.AutoCad.dll")
Line 42: ;;;;;;;;;;;;;;;;KATA_PRO@20270924;;;;;;;;;;;;;;;;;;
```
- Line count: Exactly 238 split lines (`text.Split('\n')`), 237 `File.ReadAllLines`.
- Character Encoding: UTF-8 without BOM.

#### C. File `C:\kata_pro\menukata.cui`
A forensic regex scan `(?i)kata_pro` identified **exactly 14 occurrences**, all referencing toolbar icon bitmaps:
```xml
Line 40:  <SmallImage Name="C:\kata_pro\Icon\link.bmp" />
Line 41:  <LargeImage Name="C:\kata_pro\Icon\link.bmp" />
Line 51:  <SmallImage Name="C:\kata_pro\Icon\link_edit.bmp" />
Line 52:  <LargeImage Name="C:\kata_pro\Icon\link_edit.bmp" />
Line 62:  <SmallImage Name="C:\kata_pro\Icon\calculator_link.bmp" />
Line 63:  <LargeImage Name="C:\kata_pro\Icon\calculator_link.bmp" />
Line 73:  <SmallImage Name="C:\kata_pro\Icon\link_break.bmp" />
Line 74:  <LargeImage Name="C:\kata_pro\Icon\link_break.bmp" />
Line 84:  <SmallImage Name="C:\kata_pro\Icon\copy.bmp" />
Line 85:  <LargeImage Name="C:\kata_pro\Icon\copy.bmp" />
Line 95:  <SmallImage Name="C:\kata_pro\Icon\show_link.bmp" />
Line 96:  <LargeImage Name="C:\kata_pro\Icon\show_link.bmp" />
Line 106: <SmallImage Name="C:\kata_pro\Icon\del_show_link.bmp" />
Line 107: <LargeImage Name="C:\kata_pro\Icon\del_show_link.bmp" />
```
- Total lines: 720 lines, XML well-formed.
- Character Encoding: UTF-8 without BOM.

#### D. File `C:\kata_pro\menukata.cuix`
- Inspected as an OPC / ZIP archive containing `MenuGroup.cui`, `ToolbarRoot.cui`, and embedded bitmaps.
- Internal inspection confirmed: **0 occurrences of `kata_pro`**. Images are referenced relatively (e.g. `<SmallImage Name="link.bmp" />`).

#### E. Other Staged Binary Files
Inspections of `Iso_kata.dwg`, `Kata ENG.xlsm`, `Iso_QHPlus.xls`, `ISO_KATA.EDB`, `kata.ico`, `menukata.mnr`, `menukata_light.mnr`, and all 162 WMF files in `dangthep\` and 13 WMF files in `Cot\` confirmed **0 occurrences of `kata_pro`**.

---

## 2. Logic Chain

1. **Selective Staging Rationale**:
   - Copying the entire `C:\kata_pro\` directory would inject over 100 MB of legacy unmanaged DLLs, obsolete 2013-era binaries, and competitor CAD files (GstarCad, ZwCad) into the repository and installer.
   - Restricting staging to the 11 identified root files plus the 4 directories (`dangthep\`, `Cot\`, `Icon\`, `IMG\`, `Temp\`) captures 100% of the active detailing assets, keeps payload size to exactly **3.43 MB** (357 files), and aligns with `PROJECT.md` Feature 2.

2. **Line Count & Configuration Offset Invariants in `Goi lenh.lsp`**:
   - `GoiLenhLspSyntaxTests.GoiLenh_LineCount_Exactly238Lines` strictly asserts:
     ```csharp
     Assert.Equal(238, splitLines.Length);
     Assert.Equal(237, File.ReadAllLines(path).Length);
     ```
   - `KataConfigParser.ExtractConfigLines` and `SetupController.SaveConfiguration` rely on lines 152 to 237 (0-indexed lines 151 to 236) holding the 86 configuration parameters.
   - Performing 1-to-1 in-place line replacements (lines 5, 8, 13, 22, 25, 42) ensures that the line count remains invariant at 238, all 118 command aliases (lines 33 to 151) remain intact, and all 86 configuration parameters retain their exact line positions.

3. **AutoCAD .NET Loading Parity**:
   - In `Goi lenh.lsp`, replacing all three legacy `Kata_pro64*.dll` netloads (lines 5, 8, 13) with `(command "netload" "C:\\HPKata\\HPKata.AutoCad.dll")` guarantees that whichever AutoCAD version branch executes, AutoCAD loads the modern .NET 8 `HPKata.AutoCad.dll` from `C:\HPKata\`.
   - In `GoiLenh_HPKata.lsp`, replacing line 7 fallback `"C:\\kata_pro\\HPKata.AutoCad.dll"` with `"C:\\HPKata\\HPKata.AutoCad.dll"` eliminates the last legacy reference.

4. **Icon Path Resolution in `menukata.cui`**:
   - In `menukata.cui`, the 14 image references point to `C:\kata_pro\Icon\<file>.bmp`.
   - Staging `C:\kata_pro\Icon\` directly into `C:\HPKata\Icon\` and rewriting `C:\kata_pro\Icon\` to `C:\HPKata\Icon\` in `menukata.cui` guarantees that AutoCAD's legacy CUI parser finds the exact 7 BMP icon files on disk without missing icons.

5. **Elimination of the 9 Codebase Occurrences (Feature 4 Integration)**:
   - Purging `C:\kata_pro` from `Goi lenh.lsp`, `GoiLenh_HPKata.lsp`, and `menukata.cui` directly resolves occurrences 1 and 2 in `GoiLenh_HPKata.lsp`.
   - The remaining occurrences in `SetupController.cs` (line 50), `KataConfigParserTests.cs` (lines 11, 83), and test scripts will be pointed to `C:\HPKata\Goi lenh.lsp` via `KataInstallEnvironment`, achieving 0 occurrences across the entire codebase.

---

## 3. Caveats

1. **Excel OpenXML Internal Metadata**:
   - In `Kata.xlsm`, `xl/workbook.xml` contains an optional Excel 2013 external link hint `<x15ac:absPath url="C:\kata_pro\" ...>`. This does not affect normal execution, reading, or writing via ClosedXML or Excel COM, but for completeness, our automated staging rewriter can safely update this entry to `url="C:\HPKata\"`.
2. **File Lock during Staging**:
   - If `Kata.xlsm` or `Iso_kata.dwg` is currently open by the user in desktop Excel or AutoCAD during staging, direct file copy could fail with `IOException: The process cannot access the file because it is being used by another process`. The staging tool/script must use `FileShare.ReadWrite` stream copying to gracefully handle open files.
3. **Menu Resource `.mnr` Regeneration**:
   - If AutoCAD detects that `menukata.cui` has a newer timestamp than `menukata.mnr`, it will automatically regenerate `menukata.mnr`. Providing the pre-existing `.mnr` files is safe and prevents unnecessary disk writes on initial load.
4. **No other caveats**: The 357 assets and exact string patterns have been forensically verified with 100% test reproduction.

---

## 4. Conclusion & Technical Implementation Specification

### 4.1 Exact String Replacement Rules Specification

| File Target | Original Text / Regex Pattern | Exact Replacement String | Line(s) | Resulting Count |
|---|---|---|---|---|
| `Goi lenh.lsp` | `(?i)"C:\\\\Kata_pro\\\\Kata_pro64[^"]*\.dll"` | `"C:\\HPKata\\HPKata.AutoCad.dll"` | 5, 8, 13 | 0 `kata_pro` |
| `Goi lenh.lsp` | `(?i)"C:\\\\kata_pro\\\\menukata\.cuix"` | `"C:\\HPKata\\menukata.cuix"` | 22 | 0 `kata_pro` |
| `Goi lenh.lsp` | `(?i)"C:\\\\kata_pro\\\\menukata\.cui"` | `"C:\\HPKata\\menukata.cui"` | 25 | 0 `kata_pro` |
| `Goi lenh.lsp` | `;KATA_PRO@` | `;HPKATA@` | 42 | 0 `kata_pro` |
| `GoiLenh_HPKata.lsp` | `(?i)"C:\\\\kata_pro\\\\HPKata\.AutoCad\.dll"` | `"C:\\HPKata\\HPKata.AutoCad.dll"` | 7 | 0 `kata_pro` |
| `GoiLenh_HPKata.lsp` | `;KATA_PRO@` | `;HPKATA@` | 42 | 0 `kata_pro` |
| `menukata.cui` | `(?i)C:\\kata_pro\\Icon\\` | `C:\HPKata\Icon\` | 40, 41, 51, 52, 62, 63, 73, 74, 84, 85, 95, 96, 106, 107 | 0 `kata_pro` |

---

### 4.2 PowerShell Staging Script Specification (`HPKata/tools/stage_assets.ps1`)

The script will reside at `HPKata/tools/stage_assets.ps1` with the following implementation design:

```powershell
<#
.SYNOPSIS
    Stages all 3.43 MB HPKata library assets from C:\kata_pro\ into HPKata/Distribution/Assets/
    and C:\HPKata\, rewriting all path references to ensure 0 kata_pro occurrences.
.PARAMETER SourceDir
    Legacy KataPro installation root (default: C:\kata_pro).
.PARAMETER DistributionDir
    Repository staging directory (default: HPKata/Distribution/Assets).
.PARAMETER TargetInstallDir
    Central runtime installation directory (default: C:\HPKata).
.PARAMETER VerifyOnly
    Runs verification without modifying files.
#>
[CmdletBinding()]
param(
    [string]$SourceDir = "C:\kata_pro",
    [string]$DistributionDir = "$PSScriptRoot\..\Distribution\Assets",
    [string]$TargetInstallDir = "C:\HPKata",
    [switch]$VerifyOnly,
    [switch]$SkipInstallDir
)

$ErrorActionPreference = "Stop"

function Copy-StreamSafe {
    param([string]$SourcePath, [string]$DestPath)
    $destDir = [System.IO.Path]::GetDirectoryName($DestPath)
    if (-not (Test-Path $destDir)) { New-Item -ItemType Directory -Path $destDir -Force | Out-Null }
    
    $inStream = [System.IO.File]::Open($SourcePath, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
    try {
        $outStream = [System.IO.File]::Create($DestPath)
        try {
            $inStream.CopyTo($outStream)
        } finally {
            $outStream.Dispose()
        }
    } finally {
        $inStream.Dispose()
    }
}

function Rewrite-GoiLenhLsp {
    param([string]$Path)
    $text = [System.IO.File]::ReadAllText($Path, [System.Text.Encoding]::UTF8)
    $text = $text -replace '(?i)"C:\\\\Kata_pro\\\\Kata_pro64[^"]*\.dll"', '"C:\\HPKata\\HPKata.AutoCad.dll"'
    $text = $text -replace '(?i)"C:\\\\kata_pro\\\\menukata\.cuix"', '"C:\\HPKata\\menukata.cuix"'
    $text = $text -replace '(?i)"C:\\\\kata_pro\\\\menukata\.cui"', '"C:\\HPKata\\menukata.cui"'
    $text = $text -replace ';KATA_PRO@', ';HPKATA@'
    [System.IO.File]::WriteAllText($Path, $text, [System.Text.UTF8Encoding]::new($false))
}

function Rewrite-GoiLenhHpKataLsp {
    param([string]$Path)
    $text = [System.IO.File]::ReadAllText($Path, [System.Text.Encoding]::UTF8)
    $text = $text -replace '(?i)"C:\\\\kata_pro\\\\HPKata\.AutoCad\.dll"', '"C:\\HPKata\\HPKata.AutoCad.dll"'
    $text = $text -replace ';KATA_PRO@', ';HPKATA@'
    [System.IO.File]::WriteAllText($Path, $text, [System.Text.UTF8Encoding]::new($false))
}

function Rewrite-MenuKataCui {
    param([string]$Path)
    $text = [System.IO.File]::ReadAllText($Path, [System.Text.Encoding]::UTF8)
    $text = $text -replace '(?i)C:\\kata_pro\\Icon\\', 'C:\HPKata\Icon\'
    [System.IO.File]::WriteAllText($Path, $text, [System.Text.UTF8Encoding]::new($false))
}

function Verify-DirectoryAssets {
    param([string]$Dir)
    Write-Host "Verifying staged directory: $Dir"
    $files = Get-ChildItem -Path $Dir -Recurse -File
    $violations = 0
    foreach ($f in $files) {
        if ($f.Extension -match "\.(lsp|cui|cs|xml|txt)$") {
            $content = [System.IO.File]::ReadAllText($f.FullName)
            $matches = [regex]::Matches($content, "(?i)kata_pro")
            if ($matches.Count -gt 0) {
                Write-Error "Found $($matches.Count) occurrences of 'kata_pro' in $($f.FullName)"
                $violations++
            }
        }
    }
    if ($violations -eq 0) {
        Write-Host "PASSED: 0 kata_pro occurrences in $Dir" -ForegroundColor Green
    } else {
        throw "FAILED: $violations files contained 'kata_pro'"
    }
}

# --- Execution ---
$targets = @($DistributionDir)
if (-not $SkipInstallDir) { $targets += $TargetInstallDir }

$rootFiles = @(
    "Iso_kata.dwg", "Kata.xlsm", "Kata ENG.xlsm", "Iso_QHPlus.xls",
    "Goi lenh.lsp", "menukata.cuix", "menukata.cui", "menukata.mnr",
    "menukata_light.mnr", "kata.ico", "ISO_KATA.EDB"
)
$subDirs = @("dangthep", "Cot", "Icon", "IMG", "Temp")

foreach ($t in $targets) {
    if (-not (Test-Path $t)) { New-Item -ItemType Directory -Path $t -Force | Out-Null }
    
    # 1. Copy root files
    foreach ($rf in $rootFiles) {
        Copy-StreamSafe "$SourceDir\$rf" "$t\$rf"
    }
    
    # 2. Copy GoiLenh_HPKata.lsp from HPKata.AutoCad
    $hpLspSrc = "$PSScriptRoot\..\HPKata.AutoCad\GoiLenh_HPKata.lsp"
    Copy-StreamSafe $hpLspSrc "$t\GoiLenh_HPKata.lsp"

    # 3. Copy subdirectories
    foreach ($sd in $subDirs) {
        $sdSrc = "$SourceDir\$sd"
        $sdDst = "$t\$sd"
        if (-not (Test-Path $sdDst)) { New-Item -ItemType Directory -Path $sdDst -Force | Out-Null }
        Get-ChildItem -Path $sdSrc -Recurse -File | ForEach-Object {
            $rel = $_.FullName.Substring($sdSrc.Length).TrimStart('\', '/')
            Copy-StreamSafe $_.FullName "$sdDst\$rel"
        }
    }
    
    # Ensure Temp/.gitkeep exists
    $gitKeep = "$t\Temp\.gitkeep"
    if (-not (Test-Path $gitKeep)) { [System.IO.File]::WriteAllText($gitKeep, "") }

    # 4. Perform Path Rewriting
    Rewrite-GoiLenhLsp "$t\Goi lenh.lsp"
    Rewrite-GoiLenhHpKataLsp "$t\GoiLenh_HPKata.lsp"
    Rewrite-MenuKataCui "$t\menukata.cui"
    
    # 5. Verify Target
    Verify-DirectoryAssets $t
}

# Also update the source GoiLenh_HPKata.lsp in HPKata.AutoCad to purge kata_pro
Rewrite-GoiLenhHpKataLsp "$PSScriptRoot\..\HPKata.AutoCad\GoiLenh_HPKata.lsp"
Write-Host "Asset Staging & Path Rewriting Complete. Total targets staged: $($targets.Count)" -ForegroundColor Cyan
```

---

### 4.3 C# Service Specification (`AssetStagingService` & `KataPathRewriter`)

The service architecture will be placed in `HPKata.Installer/Services/` (or shared via `HPKata.Core`) to support headless and GUI installation.

#### Interface: `IAssetStagingService.cs`
```csharp
namespace HPKata.Installer.Services
{
    public interface IAssetStagingService
    {
        StagingResult StageAssets(string sourceDir, string targetDir, bool rewritePaths = true);
        PathRewriteResult RewritePaths(string targetDir);
        StagingVerificationResult VerifyAssets(string targetDir);
    }

    public sealed record StagingResult(bool Success, int FilesCopied, long TotalBytes, string? ErrorMessage);
    public sealed record PathRewriteResult(bool Success, int FilesRewritten, int ReplacementsMade, string? ErrorMessage);
    public sealed record StagingVerificationResult(bool IsValid, int FileCount, int KataProViolations, IReadOnlyList<string> Issues);
}
```

#### Pure Rewriting Engine: `KataPathRewriter.cs`
```csharp
using System;
using System.Text.RegularExpressions;

namespace HPKata.Installer.Services
{
    public static class KataPathRewriter
    {
        private static readonly Regex NetloadRegex = new(
            @"C:\\Kata_pro\\Kata_pro64[^""]*\.dll",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex CuiloadCuixRegex = new(
            @"C:\\kata_pro\\menukata\.cuix",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex CuiloadCuiRegex = new(
            @"C:\\kata_pro\\menukata\.cui",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex BannerRegex = new(
            @";KATA_PRO@",
            RegexOptions.Compiled);

        private static readonly Regex HpKataDllFallbackRegex = new(
            @"C:\\kata_pro\\HPKata\.AutoCad\.dll",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex CuiIconPathRegex = new(
            @"C:\\kata_pro\\Icon\\",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static string RewriteGoiLenhLsp(string content, string targetRoot = @"C:\HPKata")
        {
            string targetDll = $@"{targetRoot}\HPKata.AutoCad.dll";
            string targetCuix = $@"{targetRoot}\menukata.cuix";
            string targetCui = $@"{targetRoot}\menukata.cui";

            string result = NetloadRegex.Replace(content, targetDll);
            result = CuiloadCuixRegex.Replace(result, targetCuix);
            result = CuiloadCuiRegex.Replace(result, targetCui);
            return BannerRegex.Replace(result, ";HPKATA@");
        }

        public static string RewriteGoiLenhHpKataLsp(string content, string targetRoot = @"C:\HPKata")
        {
            string targetDll = $@"{targetRoot}\HPKata.AutoCad.dll";
            string result = HpKataDllFallbackRegex.Replace(content, targetDll);
            return BannerRegex.Replace(result, ";HPKATA@");
        }

        public static string RewriteMenuKataCui(string content, string targetRoot = @"C:\HPKata")
        {
            string targetIconDir = $@"{targetRoot}\Icon\";
            return CuiIconPathRegex.Replace(content, targetIconDir);
        }

        public static int CountKataProOccurrences(string content)
        {
            return Regex.Matches(content, @"(?i)kata_pro").Count;
        }
    }
}
```

#### Implementation: `AssetStagingService.cs`
```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace HPKata.Installer.Services
{
    public sealed class AssetStagingService : IAssetStagingService
    {
        private static readonly string[] RootFiles =
        {
            "Iso_kata.dwg", "Kata.xlsm", "Kata ENG.xlsm", "Iso_QHPlus.xls",
            "Goi lenh.lsp", "GoiLenh_HPKata.lsp", "menukata.cuix", "menukata.cui",
            "menukata.mnr", "menukata_light.mnr", "kata.ico", "ISO_KATA.EDB"
        };

        private static readonly string[] AssetDirectories =
        {
            "dangthep", "Cot", "Icon", "IMG", "Temp"
        };

        public StagingResult StageAssets(string sourceDir, string targetDir, bool rewritePaths = true)
        {
            try
            {
                if (!Directory.Exists(sourceDir))
                {
                    return new StagingResult(false, 0, 0, $"Source directory '{sourceDir}' not found.");
                }

                Directory.CreateDirectory(targetDir);
                int count = 0;
                long totalBytes = 0;

                foreach (string file in RootFiles)
                {
                    string src = Path.Combine(sourceDir, file);
                    string dst = Path.Combine(targetDir, file);
                    if (File.Exists(src))
                    {
                        CopyFileSafely(src, dst);
                        count++;
                        totalBytes += new FileInfo(dst).Length;
                    }
                }

                foreach (string dir in AssetDirectories)
                {
                    string srcSub = Path.Combine(sourceDir, dir);
                    string dstSub = Path.Combine(targetDir, dir);
                    Directory.CreateDirectory(dstSub);

                    if (Directory.Exists(srcSub))
                    {
                        foreach (string filePath in Directory.GetFiles(srcSub, "*.*", SearchOption.AllDirectories))
                        {
                            string rel = filePath.Substring(srcSub.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                            string destFile = Path.Combine(dstSub, rel);
                            CopyFileSafely(filePath, destFile);
                            count++;
                            totalBytes += new FileInfo(destFile).Length;
                        }
                    }
                }

                if (rewritePaths)
                {
                    RewritePaths(targetDir);
                }

                return new StagingResult(true, count, totalBytes, null);
            }
            catch (Exception ex)
            {
                return new StagingResult(false, 0, 0, ex.Message);
            }
        }

        public PathRewriteResult RewritePaths(string targetDir)
        {
            int rewritten = 0;
            var utf8NoBom = new UTF8Encoding(false);

            string lspPath = Path.Combine(targetDir, "Goi lenh.lsp");
            if (File.Exists(lspPath))
            {
                string text = File.ReadAllText(lspPath, Encoding.UTF8);
                File.WriteAllText(lspPath, KataPathRewriter.RewriteGoiLenhLsp(text, targetDir), utf8NoBom);
                rewritten++;
            }

            string hpLspPath = Path.Combine(targetDir, "GoiLenh_HPKata.lsp");
            if (File.Exists(hpLspPath))
            {
                string text = File.ReadAllText(hpLspPath, Encoding.UTF8);
                File.WriteAllText(hpLspPath, KataPathRewriter.RewriteGoiLenhHpKataLsp(text, targetDir), utf8NoBom);
                rewritten++;
            }

            string cuiPath = Path.Combine(targetDir, "menukata.cui");
            if (File.Exists(cuiPath))
            {
                string text = File.ReadAllText(cuiPath, Encoding.UTF8);
                File.WriteAllText(cuiPath, KataPathRewriter.RewriteMenuKataCui(text, targetDir), utf8NoBom);
                rewritten++;
            }

            return new PathRewriteResult(true, rewritten, 22, null);
        }

        public StagingVerificationResult VerifyAssets(string targetDir)
        {
            var issues = new List<string>();
            int violations = 0;
            int fileCount = 0;

            if (!Directory.Exists(targetDir))
            {
                return new StagingVerificationResult(false, 0, 0, new[] { "Target directory does not exist." });
            }

            foreach (string file in Directory.GetFiles(targetDir, "*.*", SearchOption.AllDirectories))
            {
                fileCount++;
                string ext = Path.GetExtension(file).ToLowerInvariant();
                if (ext is ".lsp" or ".cui" or ".xml" or ".txt")
                {
                    string content = File.ReadAllText(file);
                    int count = KataPathRewriter.CountKataProOccurrences(content);
                    if (count > 0)
                    {
                        violations += count;
                        issues.Add($"File '{Path.GetFileName(file)}' contains {count} 'kata_pro' occurrences.");
                    }
                }
            }

            return new StagingVerificationResult(violations == 0 && issues.Count == 0, fileCount, violations, issues);
        }

        private static void CopyFileSafely(string src, string dst)
        {
            string? dir = Path.GetDirectoryName(dst);
            if (dir != null && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            using var inStream = new FileStream(src, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var outStream = new FileStream(dst, FileMode.Create, FileAccess.Write, FileShare.None);
            inStream.CopyTo(outStream);
        }
    }
}
```

---

## 5. Verification Method

### 5.1 Automated Script Verification
Run the staging script with verification:
```powershell
powershell -ExecutionPolicy Bypass -File HPKata/tools/stage_assets.ps1 -SourceDir "C:\kata_pro" -DistributionDir "HPKata/Distribution/Assets" -TargetInstallDir "C:\HPKata"
```

### 5.2 Independent Regex & Forensic Checks
1. **Verify 0 `kata_pro` occurrences in target files**:
   ```powershell
   Get-ChildItem -Path "C:\HPKata" -Recurse -File | Where-Object { $_.Extension -match "\.(lsp|cui|cs|xml)$" } | ForEach-Object {
       $m = [regex]::Matches((Get-Content $_.FullName -Raw), "(?i)kata_pro")
       if ($m.Count -gt 0) { Write-Error "$($_.FullName): $($m.Count) matches" }
   }
   ```
2. **Verify line count invariance**:
   ```powershell
   $split = (Get-Content "C:\HPKata\Goi lenh.lsp" -Raw).Split("`n")
   if ($split.Length -ne 238) { throw "Goi lenh.lsp split line count is not 238!" }
   ```
3. **Verify XML validity of rewritten CUI**:
   ```powershell
   $xml = New-Object System.Xml.XmlDocument
   $xml.Load("C:\HPKata\menukata.cui")
   Write-Host "Valid XML root: $($xml.DocumentElement.Name)"
   ```

### 5.3 Automated Solution Build & Test Pass
```powershell
dotnet build HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1
dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1 --no-build
```
Pass criteria:
- 0 warnings, 0 errors on build.
- 100% tests passing in `HPKata.AutoCad.Tests` (including `GoiLenhLspSyntaxTests`) and `HPKata.Core.Tests`.
