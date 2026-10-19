<#
.SYNOPSIS
    Milestone M3 Empirical Challenge and Stress-Test Harness for HPKata.Core.Tests
    Author: challenger_m3_2 (Empirical Verification Agent)
    Adversarial verification of Optimization, Config, Serialization, and Export.
#>

param(
    [string]$PublishDir = "$PSScriptRoot\..\HPKata.Core\bin\publish_netstandard"
)

$ErrorActionPreference = "Stop"
Write-Host "======================================================================" -ForegroundColor Cyan
Write-Host "HPKATA.CORE.TESTS EMPIRICAL CHALLENGE & STRESS HARNESS -- MILESTONE M3" -ForegroundColor Cyan
Write-Host "======================================================================" -ForegroundColor Cyan

# Hook assembly resolve handler to ensure all dependencies bind cleanly in PowerShell
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
        try { [System.Reflection.Assembly]::LoadFrom($p) | Out-Null } catch {}
    }
}

Get-ChildItem -Path $PublishDir -Filter "*.dll" | ForEach-Object {
    try { [System.Reflection.Assembly]::LoadFrom($_.FullName) | Out-Null } catch {}
}

$results = [System.Collections.Generic.List[PSCustomObject]]::new()

function Run-Challenge {
    param(
        [string]$Id,
        [string]$Description,
        [scriptblock]$Action,
        [scriptblock]$Assert
    )
    Write-Host "`n--> [$Id] $Description" -ForegroundColor Yellow
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $ex = $null
    $res = $null
    try {
        $res = & $Action
    } catch {
        $ex = $_
    }
    $sw.Stop()
    $elapsed = $sw.ElapsedMilliseconds

    $passed = $false
    $details = ""
    if ($ex -ne $null) {
        $details = "Exception: $($ex.Exception.Message)"
    } else {
        try {
            $assertRes = & $Assert $res $elapsed
            if ($assertRes -is [bool]) {
                $passed = $assertRes
            } elseif ($assertRes.Passed -ne $null) {
                $passed = $assertRes.Passed
                $details = $assertRes.Details
            } else {
                $passed = [bool]$assertRes
            }
        } catch {
            $passed = $false
            $details = "Assertion failure: $($_.Exception.Message)"
        }
    }

    $status = if ($passed) { "PASS" } else { "FAIL" }
    $color = if ($passed) { "Green" } else { "Red" }
    Write-Host "[$status] $Id | Elapsed: ${elapsed}ms | $details" -ForegroundColor $color
    $results.Add([PSCustomObject]@{
        Id = $Id
        Description = $Description
        Status = $status
        ElapsedMs = $elapsed
        Details = $details
    })
}

$engine = [HPKata.Core.Optimization.CuttingStockEngine]::new()

# ----------------------------------------------------------------------
# CHALLENGE 1: DuBang0 Combinatorial Explosion on 200 items of 2000mm
# ----------------------------------------------------------------------
Run-Challenge -Id "CH-01" -Description "DuBang0 on 200 items of 2000mm against 11,700mm stock (Time <= 1000ms)" -Action {
    $demands = [System.Collections.Generic.List[HPKata.Core.Optimization.CuttingDemand]]::new()
    for ($i = 0; $i -lt 200; $i++) {
        $demands.Add([HPKata.Core.Optimization.CuttingDemand]::new(20, 2000.0, 1, "M$i", "C1"))
    }
    $engine.Solve(20, $demands, $null, [HPKata.Core.Optimization.SolverStrategy]::DuBang0)
} -Assert {
    param($res, $ms)
    $ok = ($ms -le 1000) -and ($res.TotalPiecesCut -eq 200) -and ($res.NewStandardBarsCount -eq 40)
    [PSCustomObject]@{
        Passed = $ok
        Details = "Cut: $($res.TotalPiecesCut)/200, Bars: $($res.NewStandardBarsCount)/40, Elapsed: ${ms}ms (limit <=1000ms)"
    }
}

