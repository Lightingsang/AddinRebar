# Independent verification test of production Remove-BenchmarkSandbox
$ErrorActionPreference = 'Stop'
$repoRoot = "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar"

# 1. Load function from production script
$scriptContent = Get-Content (Join-Path $repoRoot "HPDynamo\tools\benchmark\run-benchmark.ps1") -Raw
if ($scriptContent -match '(?ms)(function Remove-BenchmarkSandbox\s*\{.*?\n\})') {
    Invoke-Expression $matches[1]
    Write-Host "Successfully extracted and defined Remove-BenchmarkSandbox from production script." -ForegroundColor Cyan
} else {
    Write-Error "Failed to extract Remove-BenchmarkSandbox."
    exit 1
}

$adversarialTargets = @(
    @{ Name = "Root Directory Attack"; Path = "C:\Windows\System32" },
    @{ Name = "Repo Root Attack"; Path = $repoRoot },
    @{ Name = "User Profile Attack"; Path = $env:USERPROFILE },
    @{ Name = "Temp Substring Spoof Attack"; Path = Join-Path $env:TEMP "NotHPDynamoBenchmark" },
    @{ Name = "Sibling Substring Attack"; Path = Join-Path $env:TEMP "HPDynamoBenchmark_Backup" },
    @{ Name = "Directory Traversal Attack"; Path = Join-Path $env:TEMP "HPDynamoBenchmark\..\OtherDir" },
    @{ Name = "Direct Sandbox Root Attack"; Path = Join-Path $env:TEMP "HPDynamoBenchmark" }
)

$passedCount = 0
$totalCount = 0

foreach ($target in $adversarialTargets) {
    $totalCount++
    $testDir = $target.Path
    $createdTemp = $false
    if (-not (Test-Path $testDir)) {
        try {
            New-Item -ItemType Directory -Path $testDir -Force | Out-Null
            $createdTemp = $true
        } catch { }
    }

    $refused = $false
    try {
        $errOut = $null
        Remove-BenchmarkSandbox -SandboxDir $testDir 2>&1 | Tee-Object -Variable errOut | Out-Null
        if ($errOut -and ($errOut -match "SECURITY REFUSAL")) {
            $refused = $true
        }
    } catch {
        if ($_.Exception.Message -match "SECURITY REFUSAL") {
            $refused = $true
        }
    }

    $stillExists = (Test-Path $testDir)
    if ($createdTemp -and (Test-Path $testDir)) {
        Remove-Item -Path $testDir -Recurse -Force -ErrorAction SilentlyContinue
    }

    $pass = $refused -and $stillExists
    $tgtName = $target.Name
    if ($pass) {
        $passedCount++
        Write-Host "[PASS] $tgtName : Safely refused (refused=$refused, stillExists=$stillExists)" -ForegroundColor Green
    } else {
        Write-Host "[FAIL] $tgtName : Security failure (refused=$refused, stillExists=$stillExists)" -ForegroundColor Red
    }
}

# Positive in-bounds test:
$totalCount++
$validSandbox = Join-Path $env:TEMP "HPDynamoBenchmark\Session_live_test_12345"
New-Item -ItemType Directory -Path $validSandbox -Force | Out-Null
Remove-BenchmarkSandbox -SandboxDir $validSandbox 2>&1 | Out-Null
$stillExists = (Test-Path $validSandbox)
if (-not $stillExists) {
    $passedCount++
    Write-Host "[PASS] Valid Sandbox Cleanup : Successfully deleted in-bounds folder" -ForegroundColor Green
} else {
    Write-Host "[FAIL] Valid Sandbox Cleanup : Failed to delete in-bounds folder" -ForegroundColor Red
}

Write-Host "Direct Production Sandbox Audit Summary: $passedCount / $totalCount Passed"
if ($passedCount -ne $totalCount) {
    exit 1
}
