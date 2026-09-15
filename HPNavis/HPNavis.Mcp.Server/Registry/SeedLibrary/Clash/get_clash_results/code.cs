string testName = args.Str("testName", "") ?? "";
int maxResults = Math.Min(1000, Math.Max(0, args.Int("maxResults", 100)));
if (!app.HasClashModule) throw new InvalidOperationException("Clash Detective is not available in this Navisworks (Simulate); results need Navisworks Manage.");

IEnumerable<ClashResult> Results(SavedItem item)
{
    if (item is ClashResult result) return new[] { result };
    if (item is GroupItem group) return group.Children.SelectMany(Results); // result groups nest one level
    return Enumerable.Empty<ClashResult>();
}

var tests = doc.GetClash().TestsData.Tests.OfType<ClashTest>()
    .Where(t => testName.Length == 0 || string.Equals(t.DisplayName, testName, StringComparison.OrdinalIgnoreCase))
    .ToList();

// counts for every test come first: a big model can exceed the result bound, and the summary must survive the cut
var summaries = new List<object>();
var details = new List<object>();
foreach (var test in tests)
{
    ct.ThrowIfCancellationRequested();
    var results = test.Children.SelectMany(Results).ToList();
    summaries.Add(new
    {
        name = test.DisplayName,
        status = test.Status.ToString(),
        testType = test.TestType.ToString(),
        toleranceMm = Math.Round(units.ToMm(test.Tolerance), 2),
        lastRun = test.LastRun,
        resultCount = results.Count,
        byStatus = results.GroupBy(r => r.Status.ToString()).ToDictionary(g => g.Key, g => g.Count()),
    });
    details.AddRange(results.Take(maxResults).Select(r => new
    {
        test = test.DisplayName,
        name = r.DisplayName,
        status = r.Status.ToString(),
        distanceMm = Math.Round(units.ToMm(r.Distance), 2),
        centerMm = r.Center,
        item1 = r.Item1 == null ? null : r.Item1.DisplayName,
        item2 = r.Item2 == null ? null : r.Item2.DisplayName,
    }));
}

log($"{tests.Count} test(s), {details.Count} result(s) listed");
return new { testCount = tests.Count, tests = summaries, results = details };
