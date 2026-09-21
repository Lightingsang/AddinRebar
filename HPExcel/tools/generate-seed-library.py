"""Generates the 12 Excel seed tools (tool.json + code.cs + examples.json) under HPExcel.Mcp.Server/Registry/SeedLibrary/.
"""
import io, os, json

root = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "HPExcel.Mcp.Server", "Registry", "SeedLibrary")

def seed(category, name, tool, code, examples):
    d = os.path.join(root, category, name)
    os.makedirs(d, exist_ok=True)
    base = {
        "name": name,
        "version": 1,
        "status": "published",
        "author": "hprebar",
        "category": category,
        "host": "excel",
        "hostVersions": ["2026"]
    }
    base.update(tool)
    io.open(os.path.join(d, "tool.json"), "w", encoding="utf-8", newline="\n").write(json.dumps(base, indent=2, ensure_ascii=False) + "\n")
    io.open(os.path.join(d, "code.cs"), "w", encoding="utf-8", newline="\n").write(code.strip("\n") + "\n")
    io.open(os.path.join(d, "examples.json"), "w", encoding="utf-8", newline="\n").write(json.dumps(examples, indent=2, ensure_ascii=False) + "\n")
    print("ok", category, name, len(code.strip("\n").split("\n")), "lines")

# -------------------------------------------------------------------------------------------------- Seed 1: read_range
seed("Data", "read_range", {
    "transaction": "none",
    "timeoutSeconds": 30,
    "destructive": False,
    "tags": ["range", "read", "cells", "values"],
    "title": "Read range",
    "description": "Read cell values, formulas, or formatted text from a worksheet range (e.g. 'A1:D10') or named range into JSON. Supports fast batch array reads. Read-only.",
    "notes": "COM: Uses Range.Value2 batch 2D array read for maximum performance. Headless: Uses ClosedXML IXLRange.",
    "inputSchema": {
        "type": "object",
        "properties": {
            "range": {"type": "string", "description": "Cell range address (e.g. 'A1:C10', 'Sheet2!B5:E20') or named range. If omitted, reads the worksheet's used range."},
            "sheet": {"type": "string", "description": "Worksheet name. Defaults to active worksheet."},
            "workbook": {"type": "string", "description": "Workbook name or file path. Defaults to active workbook."},
            "outputFormat": {"type": "string", "enum": ["values", "formulas", "formatted"], "default": "values", "description": "Output representation: 'values' (raw values), 'formulas' (formulas if present), 'formatted' (display text)."},
            "hasHeaders": {"type": "boolean", "default": False, "description": "If true, treats the first row as headers and returns an array of JSON objects."},
            "maxRows": {"type": "integer", "default": 1000, "description": "Maximum number of rows to return."}
        },
        "additionalProperties": False
    }
},
r'''
string rangeAddr = args.Str("range", null);
string sheetName = args.Str("sheet", null);
string wbName = args.Str("workbook", null);
string outputFormat = args.Str("outputFormat", "values");
bool hasHeaders = args.Bool("hasHeaders", false);
int maxRows = args.Int("maxRows", 1000);

var wb = string.IsNullOrEmpty(wbName) ? workbook : excel.Workbooks[wbName];
var ws = string.IsNullOrEmpty(sheetName) ? wb.ActiveSheet : wb.Worksheets[sheetName];
var targetRange = string.IsNullOrEmpty(rangeAddr) ? ws.UsedRange : ws.Range[rangeAddr];

int rowCount = Math.Min((int)targetRange.Rows.Count, maxRows);
int colCount = (int)targetRange.Columns.Count;
string actualAddr = (string)targetRange.Address;

object[,] rawValues = null;
if (rowCount == 1 && colCount == 1)
{
    rawValues = new object[2, 2];
    rawValues[1, 1] = outputFormat == "formulas" ? targetRange.Formula : (outputFormat == "formatted" ? targetRange.Text : targetRange.Value2);
}
else
{
    var slice = targetRange.Resize[rowCount, colCount];
    rawValues = outputFormat == "formulas" ? (object[,])slice.Formula : (outputFormat == "formatted" ? (object[,])slice.Text : (object[,])slice.Value2);
}

var headers = new List<string>();
var rows = new List<object>();

if (hasHeaders && rowCount > 1)
{
    for (int c = 1; c <= colCount; c++)
    {
        headers.Add(rawValues[1, c]?.ToString() ?? $"Col_{c}");
    }
    for (int r = 2; r <= rowCount; r++)
    {
        var rowObj = new Dictionary<string, object>();
        for (int c = 1; c <= colCount; c++)
        {
            rowObj[headers[c - 1]] = rawValues[r, c];
        }
        rows.Add(rowObj);
    }
}
else
{
    for (int r = 1; r <= rowCount; r++)
    {
        var rowList = new List<object>();
        for (int c = 1; c <= colCount; c++)
        {
            rowList.Add(rawValues[r, c]);
        }
        rows.Add(rowList);
    }
}

log($"Read {rowCount}x{colCount} from {ws.Name}!{actualAddr}");
return new
{
    sheet = (string)ws.Name,
    address = actualAddr,
    rowCount,
    colCount,
    hasHeaders,
    headers = hasHeaders ? headers : null,
    data = rows,
    summary = $"Read {rowCount} rows x {colCount} cols from {ws.Name}!{actualAddr}"
};
''',
[
    {"title": "Read active sheet used range", "args": {}},
    {"title": "Read range with headers", "args": {"range": "A1:D10", "hasHeaders": True}},
    {"title": "Read formulas from range", "args": {"range": "E2:E20", "outputFormat": "formulas"}}
])

