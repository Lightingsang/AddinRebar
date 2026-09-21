string filter = args.Str("drawingType", "ALL").ToUpperInvariant();
int limit = Math.Max(1, Math.Min(args.Int("limit", 50), 200));

var dh = new Tekla.Structures.Drawing.DrawingHandler();
var drawings = dh.GetDrawings();
var list = new List<object>();

if (drawings != null)
{
    while (drawings.MoveNext() && list.Count < limit)
    {
        var dwg = drawings.Current;
        if (dwg == null) continue;
        string dwgTypeName = dwg.GetType().Name.Replace("Drawing", "").ToUpperInvariant();

        if (filter != "ALL" && !dwgTypeName.Contains(filter)) continue;

        list.Add(new
        {
            name = dwg.Name,
            title1 = dwg.Title1,
            title2 = dwg.Title2,
            type = dwgTypeName,
            mark = dwg.Mark,
            upToDate = dwg.UpToDateStatus.ToString()
        });
    }
}

log($"Found {list.Count} drawings (type filter: {filter})");

return new
{
    success = true,
    count = list.Count,
    filter = filter,
    drawings = list
};
