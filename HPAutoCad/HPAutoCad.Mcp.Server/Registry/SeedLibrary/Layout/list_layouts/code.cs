bool includeModel = args.Bool("includeModel", true);
string current = LayoutManager.Current.CurrentLayout;

var dictionary = (DBDictionary)tr.GetObject(db.LayoutDictionaryId, OpenMode.ForRead);
var layouts = new List<Layout>();
foreach (DBDictionaryEntry entry in dictionary) layouts.Add((Layout)tr.GetObject(entry.Value, OpenMode.ForRead));

var result = layouts
    .Where(l => includeModel || !l.ModelType)
    .OrderBy(l => l.TabOrder)
    .Select(l => new
    {
        name = l.LayoutName,
        tabOrder = l.TabOrder,
        isModel = l.ModelType,
        // the first viewport of a paper-space layout is the paper itself
        viewportCount = l.ModelType ? 0 : Math.Max(0, l.GetViewports().Count - 1),
        isCurrent = string.Equals(l.LayoutName, current, StringComparison.OrdinalIgnoreCase),
    })
    .ToList();
log($"{result.Count} layouts, current = {current}");
return result;