# -------------------------------------------------------------------------------------------------- Seed 2: read_worksheet_info
seed("Workbook", "read_worksheet_info", {
    "transaction": "none",
    "timeoutSeconds": 30,
    "destructive": False,
    "tags": ["worksheet", "metadata", "tables", "charts"],
    "title": "Read worksheet info",
    "description": "Inspect worksheets in the workbook: names, indices, visibility state, used range dimensions, tables, and charts. Read-only.",
    "notes": "Enumerate Worksheets collection and their child ListObjects and ChartObjects.",
    "inputSchema": {
        "type": "object",
        "properties": {
            "sheet": {"type": "string", "description": "Specific sheet name to inspect. If omitted, inspects all sheets in workbook."},
            "workbook": {"type": "string", "description": "Workbook name or file path. Defaults to active workbook."}
        },
        "additionalProperties": False
    }
},
r'''
string sheetName = args.Str("sheet", null);
string wbName = args.Str("workbook", null);
var wb = string.IsNullOrEmpty(wbName) ? workbook : excel.Workbooks[wbName];
var sheets = new List<object>();

var targetSheets = new List<object>();
if (!string.IsNullOrEmpty(sheetName))
{
    targetSheets.Add(wb.Worksheets[sheetName]);
}
else
{
    foreach (var s in wb.Worksheets) targetSheets.Add(s);
}

foreach (var ws in targetSheets)
{
    var tables = new List<string>();
    foreach (var tbl in ws.ListObjects) tables.Add((string)tbl.Name);

    var charts = new List<string>();
    foreach (var ch in ws.ChartObjects()) charts.Add((string)ch.Name);

    var used = ws.UsedRange;
    string usedAddr = used != null ? (string)used.Address : "$A$1";
    int rows = used != null ? (int)used.Rows.Count : 0;
    int cols = used != null ? (int)used.Columns.Count : 0;
    int vis = (int)ws.Visible; // -1: xlSheetVisible, 0: xlSheetHidden, 2: xlSheetVeryHidden
    string visStr = vis == -1 ? "Visible" : (vis == 0 ? "Hidden" : "VeryHidden");

    sheets.Add(new
    {
        name = (string)ws.Name,
        index = (int)ws.Index,
        visibility = visStr,
        usedRange = usedAddr,
        rowCount = rows,
        colCount = cols,
        tables,
        charts,
        isProtected = (bool)ws.ProtectContents
    });
}

string activeName = (string)wb.ActiveSheet.Name;
log($"Workbook '{wb.Name}' has {sheets.Count} worksheet(s); active: {activeName}");
return new
{
    workbook = (string)wb.Name,
    activeSheet = activeName,
    sheetCount = sheets.Count,
    sheets,
    summary = $"Workbook contains {sheets.Count} worksheet(s); active sheet: '{activeName}'"
};
''',
[
    {"title": "Inspect all worksheets in active workbook", "args": {}},
    {"title": "Inspect specific worksheet", "args": {"sheet": "Summary"}}
])