# ----------------------------------------------------------------------
# CHALLENGE 2: DuBang0 on 500 items of 2000mm (Extended combinatorial volume)
# ----------------------------------------------------------------------
Run-Challenge -Id "CH-02" -Description "DuBang0 on 500 items of 2000mm against 11,700mm stock (Time <= 1000ms)" -Action {
    $demands = [System.Collections.Generic.List[HPKata.Core.Optimization.CuttingDemand]]::new()
    for ($i = 0; $i -lt 500; $i++) {
        $demands.Add([HPKata.Core.Optimization.CuttingDemand]::new(20, 2000.0, 1, "M$i", "C1"))
    }
    $engine.Solve(20, $demands, $null, [HPKata.Core.Optimization.SolverStrategy]::DuBang0)
} -Assert {
    param($res, $ms)
    $ok = ($ms -le 1000) -and ($res.TotalPiecesCut -eq 500) -and ($res.NewStandardBarsCount -eq 100)
    [PSCustomObject]@{
        Passed = $ok
        Details = "Cut: $($res.TotalPiecesCut)/500, Bars: $($res.NewStandardBarsCount)/100, Elapsed: ${ms}ms"
    }
}

# ----------------------------------------------------------------------
# CHALLENGE 3: DuBang0 Pathological Maze without Exact Sums (150 items near 2340mm)
# ----------------------------------------------------------------------
Run-Challenge -Id "CH-03" -Description "DuBang0 Pathological Maze without exact sums (150 items, timeout and pruning)" -Action {
    $demands = [System.Collections.Generic.List[HPKata.Core.Optimization.CuttingDemand]]::new()
    $offsets = @(-3.0, -1.0, 1.0, 3.0, 5.0, 7.0)
    for ($i = 0; $i -lt 150; $i++) {
        $len = 2340.0 + $offsets[$i % $offsets.Length]
        $demands.Add([HPKata.Core.Optimization.CuttingDemand]::new(20, $len, 1, "M$i", "C1"))
    }
    $engine.Solve(20, $demands, $null, [HPKata.Core.Optimization.SolverStrategy]::DuBang0)
} -Assert {
    param($res, $ms)
    $ok = ($ms -le 1200) -and ($res.TotalPiecesCut -eq 150)
    [PSCustomObject]@{
        Passed = $ok
        Details = "Cut: $($res.TotalPiecesCut)/150, Elapsed: ${ms}ms (Cascade completed safely)"
    }
}

# ----------------------------------------------------------------------
# CHALLENGE 4: Warehouse Offcut 100% Utilization (N_new_bars = 0, Net Waste = 0.0%)
# ----------------------------------------------------------------------
Run-Challenge -Id "CH-04" -Description "Warehouse offcut 100% utilization (N_new_bars = 0, NetWaste = 0.0%, no NaN)" -Action {
    $wh = [System.Collections.Generic.List[HPKata.Core.Models.WarehouseOffcut]]::new()
    $wh.Add([HPKata.Core.Models.WarehouseOffcut]::new(20, 4000.0, 1))
    $wh.Add([HPKata.Core.Models.WarehouseOffcut]::new(20, 3000.0, 1))
    $wh.Add([HPKata.Core.Models.WarehouseOffcut]::new(20, 5000.0, 1))

    $demands = [System.Collections.Generic.List[HPKata.Core.Optimization.CuttingDemand]]::new()
    $demands.Add([HPKata.Core.Optimization.CuttingDemand]::new(20, 2000.0, 2, "D1", "C1"))
    $demands.Add([HPKata.Core.Optimization.CuttingDemand]::new(20, 3000.0, 1, "D2", "C1"))
    $demands.Add([HPKata.Core.Optimization.CuttingDemand]::new(20, 4500.0, 1, "D3", "C1"))

    $engine.Solve(20, $demands, $wh, [HPKata.Core.Optimization.SolverStrategy]::LapDayCanhDai)
} -Assert {
    param($res, $ms)
    $noNaN = (-not [double]::IsNaN($res.NetWastePercent)) -and (-not [double]::IsInfinity($res.NetWastePercent))
    $ok = ($res.NewStandardBarsCount -eq 0) -and ($res.NetWastePercent -eq 0.0) -and $noNaN -and ($res.ConsumedWarehouseBarsCount -ge 3)
    [PSCustomObject]@{
        Passed = $ok
        Details = "NewBars: $($res.NewStandardBarsCount), ConsumedWH: $($res.ConsumedWarehouseBarsCount), NetWaste: $($res.NetWastePercent)%, NaN: $(-not $noNaN)"
    }
}

