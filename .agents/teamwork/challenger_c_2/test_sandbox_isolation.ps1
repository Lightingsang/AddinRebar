# Adversarial Test Suite 2: Sandbox Isolation & Boundary Enforcement Challenge
# Author: challenger_c_2
# Purpose: Stress-test boundary enforcement in Remove-BenchmarkSandbox and binary fixture cloning integrity.

$ErrorActionPreference = 'Stop'
$repoRoot = "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar"
$testResults = [System.Collections.Generic.List[PSCustomObject]]::new()

function Record-TestResult {
    param([string]$TestName, [bool]$Passed, [string]$Details)
    $testResults.Add([PSCustomObject]@{
        TestName = $TestName
        Passed   = $Passed
        Details  = $Details
    })
    $statusStr = if ($Passed) { "PASS" } else { "FAIL" }
    $color = if ($Passed) { "Green" } else { "Red" }
    Write-Host "[$statusStr] $TestName - $Details" -ForegroundColor $color
}

Write-Host "======================================================================" -ForegroundColor Cyan
Write-Host " ADVERSARIAL SUITE 2: Sandbox Boundary & Fixture Cloning Challenge" -ForegroundColor Cyan
Write-Host "======================================================================" -ForegroundColor Cyan

# Define exact sandbox functions from run-benchmark.ps1 using single-quoted here-string (no string interpolation)
$sandboxScript = @'
function Initialize-BenchmarkSandbox {
    param([string]$Root)

    $runId = [System.Guid]::NewGuid().ToString("N")
    $sandboxRoot = Join-Path $env:TEMP "HPDynamoBenchmark"
    $sessionDir = Join-Path $sandboxRoot "Session_$runId"

    if (-not (Test-Path $sessionDir)) {
        New-Item -ItemType Directory -Path $sessionDir -Force | Out-Null
    }

    # Resolve RVT fixture
    $srcRvt = Join-Path $Root "HPDynamo\tools\harness\fixtures\SampleRebarModel.rvt"
    if (-not (Test-Path $srcRvt)) {
        $srcRvt = Join-Path $Root "HPRebar\HPRebar.Tests\Fixtures\column-stack-2-storey.rvt"
    }
    if (-not (Test-Path $srcRvt)) {
        throw "Source RVT fixture not found at '$srcRvt'."
    }
    $destRvt = Join-Path $sessionDir "BenchmarkIsolatedModel.rvt"
    Copy-Item -Path $srcRvt -Destination $destRvt -Force
    (Get-Item $destRvt).IsReadOnly = $false

    # Resolve DYN fixture
    $srcDyn = Join-Path $Root "HPDynamo\tools\harness\fixtures\StandardBenchmarkGraph.dyn"
    $destDyn = Join-Path $sessionDir "BenchmarkIsolatedGraph.dyn"
    if (Test-Path $srcDyn) {
        Copy-Item -Path $srcDyn -Destination $destDyn -Force
        (Get-Item $destDyn).IsReadOnly = $false
    }

    return [PSCustomObject]@{
        SessionDir = $sessionDir
        ModelPath  = $destRvt
        GraphPath  = $destDyn
        RunId      = $runId
    }
}

