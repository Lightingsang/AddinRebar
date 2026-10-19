# Test proposed Remove-BenchmarkSandbox boundary logic
$ErrorActionPreference = 'Stop'
$repoRoot = "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar"

function Remove-BenchmarkSandbox-Proposed {
    param([string]$SandboxDir)

    if (-not $SandboxDir -or -not (Test-Path $SandboxDir)) { return }

    # Constraint 1: Directory must be strictly inside %TEMP%\HPDynamoBenchmark\
    $sandboxRootCanonical = [System.IO.Path]::GetFullPath((Join-Path $env:TEMP "HPDynamoBenchmark")).TrimEnd('\', '/')
    $requiredPrefix = $sandboxRootCanonical + [System.IO.Path]::DirectorySeparatorChar
    $targetCanonical = [System.IO.Path]::GetFullPath($SandboxDir).TrimEnd('\', '/')

    if (-not $targetCanonical.StartsWith($requiredPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        Write-Error "SECURITY REFUSAL: Target directory '$SandboxDir' is outside %TEMP%\HPDynamoBenchmark. Deletion aborted."
        return
    }

    # Constraint 2: Collect GC handles before deletion
    [System.GC]::Collect()
    [System.GC]::WaitForPendingFinalizers()

    # Constraint 3: Retry up to 3 times for transient file locks
    $success = $false
    for ($i = 1; $i -le 3; $i++) {
        try {
            Remove-Item -Path $targetCanonical -Recurse -Force -ErrorAction Stop
            $success = $true
            break
        }
        catch {
            Start-Sleep -Milliseconds (200 * $i)
        }
    }

    return $success
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
        Remove-BenchmarkSandbox-Proposed -SandboxDir $testDir 2>&1 | Tee-Object -Variable errOut | Out-Null
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
    if ($pass) {
        $passedCount++
        Write-Host "[PASS] $($target.Name): Safely refused (refused=$refused, stillExists=$stillExists)" -ForegroundColor Green
    } else {
        Write-Host "[FAIL] $($target.Name): Security failure (refused=$refused, stillExists=$stillExists)" -ForegroundColor Red
    }
}

# Positive in-bounds test:
$totalCount++
$validSandbox = Join-Path $env:TEMP "HPDynamoBenchmark\Session_test_12345"
New-Item -ItemType Directory -Path $validSandbox -Force | Out-Null
$cleaned = Remove-BenchmarkSandbox-Proposed -SandboxDir $validSandbox
$stillExists = (Test-Path $validSandbox)
if ($cleaned -and (-not $stillExists)) {
    $passedCount++
    Write-Host "[PASS] Valid Sandbox Cleanup: Successfully deleted in-bounds folder" -ForegroundColor Green
} else {
    Write-Host "[FAIL] Valid Sandbox Cleanup: Failed to delete in-bounds folder" -ForegroundColor Red
}

Write-Host "Summary: $passedCount / $totalCount Passed"
