namespace HPBim.Client.Verification;

public static class RevitGeometryQueryScriptTemplate
{
    public static string GetScriptCode() =>
        GetPreambleScript() +
        GetElementInspectionScript() +
        GetEpilogueScript();

    private static string GetPreambleScript() => """
        double Ft(double mm) => mm / 304.8;
        double Mm(double ft) => ft * 304.8;

        var items = args.List("items");
        var results = new List<Dictionary<string, object>>();

        var allLevels = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().ToList();
        var allGrids = new FilteredElementCollector(doc).OfClass(typeof(Grid)).Cast<Grid>().ToList();
        var allColumns = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance))
            .OfCategory(BuiltInCategory.OST_StructuralColumns).Cast<FamilyInstance>().ToList();
        var allBeams = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance))
            .OfCategory(BuiltInCategory.OST_StructuralFraming).Cast<FamilyInstance>().ToList();
        var allPiles = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance))
            .OfCategory(BuiltInCategory.OST_StructuralFoundation).Cast<FamilyInstance>().ToList();
        var allWalls = new FilteredElementCollector(doc).OfClass(typeof(Wall)).Cast<Wall>().ToList();
        var allFloors = new FilteredElementCollector(doc).OfClass(typeof(Floor)).Cast<Floor>().ToList();

        """;

    private static string GetElementInspectionScript() => """
        foreach (var item in items)
        {
            string sourceKey = item.Str("sourceKey", string.Empty);
            string category = item.Require("category");
            string mark = item.Str("mark", string.Empty);
            string targetUniqueId = item.Str("targetUniqueId", string.Empty);

            var record = new Dictionary<string, object>
            {
                ["key"] = sourceKey,
                ["category"] = category,
                ["mark"] = mark,
                ["hasGeometry"] = false
            };

            Element elem = null;
            if (!string.IsNullOrEmpty(targetUniqueId))
            {
                elem = doc.GetElement(targetUniqueId);
            }

            if (elem == null)
            {
                if (category == "Column") elem = allColumns.FirstOrDefault(c => c.Name == mark || (c.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() == mark));
                else if (category == "Beam" || category == "GradeBeam") elem = allBeams.FirstOrDefault(b => b.Name == mark || (b.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() == mark));
                else if (category == "Wall") elem = allWalls.FirstOrDefault(w => w.Name == mark);
                else if (category == "Level") elem = allLevels.FirstOrDefault(l => l.Name == mark);
                else if (category == "Grid") elem = allGrids.FirstOrDefault(g => g.Name == mark);
                else if (category == "Pile" || category == "Footing") elem = allPiles.FirstOrDefault(p => p.Name == mark || (p.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() == mark));
                else if (category == "Slab" || category == "Floor") elem = allFloors.FirstOrDefault(fl => fl.Name == mark || (fl.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() == mark));
            }

            if (elem != null)
            {
                record["uniqueId"] = elem.UniqueId;
                record["elementId"] = (long)elem.Id.Value;

                if (elem.Location is LocationPoint lp)
                {
                    record["hasGeometry"] = true;
                    record["centroid"] = new { x = Mm(lp.Point.X), y = Mm(lp.Point.Y), z = Mm(lp.Point.Z) };
                    record["rotationDegrees"] = lp.Rotation * (180.0 / Math.PI);

                    if (category == "Pile" || category == "Footing")
                    {
                        var bbPile = elem.get_BoundingBox(null);
                        if (bbPile != null) record["topElevationMm"] = Mm(bbPile.Max.Z);
                        else record["topElevationMm"] = Mm(lp.Point.Z);
                    }
                }
                else if (elem.Location is LocationCurve lc && lc.Curve is Line line)
                {
                    record["hasGeometry"] = true;
                    record["curveStart"] = new { x = Mm(line.GetEndPoint(0).X), y = Mm(line.GetEndPoint(0).Y), z = Mm(line.GetEndPoint(0).Z) };
                    record["curveEnd"] = new { x = Mm(line.GetEndPoint(1).X), y = Mm(line.GetEndPoint(1).Y), z = Mm(line.GetEndPoint(1).Z) };
                    record["topElevationMm"] = Mm(line.GetEndPoint(0).Z);
                }
                else if (elem is Level lvl)
                {
                    record["hasGeometry"] = true;
                    record["topElevationMm"] = Mm(lvl.Elevation);
                }
                else if (elem is Floor floorObj)
                {
                    record["hasGeometry"] = true;
                    var bb = floorObj.get_BoundingBox(null);
                    if (bb != null)
                    {
                        record["centroid"] = new { x = Mm((bb.Min.X + bb.Max.X) / 2.0), y = Mm((bb.Min.Y + bb.Max.Y) / 2.0), z = Mm(bb.Max.Z) };
                        record["widthMm"] = Mm(bb.Max.X - bb.Min.X);
                        record["heightMm"] = Mm(bb.Max.Y - bb.Min.Y);
                        record["topElevationMm"] = Mm(bb.Max.Z);
                        record["bottomElevationMm"] = Mm(bb.Min.Z);
                    }
                    else if (doc.GetElement(floorObj.LevelId) is Level fLvl)
                    {
                        double offFt = floorObj.get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM)?.AsDouble() ?? 0.0;
                        record["topElevationMm"] = Mm(fLvl.Elevation + offFt);
                    }
                }

                ElementType type = doc.GetElement(elem.GetTypeId()) as ElementType;
                if (type != null)
                {
                    record["typeName"] = type.Name;
                    var pB = type.LookupParameter("b") ?? type.LookupParameter("Width") ?? type.LookupParameter("B");
                    var pH = type.LookupParameter("h") ?? type.LookupParameter("Height") ?? type.LookupParameter("H") ?? type.LookupParameter("d") ?? type.LookupParameter("Depth");
                    if (pB != null && pB.StorageType == StorageType.Double) record["widthMm"] = Mm(pB.AsDouble());
                    if (pH != null && pH.StorageType == StorageType.Double) record["heightMm"] = Mm(pH.AsDouble());
                }

                if (elem is Wall wall && wall.WallType != null)
                {
                    record["thicknessMm"] = Mm(wall.WallType.Width);
                }
                else if (elem is Floor flr && flr.FloorType != null)
                {
                    var pThk = flr.FloorType.get_Parameter(BuiltInParameter.FLOOR_ATTR_DEFAULT_THICKNESS_PARAM);
                    if (pThk != null) record["thicknessMm"] = Mm(pThk.AsDouble());
                }
            }

            results.Add(record);
        }

        """;

    private static string GetEpilogueScript() => """
        return new
        {
            total = results.Count,
            actuals = results
        };
        """;
}
