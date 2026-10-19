# Empirical Verification Harness for HPKata.Core
# Tests all functional modules directly against the compiled DLL

$ErrorActionPreference = "Stop"
$dllPath = "G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.Core\bin\Release\netstandard2.0\HPKata.Core.dll"

Write-Host "================================================================================"
Write-Host "EMPIRICAL VERIFICATION HARNESS FOR HPKATA.CORE"
Write-Host "Target Assembly: $dllPath"
Write-Host "================================================================================"

$asm = [Reflection.Assembly]::LoadFrom($dllPath)
Write-Host "Successfully loaded assembly: $($asm.FullName)`n"

$passCount = 0
$failCount = 0

function Assert-Test([string]$name, [bool]$condition, [string]$details = "") {
    if ($condition) {
        Write-Host "  [PASS] $name" -ForegroundColor Green
        $script:passCount++
    } else {
        Write-Host "  [FAIL] $name - $details" -ForegroundColor Red
        $script:failCount++
    }
}

# ---------------------------------------------------------------------------------
# 1. RebarCalculator Tests
# ---------------------------------------------------------------------------------
Write-Host "--- 1. RebarCalculator Tests ---"
$calcType = $asm.GetType("HPKata.Core.Calculators.RebarCalculator")

$m6 = $calcType.GetMethod("GetUnitMass").Invoke($null, @(6))
Assert-Test "GetUnitMass(6) == 0.222 kg/m" ([Math]::Abs($m6 - 0.222) -lt 1e-4) "Actual: $m6"

$m16 = $calcType.GetMethod("GetUnitMass").Invoke($null, @(16))
Assert-Test "GetUnitMass(16) == 1.580 kg/m" ([Math]::Abs($m16 - 1.580) -lt 1e-4) "Actual: $m16"

$m25 = $calcType.GetMethod("GetUnitMass").Invoke($null, @(25))
Assert-Test "GetUnitMass(25) == 3.850 kg/m" ([Math]::Abs($m25 - 3.850) -lt 1e-4) "Actual: $m25"

$m30 = $calcType.GetMethod("GetUnitMass").Invoke($null, @(30))
$expectedM30 = 900.0 / 162.2
Assert-Test "GetUnitMass(30) uses d^2/162.2 formula" ([Math]::Abs($m30 - $expectedM30) -lt 1e-3) "Actual: $m30 vs expected: $expectedM30"

$hook90_10 = $calcType.GetMethod("Hook90Length").Invoke($null, @(10, 10.0))
Assert-Test "Hook90Length(10, 10d) == 100 mm" ($hook90_10 -eq 100.0) "Actual: $hook90_10"

$hook90_6 = $calcType.GetMethod("Hook90Length").Invoke($null, @(6, 10.0))
Assert-Test "Hook90Length(6, 10d) clamped to 75 mm minimum" ($hook90_6 -eq 75.0) "Actual: $hook90_6"

$hook135_8 = $calcType.GetMethod("Hook135Length").Invoke($null, @(8, 7.5))
Assert-Test "Hook135Length(8, 7.5d) clamped to 75 mm minimum" ($hook135_8 -eq 75.0) "Actual: $hook135_8"

$hook180_10 = $calcType.GetMethod("Hook180Length").Invoke($null, @(10))
Assert-Test "Hook180Length(10) == 90 mm" ($hook180_10 -eq 90.0) "Actual: $hook180_10"

$hook180_6 = $calcType.GetMethod("Hook180Length").Invoke($null, @(6))
Assert-Test "Hook180Length(6) clamped to 60 mm minimum" ($hook180_6 -eq 60.0) "Actual: $hook180_6"

$lap20 = $calcType.GetMethod("LapSpliceLength").Invoke($null, @(20, 30.0))
Assert-Test "LapSpliceLength(20, 30d) == 600 mm" ($lap20 -eq 600.0) "Actual: $lap20"

$roundVal = $calcType.GetMethod("RoundLength").Invoke($null, @(1234.0, 50.0))
Assert-Test "RoundLength(1234, step=50) == 1250 mm" ($roundVal -eq 1250.0) "Actual: $roundVal"

# ---------------------------------------------------------------------------------
# 2. RebarShapeCatalog Tests
# ---------------------------------------------------------------------------------
Write-Host "`n--- 2. RebarShapeCatalog Tests ---"
$catType = $asm.GetType("HPKata.Core.Shapes.RebarShapeCatalog")

# Parse various shape codes
$shape01 = $catType.GetMethod("NormalizeShapeCode").Invoke($null, @("01"))
Assert-Test "NormalizeShapeCode('01') resolves Dang01" ($shape01.ToString() -eq "Dang01") "Actual: $shape01"

