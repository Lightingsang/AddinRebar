string formula = args.Require("formula");
string sheetName = args.Str("sheet", null);
string wbName = args.Str("workbook", null);

if (formula.StartsWith("=", StringComparison.Ordinal))
{
    formula = formula.Substring(1);
}

var wb = string.IsNullOrEmpty(wbName) ? workbook : excel.Workbooks[wbName];
var ws = string.IsNullOrEmpty(sheetName) ? wb.ActiveSheet : wb.Worksheets[sheetName];
object evalResult = ws.Evaluate(formula);

string resultType = evalResult?.GetType().Name ?? "null";
bool isError = false;
string errorName = null;

if (evalResult is int errCode && errCode < 0)
{
    isError = true;
    errorName = errCode switch
    {
        -2146826281 => "#DIV/0!",
        -2146826246 => "#N/A",
        -2146826259 => "#NAME?",
        -2146826288 => "#NULL!",
        -2146826252 => "#NUM!",
        -2146826265 => "#REF!",
        -2146826273 => "#VALUE!",
        _ => $"#ERROR({errCode})"
    };
}

log($"Evaluated formula '{formula}' -> {evalResult}");
return new
{
    formula = "=" + formula,
    sheet = (string)ws.Name,
    result = isError ? (object)errorName : evalResult,
    resultType = isError ? "ExcelError" : resultType,
    isError,
    summary = $"Formula '={formula}' evaluated to {evalResult ?? "null"}"
};
