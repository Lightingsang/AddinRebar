int fatherId = args.Int("fatherId", 0);
int limit = Math.Max(1, Math.Min(args.Int("limit", 50), 200));

var modelSelector = model.GetModelObjectSelector();
ModelObjectEnumerator enumerator = modelSelector.GetAllObjects();
var list = new List<object>();

while (enumerator.MoveNext() && list.Count < limit)
{
    if (enumerator.Current is Reinforcement rebar)
    {
        if (fatherId > 0 && rebar.Father?.Identifier.ID != fatherId) continue;

        double length = 0.0, weight = 0.0;
        rebar.GetReportProperty("LENGTH", ref length);
        rebar.GetReportProperty("WEIGHT", ref weight);

        string size = "";
        int quantity = 1;
        if (rebar is SingleRebar sr)
        {
            size = sr.Size;
            quantity = 1;
        }
        else if (rebar is BaseRebarGroup group)
        {
            size = group.Size;
            if (group is RebarGroup rg)
            {
                quantity = rg.Polygons.Count > 0 ? (int)Math.Max(1, rg.GetNumberOfRebars()) : 1;
            }
        }

        list.Add(new
        {
            id = rebar.Identifier.ID,
            fatherId = rebar.Father?.Identifier.ID,
            name = rebar.Name,
            size = size,
            grade = rebar.Grade,
            quantity = quantity,
            lengthMm = length,
            weightKg = weight
        });
    }
}

log($"Found {list.Count} reinforcement items (fatherId: {fatherId}, limit: {limit})");

return new
{
    success = true,
    count = list.Count,
    rebars = list
};
