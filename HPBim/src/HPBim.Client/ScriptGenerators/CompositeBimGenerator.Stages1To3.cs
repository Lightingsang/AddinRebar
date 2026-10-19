namespace HPBim.Client.ScriptGenerators;

public sealed partial class CompositeBimGenerator
{
    private static string GetStage1LevelsScript() => """
        // --- STAGE 1: LEVELS ---
        var levelItems = args.List("levels");
        var existingLevels = new FilteredElementCollector(doc)
            .OfClass(typeof(Level))
            .Cast<Level>()
            .ToDictionary(l => l.Name, l => l, StringComparer.OrdinalIgnoreCase);

        var existingViews = new FilteredElementCollector(doc)
            .OfClass(typeof(ViewPlan))
            .Cast<ViewPlan>()
            .ToList();
        var existingViewLevelIds = existingViews
            .Where(v => v.GenLevel != null)
            .Select(v => v.GenLevel.Id)
            .ToHashSet();

        ViewFamilyType planType = new FilteredElementCollector(doc)
            .OfClass(typeof(ViewFamilyType))
            .Cast<ViewFamilyType>()
            .FirstOrDefault(v => v.ViewFamily == ViewFamily.FloorPlan);

        bool createViews = args.Bool("createViews", true);

        int countLevels = 0;
        foreach (var item in levelItems)
        {
            string name = item.Require("name");
            double elevationM = item.Double("elevationM");
            double elevationFt = elevationM * (1000.0 / 304.8);
            if (!existingLevels.TryGetValue(name, out var level))
            {
                level = Level.Create(doc, elevationFt);
                level.Name = name;
                existingLevels[name] = level;
                countLevels++;
            }
            var pStory = level.get_Parameter(BuiltInParameter.LEVEL_IS_BUILDING_STORY);
            if (pStory != null && !pStory.IsReadOnly) pStory.Set(1);

            if (createViews && planType != null && !existingViewLevelIds.Contains(level.Id))
            {
                ViewPlan.Create(doc, planType.Id, level.Id);
                existingViewLevelIds.Add(level.Id);
            }
        }
        """;

    private static string GetStage2GridsScript() => """
        // --- STAGE 2: GRIDS ---
        var gridItems = args.List("grids");
        var existingGrids = new FilteredElementCollector(doc)
            .OfClass(typeof(Grid))
            .Cast<Grid>()
            .ToDictionary(g => g.Name, g => g, StringComparer.OrdinalIgnoreCase);

        int countGrids = 0;
        foreach (var item in gridItems)
        {
            string name = item.Require("name");
            if (existingGrids.ContainsKey(name)) continue;
            var p0 = ToXyz(item.Double("x0"), item.Double("y0"), 0);
            var p1 = ToXyz(item.Double("x1"), item.Double("y1"), 0);
            if (p0.DistanceTo(p1) < 0.001) continue;
            var grid = Grid.Create(doc, Line.CreateBound(p0, p1));
            grid.Name = name;
            existingGrids[name] = grid;
            countGrids++;
        }
        """;

    private static string GetStage3ColumnsScript() => """
        // --- STAGE 3A: COLUMNS ---
        var colItems = args.List("columns");
        int countCols = 0;
        foreach (var col in colItems)
        {
            string baseLvlName = col.Require("baseLevel");
            string topLvlName = col.Str("topLevel", baseLvlName);
            if (!existingLevels.TryGetValue(baseLvlName, out var baseLvl)) continue;
            double w = col.Double("widthMm", 220);
            double d = col.Double("depthMm", 220);
            var symbol = EnsureColumnSymbol(doc, $"HP_Cot_C{(int)w}x{(int)d}", w, d);
            var loc = ToXyz(col.Double("x"), col.Double("y"), 0);
            var inst = doc.Create.NewFamilyInstance(loc, symbol, baseLvl, Autodesk.Revit.DB.Structure.StructuralType.Column);
            if (existingLevels.TryGetValue(topLvlName, out var topLvl))
            {
                inst.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM)?.Set(topLvl.Id);
            }

            double rotDeg = col.Double("rotationDegrees", col.Double("rotationDeg", 0.0));
            if (Math.Abs(rotDeg) > 1e-4)
            {
                var axis = Line.CreateBound(loc, loc + XYZ.BasisZ);
                ElementTransformUtils.RotateElement(doc, inst.Id, axis, rotDeg * (Math.PI / 180.0));
            }

            inst.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.Set(col.Require("id"));
            countCols++;
        }
        """;

    private static string GetStage3BeamsScript() => """
        // --- STAGE 3B: BEAMS ---
        var beamItems = args.List("beams");
        int countBeams = 0;
        foreach (var b in beamItems)
        {
            if (!existingLevels.TryGetValue(b.Require("level"), out var lvl)) continue;
            double w = b.Double("widthMm", 200);
            double h = b.Double("heightMm", 400);
            var symbol = EnsureBeamSymbol(doc, $"HP_Dam_D{(int)w}x{(int)h}", w, h);
            var p0 = ToXyz(b.Double("x0"), b.Double("y0"), 0);
            var p1 = ToXyz(b.Double("x1"), b.Double("y1"), 0);
            if (p0.DistanceTo(p1) < 0.001) continue;
            var beam = doc.Create.NewFamilyInstance(Line.CreateBound(p0, p1), symbol, lvl, Autodesk.Revit.DB.Structure.StructuralType.Beam);
            beam.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.Set(b.Require("id"));
            countBeams++;
        }
        """;

    private static string GetStage3SlabsScript() => """
        // --- STAGE 3C: SLABS ---
        var slabItems = args.List("slabs");
        int countSlabs = 0;
        foreach (var s in slabItems)
        {
            if (!existingLevels.TryGetValue(s.Require("level"), out var lvl)) continue;
            double thick = s.Double("thicknessMm", 100);
            var fType = EnsureFloorType(doc, $"HP_San_{(int)thick}mm", thick);
            var pts = s.List("points");
            if (pts.Count < 3) continue;
            var loop = new CurveLoop();
            for (int i = 0; i < pts.Count; i++)
            {
                var cur = ToXyz(pts[i].Double("x"), pts[i].Double("y"), 0);
                var next = ToXyz(pts[(i + 1) % pts.Count].Double("x"), pts[(i + 1) % pts.Count].Double("y"), 0);
                if (cur.DistanceTo(next) > 0.001) loop.Append(Line.CreateBound(cur, next));
            }
            if (loop.IsOpen()) continue;
            var floor = Floor.Create(doc, new List<CurveLoop> { loop }, fType.Id, lvl.Id, true, null, 0);
            floor.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.Set(s.Require("id"));
            countSlabs++;
        }
        """;
}
