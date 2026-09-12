int limit = args.Int("limit", 100);
var ids = uidoc.Selection.GetElementIds().ToList();
var elements = ids.Select(id => doc.GetElement(id)).Where(e => e != null).ToList();
if (limit > 0 && elements.Count > limit) elements = elements.Take(limit).ToList();
return new
{
    selectedCount = ids.Count,
    returned = elements.Count,
    elements = elements.Select(e =>
    {
        var typeElement = doc.GetElement(e.GetTypeId()) as ElementType;
        return new
        {
            id = e.Id.Value,
            uniqueId = e.UniqueId,
            name = e.Name,
            category = e.Category?.Name,
            typeName = typeElement?.Name,
            familyName = typeElement?.FamilyName,
            level = (doc.GetElement(e.LevelId) as Level)?.Name,
        };
    }).ToList(),
};
