namespace HPBim.Cli.Commands;

/// <summary>
/// Revit Roslyn script builder for full foundation remediation and reconstruction.
/// Clean Code compliant: methods <= 50 lines, class <= 300 lines.
/// </summary>
public static class FoundationRemediationBuilder
{
    public static string BuildPinAndPurgeScript() => """
        int pinnedGrids = 0;
        foreach (Grid g in new FilteredElementCollector(doc).OfClass(typeof(Grid)))
        {
            if (!g.Pinned) { g.Pinned = true; pinnedGrids++; }
        }
        int pinnedLevels = 0;
        foreach (Level l in new FilteredElementCollector(doc).OfClass(typeof(Level)))
        {
            if (!l.Pinned) { l.Pinned = true; pinnedLevels++; }
        }

        var beamsToDelete = new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_StructuralFraming)
            .WhereElementIsNotElementType().ToElementIds();
        if (beamsToDelete.Count > 0) doc.Delete(beamsToDelete);

        var floorsToDelete = new FilteredElementCollector(doc)
            .OfClass(typeof(Floor)).ToElementIds();
        if (floorsToDelete.Count > 0) doc.Delete(floorsToDelete);

        return new {
            pinnedGrids,
            pinnedLevels,
            deletedBeams = beamsToDelete.Count,
            deletedFloors = floorsToDelete.Count
        };
    """;

    public static string BuildCreateFootingsScript() => """
        var ft1000 = new FilteredElementCollector(doc).OfClass(typeof(FloorType))
            .Cast<FloorType>().FirstOrDefault(ft => ft.Name == "HP-ES-CAP-CON-B20-1000")
            ?? new FilteredElementCollector(doc).OfClass(typeof(FloorType)).Cast<FloorType>().FirstOrDefault(ft => ft.Name.Contains("1000"))
            ?? new FilteredElementCollector(doc).OfClass(typeof(FloorType)).Cast<FloorType>().First();
        var ft1200 = new FilteredElementCollector(doc).OfClass(typeof(FloorType))
            .Cast<FloorType>().FirstOrDefault(ft => ft.Name == "HP-ES-CAP-CON-B20-1200")
            ?? new FilteredElementCollector(doc).OfClass(typeof(FloorType)).Cast<FloorType>().FirstOrDefault(ft => ft.Name.Contains("1200"))
            ?? ft1000;
        var ft1300 = new FilteredElementCollector(doc).OfClass(typeof(FloorType))
            .Cast<FloorType>().FirstOrDefault(ft => ft.Name == "HP-ES-CAP-CON-B20-1300")
            ?? new FilteredElementCollector(doc).OfClass(typeof(FloorType)).Cast<FloorType>().FirstOrDefault(ft => ft.Name.Contains("1300"))
            ?? ft1000;

        var levelDinhDai = new FilteredElementCollector(doc).OfClass(typeof(Level))
            .Cast<Level>().FirstOrDefault(l => l.Name == "Đỉnh Đài Móng")
            ?? new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().First();

        var floorTypesDict = new Dictionary<string, FloorType>(StringComparer.OrdinalIgnoreCase);
        foreach (var ft in new FilteredElementCollector(doc).OfClass(typeof(FloorType)).Cast<FloorType>())
        {
            floorTypesDict[ft.Name] = ft;
        }

        FloorType GetOrCreateFloorType(string typeName, double thickMm)
        {
            FloorType baseType = ft1000;
            if (typeName.Contains("ĐC-6") || typeName.Contains("DC-6")) baseType = ft1300;
            else if (typeName.Contains("ĐC-4") || typeName.Contains("ĐC-5") || typeName.Contains("DC-4") || typeName.Contains("DC-5")) baseType = ft1200;

            if (floorTypesDict.TryGetValue(typeName, out var existing))
            {
                var cs = baseType.GetCompoundStructure();
                if (cs != null) existing.SetCompoundStructure(cs);
                return existing;
            }

            var newType = (FloorType)baseType.Duplicate(typeName);
            var newCs = baseType.GetCompoundStructure();
            if (newCs != null) newType.SetCompoundStructure(newCs);
            floorTypesDict[typeName] = newType;
            return newType;
        }

        var footings = args.List("footings");
        int createdCount = 0;

        foreach (var item in footings)
        {
            string id = item.Str("id", "ĐC") ?? "ĐC";
            string type = item.Str("type", "ĐC-2") ?? "ĐC-2";
            double thickMm = item.Double("thicknessMm", 1000.0);
            var verts = item.List("vertices");
            if (verts.Count < 3) continue;

            var ft = GetOrCreateFloorType(type, thickMm);
            var loop = new CurveLoop();
            for (int i = 0; i < verts.Count; i++)
            {
                var v1 = verts[i];
                var v2 = verts[(i + 1) % verts.Count];
                var p1 = new XYZ(v1.Double("x") / 304.8, v1.Double("y") / 304.8, 0.0);
                var p2 = new XYZ(v2.Double("x") / 304.8, v2.Double("y") / 304.8, 0.0);
                loop.Append(Line.CreateBound(p1, p2));
            }

            var floor = Floor.Create(doc, new List<CurveLoop> { loop }, ft.Id, levelDinhDai.Id);
            floor.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.Set(id);
            floor.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.Set(id);
            createdCount++;
        }

        return new { createdCount };
    """;

