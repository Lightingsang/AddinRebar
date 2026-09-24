<#
.SYNOPSIS
    Switch .mcp.json between Work (F:) and Home (G:) environments.

.DESCRIPTION
    Copies .mcp.work.json or .mcp.home.json to .mcp.json.
    -Profile Auto detects the drive letter of the current repository.

.EXAMPLE
    .\tools\switch-mcp.ps1 -Profile Work
    .\tools\switch-mcp.ps1 -Profile Home
    .\tools\switch-mcp.ps1 -Profile Auto
#>

[CmdletBinding()]
param(
    [ValidateSet('Work', 'Home', 'Auto')]
    [string]$Profile = 'Auto'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path (Join-Path $repoRoot '.mcp.json'))) {
    $repoRoot = (Get-Location).Path
}

$workConfig = Join-Path $repoRoot '.mcp.work.json'
$homeConfig = Join-Path $repoRoot '.mcp.home.json'
$targetConfig = Join-Path $repoRoot '.mcp.json'

if (-not (Test-Path $workConfig)) {
    throw "Work config template not found: $workConfig"
}
if (-not (Test-Path $homeConfig)) {
    throw "Home config template not found: $homeConfig"
}

$selectedProfile = $Profile
if ($selectedProfile -eq 'Auto') {
    $drive = (Get-Item $repoRoot).PSDrive.Name
    if ($drive -eq 'F') {
        $selectedProfile = 'Work'
    }
    elseif ($drive -eq 'G') {
        $selectedProfile = 'Home'
    }
    else {
        Write-Warning "Unrecognized drive '$drive' (not F or G). Defaulting to Work."
        $selectedProfile = 'Work'
    }
}

if ($selectedProfile -eq 'Work') {
    Copy-Item -Path $workConfig -Destination $targetConfig -Force
    Write-Host "[OK] Da ap dung cau hinh MCP cho WORK (O F:)" -ForegroundColor Green
}
elseif ($selectedProfile -eq 'Home') {
    Copy-Item -Path $homeConfig -Destination $targetConfig -Force
    Write-Host "[OK] Da ap dung cau hinh MCP cho HOME (O G:)" -ForegroundColor Cyan
}

Write-Host "Target config: $targetConfig" -ForegroundColor DarkGray
