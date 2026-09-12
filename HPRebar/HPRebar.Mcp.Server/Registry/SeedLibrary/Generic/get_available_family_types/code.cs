var symbols = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol)).Cast<ElementType>();
var system = new List<ElementType>();
foreach (var t in new[] { typeof(WallType), typeof(FloorType), typeof(RoofType), typeof(CeilingType), typeof(CurtainSystemType) })
    system.AddRange(new FilteredElementCollector(doc).OfClass(t).Cast<ElementType>());
IEnumerable<ElementType> all = symbols.Concat(system);

var categoryIds = args.Strings("categoryList").Select(n => Enum.TryParse<BuiltInCategory>(n, true, out var bic) ? (long)bic : (long?)null).Where(v => v != null).Select(v => v.Value).ToHashSet();
if (categoryIds.Count > 0) all = all.Where(t => t.Category != null && categoryIds.Contains(t.Category.Id.Value));

string filter = args.Str("familyNameFilter");
if (!string.IsNullOrWhiteSpace(filter))
    all = all.Where(t => (t.FamilyName ?? "").IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0 || t.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);

var list = all.OrderBy(t => t.Category?.Name).ThenBy(t => t.FamilyName).ThenBy(t => t.Name).ToList();
int total = list.Count;
int limit = args.Int("limit", 200);
if (limit > 0 && list.Count > limit) list = list.Take(limit).ToList();

return new
{
    total,
    returned = list.Count,
    types = list.Select(t => new
    {
        familyTypeId = t.Id.Value,
        uniqueId = t.UniqueId,
        familyName = t.FamilyName,
        typeName = t.Name,
        category = t.Category?.Name,
        isSystemType = !(t is FamilySymbol),
    }).ToList(),
};