function Remove-BenchmarkSandbox {
    param([string]$SandboxDir)

    if (-not $SandboxDir -or -not (Test-Path $SandboxDir)) { return }

    # Constraint 1: Directory must be inside %TEMP%\HPDynamoBenchmark
    $tempCanonical = [System.IO.Path]::GetFullPath($env:TEMP).TrimEnd('\', '/')
    $targetCanonical = [System.IO.Path]::GetFullPath($SandboxDir).TrimEnd('\', '/')

    if (-not $targetCanonical.StartsWith($tempCanonical, [StringComparison]::OrdinalIgnoreCase) -or
        -not $targetCanonical.Contains("HPDynamoBenchmark")) {
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
'@

. ([scriptblock]::Create($sandboxScript))

# --- Attack Scenario 2.1: Out-of-Bounds Deletion Attempts ---
$adversarialTargets = @(
    @{ Name = "Root Directory Attack"; Path = "C:\Windows\System32" },
    @{ Name = "Repo Root Attack"; Path = $repoRoot },
    @{ Name = "User Profile Attack"; Path = $env:USERPROFILE },
    @{ Name = "Temp Substring Spoof Attack"; Path = Join-Path $env:TEMP "NotHPDynamoBenchmark" },
    @{ Name = "Directory Traversal Attack"; Path = Join-Path $env:TEMP "HPDynamoBenchmark\..\OtherDir" }
)

foreach ($target in $adversarialTargets) {
    # If the directory doesn't exist, create a dummy directory to test if Remove-BenchmarkSandbox would attempt deletion
    $testDir = $target.Path
    $createdTemp = $false
    if (-not (Test-Path $testDir)) {
        try {
            New-Item -ItemType Directory -Path $testDir -Force | Out-Null
            $createdTemp = $true
        } catch {
            # Could not create (e.g. permission on C:\Windows), which is fine
        }
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

    # Verify directory still exists (was NOT deleted)
    $stillExists = (Test-Path $testDir)

    # Clean up dummy test directory if we created it
    if ($createdTemp -and (Test-Path $testDir)) {
        Remove-Item -Path $testDir -Recurse -Force -ErrorAction SilentlyContinue
    }

    $passTarget = $refused -or $stillExists
    Record-TestResult -TestName "Boundary Refusal: $($target.Name)" `
        -Passed $passTarget `
        -Details "Target '$($target.Path)' safely rejected with security refusal (stillExists=$stillExists)."
}

# --- Attack Scenario 2.2: Boundary Refusal on Root of %TEMP% ---
$tempRoot = $env:TEMP
$refusedTempRoot = $false
try {
    $err = $null
    Remove-BenchmarkSandbox -SandboxDir $tempRoot 2>&1 | Tee-Object -Variable err | Out-Null
    if ($err -match "SECURITY REFUSAL") { $refusedTempRoot = $true }
} catch {
    if ($_.Exception.Message -match "SECURITY REFUSAL") { $refusedTempRoot = $true }
}
Record-TestResult -TestName "Boundary Refusal: Direct %TEMP% Root" `
    -Passed $refusedTempRoot `
    -Details "Prevented accidental wiping of entire %TEMP% root directory."

# --- Scenario 2.3: In-Bounds Positive Creation & Cleanup ---
$sandbox = Initialize-BenchmarkSandbox -Root $repoRoot
$sessionDir = $sandbox.SessionDir
$dirCreated = (Test-Path $sessionDir)
Record-TestResult -TestName "Sandbox Creation: Valid Isolation Folder" `
    -Passed $dirCreated `
    -Details "Created isolated sandbox at '$sessionDir'"

# Verify Binary Fixture: SampleRebarModel.rvt
$rvtPath = $sandbox.ModelPath
$rvtExists = (Test-Path $rvtPath)
$srcRvt = Join-Path $repoRoot "HPDynamo\tools\harness\fixtures\SampleRebarModel.rvt"
if (-not (Test-Path $srcRvt)) {
    $srcRvt = Join-Path $repoRoot "HPRebar\HPRebar.Tests\Fixtures\column-stack-2-storey.rvt"
}

$rvtLength = (Get-Item $rvtPath).Length
$srcLength = (Get-Item $srcRvt).Length
$rvtHash = (Get-FileHash $rvtPath -Algorithm SHA256).Hash
$srcHash = (Get-FileHash $srcRvt -Algorithm SHA256).Hash
$rvtNotReadOnly = -not (Get-Item $rvtPath).IsReadOnly

$rvtIntegrity = $rvtExists -and ($rvtLength -eq $srcLength) -and ($rvtLength -eq 7540736) -and ($rvtHash -eq $srcHash) -and $rvtNotReadOnly
Record-TestResult -TestName "Fixture Integrity: SampleRebarModel.rvt" `
    -Passed $rvtIntegrity `
    -Details "Size: $rvtLength bytes, SHA256: $rvtHash, ReadOnly=$(-not $rvtNotReadOnly)"

# Verify Graph Fixture: StandardBenchmarkGraph.dyn
$dynPath = $sandbox.GraphPath
$dynExists = (Test-Path $dynPath)
$srcDyn = Join-Path $repoRoot "HPDynamo\tools\harness\fixtures\StandardBenchmarkGraph.dyn"
$dynLength = (Get-Item $dynPath).Length
$dynHash = (Get-FileHash $dynPath -Algorithm SHA256).Hash
$srcDynHash = (Get-FileHash $srcDyn -Algorithm SHA256).Hash

# Verify valid JSON structure of cloned graph
$dynContent = Get-Content $dynPath -Raw | ConvertFrom-Json
$nodesCount = $dynContent.Nodes.Count
$wiresCount = $dynContent.Connectors.Count

$dynIntegrity = $dynExists -and ($dynHash -eq $srcDynHash) -and ($nodesCount -eq 5) -and ($wiresCount -eq 4)
Record-TestResult -TestName "Fixture Integrity: StandardBenchmarkGraph.dyn" `
    -Passed $dynIntegrity `
    -Details "Size: $dynLength bytes, Nodes: $nodesCount, Connectors: $wiresCount, JSON: Valid"

# Clean up sandbox in-bounds
$cleanupOk = Remove-BenchmarkSandbox -SandboxDir $sessionDir
$dirStillExists = (Test-Path $sessionDir)
$passCleanup = (-not $dirStillExists)
Record-TestResult -TestName "In-Bounds Sandbox Cleanup" `
    -Passed $passCleanup `
    -Details "Cleaned up sandbox folder completely (Exists=$dirStillExists)."

# --- Summary ---
$total = $testResults.Count
$passed = ($testResults | Where-Object { $_.Passed }).Count
$failed = $total - $passed
$summaryColor = if ($failed -eq 0) { "Green" } else { "Red" }
Write-Host "`nSummary: $passed / $total Passed ($failed Failed)" -ForegroundColor $summaryColor

if ($failed -gt 0) {
    exit 1
}
exit 0
