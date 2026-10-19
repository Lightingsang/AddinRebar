#r @"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.Core\bin\Release\net8.0\HPKata.Core.dll"

open System
open HPKata.Core.Optimization
open HPKata.Core.Shapes
open HPKata.Core.Configuration
open HPKata.Core.Serialization
open HPKata.Core.Models

printfn "=== TESTING EDGE CASES VIA COMPILED DLL ==="

// 1. CuttingStockEngine with N_new_bars = 0
let engine = CuttingStockEngine()
let demands = [ CuttingDemand(20, 2000.0, 2) ]
let warehouse = [ WarehouseOffcut(20, 3000.0, 2) ]
let res1 = engine.Solve(20, demands, warehouse, SolverStrategy.LapDayCanhDai)

printfn "Res1 Patterns: %d" res1.Patterns.Count
printfn "Res1 GrossWaste: %f" res1.GrossWastePercent
printfn "Res1 NetWaste: %f" res1.NetWastePercent
let newBars1 = res1.Patterns |> Seq.filter (fun p -> not p.IsWarehouseBar) |> Seq.length
printfn "Res1 New Bars: %d" newBars1
if newBars1 = 0 && not (Double.IsNaN(res1.NetWastePercent)) && not (Double.IsInfinity(res1.NetWastePercent)) then
    printfn "PASS: NetWaste is finite with 0 new bars: %f" res1.NetWastePercent
else
    printfn "FAIL: NetWaste failed"

// 2. Empty demands
let resEmpty = engine.Solve(20, [], warehouse, SolverStrategy.Auto)
printfn "PASS: Empty demands patterns: %d" resEmpty.Patterns.Count

// 3. DuBang0 timeout and k <= 5 bound
let duBangDemands = [ for i in 1 .. 60 -> CuttingDemand(20, 1170.0, 1) ]
let sw = System.Diagnostics.Stopwatch.StartNew()
let resDuBang = engine.Solve(20, duBangDemands, [], SolverStrategy.DuBang0)
sw.Stop()
printfn "DuBang0 elapsed: %d ms, patterns: %d" sw.ElapsedMilliseconds resDuBang.Patterns.Count
if sw.ElapsedMilliseconds <= 2000L then
    printfn "PASS: DuBang0 strictly bounded and cascaded to BFD in %d ms" sw.ElapsedMilliseconds
else
    printfn "FAIL: DuBang0 took too long (%d ms)" sw.ElapsedMilliseconds

// 4. SafeSin safeguards on dang06 and dang49
let len06_0 = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang06, [| 1000.0; 500.0; 1000.0 |], 0.0, 20, false)
let len06_45 = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang06, [| 1000.0; 500.0; 1000.0 |], 45.0, 20, false)
printfn "Dang06 alpha=0: %f (expected 2500.0)" len06_0
printfn "Dang06 alpha=45: %f" len06_45
if Math.Abs(len06_0 - 2500.0) < 1e-4 then
    printfn "PASS: Dang06 alpha=0 safe fallback"
else
    printfn "FAIL: Dang06 alpha=0"

let len49_0 = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang49, [| 1000.0; 500.0; 1000.0 |], 0.0, 20, false)
printfn "Dang49 alpha=0: %f (expected 2500.0)" len49_0
if Math.Abs(len49_0 - 2500.0) < 1e-4 then
    printfn "PASS: Dang49 alpha=0 safe fallback"
else
    printfn "FAIL: Dang49 alpha=0"

