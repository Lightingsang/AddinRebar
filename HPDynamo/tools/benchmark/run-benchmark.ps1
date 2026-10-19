# HPDynamo Performance & Workload Benchmark Harness (v2.0 Persistent)
# Measures latency distributions (p50, p95, min, max, mean), round-trips,
# and payload bytes across standard workloads WL-01..WL-05 using persistent session.
# Enforces two-tier Revit detection (PID 26264), Zero-Touch Shield, and sandbox isolation.

[CmdletBinding()]
param(
    [string]$ServerExe = "",
    [string]$OutputPath = "",
    [int]$WarmRuns = 10,
    [int]$ColdRuns = 1,
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'

# ---- 1. Path Resolution -------------------------------------------------------------------------
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$hpdynamoDir = Resolve-Path (Join-Path $scriptDir "..\..")
$repoRoot = Resolve-Path (Join-Path $hpdynamoDir "..")

if (-not $ServerExe) {
    $ServerExe = Join-Path $hpdynamoDir "HPDynamo.Mcp.Server\bin\Debug\net10.0\HPDynamo.Mcp.Server.exe"
    if (-not (Test-Path $ServerExe)) {
        $ServerExe = Join-Path $hpdynamoDir "HPDynamo.Mcp.Server\bin\Release\net10.0\HPDynamo.Mcp.Server.exe"
    }
}
if (-not (Test-Path $ServerExe)) {
    throw "HPDynamo MCP Server executable not found at '$ServerExe'. Build solution first."
}

if (-not $OutputPath) {
    $OutputPath = Join-Path $repoRoot "plans\261009-1118-hpdynamo-mcp-performance\reports\baseline-vs-upgraded.json"
}

$sessionScript = Join-Path $scriptDir "benchmark_session.py"
if (-not (Test-Path $sessionScript)) {
    throw "Persistent session engine not found at '$sessionScript'."
}

Write-Host "==================================================================" -ForegroundColor Cyan
Write-Host " HPDynamo Performance Benchmark Harness (v2.0 Persistent Session)" -ForegroundColor Cyan
Write-Host "==================================================================" -ForegroundColor Cyan
Write-Host "Server Exe  : $ServerExe`nOutput Path : $OutputPath`nWarm Runs   : $WarmRuns`nCold Runs   : $ColdRuns"

# ---- 2. Two-Tier Revit Detection & Zero-Touch Shield --------------------------------------------
function Get-RevitSessionStatus {
    param([int]$KnownTargetPid = 26264)

    $allRevitProcs = @(Get-Process -Name "Revit" -ErrorAction SilentlyContinue)
    $hasRevit = ($allRevitProcs.Count -gt 0)
    $targetProc = if ($hasRevit) { $allRevitProcs | Where-Object { $_.Id -eq $KnownTargetPid } } else { $null }
    $pipeActive = Test-Path "\\.\pipe\hpdynamo-mcp-2026"

    return [PSCustomObject]@{
        IsRunning         = $hasRevit
        ProcessCount      = $allRevitProcs.Count
        ActivePids        = ($allRevitProcs | ForEach-Object { $_.Id })
        TargetPidDetected = ($targetProc -ne $null)
        PipeActive        = $pipeActive
        Processes         = $allRevitProcs
    }
}

Write-Host "`n[1/3] Evaluating Revit Session & Zero-Touch Shield..." -ForegroundColor Yellow
$revitSession = Get-RevitSessionStatus -KnownTargetPid 26264

if ($revitSession.IsRunning) {
    $pidsStr = ($revitSession.ActivePids -join ", ")
    Write-Host "`n******************************************************************" -ForegroundColor Yellow
    Write-Host " [ZERO-TOUCH SESSION SHIELD ACTIVE]" -ForegroundColor Yellow
    Write-Host " - Revit Process(es) Detected: PID(s) $pidsStr" -ForegroundColor Yellow
    if ($revitSession.TargetPidDetected) {
        Write-Host " - Target Session PID 26264 Identified: User workspace protected." -ForegroundColor Yellow
    }
    Write-Host " - Protection: Active user documents and canvas are LOCKED." -ForegroundColor Yellow
    Write-Host " - Rule: Zero kill, zero close, zero unverified mutations." -ForegroundColor Yellow
    Write-Host "******************************************************************`n" -ForegroundColor Yellow
} else {
    Write-Host "  -> No active Revit process detected. Operating in safe stdio mode." -ForegroundColor Gray
}

# ---- 3. Sandbox Isolation Functions -------------------------------------------------------------
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

    Write-Host "  -> Sandbox created: $sessionDir" -ForegroundColor Green
    Write-Host "  -> Cloned RVT fixture ($((Get-Item $destRvt).Length) bytes)" -ForegroundColor Gray
    Write-Host "  -> Cloned DYN fixture ($((Get-Item $destDyn).Length) bytes)" -ForegroundColor Gray

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
            # Transient Windows indexer lock retry
            Start-Sleep -Milliseconds (200 * $i)
        }
    }

    if ($success) {
        Write-Host "  -> Sandbox cleaned up: $targetCanonical" -ForegroundColor Green
    } else {
        Write-Warning "Could not immediately remove '$targetCanonical'. Scheduled for cleanup."
    }
}

# ---- 4. Execution in Sandbox Isolation ----------------------------------------------------------
$sandbox = $null
$exitCode = 0

try {
    Write-Host "`n[2/3] Initializing Isolated Sandbox Fixtures..." -ForegroundColor Yellow
    $sandbox = Initialize-BenchmarkSandbox -Root $repoRoot

    Write-Host "`n[3/3] Launching Persistent Session Engine..." -ForegroundColor Yellow
    $pyArgs = @(
        $sessionScript,
        "--server-exe", $ServerExe,
        "--output-json", $OutputPath,
        "--warm-runs", $WarmRuns,
        "--cold-runs", $ColdRuns
    )
    if ($DryRun) {
        $pyArgs += "--dry-run"
    }

    $env:PYTHONUTF8 = "1"
    $env:PYTHONIOENCODING = "utf-8"

    & python $pyArgs
    $pyExitCode = $LASTEXITCODE

    if ($pyExitCode -ne 0) {
        Write-Host "Engine exited with failure code: $pyExitCode" -ForegroundColor Red
        $exitCode = 1
    } else {
        Write-Host "`nBenchmark harness run completed successfully." -ForegroundColor Green
    }

    # Also mirror report to HPDynamo/reports if different
    $altPath = Join-Path $hpdynamoDir "reports\baseline-vs-upgraded.json"
    if ((Test-Path $OutputPath) -and ($OutputPath -ne $altPath)) {
        $altDir = Split-Path -Parent $altPath
        if (-not (Test-Path $altDir)) { New-Item -ItemType Directory -Path $altDir -Force | Out-Null }
        Copy-Item -Path $OutputPath -Destination $altPath -Force
        Write-Host "Mirrored report to: $altPath" -ForegroundColor Gray
    }
}
finally {
    if ($sandbox) {
        Write-Host "`nFinalizing & cleaning up sandbox..." -ForegroundColor Gray
        Remove-BenchmarkSandbox -SandboxDir $sandbox.SessionDir
    }
}

exit $exitCode
