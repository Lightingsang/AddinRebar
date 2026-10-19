# empirical_challenge_m2.ps1
# Adversarial Empirical Verification Suite for HPKata.Core Shapes and Calculators

param(
    [string]$DllPath = "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.Core\bin\Release\netstandard2.0\HPKata.Core.dll",
    [string]$JsonReportPath = "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m2_1\empirical_results.json"
)

$ErrorActionPreference = "Continue"

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host " HPKata.Core Milestone M2 Empirical Stress-Test & Challenge Suite" -ForegroundColor Cyan
Write-Host " Target DLL: $DllPath" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan

if (-not (Test-Path $DllPath)) {
    Write-Error "Target DLL not found at: $DllPath"
    exit 1
}

# 1. Load Assembly
$assembly = [System.Reflection.Assembly]::LoadFrom($DllPath)
Write-Host "Loaded Assembly: $($assembly.FullName)" -ForegroundColor Green

$results = [ordered]@{
    Timestamp = (Get-Date).ToString("o")
    DllPath = $DllPath
    AssemblyFullName = $assembly.FullName
    RebarCalculator = [ordered]@{}
    ShapeCatalog = [ordered]@{}
    Summary = [ordered]@{}
}

$totalTests = 0
$totalPassed = 0
$totalFailed = 0
$totalCrashed = 0

function Record-Test($category, $testName, $expected, $actual, $passed, $details = "") {
    $script:totalTests++
    if ($passed) {
        $script:totalPassed++
    } else {
        $script:totalFailed++
        Write-Host "[-] FAIL: [$category] $testName | Expected: $expected | Actual: $actual | $details" -ForegroundColor Red
    }
}

# =========================================================================
# SECTION 1: REBAR CALCULATOR VERIFICATION
# =========================================================================
Write-Host "`n>>> [SECTION 1] Testing RebarCalculator..." -ForegroundColor Yellow

$calcTests = [ordered]@{}

# 1.1 Hook 90 Lengths (10d >= 75)
$hook90Results = @()
$testDiameters90 = @(0, -1, 4, 6, 7, 8, 10, 12, 14, 16, 18, 20, 22, 25, 28, 32, 36, 40)
foreach ($d in $testDiameters90) {
    $val = [HPKata.Core.Calculators.RebarCalculator]::Hook90Length($d)
    $expected = if ($d -le 0) { 0.0 } else { [Math]::Max(10.0 * $d, 75.0) }
    $pass = [Math]::Abs($val - $expected) -lt 1e-6
    Record-Test "Hook90" "d=$d" $expected $val $pass
    $hook90Results += [ordered]@{ Diameter = $d; Actual = $val; Expected = $expected; Pass = $pass }
}
$calcTests["Hook90"] = $hook90Results

# 1.2 Hook 135 Lengths (7.5d >= 75)
$hook135Results = @()
$testDiameters135 = @(0, -1, 4, 6, 8, 10, 12, 14, 16, 18, 20, 25, 32)
foreach ($d in $testDiameters135) {
    $val = [HPKata.Core.Calculators.RebarCalculator]::Hook135Length($d)
    $expected = if ($d -le 0) { 0.0 } else { [Math]::Max(7.5 * $d, 75.0) }
    $pass = [Math]::Abs($val - $expected) -lt 1e-6
    Record-Test "Hook135" "d=$d" $expected $val $pass
    $hook135Results += [ordered]@{ Diameter = $d; Actual = $val; Expected = $expected; Pass = $pass }
}
$calcTests["Hook135"] = $hook135Results

# 1.3 Hook 180 Lengths (9d >= 60)
$hook180Results = @()
$testDiameters180 = @(0, -1, 4, 6, 8, 10, 12, 16, 20, 25)
foreach ($d in $testDiameters180) {
    $val = [HPKata.Core.Calculators.RebarCalculator]::Hook180Length($d)
    $expected = if ($d -le 0) { 0.0 } else { [Math]::Max(9.0 * $d, 60.0) }
    $pass = [Math]::Abs($val - $expected) -lt 1e-6
    Record-Test "Hook180" "d=$d" $expected $val $pass
    $hook180Results += [ordered]@{ Diameter = $d; Actual = $val; Expected = $expected; Pass = $pass }
}
$calcTests["Hook180"] = $hook180Results

