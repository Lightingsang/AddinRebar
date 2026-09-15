string groupBy = (args.Str("groupBy", "") ?? "").Trim();
int maxGroups = Math.Min(5000, Math.Max(1, args.Int("maxGroups", 200)));
int maxItems = Math.Max(1, args.Int("maxItems", 200000));

string category = null, property = null;
if (groupBy.Length > 0)
{
    var dot = groupBy.IndexOf('.');
    if (dot <= 0 || dot == groupBy.Length - 1) throw new ArgumentException("groupBy must be 'Category.Property', e.g. Item.Type");
    category = groupBy.Substring(0, dot);
    property = groupBy.Substring(dot + 1);
}

var counts = new Dictionary<string, int>(StringComparer.Ordinal);
var visited = 0;
foreach (var item in doc.Models.RootItems.SelectMany(r => r.DescendantsAndSelf).Take(maxItems))
{
    if ((++visited & 1023) == 0) ct.ThrowIfCancellationRequested();
    string key;
    if (category == null) key = item.ClassDisplayName;
    else
    {
        var data = item.PropertyCategories.FindPropertyByDisplayName(category, property);
        if (data == null) continue; // items without that property are not a group
        key = data.Value.IsDisplayString ? data.Value.ToDisplayString() : data.Value.ToString(); // ToDisplayString throws for other types
    }
    counts[key] = counts.TryGetValue(key, out var n) ? n + 1 : 1;
}

var groups = counts.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key, StringComparer.Ordinal).Take(maxGroups).Select(kv => new { key = kv.Key, count = kv.Value }).ToList();
log($"{visited} items visited, {counts.Count} group(s)");
return new { groupBy = category == null ? "class" : groupBy, itemsVisited = visited, complete = visited < maxItems, groupCount = counts.Count, groups };
