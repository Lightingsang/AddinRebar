<#
.SYNOPSIS
    Builds the standalone MSI installer for HPRebar containing only the Kata tools (Kata Export + Kata Settings).

.DESCRIPTION
    Compiles HPRebar for Revit 2026 with the KATA_ONLY conditional compilation symbol, publishes it into a clean
    publish folder, builds SingleUser & MultiUser .msi packages, then checks every MSI: decompiled, it must hold
    HPRebar.dll + HPRebar.addin for Revit 2026 only and no other add-in, and the DLL must embed the Kata pages.

.PARAMETER Version
    The version string for the installer. Defaults to today's date as yy.M.d: the Kata-only MSI shares its upgrade
    code with the full HPRebar MSI (same add-in files and AddInId, so the two never coexist); a date version keeps
    it from being refused as a downgrade on a machine with a recent HPRebar. Not yyyy: Windows Installer wants a
    major and minor below 256 and a build below 65536, and compares versions unpredictably beyond that (WIX1148).

.PARAMETER Configuration
    The MSBuild configuration to build (default: Release.R26).

.PARAMETER ProductName
    The product name for the installer (default: HPRebar-KataExport).

.EXAMPLE
    .\build-kata-installer.ps1
    .\build-kata-installer.ps1 -Version 2026.1.0
#>
[CmdletBinding()]
param(
    [string]$Version = (Get-Date -Format "yy.M.d"),
    [string]$Configuration = "Release.R26",
    [string]$ProductName = "HPRebar-KataExport"
)

$ErrorActionPreference = "Stop"

# Refuse a version Windows Installer cannot order before spending minutes on the build.
$VersionPrefix = [version]($Version -split '-')[0]
if ($VersionPrefix.Major -gt 255 -or $VersionPrefix.Minor -gt 255 -or $VersionPrefix.Build -gt 65535) {
    throw "Version $Version is not a valid MSI version: major and minor must be below 256, build below 65536 (e.g. $(Get-Date -Format 'yy.M.d'))."
}

$ToolsDir = $PSScriptRoot
$HpRebarDir = (Resolve-Path "$ToolsDir\..").Path

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " Building $ProductName MSI Installer ($Configuration)" -ForegroundColor Cyan
Write-Host " Version: $Version" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Ensure WiX Toolset is available (the same major as the pipeline's CreateInstallerModule)
$WixExe = Get-Command "wix.exe" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source -ErrorAction SilentlyContinue
$LocalWixDir = Join-Path $HpRebarDir ".wix"

if (-not $WixExe) {
    if (-not (Test-Path "$LocalWixDir\wix.exe")) {
        Write-Host "--> Installing WiX Toolset locally into $LocalWixDir..." -ForegroundColor Yellow
        New-Item -ItemType Directory -Force -Path $LocalWixDir | Out-Null
        dotnet tool install wix --version "7.*" --tool-path $LocalWixDir
        if ($LASTEXITCODE -ne 0) { throw "Failed to install the WiX Toolset into $LocalWixDir" }
    }
    $WixExe = "$LocalWixDir\wix.exe"
}

$WixDir = Split-Path $WixExe -Parent
$env:PATH = "$WixDir;$env:PATH"

# The UI extension must match the wix.exe version, or the MSI build cannot load it.
$WixVersion = ((& $WixExe --version) -split '\+')[0].Trim()
Write-Host "--> WiX $WixVersion located at: $WixExe" -ForegroundColor Green
& $WixExe eula accept wix7 2>$null
& $WixExe extension add -g "WixToolset.UI.wixext/$WixVersion" 2>$null

# 2. Build and publish HPRebar with KataOnly=true. DeployAddin=false keeps the developer's own Revit add-in untouched,
#    but it also turns the SDK's PublishAddin off: without PublishAddin=true the publish folder keeps whatever an
#    older build left there, and that is what the MSI would ship. Empty it first so nothing stale can slip in.
$PublishDir = Join-Path $HpRebarDir "HPRebar\bin\$Configuration\publish"
if (Test-Path $PublishDir) {
    Write-Host "`n--> Removing the previous publish folder $PublishDir" -ForegroundColor Yellow
    Remove-Item -Recurse -Force $PublishDir
}

Write-Host "`n--> Compiling HPRebar ($Configuration, KataOnly=true)..." -ForegroundColor Yellow
$HpRebarProj = Join-Path $HpRebarDir "HPRebar\HPRebar.csproj"
dotnet build $HpRebarProj -c $Configuration -p:KataOnly=true -p:DeployAddin=false -p:PublishAddin=true
if ($LASTEXITCODE -ne 0) {
    throw "Failed to compile HPRebar ($Configuration) with KataOnly=true"
}
if (-not (Test-Path (Join-Path $PublishDir "HPRebar.addin"))) {
    throw "The build did not publish HPRebar.addin into $PublishDir"
}

# 3. Build Installer Generator project
Write-Host "`n--> Compiling Installer generator project..." -ForegroundColor Yellow
$InstallerProj = Join-Path $HpRebarDir "install\Installer.csproj"
dotnet build $InstallerProj -c Release
if ($LASTEXITCODE -ne 0) {
    throw "Failed to compile Installer project"
}

# 4. Generate MSI installers
Write-Host "`n--> Packaging MSI installers via WiX / WixSharp..." -ForegroundColor Yellow
$InstallerExe = Join-Path $HpRebarDir "install\bin\Release\net10.0-windows\Installer.exe"
$OutputDir = Join-Path $HpRebarDir "output"
$ExpectedMsis = @("SingleUser", "MultiUser") | ForEach-Object { Join-Path $OutputDir "$ProductName-$Version-$_.msi" }
$ExpectedMsis | Where-Object { Test-Path $_ } | Remove-Item -Force

Push-Location $HpRebarDir
try {
    & $InstallerExe "--name=$ProductName" $Version $PublishDir
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to generate MSI installers"
    }
} finally {
    Pop-Location
}

