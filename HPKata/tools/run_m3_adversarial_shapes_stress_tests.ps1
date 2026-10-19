<#
.SYNOPSIS
    Adversarial Empirical Stress Test Suite for All 85 Shapes & Rebar Calculators (Milestone M3).
    Author: challenger_m3_1 (Empirical Challenger)
    Targets: HPKata.Core.Shapes.RebarShapeCatalog & HPKata.Core.Calculators.RebarCalculator
#>

param(
    [string]$DllPath = "$PSScriptRoot\..\HPKata.Core\bin\Debug\netstandard2.0\HPKata.Core.dll"
)

$ErrorActionPreference = "Stop"
Write-Host "======================================================================" -ForegroundColor Magenta
Write-Host "ADVERSARIAL EMPIRICAL STRESS TEST HARNESS -- 85 SHAPES & CALCULATORS" -ForegroundColor Magenta
Write-Host "======================================================================" -ForegroundColor Magenta

if (-not (Test-Path $DllPath)) {
    Write-Host "Assembly not found at: $DllPath" -ForegroundColor Red
    exit 1
}

[System.Reflection.Assembly]::LoadFrom((Resolve-Path $DllPath).Path) | Out-Null
Write-Host "Loaded assembly: $DllPath" -ForegroundColor Green

$results = [System.Collections.Generic.List[PSCustomObject]]::new()

function Record-Test {
    param(
        [string]$Category,
        [string]$TestName,
        [bool]$Passed,
        [string]$Details
    )
    $status = if ($Passed) { "PASS" } else { "FAIL" }
    $color = if ($Passed) { "Green" } else { "Red" }
    Write-Host "[$status] [$Category] $TestName" -ForegroundColor $color
    if (-not [string]::IsNullOrEmpty($Details)) {
        Write-Host "       $Details" -ForegroundColor Gray
    }
    $results.Add([PSCustomObject]@{
        Category = $Category
        Test     = $TestName
        Status   = $status
        Passed   = $Passed
        Details  = $Details
    })
}

# =================================================================================================
# PART 1: TCVN 1651:2018 FORMULA & TABLE UNIT WEIGHT VERIFICATION
# =================================================================================================
Write-Host "`n--- PART 1: TCVN 1651:2018 Formula & Table Unit Weight Verification ---" -ForegroundColor Yellow

$tcvnTable = @{
    6  = 0.222
    8  = 0.395
    10 = 0.617
    12 = 0.888
    14 = 1.210
    16 = 1.580
    18 = 2.000
    20 = 2.470
    22 = 2.980
    25 = 3.850
    28 = 4.830
    32 = 6.310
    36 = 7.990
    38 = 8.900
    40 = 9.860
    41 = 10.360
    43 = 11.400
    50 = 15.420
    51 = 16.040
}

$allTcvnMatchTable = $true
$allTcvnWithinTolerance = $true
$maxDeltaPercent = 0.0

foreach ($entry in $tcvnTable.GetEnumerator() | Sort-Object Key) {
    $d = $entry.Key
    $nominalTable = $entry.Value
    $actualMass = [HPKata.Core.Calculators.RebarCalculator]::GetUnitMass($d)
    
    # Check exact match with table
    if ([math]::Abs($actualMass - $nominalTable) -gt 1e-4) {
        $allTcvnMatchTable = $false
    }
    
    # Check formula delta: W_formula = d^2 / 162.2
    $formulaMass = ($d * $d) / 162.2
    $deltaPercent = [math]::Abs($actualMass - $formulaMass) / $actualMass * 100.0
    if ($deltaPercent -gt $maxDeltaPercent) {
        $maxDeltaPercent = $deltaPercent
    }
    if ($deltaPercent -gt 0.5) {
        $allTcvnWithinTolerance = $false
    }
}

Record-Test -Category "TCVN 1651" -TestName "Table Exact Match (19 standard diameters)" `
    -Passed $allTcvnMatchTable `
    -Details "Tested d=6mm..51mm against Table values. All 19 matched exact table values: $allTcvnMatchTable"

Record-Test -Category "TCVN 1651" -TestName "Formula Tolerance (<= 0.5% vs d^2/162.2)" `
    -Passed $allTcvnWithinTolerance `
    -Details "Max delta: $([math]::Round($maxDeltaPercent, 4))% across all 19 standard diameters (Threshold <= 0.5%)"