# -------------------------------------------------------------------------------------------------- Seed 3: find_cells
seed("Data", "find_cells", {
    "transaction": "none",
    "timeoutSeconds": 30,
    "destructive": False,
    "tags": ["find", "search", "cells", "query"],
    "title": "Find cells",
    "description": "Search for matching text, numbers, or formula patterns across a worksheet or entire workbook. Returns matched cell addresses and values. Read-only.",
    "notes": "Uses Range.Find loop with proper wrap detection.",
    "inputSchema": {
        "type": "object",
        "required": ["query"],
        "properties": {
            "query": {"type": "string", "description": "Text or value to search for."},
            "sheet": {"type": "string", "description": "Worksheet name to search. Defaults to active sheet."},
            "workbook": {"type": "string", "description": "Workbook name or file path. Defaults to active workbook."},
            "lookIn": {"type": "string", "enum": ["values", "formulas"], "default": "values", "description": "Look in cell values or formulas."},
            "exactMatch": {"type": "boolean", "default": False, "description": "Match entire cell contents vs substring."},
            "searchAllSheets": {"type": "boolean", "default": False, "description": "If true, searches across all worksheets in the workbook."},
            "maxResults": {"type": "integer", "default": 100, "description": "Maximum number of matched cells to return."}
        },
        "additionalProperties": False
    }
},
r'''
string query = args.Require("query");
string sheetName = args.Str("sheet", null);
string wbName = args.Str("workbook", null);
string lookInStr = args.Str("lookIn", "values");
bool exactMatch = args.Bool("exactMatch", false);
bool searchAllSheets = args.Bool("searchAllSheets", false);
int maxResults = args.Int("maxResults", 100);

var wb = string.IsNullOrEmpty(wbName) ? workbook : excel.Workbooks[wbName];

int lookIn = lookInStr == "formulas" ? -4123 : -4163; // xlFormulas (-4123) vs xlValues (-4163)
int lookAt = exactMatch ? 1 : 2; // xlWhole (1) vs xlPart (2)

var sheetsToSearch = new List<object>();
if (searchAllSheets)
{
    foreach (var s in wb.Worksheets) sheetsToSearch.Add(s);
}
else
{
    sheetsToSearch.Add(string.IsNullOrEmpty(sheetName) ? wb.ActiveSheet : wb.Worksheets[sheetName]);
}

var matches = new List<object>();
foreach (var ws in sheetsToSearch)
{
    var used = ws.UsedRange;
    if (used == null) continue;

    var current = used.Find(query, Type.Missing, lookIn, lookAt, 1, 1, false, Type.Missing, Type.Missing);
    if (current == null) continue;

    string firstAddress = (string)current.Address;
    do
    {
        matches.Add(new
        {
            sheet = (string)ws.Name,
            address = (string)current.Address,
            row = (int)current.Row,
            col = (int)current.Column,
            value = current.Value2,
            formula = (bool)current.HasFormula ? (string)current.Formula : null
        });

        if (matches.Count >= maxResults) break;
        current = used.FindNext(current);
    } while (current != null && (string)current.Address != firstAddress);

    if (matches.Count >= maxResults) break;
}

log($"Found {matches.Count} match(es) for query '{query}'");
return new
{
    query,
    matchCount = matches.Count,
    matches,
    summary = $"Found {matches.Count} matching cell(s) for '{query}'"
};
''',
[
    {"title": "Find text in active sheet", "args": {"query": "Total"}},
    {"title": "Exact match across workbook", "args": {"query": "Pending", "exactMatch": True, "searchAllSheets": True}}
])

# -------------------------------------------------------------------------------------------------- Seed 4: read_table
seed("Data", "read_table", {
    "transaction": "none",
    "timeoutSeconds": 30,
    "destructive": False,
    "tags": ["table", "listobject", "data", "records"],
    "title": "Read table",
    "description": "Read structured Excel table (ListObject) headers, data rows as JSON records, and totals row. Read-only.",
    "notes": "Reads ListObject.HeaderRowRange, DataBodyRange, and TotalsRowRange.",
    "inputSchema": {
        "type": "object",
        "required": ["tableName"],
        "properties": {
            "tableName": {"type": "string", "description": "Name of the Excel table (e.g. 'Table1', 'RebarList')."},
            "sheet": {"type": "string", "description": "Worksheet containing the table. If omitted, searches all worksheets."},
            "workbook": {"type": "string", "description": "Workbook name or file path. Defaults to active workbook."},
            "maxRows": {"type": "integer", "default": 1000, "description": "Maximum number of data rows to return."}
        },
        "additionalProperties": False
    }
},
r'''
string tableName = args.Require("tableName");
string sheetName = args.Str("sheet", null);
string wbName = args.Str("workbook", null);
int maxRows = args.Int("maxRows", 1000);

var wb = string.IsNullOrEmpty(wbName) ? workbook : excel.Workbooks[wbName];
ListObject targetTable = null;
Worksheet targetSheet = null;

if (!string.IsNullOrEmpty(sheetName))
{
    targetSheet = wb.Worksheets[sheetName];
    try { targetTable = targetSheet.ListObjects[tableName]; } catch { }
}
else
{
    foreach (var ws in wb.Worksheets)
    {
        try
        {
            targetTable = ws.ListObjects[tableName];
            targetSheet = ws;
            break;
        }
        catch { }
    }
}

if (targetTable == null)
{
    throw new ArgumentException($"Table '{tableName}' not found in workbook.");
}

var columns = new List<string>();
foreach (var col in targetTable.ListColumns)
{
    columns.Add((string)col.Name);
}

var body = targetTable.DataBodyRange;
var rows = new List<Dictionary<string, object>>();
int rowCount = 0;

if (body != null)
{
    rowCount = Math.Min((int)body.Rows.Count, maxRows);
    int colCount = columns.Count;
    object[,] vals = rowCount == 1 && colCount == 1
        ? new object[,] { { null, null }, { null, body.Value2 } }
        : (object[,])body.Resize[rowCount, colCount].Value2;

    for (int r = 1; r <= rowCount; r++)
    {
        var row = new Dictionary<string, object>();
        for (int c = 1; c <= colCount; c++)
        {
            row[columns[c - 1]] = vals[r, c];
        }
        rows.Add(row);
    }
}

var totals = new Dictionary<string, object>();
bool hasTotals = (bool)targetTable.ShowTotals;
if (hasTotals && targetTable.TotalsRowRange != null)
{
    var tRange = targetTable.TotalsRowRange;
    for (int c = 1; c <= columns.Count; c++)
    {
        totals[columns[c - 1]] = tRange.Cells[1, c].Value2;
    }
}

log($"Table '{tableName}' read: {rows.Count} row(s), {columns.Count} column(s)");
return new
{
    tableName = (string)targetTable.Name,
    sheet = (string)targetSheet.Name,
    range = (string)targetTable.Range.Address,
    columns,
    rowCount = rows.Count,
    hasTotalsRow = hasTotals,
    totals = hasTotals ? totals : null,
    rows,
    summary = $"Table '{tableName}' on sheet '{targetSheet.Name}': {rows.Count} rows x {columns.Count} columns"
};
''',
[
    {"title": "Read table by name", "args": {"tableName": "RebarSchedule"}},
    {"title": "Read table on specific sheet", "args": {"tableName": "Inventory", "sheet": "Stock", "maxRows": 50}}
])

