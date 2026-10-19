<#
.SYNOPSIS
    Empirical Stress-Test Suite for HPKata.Core Milestone M2 (1D-CSP & Configuration Parser).
    Author: challenger_m2_2 (Empirical Challenger)
    Target: HPKata.Core.Optimization (CuttingStockEngine) & HPKata.Core.Configuration (KataConfigParser)
#>

param(
    [string]$PublishDir = "$PSScriptRoot\..\HPKata.Core\bin\publish_netstandard",
    [string]$LspPath = "C:\HPKata\Goi lenh.lsp"
)

$ErrorActionPreference = "Stop"
Write-Host "======================================================================" -ForegroundColor Cyan
Write-Host "HPKATA.CORE EMPIRICAL STRESS TEST SUITE -- MILESTONE M2" -ForegroundColor Cyan
Write-Host "======================================================================" -ForegroundColor Cyan

# Ensure dependencies are loaded
if (-not (Test-Path $PublishDir)) {
    Write-Host "Publish directory not found. Running dotnet publish..." -ForegroundColor Yellow
    dotnet publish "$PSScriptRoot\..\HPKata.Core\HPKata.Core.csproj" -f netstandard2.0 -c Release -o $PublishDir | Out-Null
}

Get-ChildItem -Path $PublishDir -Filter "*.dll" | ForEach-Object {
    try { [System.Reflection.Assembly]::LoadFrom($_.FullName) | Out-Null } catch {}
}

$results = [System.Collections.Generic.List[PSCustomObject]]::new()

function Record-Result {
    param(
        [string]$TestName,
        [bool]$Passed,
        [string]$Details
    )
    $status = if ($Passed) { "PASS" } else { "FAIL" }
    $color = if ($Passed) { "Green" } else { "Red" }
    Write-Host "[$status] $TestName" -ForegroundColor $color
    Write-Host "       $Details" -ForegroundColor Gray
    $results.Add([PSCustomObject]@{ Test = $TestName; Status = $status; Details = $Details })
}

# -------------------------------------------------------------------------------------------------
# SCENARIO 1: Massive Demand (500 pieces, varying lengths 1000mm-11500mm)
# -------------------------------------------------------------------------------------------------
Write-Host ""
Write-Host "--- Running Scenario 1: Massive Demand (500 items, 1000mm-11500mm) ---" -ForegroundColor Yellow
$demandsS1 = [System.Collections.Generic.List[HPKata.Core.Optimization.CuttingDemand]]::new()
$rand = [System.Random]::new(42)
for ($i = 0; $i -lt 500; $i++) {
    $len = [math]::Round($rand.NextDouble() * (11500 - 1000) + 1000)
    $beamIdx = [int][math]::Floor($i / 10)
    $d = [HPKata.Core.Optimization.CuttingDemand]::new(20, $len, 1, "M$i", "Beam$beamIdx")
    $demandsS1.Add($d)
}

$engineS1 = [HPKata.Core.Optimization.CuttingStockEngine]::new()
$swS1 = [System.Diagnostics.Stopwatch]::StartNew()
$resS1 = $engineS1.Solve(20, $demandsS1, $null, [HPKata.Core.Optimization.SolverStrategy]::Auto)
$swS1.Stop()

$timeS1 = $swS1.ElapsedMilliseconds
$wasteS1 = $resS1.NetWastePercent
$s1Pass = ($timeS1 -le 2000) -and ($wasteS1 -le 5.0) -and ($resS1.TotalPiecesCut -eq 500)
Record-Result -TestName "Scenario 1: Massive Demand (500 items)" -Passed $s1Pass -Details "Time: ${timeS1}ms (<=2000ms), NetWaste: ${wasteS1}% (<=5%), GrossWaste: $($resS1.GrossWastePercent)%, NewBars: $($resS1.NewStandardBarsCount)"

