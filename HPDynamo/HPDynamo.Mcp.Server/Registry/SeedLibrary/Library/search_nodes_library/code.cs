string searchQuery = args.Require("query");
if (string.IsNullOrWhiteSpace(searchQuery))
    throw new ArgumentException("Search query cannot be empty.");

string categoryFilter = args.Str("category", "");
int maximumResults = Math.Clamp(args.Int("maxResults", 50), 1, 200);

var dynamoModel = (dynamo as Dynamo.ViewModels.DynamoViewModel)?.Model ?? (dynamo as DynamoModel);

var matchingEntries = new List<object>();

if (dynamoModel?.SearchModel == null)
{
    log($"Dynamo SearchModel is not available; returning 0 results for '{searchQuery}'");
    return new
    {
        query = searchQuery,
        categoryFilter = string.IsNullOrEmpty(categoryFilter) ? null : categoryFilter,
        totalFound = 0,
        returnedCount = 0,
        results = matchingEntries,
        summary = $"Found 0 Dynamo node types matching query '{searchQuery}'."
    };
}

foreach (var searchEntry in dynamoModel.SearchModel.Entries)
{
    ct.ThrowIfCancellationRequested();
    if (matchingEntries.Count >= maximumResults) break;

    string entryName = searchEntry.Name ?? "";
    string entryFullName = searchEntry.FullName ?? "";
    string entryDescription = searchEntry.Description ?? "";

    bool matchesQueryText = entryName.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) >= 0
        || entryFullName.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) >= 0
        || entryDescription.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) >= 0;

    if (!matchesQueryText) continue;

    bool hasCategoryFilter = !string.IsNullOrEmpty(categoryFilter);
    if (hasCategoryFilter)
    {
        bool inFullName = entryFullName.IndexOf(categoryFilter, StringComparison.OrdinalIgnoreCase) >= 0;
        bool inCategories = searchEntry.Categories != null && searchEntry.Categories.Any(c => c.IndexOf(categoryFilter, StringComparison.OrdinalIgnoreCase) >= 0);
        if (!inFullName && !inCategories) continue;
    }

    string categoryHierarchy = string.Join(" > ", searchEntry.Categories ?? Enumerable.Empty<string>());

    matchingEntries.Add(new
    {
        name = entryName,
        fullName = entryFullName,
        creationName = searchEntry.CreationName ?? entryFullName,
        category = categoryHierarchy,
        description = entryDescription,
        assembly = searchEntry.FullName ?? "DynamoCore"
    });
}

log($"Node library search for '{searchQuery}' returned {matchingEntries.Count} matches");

return new
{
    query = searchQuery,
    categoryFilter = string.IsNullOrEmpty(categoryFilter) ? null : categoryFilter,
    totalFound = matchingEntries.Count,
    returnedCount = matchingEntries.Count,
    results = matchingEntries,
    summary = $"Found {matchingEntries.Count} Dynamo node types matching query '{searchQuery}'."
};
