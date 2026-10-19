<#
.SYNOPSIS
    Adversarial Empirical Stress Test Suite for Phase 4 Subsystems:
    Stair, Lintel, Cross-Section, and ClosedXML Persistence.
    Author: challenger_m3_2 (Empirical Verification Agent)
#>

param(
    [string]$PublishDir = "$PSScriptRoot\..\HPKata.Core\bin\publish_netstandard"
)

$ErrorActionPreference = "Stop"
Write-Host "======================================================================" -ForegroundColor Cyan
Write-Host "PHASE 4 ADVERSARIAL EMPIRICAL STRESS TEST HARNESS -- MILESTONE M3" -ForegroundColor Cyan
Write-Host "======================================================================" -ForegroundColor Cyan

# Hook assembly resolve handler
$resolver = [System.ResolveEventHandler]{
    param($sender, $resolveArgs)
    if ($resolveArgs -eq $null -or [string]::IsNullOrEmpty($resolveArgs.Name)) { return $null }
    $assemblyName = [System.Reflection.AssemblyName]::new($resolveArgs.Name).Name
    $loaded = [System.AppDomain]::CurrentDomain.GetAssemblies() | Where-Object { $_.GetName().Name -eq $assemblyName } | Select-Object -First 1
    if ($loaded -ne $null) {
        return $loaded
    }
    $targetPath = Join-Path $PublishDir "$assemblyName.dll"
    if (Test-Path $targetPath) {
        return [System.Reflection.Assembly]::LoadFrom($targetPath)
    }
    return $null
}
[System.AppDomain]::CurrentDomain.add_AssemblyResolve($resolver)

$corePrereqs = @(
    "System.Buffers.dll",
    "System.Numerics.Vectors.dll",
    "System.Runtime.CompilerServices.Unsafe.dll",
    "System.Memory.dll",
    "System.IO.Packaging.dll",
    "DocumentFormat.OpenXml.Framework.dll",
    "DocumentFormat.OpenXml.dll",
    "ExcelNumberFormat.dll",
    "SixLabors.Fonts.dll",
    "ClosedXML.Parser.dll",
    "ClosedXML.dll",
    "QRCoder.dll",
    "HPKata.Core.dll"
)
foreach ($dll in $corePrereqs) {
    $p = Join-Path $PublishDir $dll
    if (Test-Path $p) {
        [System.Reflection.Assembly]::LoadFrom($p) | Out-Null
    }
}

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
# DOMAIN 1: STAIR GEOMETRY & REBAR CALCULATORS (Blondel, Scissor-Crank, Quantities, Geometry)
# =================================================================================================
Write-Host "`n--- DOMAIN 1: Stair Geometry & Rebar Calculator Stress Tests ---" -ForegroundColor Yellow

# 1.1 Blondel Ergonomic Formula Checks
# Standard formula: 2 * h_r + b_t in [580, 660]
$bVal = 0.0
$okStandard = [HPKata.Core.Stairs.Calculators.StairGeometryCalculator]::CheckErgonomics(160.0, 280.0, [ref]$bVal)
Record-Test "STAIR_BLONDEL" "Standard 160/280 comfortable stair (2*160+280=600)" ($okStandard -and $bVal -eq 600.0) "Blondel: $bVal (expected: 600.0, in [580, 660])"

# Steep riser: 220mm, flat tread: 240mm -> 2*220+240 = 680mm (> 660)
$okSteep = [HPKata.Core.Stairs.Calculators.StairGeometryCalculator]::CheckErgonomics(220.0, 240.0, [ref]$bVal)
Record-Test "STAIR_BLONDEL" "Steep riser 220mm / 240mm tread (2*220+240=680 > 660)" (-not $okSteep -and $bVal -eq 680.0) "Blondel: $bVal (expected: 680.0, rejected correctly)"

# Low riser / flat tread: 130mm, 240mm -> 2*130+240 = 500mm (< 580)
$okLow = [HPKata.Core.Stairs.Calculators.StairGeometryCalculator]::CheckErgonomics(130.0, 240.0, [ref]$bVal)
Record-Test "STAIR_BLONDEL" "Low riser 130mm / 240mm tread (2*130+240=500 < 580)" (-not $okLow -and $bVal -eq 500.0) "Blondel: $bVal (expected: 500.0, rejected correctly)"