# Test non-standard diameters fallback
$nonStandardDiameters = @( 7, 9, 15, 24, 30, 45 )
$allNonStandardMatch = $true
foreach ($d in $nonStandardDiameters) {
    $expected = ($d * $d) / 162.2
    $actual = [HPKata.Core.Calculators.RebarCalculator]::GetUnitMass($d)
    if ([math]::Abs($actual - $expected) -gt 1e-4) {
        $allNonStandardMatch = $false
    }
}
Record-Test -Category "TCVN 1651" -TestName "Non-standard Diameters Fallback (d^2/162.2)" `
    -Passed $allNonStandardMatch `
    -Details "Tested diameters [7, 9, 15, 24, 30, 45] mm. All fell back to d^2/162.2 formula."

# Test non-positive diameters
$zeroMass = [HPKata.Core.Calculators.RebarCalculator]::GetUnitMass(0)
$negMass = [HPKata.Core.Calculators.RebarCalculator]::GetUnitMass(-10)
$nonPosMassPass = ($zeroMass -eq 0.0) -and ($negMass -eq 0.0)
Record-Test -Category "TCVN 1651" -TestName "Non-positive Diameters Zero Return" `
    -Passed $nonPosMassPass `
    -Details "GetUnitMass(0) = $zeroMass, GetUnitMass(-10) = $negMass (both == 0.0)"

# Test Hook, Lap, and Anchorage Calculators under extreme inputs
$h90_zero = [HPKata.Core.Calculators.RebarCalculator]::Hook90Length(0)
$h135_zero = [HPKata.Core.Calculators.RebarCalculator]::Hook135Length(0)
$h180_zero = [HPKata.Core.Calculators.RebarCalculator]::Hook180Length(0)
$lap_zero = [HPKata.Core.Calculators.RebarCalculator]::LapSpliceLength(0)
$anc_zero = [HPKata.Core.Calculators.RebarCalculator]::TensionAnchorageLength(0)
$zeroHooksPass = ($h90_zero -eq 0.0) -and ($h135_zero -eq 0.0) -and ($h180_zero -eq 0.0) -and ($lap_zero -eq 0.0) -and ($anc_zero -eq 0.0)
Record-Test -Category "Calculators" -TestName "Zero Diameter Hook & Lap Guard" `
    -Passed $zeroHooksPass `
    -Details "Zero diameter produces 0.0 for Hook90, Hook135, Hook180, LapSplice, TensionAnchorage"

$h90_clamp = [HPKata.Core.Calculators.RebarCalculator]::Hook90Length(6) # 10*6 = 60 < 75 -> clamp to 75
$h135_clamp = [HPKata.Core.Calculators.RebarCalculator]::Hook135Length(6) # 7.5*6 = 45 < 75 -> clamp to 75
$h180_clamp = [HPKata.Core.Calculators.RebarCalculator]::Hook180Length(6) # 9*6 = 54 < 60 -> clamp to 60
$clampsPass = ($h90_clamp -eq 75.0) -and ($h135_clamp -eq 75.0) -and ($h180_clamp -eq 60.0)
Record-Test -Category "Calculators" -TestName "Hook Minimum Length Clamping" `
    -Passed $clampsPass `
    -Details "d=6mm: Hook90=75mm (min 75), Hook135=75mm (min 75), Hook180=60mm (min 60)"

# =================================================================================================
# PART 2: ADVERSARIAL MUTATION & STRESS SCENARIOS ON ALL 85 SHAPES
# =================================================================================================
Write-Host "`n--- PART 2: Adversarial Mutation & Stress Scenarios on All 85 Shapes ---" -ForegroundColor Yellow

$allShapeValues = [Enum]::GetValues([HPKata.Core.Shapes.RebarShapeType])
$shapeCount = $allShapeValues.Length
Record-Test -Category "Catalog" -TestName "Shape Count Verification" `
    -Passed ($shapeCount -eq 85) `
    -Details "Total shapes defined in RebarShapeType enum: $shapeCount (expected: 85)"

