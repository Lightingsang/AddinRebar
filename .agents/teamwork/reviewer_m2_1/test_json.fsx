open System.Text.Json
open System.Collections.Generic

let testCases = [
    null
    ""
    "   "
    "{ corrupt"
    "\"string-literal\""
    "[1, 2, 3]"
    "12345"
    "{ \"a\": 123 }"
    "{ \"a\": null }"
    "{}"
    "{\"Doc#Col:C1\":\"uid-1\"}"
]

for tc in testCases do
    try
        if System.String.IsNullOrWhiteSpace(tc) then
            printfn "Input [%A] -> null (due to IsNullOrWhiteSpace)" tc
        elif tc.Trim() = "{}" then
            printfn "Input [%A] -> empty dict" tc
        else
            let res = JsonSerializer.Deserialize<Dictionary<string, string>>(tc)
            if res = null then
                printfn "Input [%A] -> null" tc
            else
                printfn "Input [%A] -> parsed count=%d" tc res.Count
    with
    | :? JsonException as jex ->
        printfn "Input [%A] -> JsonException caught -> null" tc
    | ex ->
        printfn "Input [%A] -> UNEXPECTED EXCEPTION: %s : %s" tc (ex.GetType().FullName) ex.Message