# 1.4 Unit Masses (Table & Formula W = phi^2 / 162.2)
$unitMassResults = @()
$tcvnTable = [System.Collections.Generic.Dictionary[int, double]]::new()
$tcvnTable[6] = 0.222; $tcvnTable[8] = 0.395; $tcvnTable[10] = 0.617; $tcvnTable[12] = 0.888; $tcvnTable[14] = 1.210
$tcvnTable[16] = 1.580; $tcvnTable[18] = 2.000; $tcvnTable[20] = 2.470; $tcvnTable[22] = 2.980; $tcvnTable[25] = 3.850
$tcvnTable[28] = 4.830; $tcvnTable[32] = 6.310; $tcvnTable[36] = 7.990; $tcvnTable[38] = 8.900; $tcvnTable[40] = 9.860
$tcvnTable[41] = 10.360; $tcvnTable[43] = 11.400; $tcvnTable[50] = 15.420; $tcvnTable[51] = 16.040

foreach ($d in $tcvnTable.Keys) {
    $tableVal = $tcvnTable[$d]
    $actual = [HPKata.Core.Calculators.RebarCalculator]::GetUnitMass($d)
    $formulaVal = ($d * $d) / 162.2
    $tableMatch = [Math]::Abs($actual - $tableVal) -lt 1e-4
    $formulaDeltaPct = [Math]::Abs($tableVal - $formulaVal) / $tableVal * 100.0
    
    # Verify matches table value exactly, and formula within 0.5%
    $pass = $tableMatch -and ($formulaDeltaPct -le 0.5)
    Record-Test "UnitMass_TCVN" "d=$d" $tableVal $actual $pass "Formula: $($formulaVal.ToString('F4')) (Delta: $($formulaDeltaPct.ToString('F2'))%)"
    $unitMassResults += [ordered]@{
        Diameter = $d
        Actual = $actual
        TableValue = $tableVal
        FormulaValue = [Math]::Round($formulaVal, 4)
        DeltaPct = [Math]::Round($formulaDeltaPct, 3)
        Pass = $pass
    }
}

# Unlisted diameters (fallback formula)
$unlisted = @(7, 9, 15, 30)
foreach ($d in $unlisted) {
    $actual = [HPKata.Core.Calculators.RebarCalculator]::GetUnitMass($d)
    $expected = ($d * $d) / 162.2
    $pass = [Math]::Abs($actual - $expected) -lt 1e-4
    Record-Test "UnitMass_Unlisted" "d=$d" $expected $actual $pass
    $unitMassResults += [ordered]@{
        Diameter = $d
        Actual = $actual
        TableValue = "N/A"
        FormulaValue = [Math]::Round($expected, 4)
        DeltaPct = 0.0
        Pass = $pass
    }
}
$calcTests["UnitMasses"] = $unitMassResults

# 1.5 Bend Deductions
$bendResults = @()
foreach ($d in @(6, 8, 10, 12, 16, 20, 25, 32)) {
    # 90 degrees
    $rInner90 = if ($d -le 16) { 2.0 * $d } else { 3.5 * $d }
    $expected90 = 2.0 * ($rInner90 + $d) - 0.5 * [Math]::PI * ($rInner90 + 0.5 * $d)
    $actual90 = [HPKata.Core.Calculators.RebarCalculator]::BendDeduction($d, 90.0, $true)
    $pass90 = [Math]::Abs($actual90 - $expected90) -lt 1e-4
    Record-Test "BendDeduction90" "d=$d" $expected90 $actual90 $pass90

    # 135 degrees
    $expected135 = 1.5 * $d
    $actual135 = [HPKata.Core.Calculators.RebarCalculator]::BendDeduction($d, 135.0, $true)
    $pass135 = [Math]::Abs($actual135 - $expected135) -lt 1e-4
    Record-Test "BendDeduction135" "d=$d" $expected135 $actual135 $pass135

    # Deduction disabled
    $actualDisabled = [HPKata.Core.Calculators.RebarCalculator]::BendDeduction($d, 90.0, $false)
    $passDis = $actualDisabled -eq 0.0
    Record-Test "BendDeductionDisabled" "d=$d" 0.0 $actualDisabled $passDis

    $bendResults += [ordered]@{
        Diameter = $d
        Deduction90 = [Math]::Round($actual90, 3)
        Expected90 = [Math]::Round($expected90, 3)
        Deduction135 = [Math]::Round($actual135, 3)
        Expected135 = [Math]::Round($expected135, 3)
        Disabled = $actualDisabled
    }
}
$calcTests["BendDeductions"] = $bendResults

