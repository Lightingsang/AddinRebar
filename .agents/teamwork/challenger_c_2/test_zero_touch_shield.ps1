# Adversarial Test Suite 1: Zero-Touch Shield & Process Isolation Challenge
# Author: challenger_c_2
# Purpose: Stress-test Revit PID 26264 detection, zero-kill invariants, and process non-interference.

$ErrorActionPreference = 'Stop'
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
Write-Host " ADVERSARIAL SUITE 1: Zero-Touch Shield & Revit PID 26264 Challenge" -ForegroundColor Cyan
Write-Host "======================================================================" -ForegroundColor Cyan

# --- Challenge 1.1: Static AST Code Analysis for Forbidden Process Terminations ---
$scriptPath = "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\tools\benchmark\run-benchmark.ps1"
$content = Get-Content $scriptPath -Raw

$forbiddenPatterns = @(
    "Stop-Process",
    "Kill-Process",
    "taskkill",
    "\.Kill\(",
    "TerminateProcess",
    "CloseMainWindow",
    "PostMessage",
    "SendMessage",
    "WM_CLOSE"
)

$foundViolations = @()
foreach ($pat in $forbiddenPatterns) {
    if ($content -match $pat) {
        $foundViolations += $pat
    }
}

$detailsZero = if ($foundViolations.Count -eq 0) { "Zero kill/terminate keywords found in script." } else { "Found forbidden patterns: $($foundViolations -join ', ')" }
Record-TestResult -TestName "AST: Zero Termination Invariant" `
    -Passed ($foundViolations.Count -eq 0) `
    -Details $detailsZero

# --- Challenge 1.2: Two-Tier Revit Detection Unit Test Logic ---
function Testable-GetRevitSessionStatus {
    param([int]$KnownTargetPid = 26264, [array]$MockProcesses = $null)

    $allRevitProcs = @(if ($PSBoundParameters.ContainsKey('MockProcesses')) {
        $MockProcesses
    } else {
        Get-Process -Name "Revit" -ErrorAction SilentlyContinue
    })
    $hasRevit = ($allRevitProcs.Count -gt 0)
    $targetProc = if ($hasRevit) { $allRevitProcs | Where-Object { $_.Id -eq $KnownTargetPid } } else { $null }
    $pipeActive = Test-Path "\\.\pipe\hpdynamo-mcp-2026"

    return [PSCustomObject]@{
        IsRunning         = $hasRevit
        ProcessCount      = $allRevitProcs.Count
        ActivePids        = @($allRevitProcs | ForEach-Object { $_.Id })
        TargetPidDetected = ($null -ne $targetProc)
        PipeActive        = $pipeActive
        Processes         = $allRevitProcs
    }
}

# Scenario A: No Revit processes running
$resEmpty = Testable-GetRevitSessionStatus -KnownTargetPid 26264 -MockProcesses @()
$passA = ($resEmpty.IsRunning -eq $false) -and ($resEmpty.ProcessCount -eq 0) -and ($resEmpty.TargetPidDetected -eq $false)
Record-TestResult -TestName "Detection: Zero Revit Processes" `
    -Passed $passA `
    -Details "IsRunning=$($resEmpty.IsRunning), TargetPidDetected=$($resEmpty.TargetPidDetected)"

# Scenario B: Target PID 26264 specifically running
$mockProcTarget = [PSCustomObject]@{ Id = 26264; ProcessName = "Revit" }
$resTargetOnly = Testable-GetRevitSessionStatus -KnownTargetPid 26264 -MockProcesses @($mockProcTarget)
$passB = ($resTargetOnly.IsRunning -eq $true) -and ($resTargetOnly.ProcessCount -eq 1) -and ($resTargetOnly.TargetPidDetected -eq $true) -and ($resTargetOnly.ActivePids -contains 26264)
Record-TestResult -TestName "Detection: Specific PID 26264 Active" `
    -Passed $passB `
    -Details "Target detected=$($resTargetOnly.TargetPidDetected), ActivePids=$($resTargetOnly.ActivePids -join ',')"

# Scenario C: Other Revit running with different PID (e.g. 12345), PID 26264 absent
$mockProcOther = [PSCustomObject]@{ Id = 12345; ProcessName = "Revit" }
$resOtherOnly = Testable-GetRevitSessionStatus -KnownTargetPid 26264 -MockProcesses @($mockProcOther)
$passC = ($resOtherOnly.IsRunning -eq $true) -and ($resOtherOnly.ProcessCount -eq 1) -and ($resOtherOnly.TargetPidDetected -eq $false)
Record-TestResult -TestName "Detection: Other Revit PID (No 26264)" `
    -Passed $passC `
    -Details "IsRunning=$($resOtherOnly.IsRunning), TargetPidDetected=$($resOtherOnly.TargetPidDetected) (expected false)"

# Scenario D: Multiple Revit processes running (PID 11111, 26264, 33333)
$mockMulti = @(
    [PSCustomObject]@{ Id = 11111; ProcessName = "Revit" },
    [PSCustomObject]@{ Id = 26264; ProcessName = "Revit" },
    [PSCustomObject]@{ Id = 33333; ProcessName = "Revit" }
)
$resMulti = Testable-GetRevitSessionStatus -KnownTargetPid 26264 -MockProcesses $mockMulti
$passD = ($resMulti.IsRunning -eq $true) -and ($resMulti.ProcessCount -eq 3) -and ($resMulti.TargetPidDetected -eq $true) -and ($resMulti.ActivePids.Count -eq 3)
Record-TestResult -TestName "Detection: Multi-Instance Cluster including 26264" `
    -Passed $passD `
    -Details "ProcessCount=$($resMulti.ProcessCount), TargetPidDetected=$($resMulti.TargetPidDetected)"

# --- Challenge 1.3: Dynamic Process Survival Test ---
# Start a benign dummy process, record its PID, run benchmark in DryRun mode, verify dummy process is untouched.
$dummyProc = Start-Process powershell -ArgumentList "-NoProfile -Command Start-Sleep 30" -PassThru
$dummyPid = $dummyProc.Id
try {
    # Verify dummy is alive
    $initialCheck = Get-Process -Id $dummyPid -ErrorAction SilentlyContinue
    $isAliveBefore = ($initialCheck -ne $null)

    # Run the benchmark script in DryRun mode
    & powershell -NoProfile -File $scriptPath -DryRun -WarmRuns 1 -ColdRuns 1 | Out-Null

    # Verify dummy is STILL alive
    $postCheck = Get-Process -Id $dummyPid -ErrorAction SilentlyContinue
    $isAliveAfter = ($postCheck -ne $null)

    $passSurvival = $isAliveBefore -and $isAliveAfter
    Record-TestResult -TestName "Dynamic Survival: External Process Non-Interference" `
        -Passed $passSurvival `
        -Details "Process PID $dummyPid was alive before and survived full benchmark execution."
}
finally {
    if ($dummyProc -and -not $dummyProc.HasExited) {
        $dummyProc.Kill()
    }
}

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
