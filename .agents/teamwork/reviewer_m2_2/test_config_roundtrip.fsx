#r @"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.Core\bin\Release\net8.0\HPKata.Core.dll"

open System
open HPKata.Core.Configuration

let realPath = @"C:\kata_pro\Goi lenh.lsp"
let config1 = KataConfigParser.ParseFile(realPath)
let serializedText = KataConfigParser.SerializeToText(config1)
let config2 = KataConfigParser.ParseText(serializedText)

let props = typeof<KataConfig>.GetProperties()
let mutable diffs = 0

for p in props do
    let v1 = p.GetValue(config1)
    let v2 = p.GetValue(config2)
    if not (Object.Equals(v1, v2)) then
        printfn "Mismatch on %s: v1='%A' vs v2='%A'" p.Name v1 v2
        diffs <- diffs + 1

if diffs = 0 then
    printfn "PASS: All 86 properties match 100%% across roundtrip!"
else
    printfn "FAIL: %d property mismatches found." diffs
