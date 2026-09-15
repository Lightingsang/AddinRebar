bool includeRootItems = args.Bool("includeRootItems", false);
if (doc.IsClear) return new { isClear = true, note = "no model is open in Navisworks" };

var models = doc.Models.Select(m => new
{
    fileName = m.FileName,
    sourceFileName = m.SourceFileName,
    units = m.Units.ToString(),
    guid = m.Guid,
}).ToList();
var roots = doc.Models.RootItems.ToList();
var rootItems = includeRootItems ? roots.Select(r => new { name = r.DisplayName, className = r.ClassDisplayName, children = r.Children.Count() }).ToList() : null;

log($"{models.Count} model(s), {roots.Count} root item(s), units {doc.Units}");
return new
{
    title = doc.Title,
    fileName = doc.FileName,
    documentUnits = doc.Units.ToString(),
    isModified = doc.IsModified,
    navisworksYear = app.Year,
    hasClashModule = app.HasClashModule,
    modelCount = doc.Models.Count,
    rootItemCount = roots.Count,
    models,
    rootItems,
};
