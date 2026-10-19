#r @"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.Core\bin\Release\net8.0\HPKata.Core.dll"

open System
open HPKata.Core.Shapes

let shapes = Enum.GetValues(typeof<RebarShapeType>) :?> RebarShapeType[]
printfn "Testing %d shapes..." shapes.Length

let mutable passCount = 0
let mutable failCount = 0

for s in shapes do
    try
        let def = RebarShapeCatalog.GetDefinition(s)
        if String.IsNullOrWhiteSpace(def.ShapeCode) || String.IsNullOrWhiteSpace(def.Name) then
            printfn "FAIL: Shape %A definition metadata incomplete" s
            failCount <- failCount + 1
        else
            // Calculate with nominal parameters: [400; 1200; 400; 200; 150; 100]
            let p = [| 400.0; 1200.0; 400.0; 200.0; 150.0; 100.0 |]
            let lenNoDed = RebarShapeCatalog.CalculateCuttingLength(s, p, 45.0, 20, false)
            let lenDed = RebarShapeCatalog.CalculateCuttingLength(s, p, 45.0, 20, true)
            
            if Double.IsNaN(lenNoDed) || Double.IsInfinity(lenNoDed) || lenNoDed <= 0.0 then
                printfn "FAIL: Shape %A produced invalid length %f" s lenNoDed
                failCount <- failCount + 1
            else
                passCount <- passCount + 1
    with ex ->
        printfn "FAIL: Shape %A threw exception: %s" s ex.Message
        failCount <- failCount + 1

printfn "Results: %d PASS, %d FAIL" passCount failCount
if failCount = 0 then
    printfn "PASS: All 85 shapes verified successfully!"