# 1.6 Lap Splice, Tension Anchorage, RoundLength
$lapActual = [HPKata.Core.Calculators.RebarCalculator]::LapSpliceLength(20, 30.0)
Record-Test "LapSplice" "d=20,mult=30" 600.0 $lapActual ($lapActual -eq 600.0)

$ancActual = [HPKata.Core.Calculators.RebarCalculator]::TensionAnchorageLength(20, 40.0)
Record-Test "Anchorage" "d=20,mult=40" 800.0 $ancActual ($ancActual -eq 800.0)

$roundActual = [HPKata.Core.Calculators.RebarCalculator]::RoundLength(1234.2, 10.0)
Record-Test "RoundLength" "val=1234.2,step=10" 1240.0 $roundActual ($roundActual -eq 1240.0)

$results["RebarCalculator"] = $calcTests

# =========================================================================
# SECTION 2: 85 REBAR SHAPES STRESS-TESTING ACROSS 4 REGIMES
# =========================================================================
Write-Host "`n>>> [SECTION 2] Stress-testing 85 shapes across 4 dimension regimes..." -ForegroundColor Yellow

$allShapeValues = [System.Enum]::GetValues([HPKata.Core.Shapes.RebarShapeType])
Write-Host "Total Shapes in Enum: $($allShapeValues.Length)" -ForegroundColor Cyan
Record-Test "Catalog" "EnumCount" 85 $allShapeValues.Length ($allShapeValues.Length -eq 85)

# Define Regimes
$regimes = [ordered]@{
    "RegimeA_Nominal" = @{
        Params = [double[]]@(500.0, 1000.0, 400.0, 300.0, 200.0, 150.0)
        Angle = 45.0
        Diameter = 16
        Description = "Standard nominal dimensions (500-1000mm, Phi16, 45deg)"
    }
    "RegimeB_Minimum" = @{
        Params = [double[]]@(10.0, 10.0, 10.0, 10.0, 10.0, 10.0)
        Angle = 45.0
        Diameter = 6
        Description = "Minimum boundary dimensions (10mm, Phi6, 45deg)"
    }
    "RegimeC_LargeCivil" = @{
        Params = [double[]]@(11700.0, 11700.0, 11700.0, 11700.0, 11700.0, 11700.0)
        Angle = 60.0
        Diameter = 32
        Description = "Large civil structural dimensions (11,700mm, Phi32, 60deg)"
    }
}

$shapeRegimeMatrix = [ordered]@{}

