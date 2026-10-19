namespace HPBim.Client.ScriptGenerators;

public sealed partial class CompositeBimGenerator
{
    private static string GetStage4WallsScript() => """
        // --- STAGE 4: WALLS ---
        var wallItems = args.List("walls");
        var wallsById = new Dictionary<string, Wall>(StringComparer.OrdinalIgnoreCase);
        int countWalls = 0;
        foreach (var w in wallItems)
        {
            string id = w.Require("id");
            string baseLvlName = w.Require("baseLevel");
            if (!existingLevels.TryGetValue(baseLvlName, out var baseLvl)) continue;
            double thick = w.Double("thicknessMm", 220);
            var wallType = EnsureWallType(doc, $"HP_Tuong_{(int)thick}mm", thick);
            var p0 = ToXyz(w.Double("x0"), w.Double("y0"), 0);
            var p1 = ToXyz(w.Double("x1"), w.Double("y1"), 0);
            if (p0.DistanceTo(p1) < 0.001) continue;

            double defaultHeightFt = Ft(3000.0);
            string topLvlName = w.Str("topLevel", string.Empty);
            Level topLvl = null;
            if (!string.IsNullOrEmpty(topLvlName) && existingLevels.TryGetValue(topLvlName, out topLvl))
            {
                defaultHeightFt = Math.Max(Ft(500), topLvl.Elevation - baseLvl.Elevation);
            }

            var line = Line.CreateBound(p0, p1);
            var wall = Wall.Create(doc, line, wallType.Id, baseLvl.Id, defaultHeightFt, Ft(w.Double("baseOffsetMm")), false, false);
            if (topLvl != null)
            {
                wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE)?.Set(topLvl.Id);
                wall.get_Parameter(BuiltInParameter.WALL_TOP_OFFSET)?.Set(Ft(w.Double("topOffsetMm")));
            }
            wall.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.Set(id);
            wall.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.Set(id);
            wall.get_Parameter(BuiltInParameter.WALL_ATTR_ROOM_BOUNDING)?.Set(1);
            wallsById[id] = wall;
            countWalls++;
        }
        """;

    private static string GetStage5OpeningsScript() => """
        // --- STAGE 5: OPENINGS ---
        var opItems = args.List("openings");
        int countOpenings = 0;
        var doorSymbols = new FilteredElementCollector(doc)
            .OfClass(typeof(FamilySymbol))
            .OfCategory(BuiltInCategory.OST_Doors)
            .Cast<FamilySymbol>()
            .ToList();
        var windowSymbols = new FilteredElementCollector(doc)
            .OfClass(typeof(FamilySymbol))
            .OfCategory(BuiltInCategory.OST_Windows)
            .Cast<FamilySymbol>()
            .ToList();

        var defaultDoorSym = doorSymbols.FirstOrDefault();
        if (defaultDoorSym != null && !defaultDoorSym.IsActive) defaultDoorSym.Activate();
        var defaultWinSym = windowSymbols.FirstOrDefault();
        if (defaultWinSym != null && !defaultWinSym.IsActive) defaultWinSym.Activate();

        foreach (var op in opItems)
        {
            string hostWallId = op.Require("hostWallId");
            if (!wallsById.TryGetValue(hostWallId, out var hostWall)) continue;
            string kind = op.Str("kind", "Door");
            bool isDoor = string.Equals(kind, "Door", StringComparison.OrdinalIgnoreCase);
            var symbolList = isDoor ? doorSymbols : windowSymbols;
            if (symbolList.Count == 0) continue;

            string targetName = op.Str("symbolName", string.Empty);
            var sym = symbolList.FirstOrDefault(s => string.Equals(s.Name, targetName, StringComparison.OrdinalIgnoreCase))
                ?? symbolList.FirstOrDefault();
            if (sym == null) continue;
            if (!sym.IsActive) sym.Activate();

            var locCurve = hostWall.Location as LocationCurve;
            if (locCurve?.Curve == null) continue;
            double distFt = Ft(op.Double("distMm") + (op.Double("widthMm") / 2.0));
            double param = Math.Clamp(distFt / locCurve.Curve.Length, 0.05, 0.95);
            var centerPt = locCurve.Curve.Evaluate(param, true);
            double sillFt = Ft(op.Double("sillHeightMm"));
            var pt = new XYZ(centerPt.X, centerPt.Y, centerPt.Z + sillFt);
            var level = doc.GetElement(hostWall.LevelId) as Level;
            var inst = doc.Create.NewFamilyInstance(pt, sym, hostWall, level, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
            inst.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.Set(op.Require("id"));
            countOpenings++;
        }
        """;

    private static string GetStage6RoomsScript() => """
        // --- STAGE 6: ROOMS ---
        doc.Regenerate();
        var roomItems = args.List("rooms");
        int countRooms = 0;
        foreach (var r in roomItems)
        {
            if (!existingLevels.TryGetValue(r.Require("level"), out var lvl)) continue;
            try
            {
                var room = doc.Create.NewRoom(lvl, new UV(Ft(r.Double("x")), Ft(r.Double("y"))));
                if (room != null)
                {
                    room.get_Parameter(BuiltInParameter.ROOM_NAME)?.Set(r.Require("name"));
                    countRooms++;
                }
            }
            catch (Exception) { /* Skip room if point outside boundary */ }
        }
        """;
}
