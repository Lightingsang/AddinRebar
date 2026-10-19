<#
.SYNOPSIS
    Adversarial Stress Test Suite for DuBang0 & 1D Cutting Stock Engine (Milestone M2 Iteration 2).
    Author: challenger_m2_2_iter2
#>

param(
    [string]$PublishDir = "$PSScriptRoot\..\HPKata.Core\bin\publish_netstandard"
)

$ErrorActionPreference = "Stop"
Write-Host "======================================================================" -ForegroundColor Magenta
Write-Host "ADVERSARIAL STRESS TEST HARNESS -- HPKATA DUBANG0 & OPTIMIZATION" -ForegroundColor Magenta
Write-Host "======================================================================" -ForegroundColor Magenta

Get-ChildItem -Path $PublishDir -Filter "*.dll" | ForEach-Object {
    try { [System.Reflection.Assembly]::LoadFrom($_.FullName) | Out-Null } catch {}
}

$results = [System.Collections.Generic.List[PSCustomObject]]::new()

function Test-Case {
    param(
        [string]$Name,
        [scriptblock]$Action,
        [scriptblock]$Predicate
    )
    Write-Host "`n--> Running: $Name" -ForegroundColor Yellow
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $res = & $Action
    $sw.Stop()
    $elapsed = $sw.ElapsedMilliseconds
    $passed = & $Predicate $res $elapsed
    $status = if ($passed) { "PASS" } else { "FAIL" }
    $color = if ($passed) { "Green" } else { "Red" }
    Write-Host "[$status] $Name | Elapsed: ${elapsed}ms" -ForegroundColor $color
    $results.Add([PSCustomObject]@{ Test = $Name; Status = $status; ElapsedMs = $elapsed })
}

$engine = [HPKata.Core.Optimization.CuttingStockEngine]::new()

# ADV-1: 200 items of 2000mm (Explicitly requested by Dispatch)
Test-Case -Name "ADV-1: 200 items of 2000mm (DuBang0)" -Action {
    $demands = [System.Collections.Generic.List[HPKata.Core.Optimization.CuttingDemand]]::new()
    for ($i = 0; $i -lt 200; $i++) {
        $demands.Add([HPKata.Core.Optimization.CuttingDemand]::new(20, 2000.0, 1, "M$i", "C1"))
    }
    $engine.Solve(20, $demands, $null, [HPKata.Core.Optimization.SolverStrategy]::DuBang0)
} -Predicate {
    param($res, $ms)
    ($ms -le 1000) -and ($res.TotalPiecesCut -eq 200) -and ($res.NewStandardBarsCount -eq 40)
}

# ADV-2: 500 items of 2000mm (Large volume identical items)
Test-Case -Name "ADV-2: 500 items of 2000mm (DuBang0)" -Action {
    $demands = [System.Collections.Generic.List[HPKata.Core.Optimization.CuttingDemand]]::new()
    for ($i = 0; $i -lt 500; $i++) {
        $demands.Add([HPKata.Core.Optimization.CuttingDemand]::new(20, 2000.0, 1, "M$i", "C1"))
    }
    $engine.Solve(20, $demands, $null, [HPKata.Core.Optimization.SolverStrategy]::DuBang0)
} -Predicate {
    param($res, $ms)
    ($ms -le 1000) -and ($res.TotalPiecesCut -eq 500) -and ($res.NewStandardBarsCount -eq 100)
}

# ADV-3: Pathological Heterogeneous Combinatorial Maze WITHOUT exact sums (150 items)
# Lengths designed around 2340mm (target 11700 for 5 items) but slightly offset: 2339, 2341, 2337, 2343
# so currentSum + rem*list[0] >= target and currentSum + rem*list[^1] <= target, forcing combination traversal
Test-Case -Name "ADV-3: Pathological Maze without Exact Sums (150 items near 2340mm)" -Action {
    $demands = [System.Collections.Generic.List[HPKata.Core.Optimization.CuttingDemand]]::new()
    $offsets = @( -3.0, -1.0, 1.0, 3.0, 5.0, 7.0 )
    for ($i = 0; $i -lt 150; $i++) {
        $off = $offsets[$i % $offsets.Length]
        $len = 2340.0 + $off
        $demands.Add([HPKata.Core.Optimization.CuttingDemand]::new(20, $len, 1, "M$i", "C1"))
    }
    $engine.Solve(20, $demands, $null, [HPKata.Core.Optimization.SolverStrategy]::DuBang0)
} -Predicate {
    param($res, $ms)
    # The solver MUST respect the 1000ms budget (+ a small clock tolerance, e.g. <= 1200ms)
    # and all 150 pieces must be cut without exception!
    ($ms -le 1200) -and ($res.TotalPiecesCut -eq 150)
}