$shape02a = $catType.GetMethod("NormalizeShapeCode").Invoke($null, @("dang02a"))
Assert-Test "NormalizeShapeCode('dang02a') resolves Dang02a" ($shape02a.ToString() -eq "Dang02a") "Actual: $shape02a"

$shape67 = $catType.GetMethod("NormalizeShapeCode").Invoke($null, @("67"))
Assert-Test "NormalizeShapeCode('67') resolves Dang67" ($shape67.ToString() -eq "Dang67") "Actual: $shape67"

$shapeCoupler = $catType.GetMethod("NormalizeShapeCode").Invoke($null, @("coupler"))
Assert-Test "NormalizeShapeCode('coupler') resolves DangCoupler" ($shapeCoupler.ToString() -eq "DangCoupler") "Actual: $shapeCoupler"

# Calculate lengths
# Shape 01 (straight bar, L1 = 3500)
$calcMethod = $catType.GetMethod("CalculateCuttingLength")
$len01 = $calcMethod.Invoke($null, @($shape01, [double[]]@(3500.0), 0.0, 16, $false))
Assert-Test "CalculateCuttingLength(Dang01, L1=3500) == 3500 mm" ($len01 -eq 3500.0) "Actual: $len01"

# Shape 02 (U-bar, L1=500, L2=3000, without bend deduction -> 2*500 + 3000 = 4000)
$shape02 = $catType.GetMethod("NormalizeShapeCode").Invoke($null, @("02"))
$len02_noded = $calcMethod.Invoke($null, @($shape02, [double[]]@(500.0, 3000.0), 0.0, 16, $false))
Assert-Test "CalculateCuttingLength(Dang02, 500, 3000, no ded) == 4000 mm" ($len02_noded -eq 4000.0) "Actual: $len02_noded"

# Shape 02 with bend deduction (d=16, 90 deg -> deduction > 0)
$len02_ded = $calcMethod.Invoke($null, @($shape02, [double[]]@(500.0, 3000.0), 0.0, 16, $true))
Assert-Test "CalculateCuttingLength(Dang02, with ded) < 4000 mm" ($len02_ded -lt 4000.0 -and $len02_ded -gt 3900.0) "Actual: $len02_ded"

# Shape 14 (closed stirrup, L1=200, L2=400 -> 2*(200+400) = 1200)
$shape14 = $catType.GetMethod("NormalizeShapeCode").Invoke($null, @("14"))
$len14 = $calcMethod.Invoke($null, @($shape14, [double[]]@(200.0, 400.0), 0.0, 8, $false))
Assert-Test "CalculateCuttingLength(Dang14, 200, 400) == 1200 mm" ($len14 -eq 1200.0) "Actual: $len14"

# SafeSin angular safety clamp
$sin0 = $catType.GetMethod("SafeSin").Invoke($null, @(0.0))
Assert-Test "SafeSin(0 deg) safely clamps to 0.0 without crash" ($sin0 -eq 0.0) "Actual: $sin0"

$sin90 = $catType.GetMethod("SafeSin").Invoke($null, @(90.0))
Assert-Test "SafeSin(90 deg) == 1.0" ([Math]::Abs($sin90 - 1.0) -lt 1e-5) "Actual: $sin90"

# ---------------------------------------------------------------------------------
# 3. KataConfig & Parser Tests
# ---------------------------------------------------------------------------------
Write-Host "`n--- 3. KataConfig & Parser Tests ---"
$parserType = $asm.GetType("HPKata.Core.Configuration.KataConfigParser")
$defaultsType = $asm.GetType("HPKata.Core.Configuration.KataConfigDefaults")

$defConfig = $defaultsType.GetMethod("CreateDefault").Invoke($null, @())
Assert-Test "KataConfigDefaults creates valid config instance" ($defConfig -ne $null)
Assert-Test "Default ConcreteCoverMm == 30.0 mm" ($defConfig.ConcreteCoverMm -eq 30.0)
Assert-Test "Default StandardBarLength == 11700 mm" ($defConfig.StandardBarLength -eq 11700)
Assert-Test "Default LayerThepChu == 'kata_thep chu'" ($defConfig.LayerThepChu -eq "kata_thep chu")

# Serialize to 86 lines and parse back
$lines = $parserType.GetMethod("SerializeToLines").Invoke($null, @($defConfig))
Assert-Test "SerializeToLines produces exactly 86 lines" ($lines.Count -eq 86) "Count: $($lines.Count)"

