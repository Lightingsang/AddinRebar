// Golden-run fixture generator — builds column-stack-2-storey.rvt from the out-of-the-box metric structural template.
// Run through the Revit MCP: execute_revit_code, transaction "manual" (the script owns the new document's transaction).
// args: runDir (the active document and the output must both live under it — never a user model),
//       template (.rte), library (Revit's English\US family library), out (.rvt to write, under runDir).
// Content: Fixtures/README.md (area A, column stack + foundation + beam), a beam run (area B, x + 20 m) and a
// foundation slab (area C, y + 20 m). Every element the golden runs and the TUnit tests find carries a Mark.

string runDir = args.Str("runDir", "");
string activePath = doc.PathName ?? "";
string outPath = args.Str("out", "");
if (runDir.Length == 0
    || !activePath.StartsWith(runDir, StringComparison.OrdinalIgnoreCase)
    || !outPath.StartsWith(runDir, StringComparison.OrdinalIgnoreCase))
{
    throw new ArgumentException("Refusing to run: the active document '" + activePath + "' and the output '" + outPath
        + "' must both be under the golden-run folder '" + runDir + "'.");
}

string template = args.Require("template");
string library = args.Require("library").TrimEnd('\\') + "\\";

Func<double, double> ft = mm => UnitUtils.ConvertToInternalUnits(mm, UnitTypeId.Millimeters);
Func<double, double> mmOf = feet => Math.Round(UnitUtils.ConvertFromInternalUnits(feet, UnitTypeId.Millimeters), 1);
Func<double, double, double, XYZ> P = (x, y, z) => new XYZ(ft(x), ft(y), ft(z));

var fixture = app.NewProjectDocument(template);
var problems = new List<string>();
var report = new Dictionary<string, object>();