# -------------------------------------------------------------------------------------------------- Seed 5: write_range
seed("Data", "write_range", {
    "transaction": "auto",
    "timeoutSeconds": 60,
    "destructive": True,
    "tags": ["write", "range", "cells", "values", "batch"],
    "title": "Write range",
    "description": "Batch write 2D values, arrays, or formulas to a range starting at target cell. Auto-sizes destination range. Takes an automatic snapshot before writing.",
    "notes": "Uses Range.Resize and 2D array assignment in a single COM call for speed.",
    "inputSchema": {
        "type": "object",
        "required": ["startCell", "values"],
        "properties": {
            "startCell": {"type": "string", "description": "Starting cell address (e.g. 'A1', 'B5')."},
            "values": {
                "type": "array",
                "description": "2D array of rows and columns to write: [[row1_col1, row1_col2], [row2_col1, row2_col2]].",
                "items": {"type": "array", "items": {"type": "string"}}
            },
            "sheet": {"type": "string", "description": "Target worksheet. Defaults to active sheet."},
            "workbook": {"type": "string", "description": "Workbook name or file path. Defaults to active workbook."},
            "autoFitColumns": {"type": "boolean", "default": False, "description": "Auto-fit column widths after writing."},
            "isFormula": {"type": "boolean", "default": False, "description": "If true, treats string values starting with '=' as active formulas."}
        },
        "additionalProperties": False
    }
},
r'''
string startCell = args.Require("startCell");
string sheetName = args.Str("sheet", null);
string wbName = args.Str("workbook", null);
bool autoFit = args.Bool("autoFitColumns", false);
bool isFormula = args.Bool("isFormula", false);

var rowsList = args.List("values");
if (rowsList.Count == 0)
{
    throw new ArgumentException("Parameter 'values' must be a non-empty 2D array.");
}

int numRows = rowsList.Count;
int numCols = 0;
foreach (var r in rowsList)
{
    if (r.IsArray && r.Raw.HasValue)
    {
        int c = r.Raw.Value.GetArrayLength();
        if (c > numCols) numCols = c;
    }
}
if (numCols == 0) throw new ArgumentException("Parameter 'values' must contain at least one row with columns.");

object[,] data = new object[numRows, numCols];
for (int r = 0; r < numRows; r++)
{
    if (rowsList[r].IsArray && rowsList[r].Raw.HasValue)
    {
        var cols = rowsList[r].Raw.Value.EnumerateArray().ToList();
        for (int c = 0; c < numCols; c++)
        {
            if (c < cols.Count)
            {
                var val = cols[c];
                data[r, c] = val.ValueKind switch
                {
                    System.Text.Json.JsonValueKind.Number => val.TryGetInt64(out var l) ? (object)l : val.GetDouble(),
                    System.Text.Json.JsonValueKind.True => true,
                    System.Text.Json.JsonValueKind.False => false,
                    System.Text.Json.JsonValueKind.Null => null,
                    _ => val.GetString()
                };
            }
            else data[r, c] = null;
        }
    }
}

var wb = string.IsNullOrEmpty(wbName) ? workbook : excel.Workbooks[wbName];
var ws = string.IsNullOrEmpty(sheetName) ? wb.ActiveSheet : wb.Worksheets[sheetName];
var origin = ws.Range[startCell];
var target = origin.Resize[numRows, numCols];

if (isFormula) target.Formula = data;
else target.Value2 = data;

if (autoFit) target.Columns.AutoFit();

string targetAddr = (string)target.Address;
log($"Wrote {numRows} rows x {numCols} cols to {ws.Name}!{targetAddr}");
return new
{
    success = true,
    sheet = (string)ws.Name,
    targetRange = targetAddr,
    rowsWritten = numRows,
    colsWritten = numCols,
    summary = $"Wrote {numRows} rows x {numCols} cols to {ws.Name}!{targetAddr}; snapshot saved"
};
''',
[
    {"title": "Write 2D table with autofit", "args": {"startCell": "A1", "values": [["Mark", "Diameter", "Count"], ["B1", 16, 12], ["B2", 20, 8]], "autoFitColumns": True}},
    {"title": "Write calculated formulas", "args": {"startCell": "D2", "values": [["=B2*C2"], ["=B3*C3"]], "isFormula": True}}
])