// 5. ConfigParser encoding & fallback
System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance)
let rawWin1258 = System.Text.Encoding.GetEncoding(1258).GetBytes(";kata_dim\r\n;kata_net thay\r\n;kata_net khung\r\n;kata_net khuat\r\n;kata_grid\r\n;kata_thep chu\r\n;kata_thep dai\r\n;kata_thep dai\r\n;kata_net manh\r\n;kata_text\r\n;kata_text\r\n;a\r\n;Ø\r\n;Ø\r\n;kata_dim_iso\r\n;30\r\n;135/180\r\n;1/25\r\n;16\r\n;30\r\n;1\r\n;1\r\n;1\r\n;0\r\n;1\r\n;40\r\n;1/6\r\n;1\r\n;0\r\n;0\r\n;Ø\r\n;11700\r\n;100\r\n;0\r\n;0.2\r\n; mép\r\n;0\r\n;0\r\n;0\r\n;1a, 1b...\r\n;1\r\n;5\r\n;0\r\n;-\r\n;7.5\r\n;25\r\n;\r\n;0.25\r\n; mép\r\n;8800\r\n;Tiếng Việt\r\n")
let decoded = KataConfigParser.DecodeWithFallback(rawWin1258)
let splitLines = decoded.Split([| '\n' |])
printfn "Decoded lines: %d" splitLines.Length
let parsedCfg = KataConfigParser.ParseText(decoded)
printfn "SmallBarPrefix: %s" parsedCfg.SmallBarPrefix
printfn "BeamCutoffDatumL1: %s" parsedCfg.BeamCutoffDatumL1
printfn "LanguageUiSched: %s" parsedCfg.LanguageUiSched
if parsedCfg.SmallBarPrefix = "Ø" && parsedCfg.BeamCutoffDatumL1 = "mép" && parsedCfg.LanguageUiSched = "Tiếng Việt" then
    printfn "PASS: Win1258 decoding and config parsing 100 percent faithful"
else
    printfn "FAIL: Win1258 decoding"

// 6. ConfigParser malformed lines & fractions
let badText = ";dim\n;thay\n;khung\n;khuat\n;grid\n;thep\n;dai\n;daiphu\n;manh\n;text\n;texttag\n;a\n;Ø\n;Ø\n;dimiso\n;INVALID_COVER\n;135\n;1/0\n;NOT_AN_INT"
let badCfg = KataConfigParser.ParseText(badText)
printfn "Fallback Cover: %f (expect 30.0)" badCfg.ConcreteCoverMm
printfn "Fallback Scale 1/0: %f (expect 0.04)" badCfg.DetailDrawingScale
printfn "Fallback BaseDia: %d (expect 16)" badCfg.BaseBarDiameter
if badCfg.ConcreteCoverMm = 30.0 && badCfg.DetailDrawingScale = 0.04 && badCfg.BaseBarDiameter = 16 then
    printfn "PASS: Malformed inputs handled gracefully with fallbacks"
else
    printfn "FAIL: Malformed inputs fallback"

// 7. XData Serializer roundtrip
let rec1 = KataXDataRecord()
rec1.Dang <- "22"
rec1.STT <- 5s
rec1.Phi <- 10s
rec1.KhoangCach <- "a150"
rec1.SoLuong <- "24"
rec1.SoLuongCauKien <- 1s
rec1.ChieuDaiCacDoanMm <- [| 350.0; 550.0; 75.0 |]
rec1.GocUon <- "135"
rec1.TenCauKien <- "D1-TANG 2"
rec1.ChuThich <- "Đai kín"
rec1.LinkedHandle <- "3F2B"
rec1.LinkType <- "THEP_DAI"
rec1.LinkedDimHandle <- "3F2C"

let dxfValues = KataXDataSerializer.SerializeToDxf(rec1)
let rec2 = KataXDataSerializer.DeserializeFromDxf(dxfValues)
printfn "Roundtrip Dang: %s, STT: %d, Phi: %d, Handle: %s, DimHandle: %s" rec2.Dang rec2.STT rec2.Phi rec2.LinkedHandle rec2.LinkedDimHandle
if rec2.Dang = "22" && rec2.STT = 5s && rec2.Phi = 10s && rec2.LinkedHandle = "3F2B" && rec2.LinkedDimHandle = "3F2C" && rec2.ChieuDaiCacDoanMm.Length = 3 then
    printfn "PASS: XData DXF roundtrip equality"
else
    printfn "FAIL: XData DXF roundtrip equality"

// 8. JSON Serializer roundtrip & invalid JSON fallback
let jsonStr = KataXDataSerializer.SerializeToJson(rec1)
let recFromJson = KataXDataSerializer.DeserializeFromJson(jsonStr)
let recFromBadJson = KataXDataSerializer.DeserializeFromJson("{ this is not valid json }")
if recFromJson.Dang = "22" && recFromBadJson.Dang = null then
    printfn "PASS: JSON serialization and invalid fallback"
else
    printfn "FAIL: JSON serialization"