# Lower boundary: 2*150+280 = 580.0 (exact lower bound)
$okLowerBound = [HPKata.Core.Stairs.Calculators.StairGeometryCalculator]::CheckErgonomics(150.0, 280.0, [ref]$bVal)
Record-Test "STAIR_BLONDEL" "Exact lower bound (2*150+280=580.0)" ($okLowerBound -and $bVal -eq 580.0) "Blondel: $bVal (accepted)"

# Upper boundary: 2*180+300 = 660.0 (exact upper bound)
$okUpperBound = [HPKata.Core.Stairs.Calculators.StairGeometryCalculator]::CheckErgonomics(180.0, 300.0, [ref]$bVal)
Record-Test "STAIR_BLONDEL" "Exact upper bound (2*180+300=660.0)" ($okUpperBound -and $bVal -eq 660.0) "Blondel: $bVal (accepted)"

# Boundary precision: 579.99mm (rejected) and 660.01mm (rejected)
$okBelowBound = [HPKata.Core.Stairs.Calculators.StairGeometryCalculator]::CheckErgonomics(150.0, 279.99, [ref]$bVal)
Record-Test "STAIR_BLONDEL" "Below boundary 579.99mm" (-not $okBelowBound) "Blondel: $bVal (rejected)"

$okAboveBound = [HPKata.Core.Stairs.Calculators.StairGeometryCalculator]::CheckErgonomics(180.0, 300.01, [ref]$bVal)
Record-Test "STAIR_BLONDEL" "Above boundary 660.01mm" (-not $okAboveBound) "Blondel: $bVal (rejected)"

# 1.2 Scissor-Crank Rebar Extension Across Bar Diameters 10mm to 32mm
$diameters = @(10, 12, 14, 16, 18, 20, 22, 25, 28, 32)
$scissorAllPassed = $true
$scissorDetails = @()

foreach ($d in $diameters) {
    $flight = [HPKata.Core.Stairs.Models.StairFlightModel]::new()
    $flight.FlightMark = "ST-D$d"
    $flight.BottomTensionDiaMm = $d
    $flight.RiserCount = 10
    $flight.RiserHeightMm = 160.0
    $flight.TreadWidthMm = 280.0
    $flight.FlightWidthMm = 1200.0
    $flight.TopLandingLengthMm = 1500.0
    $flight.BottomLandingLengthMm = 1500.0

    $expectedLExt = [Math]::Max(500.0, 35.0 * $d)
    $actualLExt = $flight.ScissorCrankExtensionMm

    $lextMatches = [Math]::Abs($expectedLExt - $actualLExt) -lt 0.001
    if (-not $lextMatches) {
        $scissorAllPassed = $false
        $scissorDetails += "D$($d): expected $expectedLExt, got $actualLExt"
        continue
    }

    # Verify items generated by StairRebarCalculator
    $items = [HPKata.Core.Stairs.Calculators.StairRebarCalculator]::CalculateStairRebar($flight)
    # Item 1: Flight bottom tension bar (cutFlightBot = slopeLength + ancLower + lExt)
    $it1 = $items | Where-Object { $_.BarMark -eq "1" } | Select-Object -First 1
    # Item 2: Landing bottom bar (cutLandBot = topLanding + lExt)
    $it2 = $items | Where-Object { $_.BarMark -eq "2" } | Select-Object -First 1

    $ancLower = [Math]::Max(400.0, 30.0 * $d)
    $expCut1 = [Math]::Round($flight.SlopeLengthMm + $ancLower + $actualLExt, 1)
    $expCut2 = [Math]::Round($flight.TopLandingLengthMm + $actualLExt, 1)

    $it1Ok = [Math]::Abs($it1.CuttingLengthMm - $expCut1) -le 0.1
    $it2Ok = [Math]::Abs($it2.CuttingLengthMm - $expCut2) -le 0.1

    if (-not ($it1Ok -and $it2Ok)) {
        $scissorAllPassed = $false
        $scissorDetails += "D$($d): It1 exp $expCut1 got $($it1.CuttingLengthMm); It2 exp $expCut2 got $($it2.CuttingLengthMm)"
    } else {
        $governor = if ($actualLExt -eq 500.0) { "500mm-bound" } else { "35d-bound" }
        $scissorDetails += "D$($d): Lext=$actualLExt ($governor), Cut1=$($it1.CuttingLengthMm), Cut2=$($it2.CuttingLengthMm)"
    }
}