# 5. Check what the MSIs carry
Write-Host "`n--> Checking the MSI contents..." -ForegroundColor Yellow

function Test-KataMsi([string]$MsiPath) {
    $problems = [System.Collections.Generic.List[string]]::new()
    $work = Join-Path ([System.IO.Path]::GetTempPath()) ("kata-msi-" + [guid]::NewGuid().ToString("N"))
    New-Item -ItemType Directory -Path $work | Out-Null
    try {
        $wxsPath = Join-Path $work "package.wxs"
        & $WixExe msi decompile $MsiPath -o $wxsPath -x (Join-Path $work "files") 2>&1 | Out-Null
        if ($LASTEXITCODE -ne 0 -or -not (Test-Path $wxsPath)) {
            $problems.Add("wix msi decompile failed")
            return $problems
        }

        [xml]$wxs = Get-Content -Raw -Encoding UTF8 $wxsPath
        $files = $wxs.SelectNodes("//*[local-name()='File']")
        $names = @($files | ForEach-Object {
            if ($_.Name) { $_.Name } else { [System.IO.Path]::GetFileName($_.Source) }
        })

        foreach ($required in "HPRebar.dll", "HPRebar.addin") {
            if ($names -notcontains $required) { $problems.Add("missing $required") }
        }
        foreach ($foreign in $names | Where-Object { $_ -match '^(HPBimToolkit|HPRebar\.McpBridge|HPWebHost)' }) {
            $problems.Add("carries another add-in's file: $foreign")
        }

        # Folder names under the Addins root: the year folders the MSI writes into.
        $years = @($wxs.SelectNodes("//*[local-name()='Directory']") |
            ForEach-Object { $_.Name } | Where-Object { $_ -match '^20\d\d$' } | Sort-Object -Unique)
        if ($years.Count -ne 1 -or $years[0] -ne "2026") {
            $problems.Add("installs for Revit " + ($(if ($years) { $years -join ", " } else { "(no version folder)" })) + ", expected 2026 only")
        }

        # The Kata window and its settings window are pages embedded as wwwroot/<page> resources of HPRebar.dll;
        # resource names sit in the metadata as plain UTF-8, so a byte search needs no assembly loading.
        $dll = Get-ChildItem -Recurse -File -Path (Join-Path $work "files") -ErrorAction SilentlyContinue |
            Where-Object { $_.Length -gt 1MB } |
            Where-Object { [System.Text.Encoding]::UTF8.GetString([System.IO.File]::ReadAllBytes($_.FullName)).Contains("HPRebar.KataExport") } |
            Select-Object -First 1
        if (-not $dll) {
            $problems.Add("HPRebar.dll could not be extracted from the MSI")
        } else {
            $text = [System.Text.Encoding]::UTF8.GetString([System.IO.File]::ReadAllBytes($dll.FullName))
            foreach ($page in "wwwroot/kata.html", "wwwroot/settings.html") {
                if (-not $text.Contains($page)) { $problems.Add("HPRebar.dll does not embed $page") }
            }
        }
    } finally {
        Remove-Item -Recurse -Force $work -ErrorAction SilentlyContinue
    }
    return $problems
}

$failed = $false
foreach ($msi in $ExpectedMsis) {
    if (-not (Test-Path $msi)) {
        Write-Host " FAIL $([System.IO.Path]::GetFileName($msi)): not produced" -ForegroundColor Red
        $failed = $true
        continue
    }
    $problems = Test-KataMsi $msi
    if ($problems.Count -eq 0) {
        Write-Host " PASS $([System.IO.Path]::GetFileName($msi))" -ForegroundColor Green
    } else {
        $failed = $true
        foreach ($problem in $problems) {
            Write-Host " FAIL $([System.IO.Path]::GetFileName($msi)): $problem" -ForegroundColor Red
        }
    }
}
if ($failed) {
    throw "The Kata MSI check failed; do not hand these installers out."
}

# 6. Report generated artifacts
Write-Host "`n==========================================================" -ForegroundColor Green
Write-Host " Build, packaging and MSI check completed" -ForegroundColor Green
Write-Host " Output MSI Files:" -ForegroundColor Green
foreach ($msi in $ExpectedMsis) {
    $file = Get-Item $msi
    $sizeMb = [math]::Round($file.Length / 1MB, 2)
    Write-Host " - $($file.Name) ($sizeMb MB)" -ForegroundColor Cyan
    Write-Host "   Path: $($file.FullName)" -ForegroundColor DarkGray
}
Write-Host "==========================================================" -ForegroundColor Green
