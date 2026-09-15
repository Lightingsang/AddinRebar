int maxItems = Math.Min(50, Math.Max(1, args.Int("maxItems", 5)));
string onlyCategory = args.Str("category", "") ?? "";

// lengths in mm, everything else as text — ToDisplayString throws for anything that is not a display string
string Describe(VariantData value)
{
    if (value == null || value.IsNone) return "";
    if (value.IsDisplayString) return value.ToDisplayString();
    if (value.IsIdentifierString) return value.ToIdentifierString();
    if (value.IsDoubleLength) return Math.Round(units.ToMm(value.ToDoubleLength()), 2).ToString(System.Globalization.CultureInfo.InvariantCulture) + " mm";
    if (value.IsAnyDouble) return value.ToAnyDouble().ToString(System.Globalization.CultureInfo.InvariantCulture);
    if (value.IsInt32) return value.ToInt32().ToString(System.Globalization.CultureInfo.InvariantCulture);
    if (value.IsBoolean) return value.ToBoolean() ? "true" : "false";
    if (value.IsDateTime) return value.ToDateTime().ToString("s");
    return value.ToString();
}

var selected = doc.CurrentSelection.SelectedItems;
if (selected.Count == 0) return new { count = 0, note = "nothing is selected in Navisworks" };

var items = new List<object>();
foreach (var item in selected.Take(maxItems))
{
    ct.ThrowIfCancellationRequested();
    var categories = new List<object>();
    foreach (var category in item.PropertyCategories)
    {
        if (onlyCategory.Length > 0 && !string.Equals(category.DisplayName, onlyCategory, StringComparison.OrdinalIgnoreCase)) continue;
        categories.Add(new
        {
            category = category.DisplayName,
            properties = category.Properties.Select(p => new { name = p.DisplayName, value = Describe(p.Value) }).ToList(),
        });
    }
    items.Add(new { name = item.DisplayName, className = item.ClassDisplayName, guid = item.InstanceGuid, categories });
}

log($"{selected.Count} selected, {items.Count} described");
return new { count = selected.Count, shown = items.Count, items };
