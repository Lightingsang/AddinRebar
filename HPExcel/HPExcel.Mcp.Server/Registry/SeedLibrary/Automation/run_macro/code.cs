string macroName = args.Require("macroName");
string wbName = args.Str("workbook", null);
var macroArgs = args.List("args");

var wb = string.IsNullOrEmpty(wbName) ? workbook : excel.Workbooks[wbName];

string curWbName = (string)wb.Name;
string qualifiedMacro = macroName.Contains("!") ? macroName : $"'{curWbName}'!{macroName}";

object result = null;
if (macroArgs == null || macroArgs.Count == 0)
{
    result = excel.Run(qualifiedMacro);
}
else
{
    var parsed = macroArgs.Select(e =>
    {
        if (e.Raw.HasValue)
        {
            var val = e.Raw.Value;
            return val.ValueKind switch
            {
                System.Text.Json.JsonValueKind.Number => val.TryGetInt64(out var l) ? (object)l : val.GetDouble(),
                System.Text.Json.JsonValueKind.True => true,
                System.Text.Json.JsonValueKind.False => false,
                _ => (object)val.GetString()
            };
        }
        return (object)e.AsString();
    }).ToArray();

    result = parsed.Length switch
    {
        1 => excel.Run(qualifiedMacro, parsed[0]),
        2 => excel.Run(qualifiedMacro, parsed[0], parsed[1]),
        3 => excel.Run(qualifiedMacro, parsed[0], parsed[1], parsed[2]),
        4 => excel.Run(qualifiedMacro, parsed[0], parsed[1], parsed[2], parsed[3]),
        _ => excel.Run(qualifiedMacro, parsed[0], parsed[1], parsed[2], parsed[3], parsed[4])
    };
}

log($"Executed VBA macro '{qualifiedMacro}'");
return new
{
    success = true,
    macroName,
    returnValue = result,
    summary = $"Executed VBA macro '{macroName}'; snapshot saved"
};