    public static string BuildCreateGradeBeamsScript() => """
        var defaultSym = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_StructuralFraming)
            .OfClass(typeof(FamilySymbol)).Cast<FamilySymbol>().First();

        var levelDinhDai = new FilteredElementCollector(doc).OfClass(typeof(Level))
            .Cast<Level>().FirstOrDefault(l => l.Name == "Đỉnh Đài Móng")
            ?? new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().First();

        var symbolsDict = new Dictionary<string, FamilySymbol>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_StructuralFraming).OfClass(typeof(FamilySymbol)).Cast<FamilySymbol>())
        {
            symbolsDict[s.Name] = s;
        }

        FamilySymbol GetOrCreateSymbol(string symName, double bMm, double hMm)
        {
            if (symbolsDict.TryGetValue(symName, out var existing))
            {
                if (!existing.IsActive) existing.Activate();
                return existing;
            }
            var newSym = (FamilySymbol)defaultSym.Duplicate(symName);
            var pB = newSym.LookupParameter("b") ?? newSym.LookupParameter("Width") ?? newSym.LookupParameter("B");
            var pH = newSym.LookupParameter("h") ?? newSym.LookupParameter("Height") ?? newSym.LookupParameter("H");
            pB?.Set(bMm / 304.8);
            pH?.Set(hMm / 304.8);
            if (!newSym.IsActive) newSym.Activate();
            symbolsDict[symName] = newSym;
            return newSym;
        }

        var beams = args.List("beams");
        int createdCount = 0;
        double zElev = levelDinhDai.Elevation;

        foreach (var item in beams)
        {
            string name = item.Str("name", "GM") ?? "GM";
            double bMm = item.Double("bMm", 300.0);
            double hMm = item.Double("hMm", 400.0);
            double x1 = item.Double("x1");
            double y1 = item.Double("y1");
            double x2 = item.Double("x2");
            double y2 = item.Double("y2");
            double yOffsetMm = item.Double("yOffsetMm", 0.0);

            string symName = $"B_{bMm:F0}x{hMm:F0}";
            var sym = GetOrCreateSymbol(symName, bMm, hMm);

            var p1 = new XYZ(x1 / 304.8, y1 / 304.8, zElev);
            var p2 = new XYZ(x2 / 304.8, y2 / 304.8, zElev);
            if (p1.DistanceTo(p2) < 0.1) continue;

            var line = Line.CreateBound(p1, p2);
            var inst = doc.Create.NewFamilyInstance(line, sym, levelDinhDai, Autodesk.Revit.DB.Structure.StructuralType.Beam);
            inst.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.Set(name);
            inst.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.Set(name);
            inst.get_Parameter(BuiltInParameter.Z_JUSTIFICATION)?.Set(0); // Top
            if (Math.Abs(yOffsetMm) > 0.1)
            {
                inst.get_Parameter(BuiltInParameter.Y_OFFSET_VALUE)?.Set(yOffsetMm / 304.8);
            }
            Autodesk.Revit.DB.Structure.StructuralFramingUtils.DisallowJoinAtEnd(inst, 0);
            Autodesk.Revit.DB.Structure.StructuralFramingUtils.DisallowJoinAtEnd(inst, 1);
            createdCount++;
        }

        return new { createdCount };
    """;

