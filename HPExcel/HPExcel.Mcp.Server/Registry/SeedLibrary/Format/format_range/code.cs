string rangeAddr = args.Require("range");
string sheetName = args.Str("sheet", null);
string wbName = args.Str("workbook", null);

var wb = string.IsNullOrEmpty(wbName) ? workbook : excel.Workbooks[wbName];
var ws = string.IsNullOrEmpty(sheetName) ? wb.ActiveSheet : wb.Worksheets[sheetName];
var target = ws.Range[rangeAddr];

int HexToBgr(string hex)
{
    hex = hex.TrimStart('#');
    if (hex.Length == 6)
    {
        int r = Convert.ToInt32(hex.Substring(0, 2), 16);
        int g = Convert.ToInt32(hex.Substring(2, 2), 16);
        int b = Convert.ToInt32(hex.Substring(4, 2), 16);
        return (b << 16) | (g << 8) | r;
    }
    return 0;
}

if (args.Has("numberFormat")) target.NumberFormat = args.Str("numberFormat");
if (args.Has("bold")) target.Font.Bold = args.Bool("bold");
if (args.Has("italic")) target.Font.Italic = args.Bool("italic");
if (args.Has("fontSize")) target.Font.Size = args.Double("fontSize");
if (args.Has("fontColor")) target.Font.Color = HexToBgr(args.Str("fontColor"));
if (args.Has("backgroundColor")) target.Interior.Color = HexToBgr(args.Str("backgroundColor"));
if (args.Has("wrapText")) target.WrapText = args.Bool("wrapText");

if (args.Has("horizontalAlignment"))
{
    string align = args.Str("horizontalAlignment").ToLowerInvariant();
    target.HorizontalAlignment = align switch
    {
        "center" => -4108, // xlCenter
        "right" => -4152,  // xlRight
        "justify" => -4130,// xlJustify
        _ => -4131         // xlLeft
    };
}

if (args.Has("border"))
{
    string bType = args.Str("border");
    if (bType == "all_thin")
    {
        var borders = target.Borders;
        borders.LineStyle = 1; // xlContinuous
        borders.Weight = 2;    // xlThin
    }
    else if (bType == "outline_thick")
    {
        target.BorderAround(1, 4, -4105, Type.Missing); // xlContinuous, xlThick, xlColorIndexAutomatic
    }
    else if (bType == "bottom_double")
    {
        var b = target.Borders[9]; // xlEdgeBottom
        b.LineStyle = -4119; // xlDouble
    }
    else if (bType == "none")
    {
        target.Borders.LineStyle = -4142; // xlLineStyleNone
    }
}

string actual = (string)target.Address;
log($"Formatted range {ws.Name}!{actual}");
return new
{
    success = true,
    sheet = (string)ws.Name,
    range = actual,
    summary = $"Formatted range {ws.Name}!{actual}; snapshot saved"
};