Record-Test "STAIR_SCISSOR" "Scissor-Crank extension across D10..D32 (L_ext >= max(35d, 500mm))" $scissorAllPassed ($scissorDetails -join "; ")

# 1.3 Extreme Stair Flight Dimensions & Validation
$flightExtreme = [HPKata.Core.Stairs.Models.StairFlightModel]::new()
$flightExtreme.RiserCount = 2
$flightExtreme.RiserHeightMm = 150.0
$flightExtreme.TreadWidthMm = 300.0
$flightExtreme.ThroatThicknessMm = 80.0
$flightExtreme.FlightWidthMm = 900.0
$volSmall = [HPKata.Core.Stairs.Calculators.StairGeometryCalculator]::CalculateConcreteVolume($flightExtreme)
$fwSmall = [HPKata.Core.Stairs.Calculators.StairGeometryCalculator]::CalculateFormworkArea($flightExtreme)
Record-Test "STAIR_GEOMETRY" "Minimal flight (2 risers, 80mm throat): positive volume & formwork" ($volSmall -gt 0 -and $fwSmall -gt 0) "Vol: $($volSmall.ToString('F3')) m3, FW: $($fwSmall.ToString('F2')) m2"

$profile = [HPKata.Core.Stairs.Calculators.StairGeometryCalculator]::GenerateSectionProfile($flightExtreme)
$profileValid = $profile.Count -ge 8 -and $profile[0].Item1 -eq 0.0 -and $profile[0].Item2 -eq 0.0
Record-Test "STAIR_GEOMETRY" "Section profile polygon generation: valid closed loop vertices" $profileValid "Vertices: $($profile.Count), Start: ($($profile[0].Item1), $($profile[0].Item2))"

# =================================================================================================
# DOMAIN 2: LINTEL REBAR CALCULATOR (Bearing length, Span sensitivity, Stirrup distribution)
# =================================================================================================
Write-Host "`n--- DOMAIN 2: Lintel Rebar Calculator Stress Tests ---" -ForegroundColor Yellow

# 2.1 Bearing Length Rule Verification: L_bearing >= max(200, h/2) vs Model Validation
$lintelH400 = [HPKata.Core.Stairs.Models.LintelModel]::new()
$lintelH400.ClearSpanMm = 1200.0
$lintelH400.HeightMm = 400.0
$lintelH400.BearingLengthMm = 200.0 # h/2 = 200mm, max(200, 200) = 200mm -> satisfied
$itemsH400 = [HPKata.Core.Stairs.Calculators.LintelRebarCalculator]::CalculateLintelRebar($lintelH400)
Record-Test "LINTEL_BEARING" "Lintel h=400mm with L_bearing=200mm (L_bearing == h/2)" ($itemsH400.Count -eq 3) "Items: $($itemsH400.Count), TotalLength: $($lintelH400.TotalLengthMm)mm"

$lintelH600 = [HPKata.Core.Stairs.Models.LintelModel]::new()
$lintelH600.ClearSpanMm = 1500.0
$lintelH600.HeightMm = 600.0
$lintelH600.BearingLengthMm = 300.0 # h/2 = 300mm, max(200, 300) = 300mm -> satisfied
$itemsH600 = [HPKata.Core.Stairs.Calculators.LintelRebarCalculator]::CalculateLintelRebar($lintelH600)
Record-Test "LINTEL_BEARING" "Deep lintel h=600mm with L_bearing=300mm (L_bearing == h/2)" ($itemsH600.Count -eq 3) "Items: $($itemsH600.Count), TotalLength: $($lintelH600.TotalLengthMm)mm"