$roundtripConfig = $parserType.GetMethod("ParseLines").Invoke($null, @($lines))
Assert-Test "Roundtrip config preserves ConcreteCoverMm (30.0)" ($roundtripConfig.ConcreteCoverMm -eq 30.0)
Assert-Test "Roundtrip config preserves StandardBarLength (11700)" ($roundtripConfig.StandardBarLength -eq 11700)
Assert-Test "Roundtrip config preserves LayerThepChu" ($roundtripConfig.LayerThepChu -eq "kata_thep chu")

# ---------------------------------------------------------------------------------
# 4. CuttingStockEngine Tests
# ---------------------------------------------------------------------------------
Write-Host "`n--- 4. CuttingStockEngine Tests ---"
$engineType = $asm.GetType("HPKata.Core.Optimization.CuttingStockEngine")
$demandType = $asm.GetType("HPKata.Core.Optimization.CuttingDemand")
$engine = [System.Activator]::CreateInstance($engineType)

# Demands: 3x 3500 mm + 2x 4000 mm + 2x 2000 mm
$demands = [System.Collections.Generic.List[System.Object]]::new()
$d1 = [System.Activator]::CreateInstance($demandType)
$d1.DiameterMm = 20; $d1.LengthMm = 3500.0; $d1.Quantity = 3; $demands.Add($d1)

$d2 = [System.Activator]::CreateInstance($demandType)
$d2.DiameterMm = 20; $d2.LengthMm = 4000.0; $d2.Quantity = 2; $demands.Add($d2)

$d3 = [System.Activator]::CreateInstance($demandType)
$d3.DiameterMm = 20; $d3.LengthMm = 2000.0; $d3.Quantity = 2; $demands.Add($d3)

# Total demand: 3*3500 + 2*4000 + 2*2000 = 10500 + 8000 + 4000 = 22500 mm (at least 2 standard 11700 mm bars = 23400 mm)
$solverStratType = $asm.GetType("HPKata.Core.Optimization.SolverStrategy")
$autoStrat = [System.Enum]::Parse($solverStratType, "Auto")

$solveMethod = $engineType.GetMethod("Solve")
$result = $solveMethod.Invoke($engine, @(20, $demands, $null, $autoStrat))

Assert-Test "CuttingStockEngine solved with non-null result" ($result -ne $null)
Assert-Test "Total requested pieces == 7" ($result.TotalPiecesCut -eq 7) "Actual: $($result.TotalPiecesCut)"

# Conservation of material check: sum of all cuts in all patterns must equal 22500 mm
$totalCutMm = 0.0
foreach ($pat in $result.Patterns) {
    foreach ($cut in $pat.CutsMm) {
        $totalCutMm += $cut
    }
}
Assert-Test "Piece length conservation: total cuts == 22,500 mm" ([Math]::Abs($totalCutMm - 22500.0) -lt 1e-3) "Actual: $totalCutMm"
Assert-Test "Net waste percentage is between 0% and 15%" ($result.NetWastePercentage -ge 0.0 -and $result.NetWastePercentage -le 15.0) "Actual: $($result.NetWastePercentage)%"

# Test warehouse remnant usage
$offcutType = $asm.GetType("HPKata.Core.Models.WarehouseOffcut")
$warehouseList = [System.Collections.Generic.List[System.Object]]::new()
$off1 = [System.Activator]::CreateInstance($offcutType)
$off1.Id = "REM-01"; $off1.DiameterMm = 20; $off1.LengthMm = 6000.0; $off1.Quantity = 1; $warehouseList.Add($off1)

$resRemnant = $solveMethod.Invoke($engine, @(20, $demands, $warehouseList, $autoStrat))
Assert-Test "Remnant used: ConsumedWarehouseBars > 0" ($resRemnant.ConsumedWarehouseBars -gt 0) "Actual: $($resRemnant.ConsumedWarehouseBars)"

# ---------------------------------------------------------------------------------
# 5. KataXDataSerializer Tests
# ---------------------------------------------------------------------------------
Write-Host "`n--- 5. KataXDataSerializer Tests ---"
$xdataType = $asm.GetType("HPKata.Core.Serialization.KataXDataSerializer")
$recordType = $asm.GetType("HPKata.Core.Serialization.KataXDataRecord")

$rec = [System.Activator]::CreateInstance($recordType)
$rec.Dang = "02"; $rec.STT = 5; $rec.Phi = 18; $rec.KhoangCach = "a150"; $rec.SoLuong = "12"; $rec.SoLuongCauKien = 2
$rec.L1 = 500.0; $rec.L2 = 3200.0; $rec.TenCauKien = "D1"; $rec.LinkedHandle = "3F1A"; $rec.LinkType = "THEP_DON"