# -------------------------------------------------------------------------------------------------- Seed 6: format_range
seed("Format", "format_range", {
    "transaction": "auto",
    "timeoutSeconds": 60,
    "destructive": True,
    "tags": ["format", "style", "color", "font", "border", "numberformat"],
    "title": "Format range",
    "description": "Apply formatting to a range: number format, font styling (bold, size, color), fill background color (hex), borders, and alignment. Takes an automatic snapshot before writing.",
    "notes": "Translates Hex colors to BGR integers for Excel Range.Interior.Color and Font.Color.",
    "inputSchema": {
        "type": "object",
        "required": ["range"],
        "properties": {
            "range": {"type": "string", "description": "Cell range address (e.g. 'A1:E1', 'B2:B20')."},
            "sheet": {"type": "string", "description": "Worksheet name. Defaults to active sheet."},
            "workbook": {"type": "string", "description": "Workbook name or file path. Defaults to active workbook."},
            "numberFormat": {"type": "string", "description": "Number format string (e.g. '$#,##0.00', '0.0%', 'YYYY-MM-DD', '#,##0')."},
            "bold": {"type": "boolean"},
            "italic": {"type": "boolean"},
            "fontSize": {"type": "number"},
            "fontColor": {"type": "string", "description": "Hex color for text (e.g. '#FFFFFF', '#1A1A1A')."},
            "backgroundColor": {"type": "string", "description": "Hex fill background color (e.g. '#003366', '#E2EFDA')."},
            "horizontalAlignment": {"type": "string", "enum": ["left", "center", "right", "justify"]},
            "wrapText": {"type": "boolean"},
            "border": {"type": "string", "enum": ["all_thin", "outline_thick", "bottom_double", "none"]}
        },
        "additionalProperties": False
    }
},
r'''
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
''',
[
    {"title": "Format table header", "args": {"range": "A1:E1", "bold": True, "backgroundColor": "#003366", "fontColor": "#FFFFFF", "horizontalAlignment": "center"}},
    {"title": "Format currency column", "args": {"range": "C2:C50", "numberFormat": "$#,##0.00"}},
    {"title": "Add thin borders", "args": {"range": "A1:D20", "border": "all_thin"}}
])