# Bearing limit lower validation check: < 150mm throws
$caughtInvalidBearing = $false
try {
    $badBearing = [HPKata.Core.Stairs.Models.LintelModel]::new()
    $badBearing.BearingLengthMm = 120.0
    $badBearing.Validate()
} catch [System.ArgumentException] {
    $caughtInvalidBearing = $true
}
Record-Test "LINTEL_BEARING" "Lintel with sub-150mm bearing correctly throws ArgumentException" $caughtInvalidBearing "Threw ArgumentException as expected"

# 2.2 Short Span (600mm) vs Long Span (3000mm) Stirrup & Rebar Cut Tests
$lintelShort = [HPKata.Core.Stairs.Models.LintelModel]::new()
$lintelShort.ClearSpanMm = 600.0
$lintelShort.BearingLengthMm = 250.0
$lintelShort.HeightMm = 150.0
$lintelShort.WidthMm = 220.0
$lintelShort.StirrupSpacingEndMm = 100.0
$lintelShort.StirrupSpacingMidMm = 150.0
$itemsShort = [HPKata.Core.Stairs.Calculators.LintelRebarCalculator]::CalculateLintelRebar($lintelShort)
$tieShort = $itemsShort | Where-Object { $_.BarMark -eq "3" } | Select-Object -First 1

# EndZone = 250 + 600/4 = 400mm. countEnd = floor(400/100)+1 = 5.
# MidZone = 600/2 = 300mm. countMid = floor(300/150)+1 = 3.
# TotalStirrups = 2 * 5 + 3 = 13.
$expShortStirrups = 13
Record-Test "LINTEL_SPANS" "Short span (600mm): Stirrups count = 13" ($tieShort.CountPerMember -eq $expShortStirrups) "Count: $($tieShort.CountPerMember) (expected: $expShortStirrups)"

$lintelLong = [HPKata.Core.Stairs.Models.LintelModel]::new()
$lintelLong.ClearSpanMm = 3000.0
$lintelLong.BearingLengthMm = 300.0
$lintelLong.HeightMm = 300.0
$lintelLong.WidthMm = 220.0
$lintelLong.StirrupSpacingEndMm = 100.0
$lintelLong.StirrupSpacingMidMm = 150.0
$itemsLong = [HPKata.Core.Stairs.Calculators.LintelRebarCalculator]::CalculateLintelRebar($lintelLong)
$tieLong = $itemsLong | Where-Object { $_.BarMark -eq "3" } | Select-Object -First 1

# EndZone = 300 + 3000/4 = 1050mm. countEnd = floor(1050/100)+1 = 11.
# MidZone = 3000/2 = 1500mm. countMid = floor(1500/150)+1 = 11.
# TotalStirrups = 2 * 11 + 11 = 33.
$expLongStirrups = 33
Record-Test "LINTEL_SPANS" "Long span (3000mm): Stirrups count = 33" ($tieLong.CountPerMember -eq $expLongStirrups) "Count: $($tieLong.CountPerMember) (expected: $expLongStirrups)"

# Check hook lengths and net length for long lintel
# TotalLength = 3000 + 2*300 = 3600mm. Cover = 20mm. lNet = 3600 - 40 = 3560mm.
# hookH = max(50, 300 - 40) = 260mm.
# Top cut (d=10): 3560 + 2*260 - 4*10 = 4040mm.
$topLong = $itemsLong | Where-Object { $_.BarMark -eq "1" } | Select-Object -First 1
Record-Test "LINTEL_SPANS" "Long span top hanger bar cut length = 4040mm" ($topLong.CuttingLengthMm -eq 4040.0) "CutLength: $($topLong.CuttingLengthMm)mm (expected: 4040.0mm)"

# =================================================================================================
# DOMAIN 3: GENERIC SECTION CALCULATOR (vemc - geometry, coordinates, hoops, callouts)
# =================================================================================================
Write-Host "`n--- DOMAIN 3: Generic Section Calculator (vemc) Stress Tests ---" -ForegroundColor Yellow

