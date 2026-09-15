// one property condition: equals (display string), contains, wildcard, gt / lt (numeric); bad input is the caller's error
SearchCondition Condition(string category, string property, string op, string value)
{
    var condition = SearchCondition.HasPropertyByDisplayName(category, property);
    double Number() => double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : throw new ArgumentException($"value '{value}' is not a number (needed for op {op})");
    switch ((op ?? "equals").Trim().ToLowerInvariant())
    {
        case "equals": return condition.EqualValue(VariantData.FromDisplayString(value));
        case "contains": return condition.DisplayStringContains(value);
        case "wildcard": return condition.DisplayStringWildcard(value);
        case "gt": return condition.CompareWith(SearchConditionComparison.NumericGreaterThan, VariantData.FromDouble(Number()));
        case "lt": return condition.CompareWith(SearchConditionComparison.NumericLessThan, VariantData.FromDouble(Number()));
        default: throw new ArgumentException($"op '{op}' is not one of equals, contains, wildcard, gt, lt");
    }
}

string category = args.Require("category");
string property = args.Require("property");
string op = args.Str("op", "equals");
string value = args.Require("value");
int maxResults = Math.Min(1000, Math.Max(1, args.Int("maxResults", 200)));

var search = new Search();
search.Selection.SelectAll();
search.Locations = SearchLocations.DescendantsAndSelf;
search.PruneBelowMatch = true; // a matching item's own descendants are not reported again
search.SearchConditions.Add(Condition(category, property, op, value));

var found = search.FindAll(doc, false);
var items = found.Take(maxResults).Select(i => new
{
    name = i.DisplayName,
    className = i.ClassDisplayName,
    path = string.Join(" / ", i.Ancestors.Select(a => a.DisplayName).Reverse()),
    guid = i.InstanceGuid,
    hasGeometry = i.HasGeometry,
    bboxMm = i.HasGeometry ? i.BoundingBox() : null,
}).ToList();

log($"{found.Count} item(s) match {category}.{property} {op} '{value}'; showing {items.Count}");
return new { total = found.Count, shown = items.Count, items };