# -------------------------------------------------------------------------------------------------- Seed 7: manage_worksheet
seed("Workbook", "manage_worksheet", {
    "transaction": "auto",
    "timeoutSeconds": 60,
    "destructive": True,
    "tags": ["worksheet", "add", "rename", "delete", "hide", "duplicate"],
    "title": "Manage worksheet",
    "description": "Manage workbook sheets: add, rename, duplicate, delete, or change visibility (visible, hidden, very_hidden). Deleting a sheet is DESTRUCTIVE. Takes an automatic snapshot before execution.",
    "notes": "Deleting a worksheet turns on destructive tier gating. Sheet renaming validates invalid chars (: \\ / ? * [ ]).",
    "inputSchema": {
        "type": "object",
        "required": ["action", "sheet"],
        "properties": {
            "action": {"type": "string", "enum": ["add", "rename", "duplicate", "delete", "hide", "unhide"], "description": "Action to perform on worksheet."},
            "sheet": {"type": "string", "description": "Target sheet name."},
            "newSheetName": {"type": "string", "description": "New sheet name (required for 'rename' and optional for 'add'/'duplicate')."},
            "workbook": {"type": "string", "description": "Workbook name or file path. Defaults to active workbook."},
            "visibility": {"type": "string", "enum": ["visible", "hidden", "very_hidden"], "description": "Target visibility for 'hide'/'unhide'."}
        },
        "additionalProperties": False
    }
},
r'''
string action = args.Require("action").ToLowerInvariant();
string sheetName = args.Require("sheet");
string newName = args.Str("newSheetName", null);
string wbName = args.Str("workbook", null);
string vis = args.Str("visibility", "visible");

var wb = string.IsNullOrEmpty(wbName) ? workbook : excel.Workbooks[wbName];

Worksheet targetSheet = null;
if (action != "add")
{
    targetSheet = wb.Worksheets[sheetName];
}

string resultMsg = "";
if (action == "add")
{
    var ws = wb.Worksheets.Add(Type.Missing, wb.Worksheets[wb.Worksheets.Count]);
    if (!string.IsNullOrEmpty(newName)) ws.Name = newName;
    resultMsg = $"Added new worksheet '{ws.Name}'";
}
else if (action == "rename")
{
    if (string.IsNullOrEmpty(newName)) throw new ArgumentException("newSheetName is required for rename action.");
    string old = (string)targetSheet.Name;
    targetSheet.Name = newName;
    resultMsg = $"Renamed worksheet '{old}' to '{newName}'";
}
else if (action == "duplicate")
{
    targetSheet.Copy(Type.Missing, targetSheet);
    var copy = wb.ActiveSheet;
    if (!string.IsNullOrEmpty(newName)) copy.Name = newName;
    resultMsg = $"Duplicated worksheet '{sheetName}' as '{copy.Name}'";
}
else if (action == "delete")
{
    excel.DisplayAlerts = false;
    try { targetSheet.Delete(); }
    finally { excel.DisplayAlerts = true; }
    resultMsg = $"Deleted worksheet '{sheetName}'";
}
else if (action == "hide")
{
    targetSheet.Visible = vis == "very_hidden" ? 2 : 0; // xlSheetVeryHidden (2) vs xlSheetHidden (0)
    resultMsg = $"Set worksheet '{sheetName}' visibility to {vis}";
}
else if (action == "unhide")
{
    targetSheet.Visible = -1; // xlSheetVisible
    resultMsg = $"Unhid worksheet '{sheetName}'";
}

log(resultMsg);
return new
{
    success = true,
    action,
    sheet = sheetName,
    newSheetName = newName,
    summary = $"{resultMsg}; snapshot saved"
};
''',
[
    {"title": "Add new worksheet", "args": {"action": "add", "sheet": "NewSheet", "newSheetName": "Calculations"}},
    {"title": "Rename existing worksheet", "args": {"action": "rename", "sheet": "Sheet1", "newSheetName": "RebarSummary"}},
    {"title": "Hide worksheet", "args": {"action": "hide", "sheet": "Settings", "visibility": "very_hidden"}}
])

# -------------------------------------------------------------------------------------------------- Seed 8: create_table
seed("Data", "create_table", {
    "transaction": "auto",
    "timeoutSeconds": 60,
    "destructive": True,
    "tags": ["table", "listobject", "create", "format", "style"],
    "title": "Create table",
    "description": "Convert a range of cells into a structured Excel Table (ListObject) with headers, applied styling, and optional totals row. Takes an automatic snapshot before writing.",
    "notes": "Uses Worksheets.ListObjects.Add with xlSrcRange.",
    "inputSchema": {
        "type": "object",
        "required": ["range", "tableName"],
        "properties": {
            "range": {"type": "string", "description": "Cell range to convert to table (e.g. 'A1:E50')."},
            "tableName": {"type": "string", "description": "Unique table name (e.g. 'RebarSchedule', 'Expenses')."},
            "sheet": {"type": "string", "description": "Worksheet name. Defaults to active sheet."},
            "workbook": {"type": "string", "description": "Workbook name or file path. Defaults to active workbook."},
            "hasHeaders": {"type": "boolean", "default": True, "description": "Whether first row contains headers."},
            "tableStyle": {"type": "string", "default": "TableStyleMedium2", "description": "Excel built-in table style name."},
            "showTotalsRow": {"type": "boolean", "default": False, "description": "Whether to display the table totals row."}
        },
        "additionalProperties": False
    }
},
r'''
string rangeAddr = args.Require("range");
string tableName = args.Require("tableName");
string sheetName = args.Str("sheet", null);
string wbName = args.Str("workbook", null);
bool hasHeaders = args.Bool("hasHeaders", true);
string tableStyle = args.Str("tableStyle", "TableStyleMedium2");
bool showTotals = args.Bool("showTotalsRow", false);

var wb = string.IsNullOrEmpty(wbName) ? workbook : excel.Workbooks[wbName];
var ws = string.IsNullOrEmpty(sheetName) ? wb.ActiveSheet : wb.Worksheets[sheetName];
var targetRange = ws.Range[rangeAddr];

int xlSrcRange = 1;
int xlHeaders = hasHeaders ? 1 : 2; // xlYes (1) vs xlNo (2)

var tbl = ws.ListObjects.Add(xlSrcRange, targetRange, Type.Missing, xlHeaders, Type.Missing);
tbl.Name = tableName;
tbl.TableStyle = tableStyle;
tbl.ShowTotals = showTotals;

string finalAddr = (string)tbl.Range.Address;
log($"Created table '{tableName}' at {ws.Name}!{finalAddr}");
return new
{
    success = true,
    tableName,
    sheet = (string)ws.Name,
    range = finalAddr,
    tableStyle,
    showTotalsRow = showTotals,
    summary = $"Created table '{tableName}' on {ws.Name}!{finalAddr}; snapshot saved"
};
''',
[
    {"title": "Convert range to table", "args": {"range": "A1:D20", "tableName": "BeamRebarTable"}},
    {"title": "Create styled table with totals", "args": {"range": "A1:E100", "tableName": "MaterialCosts", "tableStyle": "TableStyleLight1", "showTotalsRow": True}}
])

