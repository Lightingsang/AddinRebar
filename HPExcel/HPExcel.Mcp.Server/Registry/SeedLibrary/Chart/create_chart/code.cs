string dataRangeAddr = args.Require("dataRange");
string chartTypeStr = args.Require("chartType");
string title = args.Str("title", null);
string sheetName = args.Str("sheet", null);
string wbName = args.Str("workbook", null);
string targetCell = args.Str("targetCell", "E2");
double width = args.Double("width", 400);
double height = args.Double("height", 250);
bool hasLegend = args.Bool("hasLegend", true);

var wb = string.IsNullOrEmpty(wbName) ? workbook : excel.Workbooks[wbName];
var ws = string.IsNullOrEmpty(sheetName) ? wb.ActiveSheet : wb.Worksheets[sheetName];
var sourceData = ws.Range[dataRangeAddr];
var placeCell = ws.Range[targetCell];

double left = (double)placeCell.Left;
double top = (double)placeCell.Top;

var chartObj = ws.ChartObjects().Add(left, top, width, height);
var ch = chartObj.Chart;

int xlType = chartTypeStr switch
{
    "ColumnStacked" => 52, // xlColumnStacked
    "Line" => 4,           // xlLine
    "LineMarkers" => 65,   // xlLineMarkers
    "Pie" => 5,            // xlPie
    "BarClustered" => 57,  // xlBarClustered
    "Area" => 1,           // xlArea
    "Scatter" => -4169,    // xlXYScatter
    _ => 51                // xlColumnClustered
};

ch.ChartType = xlType;
ch.SetSourceData(sourceData);
ch.HasLegend = hasLegend;

if (!string.IsNullOrEmpty(title))
{
    ch.HasTitle = true;
    ch.ChartTitle.Text = title;
}

log($"Created {chartTypeStr} chart at {ws.Name}!{targetCell}");
return new
{
    success = true,
    chartName = (string)chartObj.Name,
    chartType = chartTypeStr,
    title,
    sheet = (string)ws.Name,
    dataRange = (string)sourceData.Address,
    placedAt = targetCell,
    summary = $"Created {chartTypeStr} chart '{(title ?? (string)chartObj.Name)}' on {ws.Name}!{targetCell}; snapshot saved"
};
