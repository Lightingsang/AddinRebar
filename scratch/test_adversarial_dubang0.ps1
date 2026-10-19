$ErrorActionPreference = "Stop"
$PublishDir = "G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.Core\bin\publish_netstandard"

Get-ChildItem -Path $PublishDir -Filter "*.dll" | ForEach-Object {
    try { [System.Reflection.Assembly]::LoadFrom($_.FullName) | Out-Null } catch {}
}

Write-Host "=================================================="
Write-Host "ADVERSARIAL STRESS TEST: CuttingStockStrategies.DuBang0"
Write-Host "=================================================="

# Helper to run SolveDuBang0
function Invoke-DuBang0($piecesArray, $stockLen, $kerf, $whArray) {
    $piecesList = [System.Collections.Generic.List[double]]::new()
    foreach ($p in $piecesArray) { $piecesList.Add([double]$p) }
    
    $whList = [System.Collections.Generic.List[double]]::new()
    if ($whArray) {
        foreach ($w in $whArray) { $whList.Add([double]$w) }
    }

    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $result = [HPKata.Core.Optimization.CuttingStockStrategies]::SolveDuBang0($piecesList, [double]$stockLen, [double]$kerf, $whList)
    $sw.Stop()
    return @{ Result = $result; ElapsedMs = $sw.ElapsedMilliseconds }
}

# Test 1: Exact matches exist (3x3900 = 11700, 2x5850 = 11700, plus extra 1000)
$t1 = Invoke-DuBang0 @(3900, 3900, 3900, 5850, 5850, 1000) 11700 0 @()
Write-Host "Test 1 (Known Exact Subsets): Produced $($t1.Result.Count) patterns in $($t1.ElapsedMs)ms."
$has2x5850 = $false
$has3x3900 = $false
foreach ($p in $t1.Result) {
    $cuts = $p.CutsMm
    Write-Host "  Pattern: Cuts = [$($cuts -join ', ')], Rem = $($p.RemnantLengthMm)mm"
    if ($cuts.Count -eq 2 -and [Math]::Abs($p.RemnantLengthMm) -lt 1.0) { $has2x5850 = $true }
    if ($cuts.Count -eq 3 -and [Math]::Abs($p.RemnantLengthMm) -lt 1.0) { $has3x3900 = $true }
}
if ($has2x5850 -and $has3x3900) {
    Write-Host "[PASS] Test 1: Both exact subsets correctly discovered!" -ForegroundColor Green
} else {
    Write-Error "Test 1 failed to discover exact subsets"
    exit 1
}

# Test 2: Pathological tree - 50 items between 2300 and 2400 where no 5 items sum to exactly 11700
$spectrum = @()
for ($i = 0; $i -lt 50; $i++) { $spectrum += (2300 + $i * 2.0) }
$t2 = Invoke-DuBang0 $spectrum 11700 0 @()
Write-Host "Test 2 (Pathological Dense Spectrum): Duration = $($t2.ElapsedMs)ms (Budget <= 1000ms), Patterns = $($t2.Result.Count)"
if ($t2.ElapsedMs -le 1000) {
    Write-Host "[PASS] Test 2: Dense spectrum terminated strictly within budget!" -ForegroundColor Green
} else {
    Write-Error "Test 2 exceeded 1000ms budget"
    exit 1
}

# Test 3: Pathological 200 items of 2000mm (worse than 150 items)
$uniform = @(2000) * 200
$t3 = Invoke-DuBang0 $uniform 11700 0 @()
Write-Host "Test 3 (200 items of 2000mm): Duration = $($t3.ElapsedMs)ms, Patterns = $($t3.Result.Count)"
if ($t3.ElapsedMs -le 100) {
    Write-Host "[PASS] Test 3: Pruning is O(1) instant on uniform pieces!" -ForegroundColor Green
} else {
    Write-Error "Test 3 exceeded 100ms threshold"
    exit 1
}

# Test 4: Kerf = 5mm with exact kerf-compensated subset: 5000 + 5000 + 1690 = 11690 + 2*5 = 11700
$t4 = Invoke-DuBang0 @(5000, 5000, 1690, 2000) 11700 5 @()
Write-Host "Test 4 (Kerf-Compensated Exact Subset): Produced $($t4.Result.Count) patterns in $($t4.ElapsedMs)ms."
$hasKerfExact = $false
foreach ($p in $t4.Result) {
    $cuts = $p.CutsMm
    Write-Host "  Pattern: Cuts = [$($cuts -join ', ')], Used = $($p.UsedLengthMm), Rem = $($p.RemnantLengthMm)mm"
    if ($cuts.Count -eq 3 -and [Math]::Abs($p.RemnantLengthMm) -lt 1.0) { $hasKerfExact = $true }
}
if ($hasKerfExact) {
    Write-Host "[PASS] Test 4: Kerf compensation verified!" -ForegroundColor Green
} else {
    Write-Error "Test 4 failed to discover kerf-compensated exact subset"
    exit 1
}

# Test 5: Negative / Zero pieces or empty demand
$t5 = Invoke-DuBang0 @() 11700 0 @()
Write-Host "Test 5 (Empty Demand): Produced $($t5.Result.Count) patterns without error."
if ($t5.Result.Count -eq 0) {
    Write-Host "[PASS] Test 5: Empty demand handled gracefully!" -ForegroundColor Green
} else {
    Write-Error "Test 5 non-zero patterns on empty demand"
    exit 1
}

# Test 6: Single item
$t6 = Invoke-DuBang0 @(4500) 11700 0 @()
Write-Host "Test 6 (Single Item): Produced $($t6.Result.Count) patterns."
if ($t6.Result.Count -eq 1) {
    Write-Host "[PASS] Test 6: Single item cascades directly to BFD!" -ForegroundColor Green
} else {
    Write-Error "Test 6 unexpected pattern count"
    exit 1
}

Write-Host "`nALL ADVERSARIAL STRESS TESTS COMPLETED WITH 100% PASS!" -ForegroundColor Cyan