try
{
    using (var t = new Transaction(fixture, "Build golden-run fixture"))
    {
        t.Start();

        // Levels: Foundation 0 / Level 1 3000 / Level 2 6000, each with a structural plan.
        var wantedLevels = new[] { ("Foundation", 0.0), ("Level 1", 3000.0), ("Level 2", 6000.0) };
        var existing = new FilteredElementCollector(fixture).OfClass(typeof(Level)).Cast<Level>()
            .OrderBy(l => l.Elevation).ToList();
        report["templateLevels"] = existing.Select(l => l.Name + " " + mmOf(l.Elevation)).ToList();
        var levels = new List<Level>();
        for (int i = 0; i < wantedLevels.Length; i++)
        {
            var level = i < existing.Count ? existing[i] : Level.Create(fixture, ft(wantedLevels[i].Item2));
            level.Name = "tmp-level-" + i;
            levels.Add(level);
        }

        var extraLevels = existing.Skip(wantedLevels.Length).Select(l => l.Id).ToList();
        if (extraLevels.Count > 0) fixture.Delete(extraLevels);

        for (int i = 0; i < wantedLevels.Length; i++)
        {
            levels[i].Name = wantedLevels[i].Item1;
            levels[i].Elevation = ft(wantedLevels[i].Item2);
        }

        Level foundationLevel = levels[0], level1 = levels[1], level2 = levels[2];

        var planType = new FilteredElementCollector(fixture).OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>()
            .First(v => v.ViewFamily == ViewFamily.StructuralPlan);
        var plans = new FilteredElementCollector(fixture).OfClass(typeof(ViewPlan)).Cast<ViewPlan>()
            .Where(v => !v.IsTemplate && v.ViewType == ViewType.EngineeringPlan).ToList();
        var planByLevel = new Dictionary<string, ViewPlan>();
        foreach (var level in levels)
        {
            var plan = plans.FirstOrDefault(v => v.GenLevel != null && v.GenLevel.Id == level.Id)
                ?? ViewPlan.Create(fixture, planType.Id, level.Id);
            planByLevel[level.Name] = plan;
        }

        // The template's plans keep their old level names after the levels move; name each plan after its level.
        foreach (var pair in planByLevel) pair.Value.Name = "tmp-plan-" + pair.Key;
        foreach (var pair in planByLevel) pair.Value.Name = pair.Key;

        // Families.
        Func<string, string, Family> load = (relative, name) =>
        {
            Family family;
            if (fixture.LoadFamily(library + relative, out family)) return family;
            family = new FilteredElementCollector(fixture).OfClass(typeof(Family)).Cast<Family>()
                .FirstOrDefault(f => f.Name == name);
            if (family == null) throw new InvalidOperationException("Could not load " + library + relative);
            return family;
        };

        var columnFamily = load(@"Structural Columns\Concrete\M_Concrete-Rectangular-Column.rfa", "M_Concrete-Rectangular-Column");
        var beamFamily = load(@"Structural Framing\Concrete\M_Concrete-Rectangular Beam.rfa", "M_Concrete-Rectangular Beam");
        var footingFamily = load(@"Structural Foundations\M_Footing-Rectangular.rfa", "M_Footing-Rectangular");
        foreach (var shape in new[] { "M_T1", "M_T3", "M_10" })
        {
            load(@"Structural Rebar Shapes\" + shape + ".rfa", shape);
        }

        // Types sized by their type parameters.
        Func<Family, string, (string Param, double Mm)[], FamilySymbol> sized = (family, name, sizes) =>
        {
            var symbols = family.GetFamilySymbolIds().Select(id => (FamilySymbol)fixture.GetElement(id)).ToList();
            var symbol = symbols.FirstOrDefault(s => s.Name == name) ?? (FamilySymbol)symbols[0].Duplicate(name);
            foreach (var (param, mm) in sizes)
            {
                var p = symbol.LookupParameter(param);
                if (p == null)
                {
                    throw new InvalidOperationException(family.Name + " has no type parameter '" + param + "'; it has: "
                        + string.Join(", ", symbol.Parameters.Cast<Parameter>().Select(x => x.Definition.Name).OrderBy(x => x)));
                }

                p.Set(ft(mm));
            }

            if (!symbol.IsActive) symbol.Activate();
            return symbol;
        };

        var col400x600 = sized(columnFamily, "400 x 600mm", new[] { ("b", 400.0), ("h", 600.0) });
        var col300x500 = sized(columnFamily, "300 x 500mm", new[] { ("b", 300.0), ("h", 500.0) });
        var col400x400 = sized(columnFamily, "400 x 400mm", new[] { ("b", 400.0), ("h", 400.0) });
        var beam300x500 = sized(beamFamily, "300 x 500mm", new[] { ("b", 300.0), ("h", 500.0) });
        var beam300x600 = sized(beamFamily, "300 x 600mm", new[] { ("b", 300.0), ("h", 600.0) });
        var beam250x450 = sized(beamFamily, "250 x 450mm", new[] { ("b", 250.0), ("h", 450.0) });
        var footing = sized(footingFamily, "8000 x 1600 x 600mm",
            new[] { ("Width", 8000.0), ("Length", 1600.0), ("Foundation Thickness", 600.0) });

        var marked = new Dictionary<string, Element>();
        Action<Element, string> mark = (element, value) =>
        {
            element.get_Parameter(BuiltInParameter.ALL_MODEL_MARK).Set(value);
            marked[value] = element;
        };

        Func<string, FamilySymbol, double, double, Level, Level, double, FamilyInstance> column =
            (name, symbol, x, y, baseLevel, topLevel, topOffsetMm) =>
            {
                var instance = fixture.Create.NewFamilyInstance(P(x, y, 0), symbol, baseLevel, StructuralType.Column);
                instance.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_PARAM).Set(baseLevel.Id);
                instance.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM).Set(0.0);
                instance.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM).Set(topLevel.Id);
                instance.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM).Set(ft(topOffsetMm));
                mark(instance, name);
                return instance;
            };

        Func<string, FamilySymbol, XYZ, XYZ, FamilyInstance> beam = (name, symbol, start, end) =>
        {
            var instance = fixture.Create.NewFamilyInstance(Line.CreateBound(start, end), symbol, level1, StructuralType.Beam);
            mark(instance, name);
            return instance;
        };

        // Area A — the README stack.
        var c1Lower = column("C1-LOWER", col400x600, 0, 0, foundationLevel, level1, 0);
        var c1Upper = column("C1-UPPER", col300x500, 0, 0, level1, level2, 0);
        var slanted = fixture.Create.NewFamilyInstance(
            Line.CreateBound(P(3000, 0, 0), P(3600, 0, 3000)), col400x600, foundationLevel, StructuralType.Column);
        mark(slanted, "C-SLANTED");
        column("C2-DETACHED", col300x500, 0, 4000, level2, level2, 3000);
        var c3Lower = column("C3-LOWER", col300x500, 6000, 0, foundationLevel, level1, 0);
        column("C3-WIDER", col400x600, 6000, 0, level1, level2, 0);

        var foundation = fixture.Create.NewFamilyInstance(P(3000, 0, 0), footing, foundationLevel, StructuralType.Footing);
        mark(foundation, "FTG-1");
        var bC1 = beam("B-C1", beam300x500, P(0, -4000, 3000), P(0, 0, 3000));

        // Area B — a three-span beam run on Level 1 with a cantilever and a secondary beam.
        var brC1 = column("BR-C1", col400x400, 20000, 0, foundationLevel, level1, 0);
        var brC2 = column("BR-C2", col400x400, 26000, 0, foundationLevel, level1, 0);
        var brC3 = column("BR-C3", col400x400, 31000, 0, foundationLevel, level1, 0);
        var brC4 = column("BR-C4", col400x400, 23000, 3000, foundationLevel, level1, 0);
        var brB1 = beam("BR-B1", beam300x600, P(20000, 0, 3000), P(26000, 0, 3000));
        var brB2 = beam("BR-B2", beam300x600, P(26000, 0, 3000), P(31000, 0, 3000));
        var brCant = beam("BR-CANT", beam300x600, P(31000, 0, 3000), P(32800, 0, 3000));
        var brSec = beam("BR-SEC", beam250x450, P(23000, 0, 3000), P(23000, 3000, 3000));

        // Area C — a structural foundation slab in the Floors category (the Foundation feature takes floors).
        var floorTypes = new FilteredElementCollector(fixture).OfClass(typeof(FloorType)).Cast<FloorType>()
            .Where(f => !f.IsFoundationSlab && f.GetCompoundStructure() != null).ToList();
        var slabType = floorTypes.FirstOrDefault(f => f.Name == "FND 600mm")
            ?? (FloorType)floorTypes.First().Duplicate("FND 600mm");
        var structure = slabType.GetCompoundStructure();
        var layers = structure.GetLayers();
        int core = Enumerable.Range(0, layers.Count).FirstOrDefault(i => layers[i].Function == MaterialFunctionAssignment.Structure);
        double others = Enumerable.Range(0, layers.Count).Where(i => i != core).Sum(i => layers[i].Width);
        structure.SetLayerWidth(core, ft(600) - others);
        slabType.SetCompoundStructure(structure);

        var outline = new CurveLoop();
        var corners = new[] { P(0, 20000, 0), P(6000, 20000, 0), P(6000, 24000, 0), P(0, 24000, 0) };
        for (int i = 0; i < 4; i++) outline.Append(Line.CreateBound(corners[i], corners[(i + 1) % 4]));
        var slab = Floor.Create(fixture, new List<CurveLoop> { outline }, slabType.Id, foundationLevel.Id, true, null, 0.0);
        slab.get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM)?.Set(0.0);
        mark(slab, "FND-SLAB");

        fixture.Regenerate();

        // Joins: the support cuts what rests on it, the column cuts the beam framing into it, the main beam cuts
        // the secondary one. The validators read exactly these join states.
        var joins = new List<string>();
        Action<Element, Element> cutBy = (cutter, cut) =>
        {
            string pair = cutter.get_Parameter(BuiltInParameter.ALL_MODEL_MARK).AsString() + " cuts "
                + cut.get_Parameter(BuiltInParameter.ALL_MODEL_MARK).AsString();
            try
            {
                if (!JoinGeometryUtils.AreElementsJoined(fixture, cutter, cut)) JoinGeometryUtils.JoinGeometry(fixture, cutter, cut);
                if (!JoinGeometryUtils.IsCuttingElementInJoin(fixture, cutter, cut)) JoinGeometryUtils.SwitchJoinOrder(fixture, cutter, cut);
                joins.Add(pair);
            }
            catch (Exception ex)
            {
                problems.Add("join " + pair + ": " + ex.Message);
            }
        };

        cutBy(foundation, c1Lower);
        cutBy(foundation, slanted);
        cutBy(foundation, c3Lower);
        cutBy(c1Lower, bC1);
        cutBy(brC1, brB1);
        cutBy(brC2, brB1);
        cutBy(brC2, brB2);
        cutBy(brC3, brB2);
        cutBy(brC3, brCant);
        cutBy(brB1, brSec);
        cutBy(brC4, brSec);

        // Bar types D16, D10, D25, D12, D20, D8 in that order (element-id order is not diameter order on purpose).
        var oldBarTypes = new FilteredElementCollector(fixture).OfClass(typeof(RebarBarType)).Select(e => e.Id).ToList();
        report["templateBarTypes"] = oldBarTypes.Select(id => fixture.GetElement(id).Name).ToList();
        var barTypes = new List<RebarBarType>();
        foreach (var d in new[] { 16.0, 10.0, 25.0, 12.0, 20.0, 8.0 })
        {
            var barType = RebarBarType.Create(fixture);
            barType.Name = "tmp-D" + d;
            barType.BarNominalDiameter = ft(d);
            barType.BarModelDiameter = ft(d);
            barType.StandardBendDiameter = ft(4 * d);
            barType.StandardHookBendDiameter = ft(4 * d);
            barType.StirrupTieBendDiameter = ft(4 * d);
            barTypes.Add(barType);
        }

        if (oldBarTypes.Count > 0) fixture.Delete(oldBarTypes);
        foreach (var barType in barTypes) barType.Name = "D" + mmOf(barType.BarNominalDiameter);

        StartingViewSettings.GetStartingViewSettings(fixture).ViewId = planByLevel["Level 1"].Id;

        t.Commit();
        report["joins"] = joins;
    }

    // Checks: every mark, the shapes, six bar types; plan sizes from bounding boxes.
    var expectedMarks = new[]
    {
        "C1-LOWER", "C1-UPPER", "C-SLANTED", "C2-DETACHED", "C3-LOWER", "C3-WIDER", "FTG-1", "B-C1",
        "BR-C1", "BR-C2", "BR-C3", "BR-C4", "BR-B1", "BR-B2", "BR-CANT", "BR-SEC", "FND-SLAB",
    };
    var all = new FilteredElementCollector(fixture).WhereElementIsNotElementType().ToElements()
        .Where(e => e.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() is string m && m.Length > 0)
        .ToDictionary(e => e.get_Parameter(BuiltInParameter.ALL_MODEL_MARK).AsString(), e => e);
    problems.AddRange(expectedMarks.Where(m => !all.ContainsKey(m)).Select(m => "missing mark " + m));

    var shapes = new FilteredElementCollector(fixture).OfClass(typeof(RebarShape)).Cast<RebarShape>().Select(s => s.Name).ToList();
    problems.AddRange(new[] { "M_T1", "M_T3", "M_10" }.Where(s => !shapes.Contains(s)).Select(s => "missing shape " + s));

    var bars = new FilteredElementCollector(fixture).OfClass(typeof(RebarBarType)).Cast<RebarBarType>()
        .OrderBy(b => b.Id.Value)
        .Select(b => b.Name).ToList();
    if (string.Join(",", bars) != "D16,D10,D25,D12,D20,D8") problems.Add("bar types are " + string.Join(",", bars));

    report["boxes"] = expectedMarks.Where(all.ContainsKey).ToDictionary(m => m, m =>
    {
        var box = all[m].get_BoundingBox(null);
        return box == null ? "(no box)" : string.Join(" ", mmOf(box.Min.X), mmOf(box.Min.Y), mmOf(box.Min.Z), "..",
            mmOf(box.Max.X), mmOf(box.Max.Y), mmOf(box.Max.Z));
    });
    report["levels"] = new FilteredElementCollector(fixture).OfClass(typeof(Level)).Cast<Level>()
        .OrderBy(l => l.Elevation).Select(l => l.Name + " " + mmOf(l.Elevation)).ToList();
    report["barTypes"] = bars;
    report["plans"] = new FilteredElementCollector(fixture).OfClass(typeof(ViewPlan)).Cast<ViewPlan>()
        .Where(v => !v.IsTemplate && v.GenLevel != null)
        .Select(v => v.ViewType + ": " + v.Name + " @ " + v.GenLevel.Name).OrderBy(x => x).ToList();
    report["warnings"] = fixture.GetWarnings().Select(w => w.GetDescriptionText()).GroupBy(x => x)
        .ToDictionary(g => g.Key, g => g.Count());

    if (problems.Count > 0 || args.Bool("dryRun", false))
    {
        report["problems"] = problems;
        report["saved"] = false;
        return report;
    }

    fixture.SaveAs(outPath, new SaveAsOptions { OverwriteExistingFile = true, Compact = true, MaximumBackups = 1 });
    report["saved"] = outPath;
    report["revitBuild"] = app.VersionBuild;
    return report;
}
finally
{
    fixture.Close(false);
}