# 3.1 Asymmetric Bar Count: 3 Top Bars (dia 18) and 5 Bottom Bars (dia 20)
$sec = [HPKata.Core.Stairs.Calculators.GenericSectionCalculator]::CreateRectangularSection(
    "D-ASYM", 350.0, 700.0, 30.0, 3, 18, 5, 20, 8
)
Record-Test "GENERIC_SECTION" "Asymmetric beam section 350x700: created successfully" ($sec -ne $null) "Mark: $($sec.ComponentMark), Title: $($sec.SectionTitle)"
Record-Test "GENERIC_SECTION" "Total longitudinal bars = 3 + 5 = 8" ($sec.LongitudinalBars.Count -eq 8) "Count: $($sec.LongitudinalBars.Count)"

# Top bars: minX = 30 + 8 + 9 = 47mm, maxX = 350 - 47 = 303mm. step = (303-47)/2 = 128mm.
# Bar 1: X=47, Bar 2: X=175, Bar 3: X=303. Y = 700 - 47 = 653mm.
$top1 = $sec.LongitudinalBars[0]
$top2 = $sec.LongitudinalBars[1]
$top3 = $sec.LongitudinalBars[2]
$topCoordsOk = [Math]::Abs($top1.X - 47.0) -lt 0.01 -and [Math]::Abs($top2.X - 175.0) -lt 0.01 -and [Math]::Abs($top3.X - 303.0) -lt 0.01 -and [Math]::Abs($top1.Y - 653.0) -lt 0.01
Record-Test "GENERIC_SECTION" "Top bar coordinates arithmetic (X: 47, 175, 303; Y: 653)" $topCoordsOk "T1=($($top1.X),$($top1.Y)), T2=($($top2.X),$($top2.Y)), T3=($($top3.X),$($top3.Y))"

# Bottom bars: 5 bars, step = (303-47)/4 = 64mm.
# Bar 1: 47, Bar 2: 111, Bar 3: 175, Bar 4: 239, Bar 5: 303. Y = 30 + 8 + 10 = 48mm.
$bot1 = $sec.LongitudinalBars[3]
$bot3 = $sec.LongitudinalBars[5]
$bot5 = $sec.LongitudinalBars[7]
$botCoordsOk = [Math]::Abs($bot1.X - 47.0) -lt 0.01 -and [Math]::Abs($bot3.X - 175.0) -lt 0.01 -and [Math]::Abs($bot5.X - 303.0) -lt 0.01 -and [Math]::Abs($bot1.Y - 48.0) -lt 0.01
Record-Test "GENERIC_SECTION" "Bottom bar coordinates arithmetic (X: 47..303 step 64; Y: 48)" $botCoordsOk "B1=($($bot1.X),$($bot1.Y)), B3=($($bot3.X),$($bot3.Y)), B5=($($bot5.X),$($bot5.Y))"

# Stirrup hoop: 4 vertices: (30,30), (320,30), (320,670), (30,670)
$hoop = $sec.StirrupHoops[0]
$hoopOk = $hoop.Vertices.Count -eq 4 -and $hoop.Vertices[0].Item1 -eq 30.0 -and $hoop.Vertices[1].Item1 -eq 320.0 -and $hoop.Vertices[2].Item2 -eq 670.0
Record-Test "GENERIC_SECTION" "Stirrup hoop vertices boundary check (Cover 30mm)" $hoopOk "Hoop vertices: $($hoop.Vertices.Count), Box: [30, 30] to [320, 670]"

# 3.2 Single Bar Centering Check (N=1)
$secSingle = [HPKata.Core.Stairs.Calculators.GenericSectionCalculator]::CreateRectangularSection(
    "COL-1B", 250.0, 250.0, 25.0, 1, 16, 1, 16, 6
)
$sTop = $secSingle.LongitudinalBars[0]
$sBot = $secSingle.LongitudinalBars[1]
$singleCentered = ($sTop.X -eq 125.0) -and ($sBot.X -eq 125.0)
Record-Test "GENERIC_SECTION" "Single bar centering (width 250mm -> X = 125mm)" $singleCentered "TopX: $($sTop.X), BotX: $($sBot.X)"