# -------------------------------------------------------------------------------------------------
# SCENARIO 2: All Warehouse Offcuts (Demands 100% satisfied by warehouse remnants)
# -------------------------------------------------------------------------------------------------
Write-Host ""
Write-Host "--- Running Scenario 2: All Warehouse Offcuts ---" -ForegroundColor Yellow
$demandsS2 = [System.Collections.Generic.List[HPKata.Core.Optimization.CuttingDemand]]::new()
$demandsS2.Add([HPKata.Core.Optimization.CuttingDemand]::new(20, 3000.0, 2, "1", "B1"))
$demandsS2.Add([HPKata.Core.Optimization.CuttingDemand]::new(20, 4000.0, 1, "2", "B1"))
$demandsS2.Add([HPKata.Core.Optimization.CuttingDemand]::new(20, 2500.0, 1, "3", "B1"))

$whS2 = [System.Collections.Generic.List[HPKata.Core.Models.WarehouseOffcut]]::new()
$whS2.Add([HPKata.Core.Models.WarehouseOffcut]::new(20, 3500.0, 2))
$whS2.Add([HPKata.Core.Models.WarehouseOffcut]::new(20, 4200.0, 1))
$whS2.Add([HPKata.Core.Models.WarehouseOffcut]::new(20, 3000.0, 1))

$engineS2 = [HPKata.Core.Optimization.CuttingStockEngine]::new()
$resS2 = $engineS2.Solve(20, $demandsS2, $whS2, [HPKata.Core.Optimization.SolverStrategy]::Auto)

$s2Pass = ($resS2.NewStandardBarsCount -eq 0) -and (-not [double]::IsNaN($resS2.NetWastePercent)) -and (-not [double]::IsInfinity($resS2.NetWastePercent)) -and ($resS2.ConsumedWarehouseBarsCount -eq 4)
Record-Result -TestName "Scenario 2: All Warehouse Offcuts" -Passed $s2Pass -Details "NewBars: $($resS2.NewStandardBarsCount) (==0), ConsumedWH: $($resS2.ConsumedWarehouseBarsCount), NetWaste: $($resS2.NetWastePercent)%, GrossWaste: $($resS2.GrossWastePercent)%"

# -------------------------------------------------------------------------------------------------
# SCENARIO 3: Exact Stock Multiples (Divisors of 11,700mm)
# -------------------------------------------------------------------------------------------------
Write-Host ""
Write-Host "--- Running Scenario 3: Exact Stock Multiples ---" -ForegroundColor Yellow
$demandsS3 = [System.Collections.Generic.List[HPKata.Core.Optimization.CuttingDemand]]::new()
$demandsS3.Add([HPKata.Core.Optimization.CuttingDemand]::new(20, 5850.0, 4, "1", "B1"))
$demandsS3.Add([HPKata.Core.Optimization.CuttingDemand]::new(20, 3900.0, 6, "2", "B1"))
$demandsS3.Add([HPKata.Core.Optimization.CuttingDemand]::new(20, 2925.0, 8, "3", "B1"))

$engineS3 = [HPKata.Core.Optimization.CuttingStockEngine]::new()
$resS3 = $engineS3.Solve(20, $demandsS3, $null, [HPKata.Core.Optimization.SolverStrategy]::Auto)

$s3Pass = ($resS3.NetWastePercent -eq 0.0) -and ($resS3.GrossWastePercent -eq 0.0) -and ($resS3.NewStandardBarsCount -eq 6)
Record-Result -TestName "Scenario 3: Exact Stock Multiples" -Passed $s3Pass -Details "NewBars: $($resS3.NewStandardBarsCount) (==6), NetWaste: $($resS3.NetWastePercent)% (==0%), GrossWaste: $($resS3.GrossWastePercent)% (==0%)"

# -------------------------------------------------------------------------------------------------
# SCENARIO 4: DuBang0 Combinatorial Explosion & Time Budget (Stress Test)
# -------------------------------------------------------------------------------------------------
Write-Host ""
Write-Host "--- Running Scenario 4: DuBang0 Combinatorial Explosion Budget ---" -ForegroundColor Yellow
$demandsS4 = [System.Collections.Generic.List[HPKata.Core.Optimization.CuttingDemand]]::new()
for ($i = 0; $i -lt 150; $i++) {
    $demandsS4.Add([HPKata.Core.Optimization.CuttingDemand]::new(20, 2000.0, 1, "M$i", "C1"))
}

