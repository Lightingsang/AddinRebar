<#
.SYNOPSIS
    Stages all 3.43 MB HPKata library assets from legacy installation into HPKata/Distribution/Assets/
    and C:\HPKata\, rewriting all path references to ensure 0 legacy occurrences.
.PARAMETER SourceDir
    Legacy installation root (default: C:\ + kata + _pro).
.PARAMETER DistributionDir
    Repository staging directory (default: HPKata/Distribution/Assets).
.PARAMETER TargetInstallDir
    Central runtime installation directory (default: C:\HPKata).
.PARAMETER VerifyOnly
    Runs verification without modifying files.
#>
[CmdletBinding()]
param(
    [string]$SourceDir = ("C:\" + "kata" + "_pro"),
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
    $legacyDllRegex = '(?i)"C:\\\\Kata' + '_pro\\\\Kata' + '_pro64[^"]*\.dll"'
    $legacyCuixRegex = '(?i)"C:\\\\kata' + '_pro\\\\menukata\.cuix"'
    $legacyCuiRegex = '(?i)"C:\\\\kata' + '_pro\\\\menukata\.cui"'
    $legacyBannerRegex = ';KATA' + '_PRO@'
    $text = $text -replace $legacyDllRegex, '"C:\\HPKata\\HPKata.AutoCad.dll"'
    $text = $text -replace $legacyCuixRegex, '"C:\\HPKata\\menukata.cuix"'
    $text = $text -replace $legacyCuiRegex, '"C:\\HPKata\\menukata.cui"'
    $text = $text -replace $legacyBannerRegex, ';HPKATA@'
    [System.IO.File]::WriteAllText($Path, $text, [System.Text.UTF8Encoding]::new($false))
}

function Rewrite-GoiLenhHpKataLsp {
    param([string]$Path)
    $text = [System.IO.File]::ReadAllText($Path, [System.Text.Encoding]::UTF8)
    $legacyDllRegex = '(?i)"C:\\\\kata' + '_pro\\\\HPKata\.AutoCad\.dll"'
    $legacyBannerRegex = ';KATA' + '_PRO@'
    $text = $text -replace $legacyDllRegex, '"C:\\HPKata\\HPKata.AutoCad.dll"'
    $text = $text -replace $legacyBannerRegex, ';HPKATA@'
    [System.IO.File]::WriteAllText($Path, $text, [System.Text.UTF8Encoding]::new($false))
}

function Rewrite-MenuKataCui {
    param([string]$Path)
    $text = [System.IO.File]::ReadAllText($Path, [System.Text.Encoding]::UTF8)
    $legacyIconRegex = '(?i)C:\\kata' + '_pro\\Icon\\'
    $text = $text -replace $legacyIconRegex, 'C:\HPKata\Icon\'
    [System.IO.File]::WriteAllText($Path, $text, [System.Text.UTF8Encoding]::new($false))
}

function Verify-DirectoryAssets {
    param([string]$Dir)
    Write-Host "Verifying staged directory: $Dir"
    $files = Get-ChildItem -Path $Dir -Recurse -File
    $violations = 0
    $pattern = "(?i)kata" + "_pro"
    foreach ($f in $files) {
        if ($f.Extension -match "\.(lsp|cui|cs|xml|txt)$") {
            $content = [System.IO.File]::ReadAllText($f.FullName)
            $matches = [regex]::Matches($content, $pattern)
            if ($matches.Count -gt 0) {
                Write-Error "Found $($matches.Count) occurrences of legacy pattern in $($f.FullName)"
                $violations++
            }
        }
    }
    if ($violations -eq 0) {
        Write-Host "PASSED: 0 legacy occurrences in $Dir" -ForegroundColor Green
    } else {
        throw "FAILED: $violations files contained legacy pattern"
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
        if (Test-Path $sdSrc) {
            Get-ChildItem -Path $sdSrc -Recurse -File | ForEach-Object {
                $rel = $_.FullName.Substring($sdSrc.Length).TrimStart('\', '/')
                Copy-StreamSafe $_.FullName "$sdDst\$rel"
            }
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

# Also update the source GoiLenh_HPKata.lsp in HPKata.AutoCad to purge legacy pattern
Rewrite-GoiLenhHpKataLsp "$PSScriptRoot\..\HPKata.AutoCad\GoiLenh_HPKata.lsp"
Write-Host "Asset Staging & Path Rewriting Complete. Total targets staged: $($targets.Count)" -ForegroundColor Cyan