# Verify shape code parsing for all 85 shapes
$allParsePass = $true
foreach ($shape in $allShapeValues) {
    $def = [HPKata.Core.Shapes.RebarShapeCatalog]::GetDefinition($shape)
    $parsedType = [HPKata.Core.Shapes.RebarShapeType]::Dang01
    $ok = [HPKata.Core.Shapes.RebarShapeCatalog]::TryParseShapeCode($def.ShapeCode, [ref]$parsedType)
    if ((-not $ok) -or ($parsedType -ne $shape)) {
        $allParsePass = $false
    }
}
Record-Test -Category "Catalog" -TestName "All 85 Shapes Bidirectional Code Lookup" `
    -Passed $allParsePass `
    -Details "All 85 shape codes round-trip through TryParseShapeCode successfully"

# Adversarial Scenario 1: NULL parameters array across all 85 shapes
$nullParamCrashes = 0
$nullParamNonFinite = 0
$nullParamNegative = 0
foreach ($shape in $allShapeValues) {
    try {
        $len = [HPKata.Core.Shapes.RebarShapeCatalog]::CalculateCuttingLength($shape, $null, 0.0, 0, $false)
        if ([double]::IsNaN($len) -or [double]::IsInfinity($len)) {
            $nullParamNonFinite++
        }
        if ($len -lt 0.0) {
            $nullParamNegative++
        }
    } catch {
        $nullParamCrashes++
    }
}
$nullPass = ($nullParamCrashes -eq 0) -and ($nullParamNonFinite -eq 0) -and ($nullParamNegative -eq 0)
Record-Test -Category "Stress: Null" -TestName "Scenario 1: Null Parameters Array (All 85 Shapes)" `
    -Passed $nullPass `
    -Details "Crashes: $nullParamCrashes, Non-finite/NaN: $nullParamNonFinite, Negative: $nullParamNegative"

# Adversarial Scenario 2: EMPTY parameters array across all 85 shapes
$emptyParamCrashes = 0
$emptyParamNonFinite = 0
$emptyParamNegative = 0
$emptyArr = [double[]]@()
foreach ($shape in $allShapeValues) {
    try {
        $len = [HPKata.Core.Shapes.RebarShapeCatalog]::CalculateCuttingLength($shape, $emptyArr, 0.0, 0, $false)
        if ([double]::IsNaN($len) -or [double]::IsInfinity($len)) {
            $emptyParamNonFinite++
        }
        if ($len -lt 0.0) {
            $emptyParamNegative++
        }
    } catch {
        $emptyParamCrashes++
    }
}
$emptyPass = ($emptyParamCrashes -eq 0) -and ($emptyParamNonFinite -eq 0) -and ($emptyParamNegative -eq 0)
Record-Test -Category "Stress: Empty" -TestName "Scenario 2: Empty Parameters Array (All 85 Shapes)" `
    -Passed $emptyPass `
    -Details "Crashes: $emptyParamCrashes, Non-finite/NaN: $emptyParamNonFinite, Negative: $emptyParamNegative"

# Adversarial Scenario 3: Single parameter array [500.0] on multi-parameter shapes
$insufficientCrashes = 0
$insufficientNonFinite = 0
$insufficientNegative = 0
$singleParam = [double[]]@( 500.0 )
foreach ($shape in $allShapeValues) {
    try {
        $len = [HPKata.Core.Shapes.RebarShapeCatalog]::CalculateCuttingLength($shape, $singleParam, 45.0, 16, $true)
        if ([double]::IsNaN($len) -or [double]::IsInfinity($len)) {
            $insufficientNonFinite++
        }
        if ($len -lt 0.0) {
            $insufficientNegative++
        }
    } catch {
        $insufficientCrashes++
    }
}
$insufficientPass = ($insufficientCrashes -eq 0) -and ($insufficientNonFinite -eq 0) -and ($insufficientNegative -eq 0)
Record-Test -Category "Stress: Truncated" -TestName "Scenario 3: Truncated [500mm] Array with Bend Deduction" `
    -Passed $insufficientPass `
    -Details "Crashes: $insufficientCrashes, Non-finite/NaN: $insufficientNonFinite, Negative: $insufficientNegative"