# ADV-4: Pathological Heterogeneous Combinatorial Maze (300 items near 2340mm)
Test-Case -Name "ADV-4: Pathological Maze without Exact Sums (300 items near 2340mm)" -Action {
    $demands = [System.Collections.Generic.List[HPKata.Core.Optimization.CuttingDemand]]::new()
    $offsets = @( -5.0, -3.0, -1.0, 1.0, 3.0, 5.0 )
    for ($i = 0; $i -lt 300; $i++) {
        $off = $offsets[$i % $offsets.Length]
        $len = 2340.0 + $off
        $demands.Add([HPKata.Core.Optimization.CuttingDemand]::new(20, $len, 1, "M$i", "C1"))
    }
    $engine.Solve(20, $demands, $null, [HPKata.Core.Optimization.SolverStrategy]::DuBang0)
} -Predicate {
    param($res, $ms)
    ($ms -le 1200) -and ($res.TotalPiecesCut -eq 300)
}

# ADV-5: Dense Exact Sum Combinations (100 items of 5850mm = 50 exact bars)
Test-Case -Name "ADV-5: 100 exact halves of 5850mm (DuBang0 exact packing)" -Action {
    $demands = [System.Collections.Generic.List[HPKata.Core.Optimization.CuttingDemand]]::new()
    for ($i = 0; $i -lt 100; $i++) {
        $demands.Add([HPKata.Core.Optimization.CuttingDemand]::new(20, 5850.0, 1, "M$i", "C1"))
    }
    $engine.Solve(20, $demands, $null, [HPKata.Core.Optimization.SolverStrategy]::DuBang0)
} -Predicate {
    param($res, $ms)
    ($ms -le 1000) -and ($res.NewStandardBarsCount -eq 50) -and ($res.NetWastePercent -eq 0.0) -and ($res.TotalPiecesCut -eq 100)
}

# ADV-6: 1,000 items diverse demand under DuBang0
Test-Case -Name "ADV-6: 1,000 items diverse demand (DuBang0 with timeout & cascade)" -Action {
    $demands = [System.Collections.Generic.List[HPKata.Core.Optimization.CuttingDemand]]::new()
    $rand = [System.Random]::new(123)
    for ($i = 0; $i -lt 1000; $i++) {
        $len = [math]::Round($rand.NextDouble() * (10000 - 1500) + 1500)
        $demands.Add([HPKata.Core.Optimization.CuttingDemand]::new(20, $len, 1, "M$i", "C1"))
    }
    $engine.Solve(20, $demands, $null, [HPKata.Core.Optimization.SolverStrategy]::DuBang0)
} -Predicate {
    param($res, $ms)
    ($ms -le 1500) -and ($res.TotalPiecesCut -eq 1000)
}

# ADV-7: Warehouse offcut priority under DuBang0
Test-Case -Name "ADV-7: Warehouse offcut integration with DuBang0" -Action {
    $demands = [System.Collections.Generic.List[HPKata.Core.Optimization.CuttingDemand]]::new()
    $demands.Add([HPKata.Core.Optimization.CuttingDemand]::new(20, 5850.0, 2, "M1", "C1"))
    $demands.Add([HPKata.Core.Optimization.CuttingDemand]::new(20, 2000.0, 2, "M2", "C1"))
    
    $wh = [System.Collections.Generic.List[HPKata.Core.Models.WarehouseOffcut]]::new()
    $wh.Add([HPKata.Core.Models.WarehouseOffcut]::new(20, 5000.0, 1))
    
    $engine.Solve(20, $demands, $wh, [HPKata.Core.Optimization.SolverStrategy]::DuBang0)
} -Predicate {
    param($res, $ms)
    ($res.TotalPiecesCut -eq 4) -and ($res.ConsumedWarehouseBarsCount -ge 1)
}

Write-Host "`n======================================================================" -ForegroundColor Magenta
Write-Host "ADVERSARIAL STRESS TEST SUMMARY" -ForegroundColor Magenta
Write-Host "======================================================================" -ForegroundColor Magenta
$pass = @($results | Where-Object { $_.Status -eq "PASS" }).Count
$fail = @($results | Where-Object { $_.Status -eq "FAIL" }).Count
Write-Host "Total: $($results.Count) | Pass: $pass | Fail: $fail" -ForegroundColor $(if ($fail -eq 0) { "Green" } else { "Red" })

if ($fail -gt 0) { exit 1 } else { exit 0 }