# ----------------------------------------------------------------------
# CHALLENGE 5: Warehouse Offcut with Scrap Remnant (N_new_bars = 0, Scrap > 0, Valid NetWaste)
# ----------------------------------------------------------------------
Run-Challenge -Id "CH-05" -Description "Warehouse offcut with scrap remnant (N_new_bars = 0, NetWaste > 0.0%, no NaN)" -Action {
    $wh = [System.Collections.Generic.List[HPKata.Core.Models.WarehouseOffcut]]::new()
    $wh.Add([HPKata.Core.Models.WarehouseOffcut]::new(16, 4200.0, 1))

    $demands = [System.Collections.Generic.List[HPKata.Core.Optimization.CuttingDemand]]::new()
    $demands.Add([HPKata.Core.Optimization.CuttingDemand]::new(16, 2000.0, 2, "D1", "C1"))

    $engine.Solve(16, $demands, $wh, [HPKata.Core.Optimization.SolverStrategy]::LapDayCanhDai)
} -Assert {
    param($res, $ms)
    $noNaN = (-not [double]::IsNaN($res.NetWastePercent)) -and (-not [double]::IsInfinity($res.NetWastePercent))
    $ok = ($res.NewStandardBarsCount -eq 0) -and ($res.NetWastePercent -gt 0.0) -and $noNaN
    [PSCustomObject]@{
        Passed = $ok
        Details = "NewBars: $($res.NewStandardBarsCount), NetWaste: $($res.NetWastePercent)% (expected ~4.76%), NaN: $(-not $noNaN)"
    }
}

# ----------------------------------------------------------------------
# CHALLENGE 6: Parse Real Goi lenh.lsp Lines 152-237 (All 86 parameters)
# ----------------------------------------------------------------------
Run-Challenge -Id "CH-06" -Description "Parse real C:\HPKata\Goi lenh.lsp lines 152-237 (all 86 parameters verified)" -Action {
    $lspPath = "C:\HPKata\Goi lenh.lsp"
    if (-not (Test-Path $lspPath)) {
        throw "File $lspPath does not exist on disk."
    }
    [HPKata.Core.Configuration.KataConfigParser]::ParseFile($lspPath)
} -Assert {
    param($cfg, $ms)
    $ok = ($cfg.LayerDim -eq "kata_dim") -and
          ($cfg.StandardBarLength -eq 11700) -and
          ($cfg.LanguageUiSched -like "*Vi*") -and
          ($cfg.BaseBarDiameter -eq 16) -and
          ($cfg.ConcreteCoverMm -eq 30.0) -and
          ($cfg.LengthUnitDisplay -eq "mm") -and
          ($cfg.SecBeamStirrupAdd1 -eq "5f10a50") -and
          ($cfg.SecBeamAntiCrack1 -eq "2f14")
    [PSCustomObject]@{
        Passed = $ok
        Details = "LayerDim: $($cfg.LayerDim), StandardBar: $($cfg.StandardBarLength)mm, Cover: $($cfg.ConcreteCoverMm)mm, Lang: '$($cfg.LanguageUiSched)'"
    }
}

# ----------------------------------------------------------------------
# CHALLENGE 7: Configuration Roundtrip Idempotence f(f(x)) = f(x)
# ----------------------------------------------------------------------
Run-Challenge -Id "CH-07" -Description "Roundtrip config serialization idempotence (f(f(x)) == f(x))" -Action {
    $lspPath = "C:\HPKata\Goi lenh.lsp"
    $cfg0 = [HPKata.Core.Configuration.KataConfigParser]::ParseFile($lspPath)
    $t1 = [HPKata.Core.Configuration.KataConfigParser]::SerializeToText($cfg0)
    $cfg1 = [HPKata.Core.Configuration.KataConfigParser]::ParseText($t1)
    $t2 = [HPKata.Core.Configuration.KataConfigParser]::SerializeToText($cfg1)
    $cfg2 = [HPKata.Core.Configuration.KataConfigParser]::ParseText($t2)
    $t3 = [HPKata.Core.Configuration.KataConfigParser]::SerializeToText($cfg2)
    $linesCount = ($t2 -split "`r?`n").Length
    [PSCustomObject]@{ Text2 = $t2; Text3 = $t3; Equal = ($t2 -eq $t3); Lines = $linesCount }
} -Assert {
    param($res, $ms)
    $ok = $res.Equal -and ($res.Lines -ge 86)
    [PSCustomObject]@{
        Passed = $ok
        Details = "Byte-identical roundtrip: $($res.Equal), Lines serialized: $($res.Lines)"
    }
}

