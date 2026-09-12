double Ft(double mm) => UnitUtils.ConvertToInternalUnits(mm, UnitTypeId.Millimeters);
double Mm(double ft) => Math.Round(UnitUtils.ConvertFromInternalUnits(ft, UnitTypeId.Millimeters), 1);

var existing = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().ToList();
ViewFamilyType PlanType(ViewFamily family) => new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>().FirstOrDefault(v => v.ViewFamily == family);

var created = new List<object>();
var warnings = new List<string>();
foreach (var item in args.List("data"))
{
    ct.ThrowIfCancellationRequested();
    string name = item.Require("name");
    double elevationMm = item.RequireDouble("elevation");

    var clash = existing.FirstOrDefault(l => string.Equals(l.Name, name, StringComparison.OrdinalIgnoreCase));
    if (clash != null)
    {
        warnings.Add($"Level '{name}' already exists at {Mm(clash.Elevation)} mm — skipped.");
        continue;
    }

    var level = Level.Create(doc, Ft(elevationMm));
    level.Name = name;
    level.get_Parameter(BuiltInParameter.LEVEL_IS_BUILDING_STORY)?.Set(item.Bool("isBuildingStory", true) ? 1 : 0);
    existing.Add(level);

    var views = new List<string>();
    void Plan(bool wanted, ViewFamily family, string label)
    {
        if (!wanted) return;
        var type = PlanType(family);
        if (type == null) { warnings.Add($"No {label} view family type in this project — no {label} for '{name}'."); return; }
        var view = ViewPlan.Create(doc, type.Id, level.Id);
        views.Add(view.Name);
    }
    Plan(item.Bool("createFloorPlan", true), ViewFamily.FloorPlan, "floor plan");
    Plan(item.Bool("createCeilingPlan", false), ViewFamily.CeilingPlan, "ceiling plan");
    Plan(item.Bool("createStructuralPlan", false), ViewFamily.StructuralPlan, "structural plan");

    created.Add(new { id = level.Id.Value, name = level.Name, elevation = elevationMm, views });
}

return new { created = created.Count, levels = created, warnings };