# -------------------------------------------------------------------------------------------------- Seed 9: create_chart
seed("Chart", "create_chart", {
    "transaction": "auto",
    "timeoutSeconds": 60,
    "destructive": True,
    "tags": ["chart", "graph", "plot", "visualization"],
    "title": "Create chart",
    "description": "Create an embedded chart (Column, Line, Pie, Bar, Area, Scatter) linked to a data range. Custom title, size, and location. Takes an automatic snapshot before writing.",
    "notes": "Uses ChartObjects.Add and Chart.SetSourceData.",
    "inputSchema": {
        "type": "object",
        "required": ["dataRange", "chartType"],
        "properties": {
            "dataRange": {"type": "string", "description": "Cell range containing chart source data (e.g. 'A1:D10')."},
            "chartType": {"type": "string", "enum": ["ColumnClustered", "ColumnStacked", "Line", "LineMarkers", "Pie", "BarClustered", "Area", "Scatter"], "description": "Chart type to create."},
            "title": {"type": "string", "description": "Chart title text."},
            "sheet": {"type": "string", "description": "Worksheet name. Defaults to active sheet."},
            "workbook": {"type": "string", "description": "Workbook name or file path. Defaults to active workbook."},
            "targetCell": {"type": "string", "default": "E2", "description": "Top-left cell where the chart should be placed."},
            "width": {"type": "number", "default": 400, "description": "Chart width in points."},
            "height": {"type": "number", "default": 250, "description": "Chart height in points."},
            "hasLegend": {"type": "boolean", "default": True}
        },
        "additionalProperties": False
    }
},
r'''
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
''',
[
    {"title": "Create column chart", "args": {"dataRange": "A1:B10", "chartType": "ColumnClustered", "title": "Monthly Steel Usage", "targetCell": "D2"}},
    {"title": "Create pie chart", "args": {"dataRange": "A1:B5", "chartType": "Pie", "title": "Bar Size Distribution", "targetCell": "F2", "width": 350, "height": 250}}
])

# -------------------------------------------------------------------------------------------------- Seed 10: evaluate_formula
seed("Calculation", "evaluate_formula", {
    "transaction": "none",
    "timeoutSeconds": 30,
    "destructive": False,
    "tags": ["formula", "evaluate", "calculate", "expression"],
    "title": "Evaluate formula",
    "description": "Evaluate an Excel formula expression dynamically in the context of the active worksheet or workbook. Returns evaluated value and data type. Read-only.",
    "notes": "Uses Application.Evaluate or Worksheet.Evaluate.",
    "inputSchema": {
        "type": "object",
        "required": ["formula"],
        "properties": {
            "formula": {"type": "string", "description": "Excel formula string (e.g. 'SUM(A1:A10)', 'VLOOKUP(\"Rebar\", B1:F20, 3, FALSE)'). Leading '=' optional."},
            "sheet": {"type": "string", "description": "Context worksheet. Defaults to active sheet."},
            "workbook": {"type": "string", "description": "Workbook name or file path. Defaults to active workbook."}
        },
        "additionalProperties": False
    }
},
r'''
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
''',
[
    {"title": "Calculate sum of range", "args": {"formula": "SUM(B2:B20)"}},
    {"title": "Lookup value in table", "args": {"formula": "VLOOKUP(\"ColumnC1\", A1:C50, 2, FALSE)", "sheet": "Data"}}
])