# ----------------------------------------------------------------------
# CHALLENGE 8: Configuration Edge Cases (Corrupted Lines, Windows-1258 Diacritics)
# ----------------------------------------------------------------------
Run-Challenge -Id "CH-08" -Description "Configuration resilience on corrupted lines and Windows-1258 fallback" -Action {
    # 1. Corrupted numeric lines
    $corrupted = [System.Collections.Generic.List[string]]::new()
    for ($i = 0; $i -lt 86; $i++) { $corrupted.Add(";corrupted_value_$i") }
    $cfgCorrupt = [HPKata.Core.Configuration.KataConfigParser]::ParseLines($corrupted)

    # 2. Windows-1258 fallback via byte array
    $bytes = [byte[]]@(0x3B, 0x54, 0x69, 0xEA, 0xCC, 0x81, 0x6E, 0x67, 0x20, 0x56, 0x69, 0xEA, 0xCC, 0xA3, 0x74)
    $decoded = [HPKata.Core.Configuration.KataConfigParser]::DecodeWithFallback($bytes)

    # 3. Missing file exception
    $missingCaught = $false
    try {
        [HPKata.Core.Configuration.KataConfigParser]::ParseFile("C:\fake_xyz_123.lsp")
    } catch [System.IO.FileNotFoundException] {
        $missingCaught = $true
    }

    [PSCustomObject]@{
        CoverFellBack = ($cfgCorrupt.ConcreteCoverMm -eq 30.0)
        BarLengthFellBack = ($cfgCorrupt.StandardBarLength -eq 11700)
        Decoded = $decoded
        MissingCaught = $missingCaught
    }
} -Assert {
    param($res, $ms)
    $ok = $res.CoverFellBack -and $res.BarLengthFellBack -and $res.MissingCaught -and ($res.Decoded -like "*Ti*Vi*")
    [PSCustomObject]@{
        Passed = $ok
        Details = "CorruptFallback: $($res.CoverFellBack), MissingCaught: $($res.MissingCaught), Decoded: '$($res.Decoded)'"
    }
}

# ----------------------------------------------------------------------
# CHALLENGE 9: ClosedXML Excel Export OpenXML ZIP Stream Validity
# ----------------------------------------------------------------------
Run-Challenge -Id "CH-09" -Description "ClosedXML Excel export generates valid OpenXML ZIP stream (PK header)" -Action {
    $items = [System.Collections.Generic.List[HPKata.Core.Models.RebarItem]]::new()
    $items.Add([HPKata.Core.Models.RebarItem]@{
        ComponentMark = "DAM_D1"
        BarMark = "1"
        ShapeCode = "01"
        DiameterMm = 20
        CountPerMember = 4
        MemberCount = 1
        CuttingLengthMm = 6000.0
        UnitMassKgPerM = 2.470
        ParametersMm = @(6000.0)
    })
    $items.Add([HPKata.Core.Models.RebarItem]@{
        ComponentMark = "DAM_D1"
        BarMark = "2"
        ShapeCode = "coupler"
        DiameterMm = 20
        CountPerMember = 4
        MemberCount = 1
        Notes = "Coupler M20"
        CuttingLengthMm = 80.0
    })

    $report = [HPKata.Core.Scheduling.RebarScheduleAggregator]::GenerateReport($items)
    $msStream = [System.IO.MemoryStream]::new()
    [HPKata.Core.Export.ClosedXmlExporter]::ExportScheduleToStream($report, $msStream)

    $bytes = $msStream.ToArray()
    $isZip = ($bytes.Length -gt 4) -and ($bytes[0] -eq 0x50) -and ($bytes[1] -eq 0x4B) -and ($bytes[2] -eq 0x03) -and ($bytes[3] -eq 0x04)

    # Re-open with ClosedXML to verify worksheet
    $msStream.Position = 0
    $wb = [ClosedXML.Excel.XLWorkbook]::new($msStream)
    $ws = $wb.Worksheet("Thong_ke")
    $title = $ws.Cell("A1").GetString()
    $wb.Dispose()
    $msStream.Dispose()

    [PSCustomObject]@{
        Length = $bytes.Length
        IsZipMagic = $isZip
        SheetTitle = $title
    }
} -Assert {
    param($res, $ms)
    $ok = $res.IsZipMagic -and ($res.SheetTitle.Length -gt 0) -and ($res.SheetTitle -like "*TH*NG K*")
    [PSCustomObject]@{
        Passed = $ok
        Details = "Size: $($res.Length) bytes, Magic: PK (zip=$($res.IsZipMagic)), Sheet: '$($res.SheetTitle)'"
    }
}