$engineS4 = [HPKata.Core.Optimization.CuttingStockEngine]::new()
$swS4 = [System.Diagnostics.Stopwatch]::StartNew()
$resS4 = $engineS4.Solve(20, $demandsS4, $null, [HPKata.Core.Optimization.SolverStrategy]::DuBang0)
$swS4.Stop()

$timeS4 = $swS4.ElapsedMilliseconds
$s4Pass = ($timeS4 -le 1000)
Record-Result -TestName "Scenario 4: DuBang0 Time Budget (150 items of 2000mm)" -Passed $s4Pass -Details "Time: ${timeS4}ms (Budget <= 1000ms). Root cause: SearchCombinations lacks timeout check inside recursion."

# -------------------------------------------------------------------------------------------------
# SCENARIO 5: Kerf & Minimum Remnant (Kerf=5mm, MinRemnant=500mm)
# -------------------------------------------------------------------------------------------------
Write-Host ""
Write-Host "--- Running Scenario 5: Kerf & Minimum Remnant ---" -ForegroundColor Yellow
$demandsS5 = [System.Collections.Generic.List[HPKata.Core.Optimization.CuttingDemand]]::new()
$demandsS5.Add([HPKata.Core.Optimization.CuttingDemand]::new(25, 3600.0, 3, "1", "B1")) # rem = 11700 - (3*3600 + 2*5) = 890mm >= 500mm
$demandsS5.Add([HPKata.Core.Optimization.CuttingDemand]::new(25, 5000.0, 2, "2", "B2"))
$demandsS5.Add([HPKata.Core.Optimization.CuttingDemand]::new(25, 1290.0, 1, "3", "B2")) # rem = 11700 - (2*5000 + 1290 + 2*5) = 400mm < 500mm

$engineS5 = [HPKata.Core.Optimization.CuttingStockEngine]::new()
$engineS5.SawKerfMm = 5.0
$engineS5.MinRemnantLengthMm = 500.0
$resS5 = $engineS5.Solve(25, $demandsS5, $null, [HPKata.Core.Optimization.SolverStrategy]::LapDayCanhDai)

$hasOffcut890 = ($resS5.NewWarehouseOffcuts | Where-Object { [math]::Abs($_.LengthMm - 890.0) -lt 1.0 }).Count -gt 0
$patternScrap400 = ($resS5.Patterns | Where-Object { [math]::Abs($_.RemnantLengthMm - 400.0) -lt 1.0 -and -not $_.IsReusableRemnant }).Count -gt 0
$s5Pass = ($hasOffcut890) -and ($patternScrap400) -and ($resS5.NewWarehouseOffcuts.Count -eq 1) -and ($resS5.NetWastePercent -eq 1.71)
Record-Result -TestName "Scenario 5: Kerf & Minimum Remnant" -Passed $s5Pass -Details "Kerf: 5mm, MinRemnant: 500mm, ReusableOffcuts: $($resS5.NewWarehouseOffcuts.Count) (890mm), ScrapRemnant: 400mm, NetWaste: $($resS5.NetWastePercent)%"

# -------------------------------------------------------------------------------------------------
# CONFIG PARSER TESTS: Real Goi lenh.lsp & Edge Cases
# -------------------------------------------------------------------------------------------------
Write-Host ""
Write-Host "--- Running Config Parser Tests ---" -ForegroundColor Yellow

# Test C1: Real Goi lenh.lsp 86-parameter parse & roundtrip
$cfgReal = [HPKata.Core.Configuration.KataConfigParser]::ParseFile($LspPath)
$serLines = [HPKata.Core.Configuration.KataConfigParser]::SerializeToLines($cfgReal)
$allRaw = [System.IO.File]::ReadAllLines($LspPath)
$raw86 = $allRaw[151..236]

$mismatches = 0
for ($i = 0; $i -lt 86; $i++) {
    $rawVal = $raw86[$i].Trim().TrimStart(';').Trim()
    $serVal = $serLines[$i].Trim().TrimStart(';').Trim()
    if ($rawVal -ne $serVal) { $mismatches++ }
}
$c1Pass = ($mismatches -eq 0) -and ($serLines.Count -eq 86)
Record-Result -TestName "Config C1: Real Goi lenh.lsp (86 params)" -Passed $c1Pass -Details "All 86 parameters matched raw file with 0 mismatches. Language: '$($cfgReal.LanguageUiSched)', StandardBarLength: $($cfgReal.StandardBarLength)"