# 3.3 Rebar Schedule Extraction from Generic Section
$secItems = [HPKata.Core.Stairs.Calculators.GenericSectionCalculator]::SectionToRebarItems($sec, 4000.0, 3)
Record-Test "GENERIC_SECTION" "SectionToRebarItems converts 8 bars + hoop to RebarItem list" ($secItems.Count -eq 9) "Items count: $($secItems.Count)"
$totWeight = 0.0
foreach ($it in $secItems) { $totWeight += $it.TotalWeightKg }
Record-Test "GENERIC_SECTION" "Total steel weight is positive ($($totWeight.ToString('F1')) kg)" ($totWeight -gt 0) "Total weight: $($totWeight.ToString('F1')) kg"

# =================================================================================================
# DOMAIN 4: CLOSEDXML WORKBOOK GENERATORS (Sheets 'Mong', 'San', 'Thang')
# =================================================================================================
Write-Host "`n--- DOMAIN 4: ClosedXML Workbook Generators Persistence Tests ---" -ForegroundColor Yellow

# 4.1 FoundationPlanScheduleWorkbookGenerator (Sheet 'Mong')
$foundLayouts = [System.Collections.Generic.List[HPKata.Core.Foundations.Models.FoundationRebarLayout]]::new()

# Footing 1: Isolated Footing
$f1 = [HPKata.Core.Foundations.Models.IsolatedFootingModel]::new()
$f1.FootingMark = "MD1"
$f1.LengthMm = 1500.0
$f1.WidthMm = 1500.0
$f1.TotalHeightMm = 600.0
$f1.BottomMeshXDiaMm = 12
$f1.BottomMeshYDiaMm = 12
$f1.MemberCount = 4
$lay1 = [HPKata.Core.Foundations.Models.FoundationRebarLayout]::new()
$lay1.FoundationMark = "MD1"
$lay1.IsPileCap = $false
$lay1.Footing = $f1
$foundLayouts.Add($lay1)

# Footing 2: Pile Cap
$pc = [HPKata.Core.Foundations.Models.PileCapModel]::new()
$pc.CapMark = "MC1"
$pc.LengthMm = 2000.0
$pc.WidthMm = 1200.0
$pc.TotalHeightMm = 800.0
$pc.BottomMeshXDiaMm = 14
$pc.BottomMeshYDiaMm = 14
$pc.MemberCount = 2
$pc.Piles.Add([HPKata.Core.Foundations.Models.PileCoordinate]::new(1, -300.0, 0.0, 400.0, $false))
$pc.Piles.Add([HPKata.Core.Foundations.Models.PileCoordinate]::new(2, 300.0, 0.0, 400.0, $false))
$lay2 = [HPKata.Core.Foundations.Models.FoundationRebarLayout]::new()
$lay2.FoundationMark = "MC1"
$lay2.IsPileCap = $true
$lay2.PileCap = $pc
$foundLayouts.Add($lay2)

$msMong = [System.IO.MemoryStream]::new()
[HPKata.Core.Foundations.Persistence.FoundationPlanScheduleWorkbookGenerator]::GenerateWorkbookStream($foundLayouts, $msMong)
$mongBytes = $msMong.ToArray()
$mongIsZip = ($mongBytes.Length -ge 4) -and ($mongBytes[0] -eq 0x50) -and ($mongBytes[1] -eq 0x4B)
Record-Test "CLOSEDXML_MONG" "GenerateWorkbookStream produces valid OpenXML ZIP binary (PK header)" $mongIsZip "Size: $($mongBytes.Length) bytes, Magic: 0x$($mongBytes[0].ToString('X2'))$($mongBytes[1].ToString('X2'))"

$msMong.Position = 0
$wbMong = [ClosedXML.Excel.XLWorkbook]::new($msMong)
$wsMong = $wbMong.Worksheet("Mong")
$mongSheetOk = ($wsMong -ne $null) -and ($wsMong.Name -eq "Mong")
Record-Test "CLOSEDXML_MONG" "Sheet 'Mong' exists and reloaded cleanly" $mongSheetOk "Sheet name: '$($wsMong.Name)'"