foreach ($shape in $allShapeValues) {
    $shapeName = $shape.ToString()
    $shapeDef = [HPKata.Core.Shapes.RebarShapeCatalog]::GetDefinition($shape)
    $shapeEntry = [ordered]@{
        Code = $shapeDef.ShapeCode
        Name = $shapeDef.Name
        RevitEquivalent = $shapeDef.RevitShapeEquivalent
        Regimes = [ordered]@{}
    }

    foreach ($regKey in $regimes.Keys) {
        $reg = $regimes[$regKey]
        
        # Test ded = false
        $crashed = $false
        $errorMsg = ""
        $lenOutToOut = 0.0
        try {
            $lenOutToOut = [HPKata.Core.Shapes.RebarShapeCatalog]::CalculateCuttingLength(
                $shape, $reg.Params, $reg.Angle, $reg.Diameter, $false
            )
        } catch {
            $crashed = $true
            $errorMsg = $_.Exception.Message
            $script:totalCrashed++
        }

        # Test ded = true
        $lenDed = 0.0
        try {
            $lenDed = [HPKata.Core.Shapes.RebarShapeCatalog]::CalculateCuttingLength(
                $shape, $reg.Params, $reg.Angle, $reg.Diameter, $true
            )
        } catch {
            $crashed = $true
            $errorMsg = $_.Exception.Message
            $script:totalCrashed++
        }

        # Assertions: No crash, not NaN, not Infinity, non-negative
        $isNan = [double]::IsNaN($lenOutToOut) -or [double]::IsNaN($lenDed)
        $isInf = [double]::IsInfinity($lenOutToOut) -or [double]::IsInfinity($lenDed)
        $isNonNegative = ($lenOutToOut -ge 0.0) -and ($lenDed -ge 0.0)
        $pass = (-not $crashed) -and (-not $isNan) -and (-not $isInf) -and $isNonNegative

        Record-Test "Shapes_$regKey" "$shapeName" "Valid, non-negative, non-NaN" "L_out=$([Math]::Round($lenOutToOut, 2)), L_ded=$([Math]::Round($lenDed, 2))" $pass $errorMsg

        $shapeEntry.Regimes[$regKey] = [ordered]@{
            CutLengthOutToOut = [Math]::Round($lenOutToOut, 3)
            CutLengthDeduction = [Math]::Round($lenDed, 3)
            Crashed = $crashed
            IsNaN = $isNan
            IsInfinity = $isInf
            IsNonNegative = $isNonNegative
            Pass = $pass
        }
    }

    $shapeRegimeMatrix[$shapeName] = $shapeEntry
}

# =========================================================================
# SECTION 3: REGIME D — DEGENERATE & CORNER ANGLES / SAFE-SIN
# =========================================================================
Write-Host "`n>>> [SECTION 3] Regime D: Degenerate angles & SafeSin verification..." -ForegroundColor Yellow

$testAngles = @(0.0, 1e-6, 1e-5, 5e-5, 1e-4, 0.00572957, 0.1, 15.0, 30.0, 45.0, 90.0, 135.0, 180.0, 270.0, 360.0, -30.0, -90.0)
$safeSinTests = @()

foreach ($ang in $testAngles) {
    $sinVal = [HPKata.Core.Shapes.RebarShapeCatalog]::SafeSin($ang)
    $isNan = [double]::IsNaN($sinVal)
    $isInf = [double]::IsInfinity($sinVal)
    
    # If Math.Abs(Math.Sin(rad)) < 1e-4, SafeSin must be exactly 0.0
    $rad = $ang * [Math]::PI / 180.0
    $rawSin = [Math]::Sin($rad)
    $expectedZero = [Math]::Abs($rawSin) -lt 1e-4
    $zeroCorrect = if ($expectedZero) { $sinVal -eq 0.0 } else { [Math]::Abs($sinVal - $rawSin) -lt 1e-9 }
    
    $pass = (-not $isNan) -and (-not $isInf) -and $zeroCorrect
    Record-Test "SafeSin" "angle=$ang" "Safe clamped sin" "SafeSin=$sinVal (RawSin=$rawSin)" $pass
    $safeSinTests += [ordered]@{
        Angle = $ang
        RawSin = $rawSin
        SafeSin = $sinVal
        ExpectedZero = $expectedZero
        Pass = $pass
    }
}

# Specifically test dang06 and dang49 on all degenerate angles
$angularShapes = @([HPKata.Core.Shapes.RebarShapeType]::Dang06, [HPKata.Core.Shapes.RebarShapeType]::Dang49)
$angularShapeResults = @()

