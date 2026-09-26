<#
.SYNOPSIS
    Creates a desktop shortcut for HPSap2000 MCP Bridge.

.DESCRIPTION
    Publishes HPSap2000.McpBridge in Release configuration to output directory
    and creates a Windows shortcut (.lnk) on the current user's Desktop with icon
    and working directory configured.

.PARAMETER Publish
    Runs 'dotnet publish' before creating the shortcut. Default: $true.

.PARAMETER Configuration
    Build configuration ('Release' or 'Debug'). Default: 'Release'.

.PARAMETER Launch
    Automatically launches HPSap2000 MCP Bridge after shortcut creation.

.EXAMPLE
    .\tools\create-desktop-shortcut.ps1
    .\tools\create-desktop-shortcut.ps1 -Publish:$false
    .\tools\create-desktop-shortcut.ps1 -Launch
#>

[CmdletBinding()]
param(
    [switch]$Publish = $true,
    [string]$Configuration = 'Release',
    [switch]$Launch
)

$ErrorActionPreference = 'Stop'

$scriptDir = $PSScriptRoot
$sapDir = (Resolve-Path (Join-Path $scriptDir '..')).Path
$projectDir = Join-Path $sapDir 'HPSap2000.McpBridge'
$publishDir = Join-Path $sapDir 'output\HPSap2000.McpBridge'
$exePath = Join-Path $publishDir 'HPSap2000.McpBridge.exe'
$iconPath = Join-Path $projectDir 'Resources\HPSap2000McpBridge.ico'

$desktopDir = [Environment]::GetFolderPath('Desktop')
$shortcutPath = Join-Path $desktopDir 'HPSap2000 MCP Bridge.lnk'

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host " HPSap2000 MCP Bridge - Desktop Shortcut" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

# 1. Publish if requested or if exe does not exist
if ($Publish -or (-not (Test-Path $exePath))) {
    Write-Host "`n[1/3] Checking running processes..." -ForegroundColor Yellow
    $running = Get-Process -Name 'HPSap2000.McpBridge' -ErrorAction SilentlyContinue
    if ($running) {
        Write-Warning "HPSap2000.McpBridge is running (PID $($running.Id)). Stopping to avoid locked files..."
        $running | Stop-Process -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 1
    }

    Write-Host "[2/3] Publishing HPSap2000.McpBridge ($Configuration)..." -ForegroundColor Yellow
    $publishCmd = @(
        'publish',
        $projectDir,
        '-c', $Configuration,
        '-r', 'win-x64',
        '-p:SelfContained=false',
        '-o', $publishDir
    )

    Write-Host "dotnet $($publishCmd -join ' ')" -ForegroundColor DarkGray
    & dotnet @publishCmd
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed with exit code $LASTEXITCODE"
    }
} else {
    Write-Host "`n[1/3] Skipping publish (exe exists at $exePath)" -ForegroundColor DarkGray
}

if (-not (Test-Path $exePath)) {
    throw "Target exe not found at: $exePath"
}

# 2. Create shortcut on Desktop
Write-Host "[3/3] Creating shortcut on Desktop..." -ForegroundColor Yellow

$wshShell = New-Object -ComObject WScript.Shell
$shortcut = $wshShell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $exePath
$shortcut.WorkingDirectory = $publishDir
$shortcut.Description = "HPSap2000 MCP Bridge - AI Assistant Bridge for CSI SAP2000"

if (Test-Path $iconPath) {
    $shortcut.IconLocation = "$iconPath,0"
} else {
    $shortcut.IconLocation = "$exePath,0"
}

$shortcut.Save()

[System.Runtime.InteropServices.Marshal]::ReleaseComObject($shortcut) | Out-Null
[System.Runtime.InteropServices.Marshal]::ReleaseComObject($wshShell) | Out-Null

Write-Host "`nSuccessfully created desktop shortcut!" -ForegroundColor Green
Write-Host " - Shortcut : $shortcutPath"
Write-Host " - Target   : $exePath"
Write-Host " - WorkDir  : $publishDir"
Write-Host " - Icon     : $iconPath"

# 3. Launch if requested
if ($Launch) {
    Write-Host "`nLaunching HPSap2000 MCP Bridge..." -ForegroundColor Cyan
    Start-Process -FilePath $exePath -WorkingDirectory $publishDir
}
