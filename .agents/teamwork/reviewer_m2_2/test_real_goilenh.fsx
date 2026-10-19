#r @"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.Core\bin\Release\net8.0\HPKata.Core.dll"

open System
open HPKata.Core.Configuration

printfn "=== TESTING REAL Goi lenh.lsp FROM DISK ==="

let realPath = @"C:\kata_pro\Goi lenh.lsp"
let config = KataConfigParser.ParseFile(realPath)

printfn "LayerDim: %s" config.LayerDim
printfn "LayerNetThay: %s" config.LayerNetThay
printfn "SpacingPrefix: %s" config.SpacingPrefix
printfn "SmallBarPrefix: %s" config.SmallBarPrefix
printfn "DeformedBarPrefix: %s" config.DeformedBarPrefix
printfn "DimStyleName: %s" config.DimStyleName
printfn "ConcreteCoverMm: %f" config.ConcreteCoverMm
printfn "DetailDrawingScale: %f" config.DetailDrawingScale
printfn "StandardBarLength: %d" config.StandardBarLength
printfn "BeamCutoffDatumL1: %s" config.BeamCutoffDatumL1
printfn "LanguageUiSched: %s" config.LanguageUiSched
printfn "SecBeamStirrupAdd1: %s" config.SecBeamStirrupAdd1
printfn "TagCircleRadius: %f" config.TagCircleRadius
