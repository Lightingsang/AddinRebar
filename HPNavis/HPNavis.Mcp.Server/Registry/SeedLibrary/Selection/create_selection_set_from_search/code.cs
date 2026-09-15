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

string name = args.Require("name");
string category = args.Require("category");
string property = args.Require("property");
string op = args.Str("op", "equals");
string value = args.Require("value");

var search = new Search();
search.Selection.SelectAll();
search.Locations = SearchLocations.DescendantsAndSelf;
search.PruneBelowMatch = true;
search.SearchConditions.Add(Condition(category, property, op, value));

var matches = search.FindAll(doc, false);
var set = new SelectionSet(search) { DisplayName = name }; // a search set: re-evaluated by Navisworks, not a frozen list
doc.SelectionSets.AddCopy(set);
var added = doc.SelectionSets.Value.OfType<SelectionSet>().Last(s => s.DisplayName == name);

log($"search set '{name}' created; {matches.Count} item(s) match now");
return new { name, guid = added.Guid, estimatedCount = Math.Min(matches.Count, 1000), kind = "search" };
