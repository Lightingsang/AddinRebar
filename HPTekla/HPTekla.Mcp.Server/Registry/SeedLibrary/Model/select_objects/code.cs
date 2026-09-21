string filter = args.Str("typeFilter", "ALL").ToUpperInvariant();
int limit = Math.Max(1, Math.Min(args.Int("limit", 50), 200));
bool setSel = args.Bool("setSelection", false);

var modelSelector = model.GetModelObjectSelector();
ModelObjectEnumerator enumerator;
if (filter == "BEAM" || filter == "COLUMN")
    enumerator = modelSelector.GetAllObjectsWithType(ModelObject.ModelObjectEnum.BEAM);
else if (filter == "CONTOURPLATE")
    enumerator = modelSelector.GetAllObjectsWithType(ModelObject.ModelObjectEnum.CONTOURPLATE);
else if (filter == "REBAR")
    enumerator = modelSelector.GetAllObjectsWithType(ModelObject.ModelObjectEnum.REBARGROUP);
else
    enumerator = modelSelector.GetAllObjects();

var list = new List<object>();
var selectedObjects = new System.Collections.ArrayList();

while (enumerator.MoveNext() && list.Count < limit)
{
    if (enumerator.Current is ModelObject mo)
    {
        string typeStr = mo.GetType().Name;
        string name = "";
        string profile = "";
        if (mo is Part part)
        {
            name = part.Name ?? "";
            profile = part.Profile?.ProfileString ?? "";
        }

        list.Add(new
        {
            id = mo.Identifier.ID,
            guid = mo.Identifier.GUID.ToString(),
            type = typeStr,
            name = name,
            profile = profile
        });

        if (setSel) selectedObjects.Add(mo);
    }
}

if (setSel && selectedObjects.Count > 0)
{
    selector.Select(selectedObjects);
}

log($"Found {list.Count} objects matching filter '{filter}' (limit {limit})");

return new
{
    success = true,
    count = list.Count,
    filter = filter,
    objects = list
};