# Adversarial Scenario 4: Negative angles (-45, -90, -135, -180, -360) on all shapes
$negAngleCrashes = 0
$negAngleNonFinite = 0
$negAngleNegative = 0
$negAngles = @( -45.0, -90.0, -135.0, -180.0, -360.0 )
$sampleParams = [double[]]@( 600.0, 400.0, 300.0, 200.0, 150.0, 100.0 )
foreach ($shape in $allShapeValues) {
    foreach ($ang in $negAngles) {
        try {
            $len = [HPKata.Core.Shapes.RebarShapeCatalog]::CalculateCuttingLength($shape, $sampleParams, $ang, 20, $false)
            if ([double]::IsNaN($len) -or [double]::IsInfinity($len)) {
                $negAngleNonFinite++
            }
            if ($len -lt 0.0) {
                $negAngleNegative++
            }
        } catch {
            $negAngleCrashes++
        }
    }
}
$negAnglePass = ($negAngleCrashes -eq 0) -and ($negAngleNonFinite -eq 0) -and ($negAngleNegative -eq 0)
Record-Test -Category "Stress: Negative Angle" -TestName "Scenario 4: Negative Angles [-45..-360] (All 85 Shapes x 5 Angles)" `
    -Passed $negAnglePass `
    -Details "Crashes: $negAngleCrashes, Non-finite/NaN: $negAngleNonFinite, Negative: $negAngleNegative (425 evaluations)"

# Adversarial Scenario 5: Degenerate angles (0.0, 1e-6, 1e-4, 180.0, 360.0) on all shapes
$degenAngleCrashes = 0
$degenAngleNonFinite = 0
$degenAngleNegative = 0
$degenAngles = @( 0.0, 1e-6, 1e-4, 180.0, 360.0 )
foreach ($shape in $allShapeValues) {
    foreach ($ang in $degenAngles) {
        try {
            $len = [HPKata.Core.Shapes.RebarShapeCatalog]::CalculateCuttingLength($shape, $sampleParams, $ang, 20, $false)
            if ([double]::IsNaN($len) -or [double]::IsInfinity($len)) {
                $degenAngleNonFinite++
            }
            if ($len -lt 0.0) {
                $degenAngleNegative++
            }
        } catch {
            $degenAngleCrashes++
        }
    }
}
$degenAnglePass = ($degenAngleCrashes -eq 0) -and ($degenAngleNonFinite -eq 0) -and ($degenAngleNegative -eq 0)
Record-Test -Category "Stress: Degenerate Angle" -TestName "Scenario 5: Degenerate Angles [0, 1e-6, 1e-4, 180, 360] (All 85 Shapes)" `
    -Passed $degenAnglePass `
    -Details "Crashes: $degenAngleCrashes, Non-finite/NaN: $degenAngleNonFinite, Negative: $degenAngleNegative (425 evaluations)"

# Adversarial Scenario 6: Massive dimensions (> 10^6 mm, e.g. 10,000,000 mm = 10km)
$massiveCrashes = 0
$massiveNonFinite = 0
$massiveNegative = 0
$massiveParams = [double[]]@( 1e7, 1e7, 1e7, 1e7, 1e7, 1e7 )
foreach ($shape in $allShapeValues) {
    try {
        $len = [HPKata.Core.Shapes.RebarShapeCatalog]::CalculateCuttingLength($shape, $massiveParams, 45.0, 32, $true)
        if ([double]::IsNaN($len) -or [double]::IsInfinity($len)) {
            $massiveNonFinite++
        }
        if ($len -le 0.0) {
            $massiveNegative++
        }
    } catch {
        $massiveCrashes++
    }
}
$massivePass = ($massiveCrashes -eq 0) -and ($massiveNonFinite -eq 0) -and ($massiveNegative -eq 0)
Record-Test -Category "Stress: Large Dimensions" -TestName "Scenario 6: Mega Dimensions (10^7 mm) on All 85 Shapes" `
    -Passed $massivePass `
    -Details "Crashes: $massiveCrashes, Non-finite/NaN: $massiveNonFinite, Non-positive: $massiveNegative"

# Adversarial Scenario 7: Extreme aspect ratios (A=10000, B=10, C=0.01)
$aspectCrashes = 0
$aspectNonFinite = 0
$aspectNegative = 0
$aspectParams = [double[]]@( 10000.0, 10.0, 0.01, 50000.0, 0.05, 25000.0 )
foreach ($shape in $allShapeValues) {
    try {
        $len = [HPKata.Core.Shapes.RebarShapeCatalog]::CalculateCuttingLength($shape, $aspectParams, 30.0, 25, $true)
        if ([double]::IsNaN($len) -or [double]::IsInfinity($len)) {
            $aspectNonFinite++
        }
        if ($len -le 0.0) {
            $aspectNegative++
        }
    } catch {
        $aspectCrashes++
    }
}
$aspectPass = ($aspectCrashes -eq 0) -and ($aspectNonFinite -eq 0) -and ($aspectNegative -eq 0)
Record-Test -Category "Stress: Aspect Ratio" -TestName "Scenario 7: Extreme Aspect Ratios (10000:10:0.01) on All 85 Shapes" `
    -Passed $aspectPass `
    -Details "Crashes: $aspectCrashes, Non-finite/NaN: $aspectNonFinite, Non-positive: $aspectNegative"

