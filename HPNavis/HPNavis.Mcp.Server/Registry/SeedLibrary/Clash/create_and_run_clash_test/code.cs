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
double toleranceMm = Math.Max(0, args.Double("toleranceMm", 0));
string testType = args.Str("testType", "Hard");
if (!app.HasClashModule) throw new InvalidOperationException("Clash Detective is not available in this Navisworks (Simulate); tests need Navisworks Manage.");

ModelItemCollection Find(ScriptArgs side, string label)
{
    var search = new Search();
    search.Selection.SelectAll();
    search.Locations = SearchLocations.DescendantsAndSelf;
    search.PruneBelowMatch = true;
    search.SearchConditions.Add(Condition(side.Require("category"), side.Require("property"), side.Str("op", "equals"), side.Require("value")));
    var items = search.FindAll(doc, false);
    if (items.Count == 0) throw new ArgumentException($"selection {label} matched no item");
    log($"selection {label}: {items.Count} item(s)");
    return items;
}

var itemsA = Find(args.Obj("a"), "a");
var itemsB = Find(args.Obj("b"), "b");
var clash = doc.GetClash().TestsData;
var test = new ClashTest
{
    DisplayName = name,
    TestType = (ClashTestType)Enum.Parse(typeof(ClashTestType), testType, true),
    Tolerance = units.ToDrawing(toleranceMm),
};
test.SelectionA.Selection.CopyFrom(itemsA);
test.SelectionB.Selection.CopyFrom(itemsB);
clash.TestsAddCopy(test);

// the wrapper handed to TestsRunTest is disposed by the run: resolve the stored test again afterwards
var started = DateTime.UtcNow;
clash.TestsRunTest(clash.Tests.OfType<ClashTest>().Last(t => t.DisplayName == name));
var elapsedMs = (long)(DateTime.UtcNow - started).TotalMilliseconds;
var ran = clash.Tests.OfType<ClashTest>().Last(t => t.DisplayName == name);

IEnumerable<ClashResult> Results(SavedItem item) =>
    item is ClashResult result ? new[] { result } : item is GroupItem group ? group.Children.SelectMany(Results) : Enumerable.Empty<ClashResult>();
var results = ran.Children.SelectMany(Results).ToList();

log($"clash test '{name}' ran in {elapsedMs} ms: {results.Count} result(s)");
return new
{
    name,
    guid = ran.Guid,
    status = ran.Status.ToString(),
    elapsedMs,
    itemsA = itemsA.Count,
    itemsB = itemsB.Count,
    resultCount = results.Count,
    byStatus = results.GroupBy(r => r.Status.ToString()).ToDictionary(g => g.Key, g => g.Count()),
};