    public static string BuildAdjustPilesScript() => """
        var levelDinhDai = new FilteredElementCollector(doc).OfClass(typeof(Level))
            .Cast<Level>().FirstOrDefault(l => l.Name == "Đỉnh Đài Móng")
            ?? new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().First();

        var floors = new FilteredElementCollector(doc).OfClass(typeof(Floor)).Cast<Floor>().ToList();
        var floorPolys = new List<(string Mark, double ThickMm, List<XYZ> Loop)>();

        foreach (var f in floors)
        {
            var bb = f.get_BoundingBox(null);
            double topZ = bb != null ? bb.Max.Z * 304.8 : -1600.0;
            double botZ = bb != null ? bb.Min.Z * 304.8 : -2600.0;
            double thickMm = Math.Round(topZ - botZ);
            string mark = f.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? "";
            var opt = new Options();
            var geom = f.get_Geometry(opt);
            var loop = new List<XYZ>();
            if (geom != null)
            {
                foreach (var obj in geom)
                {
                    if (obj is Solid solid && solid.Volume > 0)
                    {
                        foreach (Face face in solid.Faces)
                        {
                            if (face is PlanarFace pf && pf.FaceNormal.IsAlmostEqualTo(XYZ.BasisZ))
                            {
                                foreach (EdgeArray ea in pf.EdgeLoops)
                                {
                                    foreach (Edge e in ea) loop.Add(e.AsCurve().GetEndPoint(0));
                                }
                            }
                        }
                    }
                }
            }
            if (loop.Count >= 3) floorPolys.Add((mark, thickMm, loop));
        }

        bool PointInPolygon(XYZ p, List<XYZ> poly)
        {
            bool inside = false;
            for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
            {
                if (((poly[i].Y > p.Y) != (poly[j].Y > p.Y)) &&
                    (p.X < (poly[j].X - poly[i].X) * (p.Y - poly[i].Y) / (poly[j].Y - poly[i].Y) + poly[i].X))
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        var piles = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance))
            .OfCategory(BuiltInCategory.OST_StructuralFoundation).Cast<FamilyInstance>()
            .Where(p => p.Symbol.Family.Name.Contains("Coc") || p.Symbol.Name.Contains("P400") || p.Symbol.Name.Contains("250"))
            .ToList();

        int adjusted = 0;
        foreach (var p in piles)
        {
            var loc = (LocationPoint)p.Location;
            if (loc == null) continue;
            var pt = loc.Point;

            double targetThickMm = 1000.0;
            foreach (var fp in floorPolys)
            {
                if (PointInPolygon(pt, fp.Loop))
                {
                    targetThickMm = fp.ThickMm;
                    break;
                }
            }

            var pLevel = p.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM);
            if (pLevel != null && !pLevel.IsReadOnly) pLevel.Set(levelDinhDai.Id);

            p.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM)?.Set(0.0);
            p.LookupParameter("STR_ChieuCao_DaiCoc")?.Set(targetThickMm / 304.8);

            foreach (var f in floors)
            {
                if (JoinGeometryUtils.AreElementsJoined(doc, p, f))
                {
                    JoinGeometryUtils.UnjoinGeometry(doc, p, f);
                }
            }
            adjusted++;
        }

        return new { adjustedPiles = adjusted };
    """;
}