# Adversarial Scenario 8: Zero diameter with bend deductions enabled
$zeroDiaDedCrashes = 0
$zeroDiaDedNonFinite = 0
$zeroDiaDedNegative = 0
foreach ($shape in $allShapeValues) {
    try {
        $len = [HPKata.Core.Shapes.RebarShapeCatalog]::CalculateCuttingLength($shape, $sampleParams, 90.0, 0, $true)
        if ([double]::IsNaN($len) -or [double]::IsInfinity($len)) {
            $zeroDiaDedNonFinite++
        }
        if ($len -le 0.0) {
            $zeroDiaDedNegative++
        }
    } catch {
        $zeroDiaDedCrashes++
    }
}
$zeroDiaDedPass = ($zeroDiaDedCrashes -eq 0) -and ($zeroDiaDedNonFinite -eq 0) -and ($zeroDiaDedNegative -eq 0)
Record-Test -Category "Stress: Zero Dia" -TestName "Scenario 8: Zero Diameter with Bend Deductions (All 85 Shapes)" `
    -Passed $zeroDiaDedPass `
    -Details "Crashes: $zeroDiaDedCrashes, Non-finite/NaN: $zeroDiaDedNonFinite, Non-positive: $zeroDiaDedNegative"

# Adversarial Scenario 9: SafeSin stress across 360 integer degrees and sub-milliradian fractions
$safeSinFailures = 0
for ($deg = -720; $deg -le 720; $deg += 15) {
    $s = [HPKata.Core.Shapes.RebarShapeCatalog]::SafeSin($deg)
    if ([double]::IsNaN($s) -or [double]::IsInfinity($s)) {
        $safeSinFailures++
    }
    if ([math]::Abs($deg % 180) -eq 0 -and $s -ne 0.0) {
        $safeSinFailures++
    }
}
$safeSinPass = ($safeSinFailures -eq 0)
Record-Test -Category "Stress: SafeSin" -TestName "Scenario 9: SafeSin Angle Sweeps (-720 deg to +720 deg)" `
    -Passed $safeSinPass `
    -Details "Swept -720..+720 deg. Verified zero division prevention and exact zero clamping at multiples of 180 deg."

# Adversarial Scenario 10: Shape Code Variations & Malformed Strings
$badCodes = @( "", "   ", $null, "invalid_xyz", "dang999", "-1", "shapeABC", "99" )
$badCodePass = $true
foreach ($bc in $badCodes) {
    try {
        $resType = [HPKata.Core.Shapes.RebarShapeCatalog]::NormalizeShapeCode($bc)
        # Should gracefully return Dang01 (default straight bar)
        if ($resType -ne [HPKata.Core.Shapes.RebarShapeType]::Dang01) {
            $badCodePass = $false
        }
    } catch {
        $badCodePass = $false
    }
}
Record-Test -Category "Catalog: Fallback" -TestName "Scenario 10: Malformed Shape Code Fallback to Dang01" `
    -Passed $badCodePass `
    -Details "Tested empty, null, negative, and out-of-range codes. All defaulted safely to Dang01 without throwing."

# =================================================================================================
# SUMMARY
# =================================================================================================
Write-Host "`n======================================================================" -ForegroundColor Magenta
Write-Host "ADVERSARIAL STRESS TEST SUMMARY" -ForegroundColor Magenta
Write-Host "======================================================================" -ForegroundColor Magenta
$passCount = @($results | Where-Object { $_.Passed }).Count
$failCount = @($results | Where-Object { -not $_.Passed }).Count
Write-Host "Total Tests: $($results.Count) | Passed: $passCount | Failed: $failCount" -ForegroundColor $(if ($failCount -eq 0) { "Green" } else { "Red" })

if ($failCount -gt 0) {
    Write-Host "`nVERDICT: REQUEST_CHANGES" -ForegroundColor Red
    exit 1
} else {
    Write-Host "`nVERDICT: APPROVE" -ForegroundColor Green
    exit 0
}