# -------------------------------------------------------------------------------------------------- Seed 11: export_worksheet
seed("Export", "export_worksheet", {
    "transaction": "auto",
    "timeoutSeconds": 60,
    "destructive": True,
    "tags": ["export", "pdf", "csv", "publish"],
    "title": "Export worksheet",
    "description": "Export a worksheet or entire workbook to PDF or CSV format on disk. Returns generated file path and size. Takes an automatic snapshot before export.",
    "notes": "Uses Worksheet.ExportAsFixedFormat for PDF and SaveAs with xlCSV for CSV export.",
    "inputSchema": {
        "type": "object",
        "required": ["format"],
        "properties": {
            "format": {"type": "string", "enum": ["pdf", "csv"], "description": "Export format: 'pdf' or 'csv'."},
            "outputPath": {"type": "string", "description": "Target destination file path. If omitted, exports beside the workbook."},
            "sheet": {"type": "string", "description": "Worksheet to export. Required for CSV; optional for PDF (exports full workbook if omitted)."},
            "workbook": {"type": "string", "description": "Workbook name or file path. Defaults to active workbook."},
            "landscape": {"type": "boolean", "default": False, "description": "Set orientation to landscape (PDF only)."},
            "fitToPage": {"type": "boolean", "default": True, "description": "Fit content to 1 page wide (PDF only)."}
        },
        "additionalProperties": False
    }
},
r'''
string format = args.Require("format").ToLowerInvariant();
string outputPath = args.Str("outputPath", null);
string sheetName = args.Str("sheet", null);
string wbName = args.Str("workbook", null);
bool landscape = args.Bool("landscape", false);
bool fitToPage = args.Bool("fitToPage", true);

var wb = string.IsNullOrEmpty(wbName) ? workbook : excel.Workbooks[wbName];
string wbPath = (string)wb.Path;
string defaultWbName = (string)wb.Name;
if (string.IsNullOrEmpty(outputPath))
{
    string folder = string.IsNullOrEmpty(wbPath) ? System.IO.Path.GetTempPath() : wbPath;
    string baseName = System.IO.Path.GetFileNameWithoutExtension(defaultWbName);
    string ext = format == "pdf" ? ".pdf" : ".csv";
    string tag = !string.IsNullOrEmpty(sheetName) ? $"_{sheetName}" : "";
    outputPath = System.IO.Path.Combine(folder, $"{baseName}{tag}_{DateTime.Now:yyyyMMdd_HHmmss}{ext}");
}

var targetObj = !string.IsNullOrEmpty(sheetName) ? wb.Worksheets[sheetName] : wb;

if (format == "pdf")
{
    if (!string.IsNullOrEmpty(sheetName))
    {
        var ws = wb.Worksheets[sheetName];
        if (landscape) ws.PageSetup.Orientation = 2; // xlLandscape
        if (fitToPage)
        {
            ws.PageSetup.Zoom = false;
            ws.PageSetup.FitToPagesWide = 1;
            ws.PageSetup.FitToPagesTall = false;
        }
    }
    targetObj.ExportAsFixedFormat(0, outputPath, 0, true, false, Type.Missing, Type.Missing, false, Type.Missing); // xlTypePDF (0), xlQualityStandard (0)
}
else if (format == "csv")
{
    if (string.IsNullOrEmpty(sheetName)) sheetName = (string)wb.ActiveSheet.Name;
    var ws = wb.Worksheets[sheetName];
    ws.Copy();
    var tempWb = excel.ActiveWorkbook;
    excel.DisplayAlerts = false;
    try
    {
        tempWb.SaveAs(outputPath, 6); // xlCSV (6)
    }
    finally
    {
        tempWb.Close(false);
        excel.DisplayAlerts = true;
    }
}

log($"Exported to {format.ToUpperInvariant()} at {outputPath}");
return new
{
    success = true,
    format,
    filePath = outputPath,
    sheet = sheetName,
    summary = $"Exported to {format.ToUpperInvariant()} at {outputPath}; snapshot saved"
};
''',
[
    {"title": "Export sheet to PDF", "args": {"format": "pdf", "sheet": "Summary", "landscape": True, "fitToPage": True}},
    {"title": "Export data sheet to CSV", "args": {"format": "csv", "sheet": "RawData"}}
])

# -------------------------------------------------------------------------------------------------- Seed 12: run_macro
seed("Automation", "run_macro", {
    "transaction": "auto",
    "timeoutSeconds": 120,
    "destructive": True,
    "tags": ["macro", "vba", "run", "destructive"],
    "title": "Run macro",
    "description": "DESTRUCTIVE: Execute an existing VBA macro / Sub procedure in the active workbook with optional arguments. Requires explicit Destructive toggle enabled in Bridge UI. Takes an automatic snapshot before running.",
    "notes": "Uses Application.Run. Only available in COM mode with running Excel instance.",
    "inputSchema": {
        "type": "object",
        "required": ["macroName"],
        "properties": {
            "macroName": {"type": "string", "description": "Name of the VBA macro to execute (e.g. 'RefreshAllData', 'Module1.ProcessSchedule')."},
            "args": {"type": "array", "description": "Optional parameters to pass to the macro.", "items": {"type": "string"}},
            "workbook": {"type": "string", "description": "Workbook name or file path containing the macro. Defaults to active workbook."}
        },
        "additionalProperties": False
    }
},
r'''
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
''',
[
    {"title": "Run macro without parameters", "args": {"macroName": "UpdateCalculations"}},
    {"title": "Run macro with arguments", "args": {"macroName": "GenerateReport", "args": ["Q3", True]}}
])

print("All 12 Excel seed tools generated successfully.")