$dxfValues = $xdataType.GetMethod("SerializeToDxf").Invoke($null, @($rec))
Assert-Test "SerializeToDxf outputs typed values list (>= 12 entries)" ($dxfValues.Count -ge 12) "Actual count: $($dxfValues.Count)"

$deserializedRec = $xdataType.GetMethod("DeserializeFromDxf").Invoke($null, @($dxfValues))
Assert-Test "Roundtrip DXF: Dang == '02'" ($deserializedRec.Dang -eq "02") "Actual: $($deserializedRec.Dang)"
Assert-Test "Roundtrip DXF: STT == 5" ($deserializedRec.STT -eq 5) "Actual: $($deserializedRec.STT)"
Assert-Test "Roundtrip DXF: Phi == 18" ($deserializedRec.Phi -eq 18) "Actual: $($deserializedRec.Phi)"
Assert-Test "Roundtrip DXF: L1 == 500.0, L2 == 3200.0" ($deserializedRec.L1 -eq 500.0 -and $deserializedRec.L2 -eq 3200.0)
Assert-Test "Roundtrip DXF: LinkedHandle == '3F1A'" ($deserializedRec.LinkedHandle -eq "3F1A")

# JSON roundtrip
$jsonStr = $xdataType.GetMethod("SerializeToJson").Invoke($null, @($rec))
Assert-Test "SerializeToJson outputs non-empty JSON containing '3F1A'" ($jsonStr.Contains("3F1A"))
$jsonRec = $xdataType.GetMethod("DeserializeFromJson").Invoke($null, @($jsonStr))
Assert-Test "DeserializeFromJson restores STT == 5 and Phi == 18" ($jsonRec.STT -eq 5 -and $jsonRec.Phi -eq 18)

# ---------------------------------------------------------------------------------
# 6. RebarScheduleAggregator Tests
# ---------------------------------------------------------------------------------
Write-Host "`n--- 6. RebarScheduleAggregator Tests ---"
$aggType = $asm.GetType("HPKata.Core.Scheduling.RebarScheduleAggregator")
$itemType = $asm.GetType("HPKata.Core.Models.RebarItem")

$itemList = [System.Collections.Generic.List[System.Object]]::new()

# Item 1: D=8 (wire <= 10), L=1200 mm, qty=10
$it1 = [System.Activator]::CreateInstance($itemType)
$it1.ComponentMark = "D1"; $it1.BarMark = "1"; $it1.ShapeCode = "14"; $it1.DiameterMm = 8
$it1.ParametersMm = [double[]]@(200.0, 400.0); $it1.MemberCount = 1; $it1.CountPerMember = 10
$itemList.Add($it1)

# Item 2: D=16 (deformed 10 < D <= 18), L=4000 mm, qty=4
$it2 = [System.Activator]::CreateInstance($itemType)
$it2.ComponentMark = "D1"; $it2.BarMark = "2"; $it2.ShapeCode = "01"; $it2.DiameterMm = 16
$it2.ParametersMm = [double[]]@(4000.0); $it2.MemberCount = 1; $it2.CountPerMember = 4
$itemList.Add($it2)

# Item 3: D=25 (deformed > 18), L=6000 mm, qty=2
$it3 = [System.Activator]::CreateInstance($itemType)
$it3.ComponentMark = "D1"; $it3.BarMark = "3"; $it3.ShapeCode = "01"; $it3.DiameterMm = 25
$it3.ParametersMm = [double[]]@(6000.0); $it3.MemberCount = 1; $it3.CountPerMember = 2
$itemList.Add($it3)

$report = $aggType.GetMethod("GenerateReport").Invoke($null, @($itemList, $false))
Assert-Test "GenerateReport generates 3 processed items" ($report.Items.Count -eq 3)
Assert-Test "DiameterSummaries has 3 diameter groups (8, 16, 25)" ($report.DiameterSummaries.Count -eq 3)
Assert-Test "TCVN Bins: WireLe10Kg > 0 (D=8)" ($report.Bins.WireLe10Kg -gt 0.0) "Actual: $($report.Bins.WireLe10Kg)"
Assert-Test "TCVN Bins: Deformed10To18Kg > 0 (D=16)" ($report.Bins.Deformed10To18Kg -gt 0.0) "Actual: $($report.Bins.Deformed10To18Kg)"
Assert-Test "TCVN Bins: DeformedGt18Kg > 0 (D=25)" ($report.Bins.DeformedGt18Kg -gt 0.0) "Actual: $($report.Bins.DeformedGt18Kg)"