foreach ($sh in $angularShapes) {
    foreach ($ang in $testAngles) {
        $p = [double[]]@(500.0, 1000.0, 400.0)
        $crashed = $false
        $val = 0.0
        try {
            $val = [HPKata.Core.Shapes.RebarShapeCatalog]::CalculateCuttingLength($sh, $p, $ang, 16, $false)
        } catch {
            $crashed = $true
            $script:totalCrashed++
        }

        $isNan = [double]::IsNaN($val)
        $isInf = [double]::IsInfinity($val)
        $isNonNegative = $val -ge 0.0
        
        # When |sin(alpha)| < 1e-4, mid segment falls back to L2 = 1000, so L_cut = 500 + 1000 + 400 = 1900
        $rad = $ang * [Math]::PI / 180.0
        $rawSin = [Math]::Sin($rad)
        $expectedMid = if ([Math]::Abs($rawSin) -lt 1e-4) { 1000.0 } else { 1000.0 / [Math]::Abs($rawSin) }
        $expectedTotal = 500.0 + $expectedMid + 400.0
        $delta = [Math]::Abs($val - $expectedTotal)
        $mathMatch = $delta -lt 1e-3

        $pass = (-not $crashed) -and (-not $isNan) -and (-not $isInf) -and $isNonNegative -and $mathMatch
        Record-Test "DegenerateAngle_$sh" "angle=$ang" $expectedTotal $val $pass

        $angularShapeResults += [ordered]@{
            Shape = $sh.ToString()
            Angle = $ang
            CutLength = [Math]::Round($val, 3)
            ExpectedLength = [Math]::Round($expectedTotal, 3)
            Crashed = $crashed
            IsNaN = $isNan
            IsInfinity = $isInf
            Pass = $pass
        }
    }
}

# =========================================================================
# SECTION 4: REGIME E — EXTREME BOUNDARY & DEFENSIVE ROBUSTNESS
# =========================================================================
Write-Host "`n>>> [SECTION 4] Regime E: Empty, zero, and boundary robustness..." -ForegroundColor Yellow

$robustnessTests = @()
foreach ($shape in $allShapeValues) {
    # 4.1 Empty parameters array
    $crashedEmpty = $false
    $valEmpty = 0.0
    try {
        $valEmpty = [HPKata.Core.Shapes.RebarShapeCatalog]::CalculateCuttingLength($shape, [double[]]@(), 0.0, 0, $false)
    } catch {
        $crashedEmpty = $true
        $script:totalCrashed++
    }
    $passEmpty = (-not $crashedEmpty) -and (-not [double]::IsNaN($valEmpty)) -and ($valEmpty -ge 0.0)
    Record-Test "RegimeE_EmptyParams" "$shape" "Non-crash >= 0" $valEmpty $passEmpty

    # 4.2 All zeros
    $crashedZeros = $false
    $valZeros = 0.0
    try {
        $valZeros = [HPKata.Core.Shapes.RebarShapeCatalog]::CalculateCuttingLength(
            $shape, [double[]]@(0.0, 0.0, 0.0, 0.0, 0.0, 0.0), 0.0, 0, $false
        )
    } catch {
        $crashedZeros = $true
        $script:totalCrashed++
    }
    $passZeros = (-not $crashedZeros) -and (-not [double]::IsNaN($valZeros)) -and ($valZeros -ge 0.0)
    Record-Test "RegimeE_ZeroParams" "$shape" "Non-crash >= 0" $valZeros $passZeros

    # 4.3 Null parameters array
    $crashedNull = $false
    $valNull = 0.0
    try {
        $valNull = [HPKata.Core.Shapes.RebarShapeCatalog]::CalculateCuttingLength($shape, $null, 0.0, 0, $false)
    } catch {
        $crashedNull = $true
        $script:totalCrashed++
    }
    $passNull = (-not $crashedNull) -and (-not [double]::IsNaN($valNull)) -and ($valNull -ge 0.0)
    Record-Test "RegimeE_NullParams" "$shape" "Non-crash >= 0" $valNull $passNull

    $robustnessTests += [ordered]@{
        Shape = $shape.ToString()
        EmptyLength = $valEmpty
        ZerosLength = $valZeros
        NullLength = $valNull
        Pass = $passEmpty -and $passZeros -and $passNull
    }
}

# =========================================================================
# SECTION 5: SHAPE CODE PARSING & LOOKUP
# =========================================================================
Write-Host "`n>>> [SECTION 5] Shape code normalization & lookup..." -ForegroundColor Yellow

$lookupTests = @()
$shapeOut1 = [HPKata.Core.Shapes.RebarShapeType]::Dang01
$shapeOut2 = [HPKata.Core.Shapes.RebarShapeType]::Dang01
$shapeOut3 = [HPKata.Core.Shapes.RebarShapeType]::Dang01
$dummy1 = [HPKata.Core.Shapes.RebarShapeType]::Dang01
$dummy2 = [HPKata.Core.Shapes.RebarShapeType]::Dang01

