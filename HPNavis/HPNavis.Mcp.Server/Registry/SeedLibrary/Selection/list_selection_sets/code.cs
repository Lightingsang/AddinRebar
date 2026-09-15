bool includeCounts = args.Bool("includeCounts", false);
var rows = new List<object>();

void Walk(SavedItem item, string path)
{
    ct.ThrowIfCancellationRequested();
    var here = path.Length == 0 ? item.DisplayName : path + " / " + item.DisplayName;
    if (item is SelectionSet set)
    {
        rows.Add(new
        {
            name = set.DisplayName,
            kind = set.HasSearch ? "search" : "explicit",
            guid = set.Guid,
            path = here,
            count = includeCounts ? (int?)set.GetSelectedItems(doc).Count : null,
        });
    }
    else if (item is GroupItem group)
    {
        rows.Add(new { name = group.DisplayName, kind = "folder", guid = group.Guid, path = here, count = (int?)null });
        foreach (var child in group.Children) Walk(child, here);
    }
}

foreach (var item in doc.SelectionSets.Value) Walk(item, "");
log($"{rows.Count} entries");
return rows;