# Validate Headers
$expMongHeaders = [HPKata.Core.Foundations.Persistence.FoundationPlanScheduleWorkbookGenerator]::ColumnHeaders
$headersMatch = $true
for ($c = 0; $c -lt $expMongHeaders.Length; $c++) {
    $val = $wsMong.Cell(2, $c + 2).GetString()
    if ($val -ne $expMongHeaders[$c]) {
        $headersMatch = $false
        break
    }
}
Record-Test "CLOSEDXML_MONG" "All $($expMongHeaders.Length) column headers match specification" $headersMatch "Headers validated"

# Validate Data Rows: MD1 (row 3) & MC1 (row 4)
$md1Name = $wsMong.Cell(3, 3).GetString()
$md1Vol = $wsMong.Cell(3, 16).GetDouble()
$md1Weight = $wsMong.Cell(3, 19).GetDouble()
Record-Test "CLOSEDXML_MONG" "Row 3: MD1 (Móng Đơn) non-zero volume & rebar weight" ($md1Name -eq "MD1" -and $md1Vol -gt 0 -and $md1Weight -gt 0) "Mark: $md1Name, Vol: $md1Vol m3, Weight: $md1Weight kg"

$mc1Name = $wsMong.Cell(4, 3).GetString()
$mc1Piles = $wsMong.Cell(4, 9).GetDouble()
Record-Test "CLOSEDXML_MONG" "Row 4: MC1 (Đài Cọc) pile count = 2" ($mc1Name -eq "MC1" -and $mc1Piles -eq 2) "Mark: $mc1Name, Piles: $mc1Piles"

# 4.2 SlabPlanScheduleWorkbookGenerator (Sheet 'San')
$slabLayouts = [System.Collections.Generic.List[HPKata.Core.Slabs.Models.SlabRebarLayout]]::new()
$panel = [HPKata.Core.Slabs.Models.SlabPanelModel]::new()
$panel.PanelMark = "S1"
$panel.SpanXMm = 4000.0
$panel.SpanYMm = 5000.0
$panel.ThicknessMm = 120.0
$panel.BottomMeshXDiaMm = 10
$panel.BottomMeshYDiaMm = 10
$panel.TopSupportDiaMm = 10
$panel.MemberCount = 3
$sLay = [HPKata.Core.Slabs.Models.SlabRebarLayout]::new()
$sLay.Panel = $panel
$slabLayouts.Add($sLay)

$msSan = [System.IO.MemoryStream]::new()
[HPKata.Core.Slabs.Persistence.SlabPlanScheduleWorkbookGenerator]::GenerateWorkbookStream($slabLayouts, $msSan)
$sanBytes = $msSan.ToArray()
$sanIsZip = ($sanBytes.Length -ge 4) -and ($sanBytes[0] -eq 0x50) -and ($sanBytes[1] -eq 0x4B)
Record-Test "CLOSEDXML_SAN" "GenerateWorkbookStream produces valid OpenXML ZIP binary (PK header)" $sanIsZip "Size: $($sanBytes.Length) bytes"

$msSan.Position = 0
$wbSan = [ClosedXML.Excel.XLWorkbook]::new($msSan)
$wsSan = $wbSan.Worksheet("San")
$sanSheetOk = ($wsSan -ne $null) -and ($wsSan.Name -eq "San")
Record-Test "CLOSEDXML_SAN" "Sheet 'San' exists and reloaded cleanly" $sanSheetOk "Sheet name: '$($wsSan.Name)'"

$s1Name = $wsSan.Cell(3, 3).GetString()
$s1Vol = $wsSan.Cell(3, 12).GetDouble()
$s1Weight = $wsSan.Cell(3, 14).GetDouble()
Record-Test "CLOSEDXML_SAN" "Row 3: S1 non-zero concrete volume ($s1Vol m3) & weight ($s1Weight kg)" ($s1Name -eq "S1" -and $s1Vol -gt 0 -and $s1Weight -gt 0) "Mark: $s1Name, Vol: $s1Vol m3, Weight: $s1Weight kg"