foreach ($shape in $allShapeValues) {
    $def = [HPKata.Core.Shapes.RebarShapeCatalog]::GetDefinition($shape)
    $code = $def.ShapeCode

    # Test raw code
    $pass1 = [HPKata.Core.Shapes.RebarShapeCatalog]::TryParseShapeCode($code, [ref]$shapeOut1) -and ($shapeOut1 -eq $shape)
    Record-Test "Lookup_Raw" "$code" "$shape" "$shapeOut1" $pass1

    # Test "dang" + code
    $pass2 = [HPKata.Core.Shapes.RebarShapeCatalog]::TryParseShapeCode("dang$code", [ref]$shapeOut2) -and ($shapeOut2 -eq $shape)
    Record-Test "Lookup_Dang" "dang$code" "$shape" "$shapeOut2" $pass2

    # Test "shape" + code
    $pass3 = [HPKata.Core.Shapes.RebarShapeCatalog]::TryParseShapeCode("shape$code", [ref]$shapeOut3) -and ($shapeOut3 -eq $shape)
    Record-Test "Lookup_Shape" "shape$code" "$shape" "$shapeOut3" $pass3

    $lookupTests += [ordered]@{
        Shape = $shape.ToString()
        Code = $code
        RawMatch = $pass1
        DangMatch = $pass2
        ShapeMatch = $pass3
    }
}

# Invalid codes fallback
$passInv1 = (-not [HPKata.Core.Shapes.RebarShapeCatalog]::TryParseShapeCode("invalid_code", [ref]$dummy1))
$passInv2 = (-not [HPKata.Core.Shapes.RebarShapeCatalog]::TryParseShapeCode($null, [ref]$dummy2))
Record-Test "Lookup_Invalid" "invalid_code" "false" $passInv1 $passInv1
Record-Test "Lookup_Null" "null" "false" $passInv2 $passInv2

# =========================================================================
# FINAL COMPILATION & SUMMARY
# =========================================================================
$results["ShapeCatalog"]["Shapes"] = $shapeRegimeMatrix
$results["ShapeCatalog"]["SafeSin"] = $safeSinTests
$results["ShapeCatalog"]["DegenerateAngularShapes"] = $angularShapeResults
$results["ShapeCatalog"]["Robustness"] = $robustnessTests
$results["ShapeCatalog"]["CodeLookup"] = $lookupTests

$results["Summary"] = [ordered]@{
    TotalTestsExecuted = $totalTests
    Passed = $totalPassed
    Failed = $totalFailed
    Crashed = $totalCrashed
    SuccessRatePct = [Math]::Round(($totalPassed / $totalTests) * 100.0, 2)
    Verdict = if ($totalFailed -eq 0 -and $totalCrashed -eq 0) { "APPROVE" } else { "REQUEST_CHANGES" }
}

Write-Host "`n=================================================================" -ForegroundColor Cyan
Write-Host " EMPIRICAL CHALLENGE SUITE SUMMARY" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host " Total Tests Executed : $totalTests" -ForegroundColor White
Write-Host " Passed               : $totalPassed" -ForegroundColor Green
Write-Host " Failed               : $totalFailed" -ForegroundColor $(if ($totalFailed -eq 0) { "Green" } else { "Red" })
Write-Host " Crashed / Exceptions : $totalCrashed" -ForegroundColor $(if ($totalCrashed -eq 0) { "Green" } else { "Red" })
Write-Host " Success Rate         : $($results['Summary']['SuccessRatePct'])%" -ForegroundColor Green
Write-Host " FINAL VERDICT        : $($results['Summary']['Verdict'])" -ForegroundColor $(if ($results['Summary']['Verdict'] -eq "APPROVE") { "Green" } else { "Red" })
Write-Host "=================================================================" -ForegroundColor Cyan

# Save JSON output
$jsonDir = [System.IO.Path]::GetDirectoryName($JsonReportPath)
if (-not (Test-Path $jsonDir)) { New-Item -ItemType Directory -Path $jsonDir -Force | Out-Null }
$results | ConvertTo-Json -Depth 6 | Set-Content -Path $JsonReportPath -Encoding UTF8
Write-Host "Full results serialized to: $JsonReportPath" -ForegroundColor Cyan