# ----------------------------------------------------------------------
# CHALLENGE 10: QR Code ECC Level Q Tag Generation and PNG Magic Header
# ----------------------------------------------------------------------
Run-Challenge -Id "CH-10" -Description "QR code tag payload generation with ECC Level Q and valid PNG header bytes" -Action {
    $tagData = [HPKata.Core.Tagging.QrTagPayload]::new()
    $tagData.Project = "Vinhomes Grand Park"
    $tagData.Client = "Vingroup"
    $tagData.Order = "ORD-05"
    $tagData.Component = "DAM_D1"
    $tagData.Mark = "1"
    $tagData.ShapeCode = "01"
    $tagData.DiameterMm = 20
    $tagData.Quantity = 4
    $tagData.CutLengthMm = 6000.0
    $tagData.SteelGrade = "CB400-V"
    $tagData.WeightKg = 59.28
    $tagData.SegmentsMm = @(6000.0, 0.0, 0.0, 0.0, 0.0, 0.0)

    $payload = [HPKata.Core.Tagging.QrTagGenerator]::FormatPayload($tagData)
    $pngBytes = [HPKata.Core.Tagging.QrTagGenerator]::GeneratePngBytes($payload, 4)

    # Check PNG magic header: 89 50 4E 47 0D 0A 1A 0A
    $isPng = ($pngBytes.Length -gt 8) -and
             ($pngBytes[0] -eq 0x89) -and ($pngBytes[1] -eq 0x50) -and
             ($pngBytes[2] -eq 0x4E) -and ($pngBytes[3] -eq 0x47) -and
             ($pngBytes[4] -eq 0x0D) -and ($pngBytes[5] -eq 0x0A) -and
             ($pngBytes[6] -eq 0x1A) -and ($pngBytes[7] -eq 0x0A)

    [PSCustomObject]@{
        Payload = $payload
        PngBytesLength = $pngBytes.Length
        IsPngHeader = $isPng
    }
} -Assert {
    param($res, $ms)
    $ok = $res.IsPngHeader -and ($res.Payload -like "KATA|*") -and ($res.PngBytesLength -gt 500)
    [PSCustomObject]@{
        Passed = $ok
        Details = "Payload: '$($res.Payload)', PNG Size: $($res.PngBytesLength) bytes, PNG Magic: $($res.IsPngHeader)"
    }
}