# 4.3 StairPlanScheduleWorkbookGenerator (Sheet 'Thang')
$stairModels = [System.Collections.Generic.List[HPKata.Core.Stairs.Models.StairReinforcementModel]]::new()
$stFlight = [HPKata.Core.Stairs.Models.StairFlightModel]::new()
$stFlight.FlightMark = "TH1"
$stFlight.RiserCount = 11
$stFlight.MemberCount = 2
$stReinf = [HPKata.Core.Stairs.Models.StairReinforcementModel]::new()
$stReinf.Flight = $stFlight
$stairModels.Add($stReinf)

$msThang = [System.IO.MemoryStream]::new()
[HPKata.Core.Stairs.Persistence.StairPlanScheduleWorkbookGenerator]::GenerateWorkbookStream($stairModels, $msThang)
$thangBytes = $msThang.ToArray()
$thangIsZip = ($thangBytes.Length -ge 4) -and ($thangBytes[0] -eq 0x50) -and ($thangBytes[1] -eq 0x4B)
Record-Test "CLOSEDXML_THANG" "GenerateWorkbookStream produces valid OpenXML ZIP binary (PK header)" $thangIsZip "Size: $($thangBytes.Length) bytes"

$msThang.Position = 0
$wbThang = [ClosedXML.Excel.XLWorkbook]::new($msThang)
$wsThang = $wbThang.Worksheet("Thang")
$thangSheetOk = ($wsThang -ne $null) -and ($wsThang.Name -eq "Thang")
Record-Test "CLOSEDXML_THANG" "Sheet 'Thang' exists and reloaded cleanly" $thangSheetOk "Sheet name: '$($wsThang.Name)'"

$th1Name = $wsThang.Cell(3, 3).GetString()
$th1Vol = $wsThang.Cell(3, 12).GetDouble()
$th1Weight = $wsThang.Cell(3, 14).GetDouble()
Record-Test "CLOSEDXML_THANG" "Row 3: TH1 non-zero concrete volume ($th1Vol m3) & weight ($th1Weight kg)" ($th1Name -eq "TH1" -and $th1Vol -gt 0 -and $th1Weight -gt 0) "Mark: $th1Name, Vol: $th1Vol m3, Weight: $th1Weight kg"

# 4.4 File on Disk Generation Test
$tempFilePath = Join-Path ([System.IO.Path]::GetTempPath()) "Kata_Thang_Test_$([System.Guid]::NewGuid()).xlsx"
try {
    [HPKata.Core.Stairs.Persistence.StairPlanScheduleWorkbookGenerator]::GenerateWorkbookFile($stairModels, $tempFilePath)
    $fileExists = Test-Path $tempFilePath
    $fileLen = if ($fileExists) { (Get-Item $tempFilePath).Length } else { 0 }
    Record-Test "CLOSEDXML_DISK" "GenerateWorkbookFile writes genuine .xlsx file to disk" ($fileExists -and $fileLen -gt 1000) "File: $tempFilePath (Size: $fileLen bytes)"
} finally {
    if (Test-Path $tempFilePath) {
        Remove-Item -Force $tempFilePath -ErrorAction SilentlyContinue
    }
}

# =================================================================================================
# SUMMARY
# =================================================================================================
Write-Host "`n======================================================================" -ForegroundColor Cyan
Write-Host "PHASE 4 ADVERSARIAL STRESS TEST SUMMARY" -ForegroundColor Cyan
Write-Host "======================================================================" -ForegroundColor Cyan

$passedCount = ($results | Where-Object { $_.Passed }).Count
$failedCount = ($results | Where-Object { -not $_.Passed }).Count
$totalCount = $results.Count

$summaryColor = if ($failedCount -eq 0) { "Green" } else { "Red" }
Write-Host "Total Tests: $totalCount | Passed: $passedCount | Failed: $failedCount" -ForegroundColor $summaryColor

if ($failedCount -eq 0) {
    Write-Host "VERDICT: CONFIRMED_CORRECT" -ForegroundColor Green
    exit 0
} else {
    Write-Host "VERDICT: DEFECT_FOUND" -ForegroundColor Red
    exit 1
}