# ---------------------------------------------------------------------------------
# 7. QrTagGenerator Tests
# ---------------------------------------------------------------------------------
Write-Host "`n--- 7. QrTagGenerator Tests ---"
$qrType = $asm.GetType("HPKata.Core.Tagging.QrTagGenerator")
$qrPayloadType = $asm.GetType("HPKata.Core.Tagging.QrTagPayload")

$qrp = [System.Activator]::CreateInstance($qrPayloadType)
$qrp.Project = "VINHOMES_GRAND_PARK"; $qrp.Client = "VNGROUP"; $qrp.Order = "ORD-2026-09"; $qrp.Component = "B101"
$qrp.Mark = "01"; $qrp.ShapeCode = "02"; $qrp.DiameterMm = 20; $qrp.CutLengthMm = 4000.0; $qrp.Quantity = 50
$qrp.WeightKg = 494.0; $qrp.SteelGrade = "CB400-V"; $qrp.SegmentsMm = [double[]]@(500.0, 3000.0, 500.0, 0.0, 0.0, 0.0)

$formattedPayload = $qrType.GetMethod("FormatPayload").Invoke($null, @($qrp))
Assert-Test "FormatPayload starts with 'KATA|'" ($formattedPayload.StartsWith("KATA|")) "Actual: $formattedPayload"

$parsedQr = $qrType.GetMethod("ParsePayload").Invoke($null, @($formattedPayload))
Assert-Test "ParsePayload restores Project == 'VINHOMES_GRAND_PARK'" ($parsedQr.Project -eq "VINHOMES_GRAND_PARK")
Assert-Test "ParsePayload restores DiameterMm == 20" ($parsedQr.DiameterMm -eq 20)
Assert-Test "ParsePayload restores SegmentsMm[1] == 3000.0" ($parsedQr.SegmentsMm[1] -eq 3000.0)

# Generate PNG bytes
$pngBytes = $qrType.GetMethod("GeneratePngBytes").Invoke($null, @($formattedPayload, 10))
Assert-Test "GeneratePngBytes produces valid byte array (> 500 bytes)" ($pngBytes -ne $null -and $pngBytes.Length -gt 500) "Length: $($pngBytes.Length)"

# Verify PNG magic header: 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A
$isPngMagic = ($pngBytes[0] -eq 0x89 -and $pngBytes[1] -eq 0x50 -and $pngBytes[2] -eq 0x4E -and $pngBytes[3] -eq 0x47 -and
               $pngBytes[4] -eq 0x0D -and $pngBytes[5] -eq 0x0A -and $pngBytes[6] -eq 0x1A -and $pngBytes[7] -eq 0x0A)
Assert-Test "GeneratePngBytes output has genuine PNG magic header (89 50 4E 47 0D 0A 1A 0A)" $isPngMagic

# ---------------------------------------------------------------------------------
# 8. ClosedXmlExporter Tests
# ---------------------------------------------------------------------------------
Write-Host "`n--- 8. ClosedXmlExporter Tests ---"
$exportType = $asm.GetType("HPKata.Core.Export.ClosedXmlExporter")

$ms = [System.IO.MemoryStream]::new()
$exportMethod = $exportType.GetMethod("ExportScheduleToStream")
$exportMethod.Invoke($null, @($report, $ms))
$excelBytes = $ms.ToArray()
Assert-Test "ExportScheduleToStream generates Excel file (> 3000 bytes)" ($excelBytes.Length -gt 3000) "Length: $($excelBytes.Length)"

# Verify zip PK header for .xlsx (0x50, 0x4B, 0x03, 0x04)
$isZipMagic = ($excelBytes[0] -eq 0x50 -and $excelBytes[1] -eq 0x4B -and $excelBytes[2] -eq 0x03 -and $excelBytes[3] -eq 0x04)
Assert-Test "ExportScheduleToStream output has genuine OpenXML Zip magic header (PK..)" $isZipMagic

Write-Host "`n================================================================================"
Write-Host "EMPIRICAL VERIFICATION SUMMARY"
Write-Host "Total Tests: $($passCount + $failCount)"
Write-Host "Passed:      $passCount" -ForegroundColor Green
$failColor = if ($failCount -gt 0) { "Red" } else { "Green" }
Write-Host "Failed:      $failCount" -ForegroundColor $failColor
Write-Host "================================================================================"

if ($failCount -gt 0) {
    exit 1
} else {
    exit 0
}