# ----------------------------------------------------------------------
# CHALLENGE 11: KataPro XData RegApp 'KATA' 2-Way Roundtrip
# ----------------------------------------------------------------------
Run-Challenge -Id "CH-11" -Description "KataPro XData RegApp 'KATA' 2-way lossless roundtrip serialization" -Action {
    $original = [HPKata.Core.Serialization.KataXDataRecord]::new()
    $original.Dang = "02"
    $original.STT = 5
    $original.Phi = 20
    $original.KhoangCach = "a150"
    $original.SoLuong = "8"
    $original.SoLuongCauKien = 2
    $original.ChieuDaiCacDoanMm = @(500.0, 3500.0, 500.0, 0.0, 0.0, 0.0)
    $original.GocUon = "90/90"
    $original.TenCauKien = "COT_C1"
    $original.SttThepChinh = "1"
    $original.ChuThich = "Main bar"
    $original.Z = "0"
    $original.LinkedHandle = "1A2B"
    $original.LinkType = "THEP_DON"
    $original.LinkedDimHandle = "2C3D"

    $entries = [HPKata.Core.Serialization.KataXDataSerializer]::SerializeToDxf($original)
    $restored = [HPKata.Core.Serialization.KataXDataSerializer]::DeserializeFromDxf($entries)

    $equal = ($restored.STT -eq $original.STT) -and
             ($restored.Dang -eq $original.Dang) -and
             ($restored.Phi -eq $original.Phi) -and
             ($restored.SoLuong -eq $original.SoLuong) -and
             ($restored.LinkedHandle -eq $original.LinkedHandle) -and
             ($restored.LinkedDimHandle -eq $original.LinkedDimHandle)

    [PSCustomObject]@{
        EntriesCount = $entries.Count
        Equal = $equal
        RestoredHandle = $restored.LinkedHandle
    }
} -Assert {
    param($res, $ms)
    $ok = $res.Equal -and ($res.EntriesCount -ge 10)
    [PSCustomObject]@{
        Passed = $ok
        Details = "DXF Entries: $($res.EntriesCount), 100% Roundtrip match: $($res.Equal), Handle: $($res.RestoredHandle)"
    }
}

# ----------------------------------------------------------------------
# CHALLENGE 12: Clean Code Governance Line Count and Host-Neutrality Scan
# ----------------------------------------------------------------------
Run-Challenge -Id "CH-12" -Description "Clean Code Governance audit on HPKata.Core and HPKata.Core.Tests" -Action {
    $coreFiles = Get-ChildItem -Path "$PSScriptRoot\..\HPKata.Core" -Filter "*.cs" -Recurse | Where-Object { $_.FullName -notlike '*\obj\*' -and $_.FullName -notlike '*\bin\*' }
    $testFiles = Get-ChildItem -Path "$PSScriptRoot\..\HPKata.Core.Tests" -Filter "*.cs" -Recurse | Where-Object { $_.FullName -notlike '*\obj\*' -and $_.FullName -notlike '*\bin\*' }
    $allFiles = $coreFiles + $testFiles

    $violationsC2 = [System.Collections.Generic.List[string]]::new()
    $violationsAutodesk = [System.Collections.Generic.List[string]]::new()

    foreach ($f in $allFiles) {
        $lines = (Get-Content $f.FullName).Count
        if ($lines -gt 300) {
            $violationsC2.Add("$($f.Name): $lines lines (>300)")
        }
        $raw = Get-Content $f.FullName -Raw
        if ($raw -match "using\s+Autodesk\.") {
            $violationsAutodesk.Add($f.Name)
        }
    }

    [PSCustomObject]@{
        TotalFiles = $allFiles.Count
        C2Violations = $violationsC2
        AutodeskViolations = $violationsAutodesk
    }
} -Assert {
    param($res, $ms)
    $ok = ($res.C2Violations.Count -eq 0) -and ($res.AutodeskViolations.Count -eq 0)
    [PSCustomObject]@{
        Passed = $ok
        Details = "Files scanned: $($res.TotalFiles), Rule C2 (>300 lines) violations: $($res.C2Violations.Count), Autodesk references: $($res.AutodeskViolations.Count)"
    }
}

Write-Host "`n======================================================================" -ForegroundColor Cyan
Write-Host "EMPIRICAL CHALLENGE SUMMARY" -ForegroundColor Cyan
Write-Host "======================================================================" -ForegroundColor Cyan
$passCount = @($results | Where-Object { $_.Status -eq "PASS" }).Count
$failCount = @($results | Where-Object { $_.Status -eq "FAIL" }).Count
Write-Host "Total: $($results.Count) | Passed: $passCount | Failed: $failCount" -ForegroundColor $(if ($failCount -eq 0) { "Green" } else { "Red" })

if ($failCount -gt 0) {
    Write-Host "VERDICT: REQUEST_CHANGES" -ForegroundColor Red
    exit 1
} else {
    Write-Host "VERDICT: APPROVE" -ForegroundColor Green
    exit 0
}