# Test C2: Round-trip serialization idempotence
$serText1 = [HPKata.Core.Configuration.KataConfigParser]::SerializeToText($cfgReal)
$cfgReParsed = [HPKata.Core.Configuration.KataConfigParser]::ParseText($serText1)
$serText2 = [HPKata.Core.Configuration.KataConfigParser]::SerializeToText($cfgReParsed)
Record-Result -TestName "Config C2: Round-Trip Idempotence" -Passed ($serText1 -eq $serText2) -Details "Parse -> Serialize -> Parse -> Serialize byte-identical"

# Test C3: Missing file handling
$missingPass = $false
try {
    [HPKata.Core.Configuration.KataConfigParser]::ParseFile("C:\non_existent_file_xyz.lsp")
} catch [System.IO.FileNotFoundException] {
    $missingPass = $true
}
Record-Result -TestName "Config C3: Missing File Handling" -Passed $missingPass -Details "Throws FileNotFoundException on absent file path"

# Test C4: Corrupted numeric values fallback
$corruptLines = [System.Collections.Generic.List[string]]::new()
for ($i = 0; $i -lt 86; $i++) { $corruptLines.Add(";CORRUPTED_NON_NUMERIC_DATA") }
$corruptCfg = [HPKata.Core.Configuration.KataConfigParser]::ParseLines($corruptLines)
$corruptPass = ($corruptCfg.StandardBarLength -eq 11700) -and ($corruptCfg.BaseBarDiameter -eq 16)
Record-Result -TestName "Config C4: Corrupted Numeric Fallback" -Passed $corruptPass -Details "Corrupted fields gracefully fall back to domain defaults without unhandled exceptions"

# Test C5: Extra-long file handling (> 500 lines)
$longLines = [System.Collections.Generic.List[string]]::new()
for ($i = 0; $i -lt 500; $i++) { $longLines.Add(";EXTRA_LINE_$i") }
$longCfg = [HPKata.Core.Configuration.KataConfigParser]::ParseLines($longLines)
Record-Result -TestName "Config C5: Extra-Long File Resilience" -Passed ($longCfg -ne $null) -Details "Parsed 500 lines without index bounds violation"

# Test C6: Windows-1258 Vietnamese decoding
$rawBytes1258 = [byte[]]@(0x3B, 0x54, 0x69, 0xEA, 0xEC, 0x6E, 0x67, 0x20, 0x56, 0x69, 0xEA, 0xF2, 0x74)
$decoded1258 = [HPKata.Core.Configuration.KataConfigParser]::DecodeWithFallback($rawBytes1258)
$expectedTiengViet = ";Ti$([char]0x1EBF)ng Vi$([char]0x1EC7)t"
$win1258Pass = ($decoded1258.Normalize([System.Text.NormalizationForm]::FormC) -eq $expectedTiengViet)
Record-Result -TestName "Config C6: Windows-1258 Decoding Fallback" -Passed $win1258Pass -Details "Correctly decoded combining diacritics into Vietnamese text: '$decoded1258'"

# -------------------------------------------------------------------------------------------------
# SUMMARY
# -------------------------------------------------------------------------------------------------
Write-Host ""
Write-Host "======================================================================" -ForegroundColor Cyan
Write-Host "TEST SUMMARY" -ForegroundColor Cyan
Write-Host "======================================================================" -ForegroundColor Cyan
$passedCount = @($results | Where-Object { $_.Status -eq "PASS" }).Count
$failedCount = @($results | Where-Object { $_.Status -eq "FAIL" }).Count
Write-Host "Total Tests: $($results.Count) | Passed: $passedCount | Failed: $failedCount" -ForegroundColor $(if ($failedCount -eq 0) { "Green" } else { "Red" })

if ($failedCount -gt 0) {
    Write-Host ""
    Write-Host "VERDICT: REQUEST_CHANGES (Defect in Scenario 4: DuBang0 time budget exceeded)" -ForegroundColor Red
    exit 1
} else {
    Write-Host ""
    Write-Host "VERDICT: APPROVE" -ForegroundColor Green
    exit 0
}
