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

var sheetsToSearch = new List<Worksheet>();
if (searchAllSheets)
{
    foreach (Worksheet s in wb.Worksheets) sheetsToSearch.Add(s);
}
else
{
    sheetsToSearch.Add((Worksheet)(string.IsNullOrEmpty(sheetName) ? wb.ActiveSheet : wb.Worksheets[sheetName]));
}

var matches = new List<object>();
foreach (Worksheet ws in sheetsToSearch)
{
    var used = ws.UsedRange;
    if (used == null) continue;

    var current = used.Find(query, Type.Missing, lookIn, lookAt, XlSearchOrder.xlByRows, XlSearchDirection.xlNext, false, Type.Missing, Type.Missing);
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
