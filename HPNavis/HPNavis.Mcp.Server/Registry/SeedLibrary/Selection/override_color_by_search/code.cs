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
bool reset = args.Bool("reset", false);

var search = new Search();
search.Selection.SelectAll();
search.Locations = SearchLocations.DescendantsAndSelf;
search.PruneBelowMatch = true;
search.SearchConditions.Add(Condition(category, property, op, value));
var items = search.FindAll(doc, false);
if (items.Count == 0) return new { count = 0, note = "no item matched; nothing changed" };

if (reset)
{
    doc.Models.ResetPermanentMaterials(items);
    log($"overrides removed from {items.Count} item(s)");
    return new { count = items.Count, reset = true };
}

byte Channel(int value) => (byte)Math.Min(255, Math.Max(0, value));
var color = Color.FromByteRGB(Channel(args.Int("r", 255)), Channel(args.Int("g", 0)), Channel(args.Int("b", 0)));
doc.Models.OverridePermanentColor(items, color);
var transparency = args.DoubleOrNull("transparency");
if (transparency.HasValue) doc.Models.OverridePermanentTransparency(items, Math.Min(1.0, Math.Max(0.0, transparency.Value)));

log($"{items.Count} item(s) coloured");
return new { count = items.Count, reset = false, transparency };
